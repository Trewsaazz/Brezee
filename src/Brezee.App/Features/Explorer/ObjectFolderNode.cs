using System.Globalization;
using Brezee.App.Connections;
using Brezee.App.Resources;
using Brezee.Bridge;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Features.Explorer;

// "Tables", "Views", ... under a connected database. Loads its objects the first time it is opened.
public sealed partial class ObjectFolderNode : ExplorerNode
{
    private readonly IDatabaseSession _session;
    private bool _loaded;

    public ObjectFolderNode(DatabaseNodeViewModel database, IDatabaseSession session, DatabaseObjectType type)
    {
        Database = database;
        _session = session;
        Type = type;
        Title = TitleFor(type);

        // A placeholder child so the tree shows an expander before anything is loaded.
        Children.Add(new MessageNode(database, Strings.Explorer_Loading));
    }

    public override DatabaseNodeViewModel Database { get; }

    public DatabaseObjectType Type { get; }

    public string Title { get; }

    // Number of objects once loaded; null before.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText))]
    public partial int? Count { get; private set; }

    public string CountText => Count is { } count ? count.ToString(CultureInfo.CurrentCulture) : string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    // The folders shown under every database, in order.
    public static IReadOnlyList<DatabaseObjectType> Order { get; } =
    [
        DatabaseObjectType.Domain,
        DatabaseObjectType.Table,
        DatabaseObjectType.View,
        DatabaseObjectType.Procedure,
        DatabaseObjectType.Function,
        DatabaseObjectType.Package,
        DatabaseObjectType.Trigger,
        DatabaseObjectType.Generator,
        DatabaseObjectType.Exception,
        DatabaseObjectType.Role,
        DatabaseObjectType.Index,
    ];

    protected override void OnExpanded()
    {
        if (!_loaded && !IsLoading)
            _ = LoadAsync();
    }

    // Reloads the objects, e.g. after they were changed elsewhere.
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var objects = await _session.ListObjectsAsync(Type);

            Children.Clear();
            foreach (var info in objects)
                Children.Add(new ObjectNode(Database, info));
            if (objects.Count == 0)
                Children.Add(new MessageNode(Database, Strings.Explorer_None));

            Count = objects.Count;
            _loaded = true;
        }
        catch (CoreException ex)
        {
            // Shown in place of the objects; the user can refresh to try again.
            Children.Clear();
            Children.Add(new MessageNode(Database, ex.Message, isError: true));
            Count = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string TitleFor(DatabaseObjectType type) => type switch
    {
        DatabaseObjectType.Table => Strings.Explorer_Tables,
        DatabaseObjectType.View => Strings.Explorer_Views,
        DatabaseObjectType.Procedure => Strings.Explorer_Procedures,
        DatabaseObjectType.Function => Strings.Explorer_Functions,
        DatabaseObjectType.Package => Strings.Explorer_Packages,
        DatabaseObjectType.Trigger => Strings.Explorer_Triggers,
        DatabaseObjectType.Generator => Strings.Explorer_Generators,
        DatabaseObjectType.Domain => Strings.Explorer_Domains,
        DatabaseObjectType.Exception => Strings.Explorer_Exceptions,
        DatabaseObjectType.Role => Strings.Explorer_Roles,
        DatabaseObjectType.Index => Strings.Explorer_Indices,
        _ => type.ToString(),
    };
}
