namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface IBaseServiceProvider
    {
        IAuthenticationService AuthenticationService { get; }
        ILoggingService LoggingService { get; }
        IMessagesApiProvider MessagesApiProvider { get; }
        IPermanentCacheService PermanentCacheService { get;  }
        ISettingsProvider SettingsProvider { get; }
        ISharedPreferencesProvider SharedPreferencesProvider { get; }
    }
}