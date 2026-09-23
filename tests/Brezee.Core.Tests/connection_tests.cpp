#include <brezee/core/connection.h>
#include <brezee/core/error.h>

#include "test_support.h"

#include <doctest.h>

using brezee::core::Connection;
using brezee::core::ConnectionParameters;
using brezee::core::Error;
using brezee::core::ErrorKind;

namespace {

// Embedded connection (no host) to a database file. Embedded mode trusts the given user name.
ConnectionParameters embedded(const std::string& database)
{
    ConnectionParameters parameters;
    parameters.database = database;
    parameters.user = "SYSDBA";
    return parameters;
}

} // namespace

TEST_CASE("connection_string")
{
    ConnectionParameters parameters;
    parameters.database = "C:/data/employee.fdb";

    SUBCASE("local connections use the database path as is")
    {
        CHECK(brezee::core::connection_string(parameters) == "C:/data/employee.fdb");
    }

    SUBCASE("remote connections use host/port:database")
    {
        parameters.host = "db.example.com";
        parameters.port = 3051;
        CHECK(brezee::core::connection_string(parameters) == "db.example.com/3051:C:/data/employee.fdb");
    }

    SUBCASE("aliases are passed through")
    {
        parameters.host = "localhost";
        parameters.database = "employee";
        CHECK(brezee::core::connection_string(parameters) == "localhost/3050:employee");
    }
}

TEST_CASE("create, inspect, close and reopen a database")
{
    test_support::initialize_core();
    test_support::TempDatabase database;

    {
        auto connection = Connection::create(embedded(database.path()), 8192);
        REQUIRE(connection->is_open());
        CHECK(database.exists());

        const auto info = connection->info();
        CHECK(info.page_size == 8192);
        CHECK(info.sql_dialect == 3);
        CHECK(info.ods_major >= 12); // Firebird 3.0 or newer
        CHECK(info.server_version.find("Firebird") != std::string::npos);

        connection->close();
        CHECK_FALSE(connection->is_open());
        CHECK_NOTHROW(connection->close()); // Closing twice is harmless.
    }

    auto reopened = Connection::open(embedded(database.path()));
    CHECK(reopened->is_open());
    CHECK(reopened->parameters().database == database.path());
}

TEST_CASE("the destructor closes the connection")
{
    test_support::initialize_core();
    test_support::TempDatabase database;

    Connection::create(embedded(database.path())).reset();

    // If the first attachment were still open, it would still hold the file; reopening proves it closed cleanly.
    CHECK_NOTHROW(Connection::open(embedded(database.path()))->close());
}

TEST_CASE("opening a missing database fails with a readable Connection error")
{
    test_support::initialize_core();
    test_support::TempDatabase missing;

    try
    {
        (void)Connection::open(embedded(missing.path()));
        FAIL("expected brezee::core::Error");
    }
    catch (const Error& e)
    {
        CHECK(e.kind() == ErrorKind::Connection);
        CHECK(std::string(e.what()).find(missing.path()) != std::string::npos);
    }
}

TEST_CASE("info on a closed connection is rejected")
{
    test_support::initialize_core();
    test_support::TempDatabase database;

    auto connection = Connection::create(embedded(database.path()));
    connection->close();

    CHECK_THROWS_AS((void)connection->info(), Error);
}

TEST_CASE("connect to a Firebird server over the network")
{
    // Needs a running server: CI starts one with eng/start-test-server.ps1. Locally this is skipped
    // unless BREZEE_TEST_SERVER is set; on CI a missing server is an error, never a silent skip.
    const auto server = test_support::environment("BREZEE_TEST_SERVER");
    if (server.empty())
    {
        REQUIRE_MESSAGE(test_support::environment("CI") != "true", "CI must set BREZEE_TEST_SERVER");
        MESSAGE("Skipped: set BREZEE_TEST_SERVER (and BREZEE_TEST_PASSWORD) to run against a server.");
        return;
    }

    test_support::initialize_core();
    test_support::TempDatabase database; // The server runs on this machine, so the path is shared.

    ConnectionParameters parameters;
    parameters.host = server;
    parameters.database = database.path();
    parameters.user = "SYSDBA";
    parameters.password = test_support::environment("BREZEE_TEST_PASSWORD");

    SUBCASE("create and reopen a database through the server")
    {
        {
            auto created = Connection::create(parameters);
            const auto info = created->info();
            CHECK(info.server_version.find("Firebird") != std::string::npos);
            CHECK(info.ods_major >= 12);
        }

        auto reopened = Connection::open(parameters);
        CHECK(reopened->is_open());
    }

    SUBCASE("a wrong password is rejected with a Connection error")
    {
        Connection::create(parameters).reset();
        parameters.password = "definitely-not-the-password";

        try
        {
            (void)Connection::open(parameters);
            FAIL("expected brezee::core::Error");
        }
        catch (const Error& e)
        {
            CHECK(e.kind() == ErrorKind::Connection);
        }
    }
}
