#include "CoreInfo.h"
#include "Interop.h"

#include <brezee/core/version.h>

namespace Brezee::Bridge {

System::String^ CoreInfo::Version::get()
{
    return Interop::ToManaged(brezee::core::version());
}

} // namespace Brezee::Bridge
