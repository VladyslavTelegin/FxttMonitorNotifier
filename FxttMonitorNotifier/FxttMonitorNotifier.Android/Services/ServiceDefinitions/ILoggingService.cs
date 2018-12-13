using FxttMonitorNotifier.Droid.Enums.Logging;
using FxttMonitorNotifier.Droid.Services.Implementations;

using System;

using Xamarin.Forms;

[assembly: Dependency(typeof(LoggingService))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface ILoggingService
    {
        void Log(Exception exception, LogType logType, LogSourceType logSourceType = LogSourceType.Local);

        void Log(string message, LogType logType, LogSourceType logSourceType = LogSourceType.Local);

        string GetTodayLogs();

        void ClearLogs();
    }
}