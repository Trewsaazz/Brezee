#include <brezee/core/runtime.h>

#include <brezee/core/log.h>
#include <brezee/core/version.h>

#include "firebird/client.h"

#include <string>

namespace brezee::core {

namespace {

#ifdef _WIN32
constexpr const char* default_client_library = "fbclient.dll";
#else
constexpr const char* default_client_library = "libfbclient.so.2";
#endif

} // namespace

void initialize(const RuntimeOptions& options)
{
    const std::string library = options.client_library.empty() ? default_client_library : options.client_library;
    firebird::Client::load(library);

    const auto& client = firebird::Client::get();
    log(LogLevel::Info, "Runtime",
        "Firebird client " + std::to_string(client.major_version()) + "." + std::to_string(client.minor_version())
            + " loaded from " + client.library());

    log(LogLevel::Info, "Runtime", "Brezee core " + std::string(version()) + " initialized");
}

} // namespace brezee::core
