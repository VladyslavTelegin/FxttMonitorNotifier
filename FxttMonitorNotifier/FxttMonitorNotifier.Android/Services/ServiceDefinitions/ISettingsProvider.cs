using FxttMonitorNotifier.Droid.Services.Implementations;

using Xamarin.Forms;

[assembly: Dependency(typeof(SettingsProvider))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface ISettingsProvider
    {
        bool IsDndModeEnabled { get; }

        void ToggleDndSettings();

        void DisableDnd();
    }
}