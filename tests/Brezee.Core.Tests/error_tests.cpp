#include <brezee/core/error.h>

#include <doctest.h>

#include <string>

using brezee::core::Error;
using brezee::core::ErrorKind;

TEST_CASE("Error carries its kind and message")
{
    const Error error(ErrorKind::Connection, "Server not reachable");

    CHECK(error.kind() == ErrorKind::Connection);
    CHECK(std::string(error.what()) == "Server not reachable");
}

TEST_CASE("Error can be caught as std::exception")
{
    try
    {
        throw Error(ErrorKind::Database, "Table not found");
    }
    catch (const std::exception& e)
    {
        CHECK(std::string(e.what()) == "Table not found");
        return;
    }

    FAIL("exception was not caught");
}
