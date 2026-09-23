#pragma once

#include <string_view>

namespace Brezee::Bridge::Interop {

// Converts a UTF-8 string from the native core into a managed string.
inline System::String^ ToManaged(std::string_view utf8)
{
    if (utf8.empty())
        return System::String::Empty;

    // Encoding::GetString takes a non-const pointer but never writes through it.
    auto* bytes = reinterpret_cast<unsigned char*>(const_cast<char*>(utf8.data()));
    return System::Text::Encoding::UTF8->GetString(bytes, static_cast<int>(utf8.size()));
}

} // namespace Brezee::Bridge::Interop
