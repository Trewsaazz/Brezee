#pragma once

#include <functional>
#include <string_view>

namespace brezee::core {

enum class LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
};

// Receives every log message from the core. Strings are UTF-8 and only valid during the call.
// May be called from any thread.
using LogSink = std::function<void(LogLevel level, std::string_view category, std::string_view message)>;

// Routes core log messages to the host application. Pass an empty sink to detach.
void set_log_sink(LogSink sink);

// Writes a message to the attached sink. Does nothing if no sink is attached.
void log(LogLevel level, std::string_view category, std::string_view message);

} // namespace brezee::core
