#pragma once

#include <stdexcept>
#include <string>

namespace brezee::core {

// Broad category of a core failure. The bridge mirrors these values in CoreErrorKind.
enum class ErrorKind
{
    Internal,        // A bug or unexpected state inside Brezee.
    InvalidArgument, // The caller passed something invalid.
    Connection,      // Could not reach or talk to the Firebird server.
    Database,        // Firebird reported an error for a statement or operation.
};

// The exception type thrown by the core. Messages are UTF-8 and meant to be shown to users.
class Error : public std::runtime_error
{
public:
    Error(ErrorKind kind, const std::string& message)
        : std::runtime_error(message)
        , kind_(kind)
    {
    }

    [[nodiscard]] ErrorKind kind() const noexcept { return kind_; }

private:
    ErrorKind kind_;
};

} // namespace brezee::core
