#include "statement.h"

#include "client.h"

#include <brezee/core/error.h>

#include <cstring>
#include <string>
#include <utility>

namespace brezee::core::firebird {

namespace {

using Firebird::FbException;
using Firebird::IMessageMetadata;
using Firebird::ThrowStatusWrapper;

constexpr unsigned charset_octets = 1;
constexpr unsigned charset_unicode_fss = 3;
constexpr unsigned charset_utf8 = 4;

// Releases a reference-counted Firebird interface when it goes out of scope.
template <typename T>
class Ref
{
public:
    explicit Ref(T* pointer = nullptr) noexcept : pointer_(pointer) {}
    ~Ref() { reset(); }

    Ref(const Ref&) = delete;
    Ref& operator=(const Ref&) = delete;

    Ref(Ref&& other) noexcept : pointer_(other.release()) {}
    Ref& operator=(Ref&& other) noexcept
    {
        reset(other.release());
        return *this;
    }

    T* get() const noexcept { return pointer_; }
    T* operator->() const noexcept { return pointer_; }

    T* release() noexcept { return std::exchange(pointer_, nullptr); }

    void reset(T* pointer = nullptr) noexcept
    {
        if (pointer_)
            pointer_->release();
        pointer_ = pointer;
    }

private:
    T* pointer_;
};

// A transaction that rolls back unless committed.
class Transaction
{
public:
    Transaction(ThrowStatusWrapper* status, Firebird::IAttachment* attachment)
    {
        auto* tpb = Client::get().util()->getXpbBuilder(status, Firebird::IXpbBuilder::TPB, nullptr, 0);
        try
        {
            // Read committed with record versions: sees committed data, never waits on locks.
            // Firebird 4+ upgrades this to read consistency by default.
            tpb->insertTag(status, isc_tpb_write);
            tpb->insertTag(status, isc_tpb_read_committed);
            tpb->insertTag(status, isc_tpb_rec_version);
            tpb->insertTag(status, isc_tpb_nowait);
            transaction_ = attachment->startTransaction(status, tpb->getBufferLength(status), tpb->getBuffer(status));
        }
        catch (...)
        {
            tpb->dispose();
            throw;
        }
        tpb->dispose();
    }

    ~Transaction()
    {
        if (!transaction_)
            return;

        // Rolling back after an error; a second failure here has nothing useful to report.
        Status status;
        try
        {
            transaction_->rollback(status.get());
        }
        catch (const FbException&)
        {
            transaction_->release();
        }
    }

    Transaction(const Transaction&) = delete;
    Transaction& operator=(const Transaction&) = delete;

    Firebird::ITransaction* get() const noexcept { return transaction_; }

