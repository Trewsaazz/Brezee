#pragma once

namespace Brezee::Bridge {

// Mirrors brezee::core::ErrorKind.
public enum class CoreErrorKind
{
    Internal,
    InvalidArgument,
    Connection,
    Database,
};

// A failure reported by the native core. The message is meant to be shown to users.
public ref class CoreException : public System::Exception
{
public:
    CoreException(System::String^ message, CoreErrorKind kind)
        : System::Exception(message)
        , _kind(kind)
    {
    }

    property CoreErrorKind Kind { CoreErrorKind get() { return _kind; } }

private:
    CoreErrorKind _kind;
};

} // namespace Brezee::Bridge
