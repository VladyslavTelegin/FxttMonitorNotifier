using FxttMonitorNotifier.Droid.Services.Implementations;

using Xamarin.Forms;

[assembly: Dependency(typeof(SharedPreferencesProvider))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface ISharedPreferencesProvider
    {
        T Get<T>(string key);

        void Save<T>(string key, T value);

        void Delete(string key);

        bool Exists(string key);
    }
}