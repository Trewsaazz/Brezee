using Brezee.App.Resources;
using Brezee.App.Shell;

namespace Brezee.App.Features.Welcome;

// Start page shown when Brezee opens.
public sealed class WelcomeViewModel : DocumentViewModel
{
    public WelcomeViewModel(string coreVersion)
        : base("welcome", Strings.Welcome_Title)
    {
        CoreVersion = coreVersion;
    }

    public string CoreVersion { get; }
}
