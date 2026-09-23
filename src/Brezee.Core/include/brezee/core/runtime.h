#pragma once

#include <string>

namespace brezee::core {

struct RuntimeOptions
{
    // Path or file name of the Firebird client library. Empty means the platform default
    // (fbclient.dll on Windows, libfbclient.so.2 elsewhere), found next to the application first.
    std::string client_library;
};

// Prepares the core for use and loads the Firebird client. Call once at startup, after attaching
// a log sink. Throws brezee::core::Error on failure.
void initialize(const RuntimeOptions& options = {});

} // namespace brezee::core
