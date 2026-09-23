#pragma once

namespace brezee::core {
class Connection;
}

namespace Brezee::Bridge {

// Where and how to connect. Mirrors brezee::core::ConnectionParameters.
public ref class ConnectionSettings
{
public:
    ConnectionSettings()
    {
        Port = 3050;
        Charset = "UTF8";
    }

    // Server host name or IP. Empty opens the database locally (embedded).
    property System::String^ Host;
    property int Port;

    // Database file path on the server, or an alias.
    property System::String^ Database;

    property System::String^ User;
    property System::String^ Password;
    property System::String^ Role;
    property System::String^ Charset;
};

// Mirrors brezee::core::ObjectType.
public enum class DatabaseObjectType
{
    Table,
    View,
    Procedure,
    Function,
    Package,
    Trigger,
    Generator,
    Domain,
    Exception,
    Role,
    Index,
};

// One database object. Mirrors brezee::core::DatabaseObject.
public ref class DatabaseObjectInfo sealed
{
public:
    property DatabaseObjectType Type;
    property System::String^ Name;

    // The table or view an index or trigger belongs to; empty otherwise.
    property System::String^ Parent;

    property System::String^ Description;
    property bool IsSystem;

    // E.g. "unique", "inactive", "global temporary", "legacy UDF".
    property System::Collections::Generic::IReadOnlyList<System::String^>^ Flags;
};

// Facts about an open database. Mirrors brezee::core::DatabaseInfo.
public ref class DatabaseDetails sealed
{
public:
    property System::String^ ServerVersion;
    property int OdsMajor;
    property int OdsMinor;
    property int PageSize;
    property int SqlDialect;
};

// An open attachment to a Firebird database. Dispose it to disconnect.
// Methods may be called from any thread, one call at a time. Failures throw CoreException.
public ref class DatabaseConnection sealed
{
public:
    // Attaches to an existing database.
    static DatabaseConnection^ Open(ConnectionSettings^ settings);

    // Creates a new database and attaches to it.
    static DatabaseConnection^ Create(ConnectionSettings^ settings);

    // Disconnects, ignoring errors.
    ~DatabaseConnection();
    !DatabaseConnection();

    property bool IsOpen { bool get(); }

    DatabaseDetails^ GetDetails();

    // Lists the objects of one type, ordered by name; system objects only if asked for.
    System::Collections::Generic::IReadOnlyList<DatabaseObjectInfo^>^ ListObjects(DatabaseObjectType type, bool includeSystem);

    // Disconnects, reporting errors as CoreException.
    void Close();

private:
    explicit DatabaseConnection(brezee::core::Connection* connection);

    brezee::core::Connection* _connection;
};

} // namespace Brezee::Bridge
