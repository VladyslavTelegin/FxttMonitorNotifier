namespace FxttMonitorNotifier.StressTest
{
    using FxttMonitorNotifier.StressTest.ApiModels;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        #region Constants

        private const string CachingServerUrl = "http://demo3.genie-solution.com/api/messages";

        private const uint TestTasksCount = 200;

        #endregion

        #region PrivateFields

        private static readonly Random Randomizer = new Random();

        private static readonly string[] States = new string[] { "Accepted", "Rejected", "Pending" };

        private static IServiceProvider _serviceProvider;

        private static DateTime _startDateTime;

        private static int _completedTasksCount;

        #endregion

        #region EntryPoint

        private static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(config => config.AddConsole());

            try
            {
                _serviceProvider = serviceCollection.BuildServiceProvider();

                Logger.LogInformation($"Stress test service is running...");

                RunTest();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }

            Console.ReadKey();
        }

        #endregion

        #region Properties

        protected static ILogger<Program> Logger => _serviceProvider.GetService<ILogger<Program>>();

        #endregion

        #region PrivateMethods

        private static void RunTest()
        {
            _startDateTime = DateTime.UtcNow;

            var tasks = GetTasks();

            Task.WaitAll(tasks.ToArray());

            Logger.LogInformation($"Completed tasks count: {_completedTasksCount.ToString()}.\nEllapsed time: {(DateTime.UtcNow - _startDateTime).TotalSeconds} s.");
        }

        private static IEnumerable<Task> GetTasks()
        {
            for (int i = 0; i < TestTasksCount; i++)
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(Randomizer.Next(500, 5000));

                    var apiModel = new Message
                    {
                        Text = GetRandomString(),
                        ServerInfo = GetRandomString(),
                        Type = "Call",
                        State = States[Randomizer.Next(0, 3)],
                        Priority = Randomizer.Next(1, 4)
                    };

                    var serializedModel = JsonConvert.SerializeObject(apiModel);

                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                        webClient.UploadString(new Uri(CachingServerUrl), serializedModel);
                    }

                    Interlocked.Increment(ref _completedTasksCount);
                    Logger.LogInformation("Task runned.");
                });

                yield return task;
            }
        }

        private static string GetRandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#$%^&*?!";

            var length = Randomizer.Next(20, 257);

            return new string(Enumerable.Repeat(chars, length).Select(s => s[Randomizer.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}