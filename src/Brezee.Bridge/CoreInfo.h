#pragma once

namespace Brezee::Bridge {

// Information about the native Brezee core library.
public ref class CoreInfo abstract sealed
{
public:
    static property System::String^ Version { System::String^ get(); }
};

} // namespace Brezee::Bridge
