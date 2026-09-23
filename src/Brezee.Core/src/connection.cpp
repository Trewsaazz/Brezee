#include <brezee/core/connection.h>

#include <brezee/core/error.h>
#include <brezee/core/log.h>

#include "firebird/client.h"
#include "firebird/statement.h"

#include <mutex>
#include <string>

namespace brezee::core {

namespace {

using firebird::Client;
using firebird::Status;

// Builds the database parameter block shared by attach and create.
Firebird::IXpbBuilder* build_dpb(Status& status, const ConnectionParameters& parameters)
{
    auto* dpb = Client::get().util()->getXpbBuilder(status.get(), Firebird::IXpbBuilder::DPB, nullptr, 0);

    // Paths and names are UTF-8 throughout Brezee.
    dpb->insertTag(status.get(), isc_dpb_utf8_filename);
    if (!parameters.user.empty())
        dpb->insertString(status.get(), isc_dpb_user_name, parameters.user.c_str());
    if (!parameters.password.empty())
        dpb->insertString(status.get(), isc_dpb_password, parameters.password.c_str());
    if (!parameters.role.empty())
        dpb->insertString(status.get(), isc_dpb_sql_role_name, parameters.role.c_str());
    if (!parameters.charset.empty())
        dpb->insertString(status.get(), isc_dpb_lc_ctype, parameters.charset.c_str());

    // Without a host, always use the embedded engine. Otherwise fbclient would first try a Firebird
    // server running on this machine, which behaves differently (passwords, file locks).
    if (parameters.host.empty())
        dpb->insertString(status.get(), isc_dpb_config, "Providers=Engine13");

    return dpb;
}

// Reads a little-endian integer of `length` bytes from an info response.
int read_int(const unsigned char* data, unsigned length)
{
    unsigned value = 0;
    for (unsigned i = 0; i < length && i < sizeof(value); ++i)
        value |= static_cast<unsigned>(data[i]) << (8 * i);
    return static_cast<int>(value);
}

DatabaseInfo parse_info(const unsigned char* buffer, std::size_t size)
{
    DatabaseInfo info;
    const unsigned char* p = buffer;
    const unsigned char* end = buffer + size;

    while (p < end && *p != isc_info_end)
    {
        const unsigned char item = *p++;
        if (item == isc_info_truncated || item == isc_info_error || end - p < 2)
            break;

        const unsigned length = static_cast<unsigned>(read_int(p, 2));
        p += 2;
        if (static_cast<std::size_t>(end - p) < length)
            break;

        switch (item)
        {
        case isc_info_firebird_version:
            // A count, then that many length-prefixed strings. The first describes the server.
            if (length >= 2 && p[0] > 0 && p[1] <= length - 2)
                info.server_version.assign(reinterpret_cast<const char*>(p + 2), p[1]);
            break;
        case isc_info_ods_version:
            info.ods_major = read_int(p, length);
            break;
        case isc_info_ods_minor_version:
            info.ods_minor = read_int(p, length);
            break;
        case isc_info_page_size:
            info.page_size = read_int(p, length);
            break;
        case isc_info_db_sql_dialect:
            info.sql_dialect = read_int(p, length);
            break;
        default:
            break;
        }

        p += length;
    }

    return info;
}

} // namespace

std::string connection_string(const ConnectionParameters& parameters)
{
    if (parameters.host.empty())
        return parameters.database;

    return parameters.host + "/" + std::to_string(parameters.port) + ":" + parameters.database;
}

struct Connection::Impl
{
    ConnectionParameters parameters;
    Firebird::IAttachment* attachment = nullptr;
    std::mutex mutex;
};

Connection::Connection(std::unique_ptr<Impl> impl)
    : impl_(std::move(impl))
{
}

Connection::~Connection()
{
    try
    {
        close();
    }
    catch (const Error&)
    {
        // Already logged by throw_error; a destructor must not throw.
    }
}

std::unique_ptr<Connection> Connection::open(const ConnectionParameters& parameters)
{
    const auto target = connection_string(parameters);
    log(LogLevel::Info, "Connection", "Connecting to " + target + " as " + parameters.user);

    Status status;
    Firebird::IXpbBuilder* dpb = nullptr;
    try
    {
        dpb = build_dpb(status, parameters);
        auto impl = std::make_unique<Impl>();
        impl->parameters = parameters;
        impl->attachment = Client::get().provider()->attachDatabase(status.get(), target.c_str(),
            dpb->getBufferLength(status.get()), dpb->getBuffer(status.get()));
        dpb->dispose();

        log(LogLevel::Info, "Connection", "Connected to " + target);
        return std::unique_ptr<Connection>(new Connection(std::move(impl)));
    }
    catch (const Firebird::FbException& e)
    {
        if (dpb)
            dpb->dispose();
        firebird::throw_error(e, ErrorKind::Connection, "Could not connect to " + target);
    }
}

std::unique_ptr<Connection> Connection::create(const ConnectionParameters& parameters, int page_size)
{
    const auto target = connection_string(parameters);
    log(LogLevel::Info, "Connection", "Creating database " + target);

    Status status;
    Firebird::IXpbBuilder* dpb = nullptr;
    try
    {
        dpb = build_dpb(status, parameters);
        dpb->insertInt(status.get(), isc_dpb_page_size, page_size);
        dpb->insertInt(status.get(), isc_dpb_sql_dialect, 3);
        if (!parameters.charset.empty())
            dpb->insertString(status.get(), isc_dpb_set_db_charset, parameters.charset.c_str());

        auto impl = std::make_unique<Impl>();
        impl->parameters = parameters;
        impl->attachment = Client::get().provider()->createDatabase(status.get(), target.c_str(),
            dpb->getBufferLength(status.get()), dpb->getBuffer(status.get()));
        dpb->dispose();

        log(LogLevel::Info, "Connection", "Created database " + target);
        return std::unique_ptr<Connection>(new Connection(std::move(impl)));
    }
    catch (const Firebird::FbException& e)
    {
        if (dpb)
            dpb->dispose();
        firebird::throw_error(e, ErrorKind::Connection, "Could not create database " + target);
    }
}

const ConnectionParameters& Connection::parameters() const noexcept
{
    return impl_->parameters;
}

bool Connection::is_open() const noexcept
{
    std::lock_guard lock(impl_->mutex);
    return impl_->attachment != nullptr;
}

DatabaseInfo Connection::info()
{
    std::lock_guard lock(impl_->mutex);
    if (!impl_->attachment)
        throw Error(ErrorKind::InvalidArgument, "The connection is closed.");

    static constexpr unsigned char items[] = {
        isc_info_firebird_version,
        isc_info_ods_version,
        isc_info_ods_minor_version,
        isc_info_page_size,
        isc_info_db_sql_dialect,
        isc_info_end,
    };
    unsigned char buffer[1024] = {};

    Status status;
    try
    {
        impl_->attachment->getInfo(status.get(), sizeof(items), items, sizeof(buffer), buffer);
    }
    catch (const Firebird::FbException& e)
    {
        firebird::throw_error(e, ErrorKind::Database, "Could not read database information");
    }

    return parse_info(buffer, sizeof(buffer));
}

QueryResult Connection::execute(std::string_view sql, const Parameters& parameters, const QueryOptions& options)
{
    std::lock_guard lock(impl_->mutex);
    if (!impl_->attachment)
        throw Error(ErrorKind::InvalidArgument, "The connection is closed.");

    return firebird::run_statement(impl_->attachment, sql, parameters, options);
}

void Connection::close()
{
    std::lock_guard lock(impl_->mutex);
    if (!impl_->attachment)
        return;

    auto* attachment = impl_->attachment;
    impl_->attachment = nullptr;

    Status status;
    try
    {
        attachment->detach(status.get()); // Releases the interface on success.
        log(LogLevel::Info, "Connection", "Disconnected from " + connection_string(impl_->parameters));
    }
    catch (const Firebird::FbException& e)
    {
        attachment->release();
        firebird::throw_error(e, ErrorKind::Database, "Error while disconnecting");
    }
}

} // namespace brezee::core
