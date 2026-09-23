#pragma once

// Helpers for tests that need a real Firebird client. They run against the embedded engine, so no
// server is needed: BREZEE_FIREBIRD_ROOT must point at a Firebird kit folder (eng/run-tests.ps1 and
// the Visual Studio debugger settings set it to the kit downloaded by the build).

#include <brezee/core/runtime.h>

#include <doctest.h>

#include <atomic>
#include <chrono>
#include <cstdlib>
#include <filesystem>
#include <string>
#include <system_error>

namespace test_support {

inline std::string environment(const char* name)
{
#ifdef _WIN32
    char* value = nullptr;
    std::size_t length = 0;
    if (_dupenv_s(&value, &length, name) != 0 || value == nullptr)
        return {};
    std::string result(value);
    std::free(value);
    return result;
#else
    const char* value = std::getenv(name);
    return value ? value : "";
#endif
}

inline std::string firebird_client_library()
{
    const auto root = environment("BREZEE_FIREBIRD_ROOT");
    REQUIRE_MESSAGE(!root.empty(),
        "BREZEE_FIREBIRD_ROOT is not set. Point it at the Firebird kit (third_party/firebird/<version>), "
        "or run the tests with eng/run-tests.ps1.");

#ifdef _WIN32
    return (std::filesystem::path(root) / "fbclient.dll").string();
#else
    return (std::filesystem::path(root) / "lib" / "libfbclient.so.2").string();
#endif
}

// Loads the Firebird client from the kit. Safe to call from every test.
inline void initialize_core()
{
    brezee::core::initialize({firebird_client_library()});
}

// A unique database file path in the temp folder, deleted again when the test ends.
class TempDatabase
{
public:
    TempDatabase()
    {
        static std::atomic<int> counter{0};
        const auto stamp = std::chrono::steady_clock::now().time_since_epoch().count();
        path_ = std::filesystem::temp_directory_path()
            / ("brezee-test-" + std::to_string(stamp) + "-" + std::to_string(counter++) + ".fdb");
    }

    ~TempDatabase()
    {
        std::error_code ignored;
        std::filesystem::remove(path_, ignored);
    }

    TempDatabase(const TempDatabase&) = delete;
    TempDatabase& operator=(const TempDatabase&) = delete;

    std::string path() const { return path_.string(); }
    bool exists() const { return std::filesystem::exists(path_); }

private:
    std::filesystem::path path_;
};

} // namespace test_support
