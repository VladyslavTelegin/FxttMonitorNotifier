using FxttMonitorNotifier.Droid.Services.Implementations;
using Xamarin.Forms;

[assembly: Dependency(typeof(PermanentCacheService))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface IPermanentCacheService
    {
        T Get<T>(string key);

        bool IsSet(string key);

        void Set<T>(string key, T value);

        void Invalidate(string key);
    }
}