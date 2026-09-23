#pragma once

namespace Brezee::Bridge {

// Mirrors brezee::core::LogLevel.
public enum class CoreLogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
};

// Receives log messages from the native core. May be called from any thread.
public delegate void CoreLogHandler(CoreLogLevel level, System::String^ category, System::String^ message);

// Starts and stops the native core.
public ref class CoreRuntime abstract sealed
{
public:
    // Routes core log messages to the handler, then initializes the core.
    // Throws CoreException if the core cannot start.
    static void Initialize(CoreLogHandler^ logHandler);

    // Detaches the log handler. Call before the application exits.
    static void Shutdown();
};

} // namespace Brezee::Bridge
