#pragma once

// Internal to Brezee.Core: the only place that talks to the Firebird OO API.
// Public core headers never include this, so Firebird types stay out of the bridge.

#include <brezee/core/error.h>

#include <firebird/Interface.h>

#include <string>
#include <string_view>

namespace brezee::core::firebird {

// The loaded Firebird client library (fbclient). Loaded once and kept until the process exits.
class Client
{
public:
    // Loads the client library. Does nothing if a client is already loaded. Throws Error on failure.
    static void load(const std::string& library);

    // The loaded client. Throws Error(Internal) if load() has not succeeded.
    static Client& get();

    Firebird::IMaster* master() const noexcept { return master_; }
    Firebird::IUtil* util() const noexcept { return util_; }
    Firebird::IProvider* provider() const noexcept { return provider_; }

    const std::string& library() const noexcept { return library_; }
    unsigned major_version() const noexcept { return util_->getClientVersion() >> 8; }
    unsigned minor_version() const noexcept { return util_->getClientVersion() & 0xFF; }

private:
    Client(std::string library, Firebird::IMaster* master);

    std::string library_;
    Firebird::IMaster* master_;
    Firebird::IUtil* util_;
    Firebird::IProvider* provider_;
};

// Owns a Firebird status object for the duration of one or more API calls.
class Status
{
public:
    Status();
    ~Status();

    Status(const Status&) = delete;
    Status& operator=(const Status&) = delete;

    Firebird::ThrowStatusWrapper* get() noexcept { return &wrapper_; }

private:
    Firebird::ThrowStatusWrapper wrapper_;
};

// Converts a Firebird error into brezee::core::Error of the given kind, logging it with context.
[[noreturn]] void throw_error(const Firebird::FbException& error, ErrorKind kind, std::string_view context);

} // namespace brezee::core::firebird
