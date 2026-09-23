#include <brezee/core/metadata.h>

#include <brezee/core/connection.h>

#include <string>

namespace brezee::core {

namespace {

// Every query returns: name, parent, description, system flag, then optional flag columns.
// Names are CHAR columns; Connection::execute already strips their padding.
struct ObjectQuery
{
    const char* sql;
    // Names of the flags that the extra columns represent, in order. A column that is true (1)
    // adds its flag.
    std::vector<const char*> flag_names;
};

ObjectQuery query_for(ObjectType type)
{
    switch (type)
    {
    case ObjectType::Table:
        // Relation types: 0 persistent, 2 external, 4/5 global temporary. 1 is a view, 3 virtual (MON$).
        return {R"(
            select rdb$relation_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0),
                   iif(rdb$relation_type = 2, 1, 0), iif(rdb$relation_type in (4, 5), 1, 0)
            from rdb$relations
            where rdb$view_blr is null and coalesce(rdb$relation_type, 0) in (0, 2, 4, 5)
            order by 1)",
            {"external", "global temporary"}};
    case ObjectType::View:
        return {R"(
            select rdb$relation_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0)
            from rdb$relations
            where rdb$view_blr is not null
            order by 1)",
            {}};
    case ObjectType::Procedure:
        return {R"(
            select rdb$procedure_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0)
            from rdb$procedures
            where rdb$package_name is null
            order by 1)",
            {}};
    case ObjectType::Function:
        return {R"(
            select rdb$function_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0),
                   iif(rdb$legacy_flag = 1, 1, 0)
            from rdb$functions
            where rdb$package_name is null
            order by 1)",
            {"legacy UDF"}};
    case ObjectType::Package:
        return {R"(
            select rdb$package_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0)
            from rdb$packages
            order by 1)",
            {}};
    case ObjectType::Trigger:
        return {R"(
            select rdb$trigger_name, rdb$relation_name, rdb$description, coalesce(rdb$system_flag, 0),
                   iif(rdb$trigger_inactive = 1, 1, 0)
            from rdb$triggers
            order by 1)",
            {"inactive"}};
    case ObjectType::Generator:
        return {R"(
            select rdb$generator_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0)
            from rdb$generators
            order by 1)",
            {}};
    case ObjectType::Domain:
        // Firebird creates an implicit RDB$nnn domain for every column declared without one.
        return {R"(
            select rdb$field_name, cast(null as varchar(63)), rdb$description,
                   iif(coalesce(rdb$system_flag, 0) <> 0 or rdb$field_name starting with 'RDB$', 1, 0)
            from rdb$fields
            order by 1)",
            {}};
    case ObjectType::Exception:
        return {R"(
            select rdb$exception_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0)
            from rdb$exceptions
            order by 1)",
            {}};
    case ObjectType::Role:
        return {R"(
            select rdb$role_name, cast(null as varchar(63)), rdb$description, coalesce(rdb$system_flag, 0)
            from rdb$roles
            order by 1)",
            {}};
    case ObjectType::Index:
        return {R"(
            select rdb$index_name, rdb$relation_name, rdb$description, coalesce(rdb$system_flag, 0),
                   iif(rdb$index_inactive = 1, 1, 0), iif(rdb$unique_flag = 1, 1, 0)
            from rdb$indices
            order by 1)",
            {"inactive", "unique"}};
    }
    return {"", {}};
}

std::string text_of(const Value& value)
{
    const auto* text = std::get_if<std::string>(&value);
    return text ? *text : std::string{};
}

bool is_set(const Value& value)
{
    const auto* number = std::get_if<std::int64_t>(&value);
    return number && *number != 0;
}

} // namespace

std::vector<DatabaseObject> list_objects(Connection& connection, ObjectType type, bool include_system)
{
    const auto query = query_for(type);
    const auto result = connection.execute(query.sql);

    std::vector<DatabaseObject> objects;
    objects.reserve(result.rows.size());

    for (const auto& row : result.rows)
    {
        DatabaseObject object;
        object.type = type;
        object.name = text_of(row[0]);
        object.parent = text_of(row[1]);
        object.description = text_of(row[2]);
        object.is_system = is_set(row[3]);

        if (object.is_system && !include_system)
            continue;

        for (std::size_t i = 0; i < query.flag_names.size(); ++i)
        {
            if (is_set(row[4 + i]))
                object.flags.emplace_back(query.flag_names[i]);
        }

        objects.push_back(std::move(object));
    }

    return objects;
}

} // namespace brezee::core
