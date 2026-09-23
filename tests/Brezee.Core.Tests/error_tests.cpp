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

TEST_CASE("Error is a std::exception")
{
    // Checked through a base reference rather than throw/catch, which MSVC flags as unreachable code.
    const Error error(ErrorKind::Database, "Table not found");
    const std::exception& base = error;

    CHECK(std::string(base.what()) == "Table not found");
}
