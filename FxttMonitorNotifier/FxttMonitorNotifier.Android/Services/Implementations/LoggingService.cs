using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FxttMonitorNotifier.Droid.Enums.Logging;
using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

using static FxttMonitorNotifier.Droid.Models.GlobalConstants;

namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    public class LoggingService : ILoggingService
   {
        private static readonly object _logWriteActionSyncLock = new object();

        protected string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                    LoggingServiceConstants.LogsFolderName);

        protected string FilePath => Path.Combine(FolderPath, $"{DateTime.UtcNow.ToString(DefaultDateTimeFormat.Split(' ').First())}.log");

        public void ClearLogs()
        {
            try
            {
                var logFilesDirectory = new DirectoryInfo(FolderPath);

                logFilesDirectory.GetFiles()
                                 .AsParallel()
                                 .ForAll(_ => _.Delete());
            }
            catch (Exception ex)
            {
                this.Log(ex, LogType.Error);
            }
        }

        public void Log(Exception exception, LogType logType, LogSourceType logSourceType = LogSourceType.Local)
        {
            if (logType != LogType.Suppress)
            {
                try
                {
                    if (logSourceType == LogSourceType.Local)
                    {
                        this.CreateLogFileIfNecessary();

                        var exceptionsList = new List<Exception>();

                        if (exception is AggregateException aggregateException)
                        {
                            var flattenException = aggregateException.Flatten();
                            exceptionsList = flattenException.InnerExceptions.ToList();
                        }
                        else
                        {
                            exceptionsList.Add(exception);
                        }

                        foreach (var currentException in exceptionsList)
                        {
                            var logMessage = $"{DateTime.UtcNow.ToString(DefaultDateTimeFormat)} {logType.ToString().ToUpperInvariant()}:" +
                                       $"\nException Message: {currentException.Message}\nStack Trace:\n{currentException.StackTrace}";

                            var currentFileContent = File.ReadAllText(this.FilePath);

                            var newFileContent = $"{logMessage}\n\n\n{currentFileContent}";

                            lock (_logWriteActionSyncLock)
                            {
                                using (var fileWriter = File.CreateText(FilePath))
                                {
                                    fileWriter.WriteLine(newFileContent);
                                }
                            }
                        }
                    }
                    else if (logSourceType == LogSourceType.Remote)
                    {
                        // TODO: Impelement remote logging here (if necessary).
                    }
                }
                catch (Exception ex)
                {
                    this.Log(ex, LogType.Suppress);
                }
            }
        }

        public void Log(string message, LogType logType, LogSourceType logSourceType = LogSourceType.Local)
        {
            if (logType != LogType.Suppress)
            {
                try
                {
                    if (logSourceType == LogSourceType.Local)
                    {
                        this.CreateLogFileIfNecessary();

                        var logMessage = $"{DateTime.UtcNow.ToString()} {logType.ToString().ToUpperInvariant()}:\nMessage: {message}";

                        var currentFileContent = File.ReadAllText(this.FilePath);

                        var newFileContent = $"{logMessage}\n\n\n{currentFileContent}";

                        lock (_logWriteActionSyncLock)
                        {
                            using (var fileWriter = File.CreateText(FilePath))
                            {
                                fileWriter.WriteLine(newFileContent);
                            }
                        }
                    }
                    else if (logSourceType == LogSourceType.Remote)
                    {
                        // TODO: Impelement remote logging here (if necessary).
                    }
                }
                catch (Exception ex)
                {
                    this.Log(ex, LogType.Suppress);
                }
            }
        }

        public string GetTodayLogs()
        {
            string result = null;

            try
            {
                if (File.Exists(this.FilePath))
                {
                    result = File.ReadAllText(this.FilePath);
                }
            }
            catch (Exception ex)
            {
                this.Log(ex, LogType.Suppress);
            }

            if (result == string.Empty)
            {
                result = null;
            }

            return result;
        }

        private void CreateLogFileIfNecessary()
        {
            this.ResetFileIfSizeOverflows();

            this.ClearObsoleteLogsIfNecessary();

            if (!Directory.Exists(this.FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            if (!File.Exists(this.FilePath))
            {
                File.Create(this.FilePath);
            }
        }

        private void ResetFileIfSizeOverflows()
        {
            if (File.Exists(this.FilePath))
            {
                var fileInfo = new FileInfo(this.FilePath);
                if (fileInfo.Length > 1048576)
                {
                    this.ClearLogs();
                }
            }
        }

        private void ClearObsoleteLogsIfNecessary()
        {
            try
            {
                if (Directory.Exists(this.FolderPath))
                {
                    var logFilesDirectory = new DirectoryInfo(this.FolderPath);

                    logFilesDirectory.GetFiles()
                                     .Where(_ => _.CreationTimeUtc.Date < DateTime.UtcNow.Date)
                                     .AsParallel()
                                     .ForAll(_ => _.Delete());
                }
            }
            catch (Exception ex)
            {
                this.Log(ex, LogType.Suppress);
            }
        }
    }
}