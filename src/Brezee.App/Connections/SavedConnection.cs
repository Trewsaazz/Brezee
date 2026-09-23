using System.Text.Json.Serialization;
using Brezee.Bridge;

namespace Brezee.App.Connections;

// A registered database: everything needed to connect again, under a name the user chose.
// The password is only kept if the user asked, and then only encrypted (see ICredentialProtector).
public sealed record SavedConnection
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    // True for a local database file opened with the embedded engine.
    public bool IsLocal { get; init; }

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 3050;

    public required string Database { get; init; }

    public string User { get; init; } = "SYSDBA";

    public string Role { get; init; } = string.Empty;

    public string Charset { get; init; } = "UTF8";

    // The remembered password, encrypted with ICredentialProtector. Null when not remembered.
    public string? ProtectedPassword { get; init; }

    [JsonIgnore]
    public bool HasSavedPassword => ProtectedPassword is not null;

    public ConnectionSettings ToSettings(string password) => new()
    {
        Host = IsLocal ? string.Empty : Host,
        Port = Port,
        Database = Database,
        User = User,
        Password = password,
        Role = Role,
        Charset = Charset,
    };
}
