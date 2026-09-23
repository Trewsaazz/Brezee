#include <brezee/core/runtime.h>

#include <brezee/core/log.h>
#include <brezee/core/version.h>

#include <string>

namespace brezee::core {

void initialize()
{
    // The Firebird client library will be loaded here once connections land.
    log(LogLevel::Info, "Runtime", "Brezee core " + std::string(version()) + " initialized");
}

} // namespace brezee::core
