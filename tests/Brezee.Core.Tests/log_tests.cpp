#include <brezee/core/log.h>
#include <brezee/core/runtime.h>

#include "test_support.h"

#include <doctest.h>

#include <algorithm>
#include <string>
#include <vector>

using brezee::core::LogLevel;

namespace {

struct Entry
{
    LogLevel level;
    std::string category;
    std::string message;
};

// Attaches a sink that records messages, and detaches it again when the test ends.
class CapturingSink
{
public:
    CapturingSink()
    {
        brezee::core::set_log_sink([this](LogLevel level, std::string_view category, std::string_view message) {
            entries.push_back({level, std::string(category), std::string(message)});
        });
    }

    ~CapturingSink() { brezee::core::set_log_sink({}); }

    CapturingSink(const CapturingSink&) = delete;
    CapturingSink& operator=(const CapturingSink&) = delete;

    std::vector<Entry> entries;
};

} // namespace

TEST_CASE("log forwards level, category and message to the sink")
{
    CapturingSink sink;

    brezee::core::log(LogLevel::Warning, "Test", "something happened");

    REQUIRE(sink.entries.size() == 1);
    CHECK(sink.entries[0].level == LogLevel::Warning);
    CHECK(sink.entries[0].category == "Test");
    CHECK(sink.entries[0].message == "something happened");
}

TEST_CASE("log without a sink does nothing")
{
    brezee::core::set_log_sink({});

    CHECK_NOTHROW(brezee::core::log(LogLevel::Error, "Test", "nobody is listening"));
}

TEST_CASE("detaching the sink stops delivery")
{
    std::vector<std::string> received;
    brezee::core::set_log_sink([&](LogLevel, std::string_view, std::string_view message) {
        received.emplace_back(message);
    });

    brezee::core::log(LogLevel::Info, "Test", "first");
    brezee::core::set_log_sink({});
    brezee::core::log(LogLevel::Info, "Test", "second");

    REQUIRE(received.size() == 1);
    CHECK(received[0] == "first");
}

TEST_CASE("a sink may replace itself while handling a message")
{
    int calls = 0;
    brezee::core::set_log_sink([&](LogLevel, std::string_view, std::string_view) {
        ++calls;
        brezee::core::set_log_sink({});
    });

    brezee::core::log(LogLevel::Info, "Test", "first");
    brezee::core::log(LogLevel::Info, "Test", "second");

    CHECK(calls == 1);
}

TEST_CASE("initialize logs the Firebird client and that the core started")
{
    CapturingSink sink;

    test_support::initialize_core();

    const auto logged = [&](std::string_view text) {
        return std::any_of(sink.entries.begin(), sink.entries.end(), [&](const Entry& entry) {
            return entry.level == LogLevel::Info && entry.category == "Runtime"
                && entry.message.find(text) != std::string::npos;
        });
    };
    CHECK(logged("Firebird client"));
    CHECK(logged("initialized"));
}