    void commit(ThrowStatusWrapper* status)
    {
        transaction_->commit(status); // Releases the interface on success.
        transaction_ = nullptr;
    }

private:
    Firebird::ITransaction* transaction_ = nullptr;
};

// Bytes per character, used to show VARCHAR(n) in characters rather than bytes.
unsigned bytes_per_char(unsigned charset)
{
    switch (charset)
    {
    case charset_utf8: return 4;
    case charset_unicode_fss: return 3;
    default: return 1;
    }
}

std::string numeric_type(const char* storage_precision, int sub_type, int scale)
{
    if (scale == 0 && sub_type == 0)
        return {};
    return std::string(sub_type == 2 ? "DECIMAL(" : "NUMERIC(") + storage_precision + "," + std::to_string(-scale) + ")";
}

// The SQL type name shown to users, from the statement's original (uncoerced) metadata.
std::string type_name(ThrowStatusWrapper* status, IMessageMetadata* metadata, unsigned index)
{
    const unsigned type = metadata->getType(status, index);
    const int sub_type = metadata->getSubType(status, index);
    const int scale = metadata->getScale(status, index);
    const unsigned length = metadata->getLength(status, index);
    const unsigned chars = length / bytes_per_char(metadata->getCharSet(status, index));

    switch (type)
    {
    case SQL_TEXT: return "CHAR(" + std::to_string(chars) + ")";
    case SQL_VARYING: return "VARCHAR(" + std::to_string(chars) + ")";
    case SQL_SHORT: { auto n = numeric_type("4", sub_type, scale); return n.empty() ? "SMALLINT" : n; }
    case SQL_LONG: { auto n = numeric_type("9", sub_type, scale); return n.empty() ? "INTEGER" : n; }
    case SQL_INT64: { auto n = numeric_type("18", sub_type, scale); return n.empty() ? "BIGINT" : n; }
    case SQL_INT128: { auto n = numeric_type("38", sub_type, scale); return n.empty() ? "INT128" : n; }
    case SQL_FLOAT: return "FLOAT";
    case SQL_DOUBLE:
    case SQL_D_FLOAT: return "DOUBLE PRECISION";
    case SQL_DEC16: return "DECFLOAT(16)";
    case SQL_DEC34: return "DECFLOAT(34)";
    case SQL_BOOLEAN: return "BOOLEAN";
    case SQL_TYPE_DATE: return "DATE";
    case SQL_TYPE_TIME: return "TIME";
    case SQL_TIME_TZ:
    case SQL_TIME_TZ_EX: return "TIME WITH TIME ZONE";
    case SQL_TIMESTAMP: return "TIMESTAMP";
    case SQL_TIMESTAMP_TZ:
    case SQL_TIMESTAMP_TZ_EX: return "TIMESTAMP WITH TIME ZONE";
    case SQL_BLOB: return sub_type == 1 ? "BLOB SUB_TYPE TEXT" : "BLOB SUB_TYPE " + std::to_string(sub_type);
    case SQL_ARRAY: return "ARRAY";
    case SQL_NULL: return "NULL";
    default: return "TYPE " + std::to_string(type);
    }
}

// Types Brezee asks Firebird to send as text, because Firebird formats them exactly (INT128,
// DECFLOAT) or because decoding them needs time zone data on the client (the TZ types).
bool read_as_text(unsigned type)
{
    switch (type)
    {
    case SQL_INT128:
    case SQL_DEC16:
    case SQL_DEC34:
    case SQL_TIME_TZ:
    case SQL_TIME_TZ_EX:
    case SQL_TIMESTAMP_TZ:
    case SQL_TIMESTAMP_TZ_EX:
        return true;
    default:
        return false;
    }
}

ColumnKind column_kind(unsigned original_type, int sub_type, int scale, unsigned charset)
{
    switch (original_type)
    {
    case SQL_BOOLEAN: return ColumnKind::Boolean;
    case SQL_SHORT:
    case SQL_LONG:
    case SQL_INT64: return scale == 0 ? ColumnKind::Integer : ColumnKind::Decimal;
    case SQL_INT128:
    case SQL_DEC16:
    case SQL_DEC34: return ColumnKind::Decimal;
    case SQL_FLOAT:
    case SQL_DOUBLE:
    case SQL_D_FLOAT: return ColumnKind::Float;
    case SQL_TEXT:
    case SQL_VARYING: return charset == charset_octets ? ColumnKind::Binary : ColumnKind::Text;
    case SQL_TIME_TZ:
    case SQL_TIME_TZ_EX:
    case SQL_TIMESTAMP_TZ:
    case SQL_TIMESTAMP_TZ_EX: return ColumnKind::Text;
    case SQL_TYPE_DATE: return ColumnKind::Date;
    case SQL_TYPE_TIME: return ColumnKind::Time;
    case SQL_TIMESTAMP: return ColumnKind::Timestamp;
    case SQL_BLOB: return sub_type == 1 ? ColumnKind::Text : ColumnKind::Binary;
    default: return ColumnKind::Unknown;
    }
}

// The output metadata Brezee reads with: the statement's own, with read_as_text types switched to
// UTF-8 text so the server converts them.
IMessageMetadata* reading_metadata(ThrowStatusWrapper* status, IMessageMetadata* original)
{
    const unsigned count = original->getCount(status);
    bool needs_coercion = false;
    for (unsigned i = 0; i < count; ++i)
        needs_coercion = needs_coercion || read_as_text(original->getType(status, i));

    if (!needs_coercion)
    {
        original->addRef();
        return original;
    }

    Ref builder(original->getBuilder(status));
    for (unsigned i = 0; i < count; ++i)
    {
        if (!read_as_text(original->getType(status, i)))
            continue;
        builder->setType(status, i, SQL_VARYING);
        builder->setCharSet(status, i, charset_utf8);
        builder->setLength(status, i, 64 * 4);
        builder->setScale(status, i, 0);
    }
    return builder->getMetadata(status);
}

// Formats a scaled integer exactly: 123456 with scale -2 is "1234.56".
std::string format_scaled(std::int64_t value, int scale)
{
    const bool negative = value < 0;
    // Work on the magnitude as unsigned so the most negative value does not overflow.
    const auto magnitude = negative ? static_cast<std::uint64_t>(-(value + 1)) + 1 : static_cast<std::uint64_t>(value);
    std::string digits = std::to_string(magnitude);

    const auto decimals = static_cast<std::size_t>(-scale);
    if (digits.size() <= decimals)
        digits.insert(0, decimals - digits.size() + 1, '0');
    digits.insert(digits.size() - decimals, 1, '.');

    return negative ? "-" + digits : digits;
}

std::string read_blob_bytes(ThrowStatusWrapper* status, Firebird::IAttachment* attachment,
    Firebird::ITransaction* transaction, ISC_QUAD* id, std::size_t limit)
{
    Ref blob(attachment->openBlob(status, transaction, id, 0, nullptr));
    std::string data;
    char segment[32 * 1024];
    unsigned length = 0;

    while (data.size() < limit)
    {
        const int result = blob->getSegment(status, sizeof(segment), segment, &length);
        if (result == Firebird::IStatus::RESULT_NO_DATA)
            break;
        data.append(segment, std::min<std::size_t>(length, limit - data.size()));
    }

    blob->close(status); // Releases the interface on success.
    blob.release();
    return data;
}

struct Reader
{
    ThrowStatusWrapper* status;
    Firebird::IAttachment* attachment;
    Firebird::ITransaction* transaction;
    IMessageMetadata* metadata;
    const QueryOptions& options;

