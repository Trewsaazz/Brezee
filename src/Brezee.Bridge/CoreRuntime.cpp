#include "CoreRuntime.h"
#include "CoreErrors.h"
#include "Interop.h"

#include <brezee/core/log.h>
#include <brezee/core/runtime.h>

#include <vcclr.h>

namespace {

using namespace Brezee::Bridge;

// Core log sink that forwards to a managed handler. A named type rather than a lambda: lambdas are
// not allowed inside member functions of managed classes (C3923).
struct ManagedLogSink
{
    gcroot<CoreLogHandler^> handler;

    void operator()(brezee::core::LogLevel level, std::string_view category, std::string_view message) const
    {
        // A failing log handler must never take down the core, so swallow its exceptions.
        try
        {
            handler->Invoke(static_cast<CoreLogLevel>(static_cast<int>(level)),
                Interop::ToManaged(category), Interop::ToManaged(message));
        }
        catch (System::Exception^)
        {
        }
    }
};

} // namespace

namespace Brezee::Bridge {

void CoreRuntime::Initialize(CoreLogHandler^ logHandler)
{
    if (logHandler == nullptr)
        throw gcnew System::ArgumentNullException("logHandler");

    brezee::core::set_log_sink(ManagedLogSink{logHandler});

    try
    {
        brezee::core::initialize();
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

void CoreRuntime::Shutdown()
{
    // The sink holds a GC handle; release it while the CLR is still running.
    brezee::core::set_log_sink({});
}

} // namespace Brezee::Bridge
