#pragma once

#include <string_view>

namespace Brezee::Bridge::Interop {

// Converts a UTF-8 string from the native core into a managed string.
inline System::String^ ToManaged(std::string_view utf8)
{
    if (utf8.empty())
        return System::String::Empty;

    auto bytes = gcnew array<System::Byte>(static_cast<int>(utf8.size()));
    System::Runtime::InteropServices::Marshal::Copy(
        System::IntPtr(const_cast<char*>(utf8.data())), bytes, 0, bytes->Length);
    return System::Text::Encoding::UTF8->GetString(bytes);
}

} // namespace Brezee::Bridge::Interop
