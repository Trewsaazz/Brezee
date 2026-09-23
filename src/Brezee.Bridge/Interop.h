#pragma once

#include <string>
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

// Converts a managed string into UTF-8 for the native core. A null string becomes empty.
inline std::string ToNative(System::String^ text)
{
    if (System::String::IsNullOrEmpty(text))
        return {};

    auto bytes = System::Text::Encoding::UTF8->GetBytes(text);
    pin_ptr<unsigned char> pinned = &bytes[0];
    return std::string(reinterpret_cast<const char*>(pinned), static_cast<std::size_t>(bytes->Length));
}

} // namespace Brezee::Bridge::Interop
