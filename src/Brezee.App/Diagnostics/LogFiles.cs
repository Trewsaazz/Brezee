using System.IO;

namespace Brezee.App.Diagnostics;

// Where Brezee writes its log files: %LOCALAPPDATA%\Brezee\logs, one file per day.
public sealed class LogFiles
{
    public LogFiles()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brezee", "logs"))
    {
    }

    public LogFiles(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }

    // Serilog adds the date before the extension: brezee-20260923.log.
    public string FilePattern => Path.Combine(Directory, "brezee-.log");

    public const int RetainedDays = 14;
}
