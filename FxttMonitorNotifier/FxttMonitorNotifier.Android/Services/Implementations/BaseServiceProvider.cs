namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using System;

    using Xamarin.Forms;

    public sealed class BaseServiceProvider : IBaseServiceProvider
    {
        #region PrivateFields

        private IAuthenticationService _authenticationService;
        private ILoggingService _loggingService;
        private IMessagesApiProvider _messagesApiProvider;
        private IPermanentCacheService _permanentCacheService;
        private ISettingsProvider _settingsProvider;
        private ISharedPreferencesProvider _sharedPreferencesProvider;

        #endregion

        #region Sigleton Impl.

        private static readonly Lazy<BaseServiceProvider> Object = new Lazy<BaseServiceProvider>(() => new BaseServiceProvider());

        public static BaseServiceProvider Instance => Object.Value;

        // Singleton Ctor.
        private BaseServiceProvider() { }

        #endregion

        #region ServicesAndProviders

        public IAuthenticationService AuthenticationService => _authenticationService ?? (_authenticationService = DependencyService.Get<IAuthenticationService>());
        public ILoggingService LoggingService => _loggingService ?? (_loggingService = DependencyService.Get<ILoggingService>());
        public IMessagesApiProvider MessagesApiProvider => _messagesApiProvider ?? (_messagesApiProvider = DependencyService.Get<IMessagesApiProvider>());
        public IPermanentCacheService PermanentCacheService => _permanentCacheService ?? (_permanentCacheService = DependencyService.Get<IPermanentCacheService>());
        public ISettingsProvider SettingsProvider => _settingsProvider ?? (_settingsProvider = DependencyService.Get<ISettingsProvider>());
        public ISharedPreferencesProvider SharedPreferencesProvider => _sharedPreferencesProvider ?? (_sharedPreferencesProvider = DependencyService.Get<ISharedPreferencesProvider>());
    
        #endregion
    }
}