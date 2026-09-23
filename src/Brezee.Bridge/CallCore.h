#pragma once

#include "CoreException.h"
#include "Interop.h"

#include <brezee/core/error.h>

#include <exception>

namespace Brezee::Bridge {

// Runs a call into the native core and turns any C++ exception into a managed CoreException.
// Every bridge method that can fail must go through this, so native exceptions never reach .NET raw.
template <typename F>
decltype(auto) CallCore(F&& call)
{
    try
    {
        return call();
    }
    catch (const brezee::core::Error& e)
    {
        throw gcnew CoreException(Interop::ToManaged(e.what()), static_cast<CoreErrorKind>(static_cast<int>(e.kind())));
    }
    catch (const std::exception& e)
    {
        throw gcnew CoreException(Interop::ToManaged(e.what()), CoreErrorKind::Internal);
    }
}

} // namespace Brezee::Bridge
