#pragma once

#include <string>
#include <vector>

namespace brezee::core {

class Connection;

// The kinds of objects the database explorer lists.
enum class ObjectType
{
    Table,
    View,
    Procedure,
    Function,
    Package,
    Trigger,
    Generator,
    Domain,
    Exception,
    Role,
    Index,
};

// One database object, as listed in the explorer.
struct DatabaseObject
{
    ObjectType type = ObjectType::Table;
    std::string name;

    // The table or view an index or trigger belongs to. Empty for database-level objects
    // (and for database or DDL triggers).
    std::string parent;

    // COMMENT ON text, if any.
    std::string description;

    // True for Firebird's own objects (RDB$, MON$, SEC$...).
    bool is_system = false;

    // Extra facts that vary by type: "external", "global temporary" (tables), "legacy UDF"
    // (functions), "inactive" (triggers, indices), "unique" (indices).
    std::vector<std::string> flags;
};

// Lists the objects of one type, ordered by name. System objects are only included on request.
// Throws Error(Database) on failure.
[[nodiscard]] std::vector<DatabaseObject> list_objects(Connection& connection, ObjectType type, bool include_system = false);

} // namespace brezee::core
