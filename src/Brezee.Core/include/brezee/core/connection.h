#pragma once

#include <cstdint>
#include <memory>
#include <string>

namespace brezee::core {

// Everything needed to attach to a Firebird database. Strings are UTF-8.
struct ConnectionParameters
{
    // Server host name or IP. Empty opens the database file directly with the embedded engine,
    // even if a Firebird server runs on this machine (use "localhost" to go through that server).
    std::string host;
    std::uint16_t port = 3050;

    // Database file path on the server, or an alias from the server's databases.conf.
    std::string database;

    std::string user;
    std::string password;
    std::string role;

    // Connection character set. UTF8 keeps every string lossless end to end.
    std::string charset = "UTF8";
};

// Facts about an open database.
struct DatabaseInfo
{
    std::string server_version; // e.g. "WI-V5.0.4.1812 Firebird 5.0"
    int ods_major = 0;          // On-disk structure version, e.g. 13.1 for Firebird 5
    int ods_minor = 0;
    int page_size = 0;          // In bytes
    int sql_dialect = 0;
};

// The Firebird connection string for the parameters: "host/port:database", or just the
// database for a local connection.
[[nodiscard]] std::string connection_string(const ConnectionParameters& parameters);

// An attachment to a Firebird database. Not copyable; closes itself when destroyed.
// Safe to use from any thread, one call at a time.
class Connection
{
public:
    // Attaches to an existing database. Throws Error(Connection) on failure.
    [[nodiscard]] static std::unique_ptr<Connection> open(const ConnectionParameters& parameters);

    // Creates a new database and attaches to it. Throws Error(Connection) on failure.
    [[nodiscard]] static std::unique_ptr<Connection> create(const ConnectionParameters& parameters, int page_size = 16384);

    ~Connection();

    Connection(const Connection&) = delete;
    Connection& operator=(const Connection&) = delete;

    [[nodiscard]] const ConnectionParameters& parameters() const noexcept;
    [[nodiscard]] bool is_open() const noexcept;

    // Reads server and database details. Throws Error(Database) on failure.
    [[nodiscard]] DatabaseInfo info();

    // Detaches from the database. Throws Error(Database) if Firebird reports a problem.
    // Does nothing if already closed.
    void close();

private:
    struct Impl;
    explicit Connection(std::unique_ptr<Impl> impl);

    std::unique_ptr<Impl> impl_;
};

} // namespace brezee::core
