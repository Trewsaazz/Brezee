#include <brezee/core/connection.h>
#include <brezee/core/metadata.h>

#include "test_support.h"

#include <doctest.h>

#include <algorithm>
#include <memory>

using namespace brezee::core;

namespace {

// A database with one of each kind of object.
struct SampleDatabase
{
    test_support::TempDatabase file;
    std::unique_ptr<Connection> connection;

    SampleDatabase()
    {
        test_support::initialize_core();
        ConnectionParameters parameters;
        parameters.database = file.path();
        parameters.user = "SYSDBA";
        connection = Connection::create(parameters);

        for (const char* sql : {
                 "create domain d_money as numeric(12,2)",
                 "create table customers (id integer not null primary key, name varchar(60), balance d_money)",
                 "create global temporary table scratch (id integer) on commit delete rows",
                 "create view big_customers as select * from customers where balance > 1000",
                 "create unique index ix_customers_name on customers (name)",
                 "create sequence seq_customers",
                 "create exception e_overdrawn 'Account overdrawn'",
                 "create role clerk",
                 "create procedure get_answer returns (answer integer) as begin answer = 42; suspend; end",
                 "create function twice (x integer) returns integer as begin return x * 2; end",
                 "create package tools as begin function one returns integer; end",
                 "create package body tools as begin function one returns integer as begin return 1; end end",
                 "create trigger customers_bi for customers before insert as begin end",
                 "create trigger on_connect on connect as begin end",
                 "alter trigger on_connect inactive",
                 "comment on table customers is 'People who buy things'",
             })
        {
            connection->execute(sql);
        }
    }

    std::vector<DatabaseObject> list(ObjectType type, bool include_system = false)
    {
        return list_objects(*connection, type, include_system);
    }
};

std::vector<std::string> names(const std::vector<DatabaseObject>& objects)
{
    std::vector<std::string> result;
    for (const auto& object : objects)
        result.push_back(object.name);
    return result;
}

const DatabaseObject& find(const std::vector<DatabaseObject>& objects, const std::string& name)
{
    auto it = std::find_if(objects.begin(), objects.end(), [&](const DatabaseObject& o) { return o.name == name; });
    REQUIRE_MESSAGE(it != objects.end(), name, " not listed");
    return *it;
}

bool has_flag(const DatabaseObject& object, const std::string& flag)
{
    return std::find(object.flags.begin(), object.flags.end(), flag) != object.flags.end();
}

} // namespace

TEST_CASE("list_objects lists each kind of user object")
{
    SampleDatabase db;

    CHECK(names(db.list(ObjectType::Table)) == std::vector<std::string>{"CUSTOMERS", "SCRATCH"});
    CHECK(names(db.list(ObjectType::View)) == std::vector<std::string>{"BIG_CUSTOMERS"});
    CHECK(names(db.list(ObjectType::Procedure)) == std::vector<std::string>{"GET_ANSWER"});
    CHECK(names(db.list(ObjectType::Function)) == std::vector<std::string>{"TWICE"}); // Not the packaged ONE
    CHECK(names(db.list(ObjectType::Package)) == std::vector<std::string>{"TOOLS"});
    CHECK(names(db.list(ObjectType::Generator)) == std::vector<std::string>{"SEQ_CUSTOMERS"});
    CHECK(names(db.list(ObjectType::Domain)) == std::vector<std::string>{"D_MONEY"}); // No implicit RDB$ domains
    CHECK(names(db.list(ObjectType::Exception)) == std::vector<std::string>{"E_OVERDRAWN"});
    CHECK(names(db.list(ObjectType::Role)) == std::vector<std::string>{"CLERK"});
}

TEST_CASE("objects carry their parent, flags and description")
{
    SampleDatabase db;

    const auto tables = db.list(ObjectType::Table);
    CHECK(find(tables, "CUSTOMERS").description == "People who buy things");
    CHECK(has_flag(find(tables, "SCRATCH"), "global temporary"));
    CHECK(find(tables, "CUSTOMERS").flags.empty());

    const auto triggers = db.list(ObjectType::Trigger);
    CHECK(find(triggers, "CUSTOMERS_BI").parent == "CUSTOMERS");
    CHECK(find(triggers, "ON_CONNECT").parent.empty()); // Database trigger
    CHECK(has_flag(find(triggers, "ON_CONNECT"), "inactive"));

    const auto indices = db.list(ObjectType::Index);
    const auto by_name = find(indices, "IX_CUSTOMERS_NAME");
    CHECK(by_name.parent == "CUSTOMERS");
    CHECK(has_flag(by_name, "unique"));
    // The primary key's index is created by Firebird but belongs to the user's table.
    CHECK(std::any_of(indices.begin(), indices.end(), [](const DatabaseObject& o) {
        return o.parent == "CUSTOMERS" && o.name.rfind("RDB$PRIMARY", 0) == 0;
    }));
}

TEST_CASE("system objects are hidden unless requested")
{
    SampleDatabase db;

    const auto user_tables = db.list(ObjectType::Table);
    CHECK(std::none_of(user_tables.begin(), user_tables.end(), [](const DatabaseObject& o) { return o.is_system; }));

    const auto all_tables = db.list(ObjectType::Table, true);
    const auto relations = find(all_tables, "RDB$RELATIONS");
    CHECK(relations.is_system);
}

TEST_CASE("an empty database lists no user objects")
{
    test_support::initialize_core();
    test_support::TempDatabase file;
    ConnectionParameters parameters;
    parameters.database = file.path();
    parameters.user = "SYSDBA";
    auto connection = Connection::create(parameters);

    for (auto type : {ObjectType::Table, ObjectType::View, ObjectType::Procedure, ObjectType::Function,
             ObjectType::Package, ObjectType::Trigger, ObjectType::Generator, ObjectType::Domain,
             ObjectType::Exception, ObjectType::Role, ObjectType::Index})
    {
        CAPTURE(static_cast<int>(type));
        CHECK(list_objects(*connection, type).empty());
    }
}
