namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    public class PermanentCacheService : IPermanentCacheService
    {
        #region Constants

        private const string CacheKey  = "fxtt_monitor_notifier_p.cache";

        #endregion

        #region ServicesAndProviders

        protected IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

        protected ISharedPreferencesProvider SharedPreferencesProvider => this.ServiceProvider.SharedPreferencesProvider;

        #endregion

        public T Get<T>(string key) => this.SharedPreferencesProvider.Get<T>($"{CacheKey}:{key}");

        public bool IsSet(string key) => this.SharedPreferencesProvider.Exists($"{CacheKey}:{key}");

        public void Set<T>(string key, T value)
        {
            this.SharedPreferencesProvider.Save($"{CacheKey}:{key}", value);
        }

        public void Invalidate(string key)
        {
            if (this.IsSet(key))
            {
                this.SharedPreferencesProvider.Delete($"{CacheKey}:{key}");
            }
        }
    }
}