    // exact_as_text: the column was switched to text by reading_metadata but holds an exact number.
    Value read(const unsigned char* message, unsigned index, bool exact_as_text) const
    {
        if (*reinterpret_cast<const short*>(message + metadata->getNullOffset(status, index)) != 0)
            return std::monostate{};

        const unsigned char* data = message + metadata->getOffset(status, index);
        const unsigned type = metadata->getType(status, index);
        const unsigned charset = metadata->getCharSet(status, index);
        const int scale = metadata->getScale(status, index);
        auto* util = Client::get().util();

        switch (type)
        {
        case SQL_TEXT:
        {
            std::string text(reinterpret_cast<const char*>(data), metadata->getLength(status, index));
            if (charset == charset_octets)
                return Bytes(text.begin(), text.end());
            // CHAR is space-padded to its full byte length; the padding is not part of the value.
            text.erase(text.find_last_not_of(' ') + 1);
            return text;
        }
        case SQL_VARYING:
        {
            unsigned short length = 0;
            std::memcpy(&length, data, sizeof(length));
            std::string text(reinterpret_cast<const char*>(data + sizeof(length)), length);
            if (charset == charset_octets)
                return Bytes(text.begin(), text.end());
            if (exact_as_text)
                return Decimal{std::move(text)};
            return text;
        }
        case SQL_SHORT: return scaled(*reinterpret_cast<const std::int16_t*>(data), scale);
        case SQL_LONG: return scaled(*reinterpret_cast<const std::int32_t*>(data), scale);
        case SQL_INT64: return scaled(*reinterpret_cast<const std::int64_t*>(data), scale);
        case SQL_FLOAT: return static_cast<double>(*reinterpret_cast<const float*>(data));
        case SQL_DOUBLE:
        case SQL_D_FLOAT: return *reinterpret_cast<const double*>(data);
        case SQL_BOOLEAN: return *data != 0;
        case SQL_TYPE_DATE: return date(util, *reinterpret_cast<const ISC_DATE*>(data));
        case SQL_TYPE_TIME: return time(util, *reinterpret_cast<const ISC_TIME*>(data));
        case SQL_TIMESTAMP:
        {
            const auto* stamp = reinterpret_cast<const ISC_TIMESTAMP*>(data);
            return Timestamp{date(util, stamp->timestamp_date), time(util, stamp->timestamp_time)};
        }
        case SQL_BLOB:
        {
            ISC_QUAD id;
            std::memcpy(&id, data, sizeof(id));
            auto bytes = read_blob_bytes(status, attachment, transaction, &id, options.max_blob_bytes);
            if (metadata->getSubType(status, index) == 1)
                return bytes;
            return Bytes(bytes.begin(), bytes.end());
        }
        default:
            return std::monostate{}; // Arrays and anything unknown.
        }
    }

private:
    static Value scaled(std::int64_t value, int scale)
    {
        if (scale == 0)
            return value;
        return Decimal{format_scaled(value, scale)};
    }

