#pragma once

#include "CoreException.h"
#include "Interop.h"

#include <brezee/core/error.h>

#include <exception>

namespace Brezee::Bridge {

// Converts a native core exception into the managed CoreException thrown to .NET.
// Every bridge method that calls the core catches std::exception and throws this, so native
// exceptions never reach .NET raw:
//
//     try { ... } catch (const std::exception& e) { throw ToManagedException(e); }
inline CoreException^ ToManagedException(const std::exception& error)
{
    const auto* core = dynamic_cast<const brezee::core::Error*>(&error);
    const auto kind = core ? static_cast<CoreErrorKind>(static_cast<int>(core->kind())) : CoreErrorKind::Internal;
    return gcnew CoreException(Interop::ToManaged(error.what()), kind);
}

} // namespace Brezee::Bridge