    static Date date(Firebird::IUtil* util, ISC_DATE value)
    {
        unsigned year = 0, month = 0, day = 0;
        util->decodeDate(value, &year, &month, &day);
        return {static_cast<int>(year), static_cast<int>(month), static_cast<int>(day)};
    }

    static Time time(Firebird::IUtil* util, ISC_TIME value)
    {
        unsigned hours = 0, minutes = 0, seconds = 0, fractions = 0;
        util->decodeTime(value, &hours, &minutes, &seconds, &fractions);
        return {static_cast<int>(hours), static_cast<int>(minutes), static_cast<int>(seconds), static_cast<int>(fractions)};
    }
};

// Builds the input message: every parameter sent as UTF-8 text (or NULL), converted by Firebird.
struct Input
{
    Ref<IMessageMetadata> metadata;
    std::vector<unsigned char> buffer;
};

Input build_input(ThrowStatusWrapper* status, Firebird::IStatement* statement, const Parameters& parameters)
{
    Ref original(statement->getInputMetadata(status));
    const unsigned count = original->getCount(status);
    if (count != parameters.size())
        throw Error(ErrorKind::InvalidArgument,
            "The statement has " + std::to_string(count) + " parameter(s) but " + std::to_string(parameters.size())
                + " value(s) were given.");

    Input input;
    if (count == 0)
        return input;

    Ref builder(original->getBuilder(status));
    for (unsigned i = 0; i < count; ++i)
    {
        const auto length = parameters[i] ? parameters[i]->size() : 0;
        builder->setType(status, i, SQL_VARYING);
        builder->setCharSet(status, i, charset_utf8);
        builder->setLength(status, i, static_cast<unsigned>(std::max<std::size_t>(length, 1)));
        builder->setScale(status, i, 0);
        builder->setSubType(status, i, 0);
    }
    input.metadata.reset(builder->getMetadata(status));
    input.buffer.assign(input.metadata->getMessageLength(status), 0);

    for (unsigned i = 0; i < count; ++i)
    {
        auto* null_flag = reinterpret_cast<short*>(input.buffer.data() + input.metadata->getNullOffset(status, i));
        if (!parameters[i])
        {
            *null_flag = -1;
            continue;
        }

        *null_flag = 0;
        auto* data = input.buffer.data() + input.metadata->getOffset(status, i);
        const auto length = static_cast<unsigned short>(parameters[i]->size());
        std::memcpy(data, &length, sizeof(length));
        std::memcpy(data + sizeof(length), parameters[i]->data(), length);
    }
    return input;
}

std::vector<Column> describe(ThrowStatusWrapper* status, IMessageMetadata* original)
{
    std::vector<Column> columns;
    const unsigned count = original->getCount(status);
    columns.reserve(count);

    for (unsigned i = 0; i < count; ++i)
    {
        Column column;
        column.name = original->getAlias(status, i);
        column.field = original->getField(status, i);
        column.relation = original->getRelation(status, i);
        column.type = type_name(status, original, i);
        column.kind = column_kind(original->getType(status, i), original->getSubType(status, i),
            original->getScale(status, i), original->getCharSet(status, i));
        column.nullable = original->isNullable(status, i);
        columns.push_back(std::move(column));
    }
    return columns;
}

} // namespace

QueryResult run_statement(Firebird::IAttachment* attachment, std::string_view sql,
    const Parameters& parameters, const QueryOptions& options)
{
    Status status_holder;
    auto* status = status_holder.get();

    try
    {
        Transaction transaction(status, attachment);

        Ref statement(attachment->prepare(status, transaction.get(), static_cast<unsigned>(sql.size()), sql.data(),
            SQL_DIALECT_V6, Firebird::IStatement::PREPARE_PREFETCH_METADATA));

        auto input = build_input(status, statement.get(), parameters);
        Ref original(statement->getOutputMetadata(status));

        QueryResult result;
        result.columns = describe(status, original.get());

        Ref reading(reading_metadata(status, original.get()));
        std::vector<unsigned char> message(reading->getMessageLength(status));

        // INT128 and DECFLOAT arrive as text (see reading_metadata) but are exact numbers.
        std::vector<bool> exact_as_text;
        for (unsigned i = 0; i < result.columns.size(); ++i)
        {
            const auto type = original->getType(status, i);
            exact_as_text.push_back(type == SQL_INT128 || type == SQL_DEC16 || type == SQL_DEC34);
        }

        const Reader reader{status, attachment, transaction.get(), reading.get(), options};
        const auto read_row = [&] {
            std::vector<Value> row;
            row.reserve(result.columns.size());
            for (unsigned i = 0; i < result.columns.size(); ++i)
                row.push_back(reader.read(message.data(), i, exact_as_text[i]));
            return row;
        };

        const unsigned type = statement->getType(status);
        if (type == isc_info_sql_stmt_select || type == isc_info_sql_stmt_select_for_upd)
        {
            Ref cursor(statement->openCursor(status, transaction.get(), input.metadata.get(),
                input.buffer.empty() ? nullptr : input.buffer.data(), reading.get(), 0));

            while (cursor->fetchNext(status, message.data()) == Firebird::IStatus::RESULT_OK)
            {
                if (options.max_rows && result.rows.size() >= *options.max_rows)
                {
                    result.truncated = true;
                    break;
                }
                result.rows.push_back(read_row());
            }

            cursor->close(status); // Releases the interface on success.
            cursor.release();
        }
        else
        {
            // DDL, DML, or EXECUTE PROCEDURE (which returns at most one row).
            statement->execute(status, transaction.get(), input.metadata.get(),
                input.buffer.empty() ? nullptr : input.buffer.data(), reading.get(),
                message.empty() ? nullptr : message.data());
            if (!result.columns.empty())
                result.rows.push_back(read_row());
        }

        statement->free(status); // Releases the interface on success.
        statement.release();
        transaction.commit(status);
        return result;
    }
    catch (const FbException& e)
    {
        throw_error(e, ErrorKind::Database, "Statement failed");
    }
}

void refine_declared_types(Firebird::IAttachment* attachment, QueryResult& result)
{
    for (auto& column : result.columns)
    {
        const bool numeric = column.type.rfind("NUMERIC(", 0) == 0 || column.type.rfind("DECIMAL(", 0) == 0;
        if (!numeric || column.relation.empty() || column.field.empty())
            continue;

        const auto lookup = run_statement(attachment,
            "select f.rdb$field_precision from rdb$relation_fields rf "
            "join rdb$fields f on f.rdb$field_name = rf.rdb$field_source "
            "where rf.rdb$relation_name = ? and rf.rdb$field_name = ?",
            {column.relation, column.field}, {});

        if (lookup.rows.empty())
            continue;
        const auto* precision = std::get_if<std::int64_t>(&lookup.rows[0][0]);
        if (!precision || *precision <= 0)
            continue;

        // "NUMERIC(18,2)" -> "NUMERIC(10,2)": keep the name and scale, replace the precision.
        const auto open = column.type.find('(');
        const auto comma = column.type.find(',');
        column.type = column.type.substr(0, open + 1) + std::to_string(*precision) + column.type.substr(comma);
    }
}

} // namespace brezee::core::firebird
