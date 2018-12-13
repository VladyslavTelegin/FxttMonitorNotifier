namespace FxttMonitorNotifier.Droid
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Widget;

    using FxttMonitorNotifier.Droid.Broadcasting;
    using FxttMonitorNotifier.Droid.Enums.Logging;

    using FxttMonitorNotifier.Droid.Services.Implementations.ForegroundServices;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using Plugin.CurrentActivity;

    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Xamarin.Forms;

    using ApiModels = Models.Api;

    using static Droid.Models.GlobalConstants;
    using static Android.OS.PowerManager;
    using static Android.Provider.Settings;

    [Activity(MainLauncher = true,
              Icon = "@mipmap/icon",
              Label = "FXTT Monitor Notifier",
              Theme = "@style/MainTheme",
              ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]

    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private static bool _isPreviousNetworkStateFailed = false;

        private ILoggingService _loggingService;
        private IAuthenticationService _authenticationService;

        private UiNotificationBroadcastReceiver _uiNotificationBroadcastReceiver;

        private static readonly global::System.Timers.Timer _networkStatusTimer = new global::System.Timers.Timer { Interval = 5000 };

        private static WakeLock _wakeLock;

        public MainActivity() : base() { }

        #region ServicesAndProviders

        public IAuthenticationService AuthenticationService =>
            _authenticationService ?? (_authenticationService = DependencyService.Get<IAuthenticationService>());

        public ILoggingService LoggingService => _loggingService ?? (_loggingService = DependencyService.Get<ILoggingService>());

        #endregion

        public static App CurrentUiApp { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            if (int.Parse(Build.VERSION.Sdk) >= 26)
            {
                _uiNotificationBroadcastReceiver = new UiNotificationBroadcastReceiver();
            }

            this.InitializeGlobalExceptionHandlers();

            this.CancelAllAppNotifications();

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            CrossCurrentActivity.Current.Activity = this;

            _networkStatusTimer.Elapsed += this.CheckNetworkStatus;
            _networkStatusTimer.Start();

            global::Xamarin.Forms.Forms.Init(this, bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);

            if (this.Intent?.Extras?.GetBoolean(BroadcastingConstants.IsAfterBootCompleteExtraKey) == true)
            {
               this.LoggingService.Log(BroadcastingConstants.SucceessfulLogInfo, LogType.Info);
            }

            var serializedModel = this.Intent?.Extras?.GetString(MainActivityConstants.SerializedMessageModelExtraKey);
            if (string.IsNullOrEmpty(serializedModel))
            {
                this.LoadApplication(CurrentUiApp = new App());
            }
            else
            {
                var messageModel = JsonConvert.DeserializeObject<ApiModels.Message>(serializedModel);
                this.LoadApplication(CurrentUiApp = new App(messageModel));
            }
        }

        #region GlobalExceptionsHandling

        protected virtual void InitializeGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainOnException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        protected virtual void AppDomainOnException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e?.ExceptionObject is Exception exception)
            {
                this.HandleGlobalException(exception);
            }
        }

        protected virtual void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            this.HandleGlobalException(e.Exception);
        }

        private void HandleGlobalException(Exception ex) => this.LoggingService.Log(ex, LogType.Error);

        #endregion

        #region PollingServiceActions

        public void StartPollingService()
        {
            if (!IsForegroundServiceRunning() && this.CheckInternetConnection())
            {
                var serviceIntent = new Intent(this, typeof(PollingService));

                this.DisableBatteryOptimizationsForIntent(serviceIntent);

                base.StartService(serviceIntent);
                
                var notificationMessage = MainActivityConstants.DefaultServiceRunningToastMessage;

                this.LoggingService.Log(notificationMessage, LogType.Info);

                this.ShowToast(notificationMessage);
            }
        }

        public void StopPollingService()
        {
            if (IsForegroundServiceRunning())
            {
                base.StopService(new Intent(this, typeof(PollingService)));

                var notificationMessage = MainActivityConstants.DefaultServiceStoppedToastMessage;

                this.LoggingService.Log(notificationMessage, LogType.Info);

                this.ShowToast(notificationMessage);

                _wakeLock?.Release();
            }
        }

        private void DisableBatteryOptimizationsForIntent( Intent intent)
        {
            var packageName = this.ApplicationContext.PackageName;

            var powerManager = (PowerManager)this.ApplicationContext.GetSystemService(PowerService);
            if (powerManager.IsIgnoringBatteryOptimizations(packageName))
            {
                intent.SetAction(ActionIgnoreBatteryOptimizationSettings);
            }
            else
            {
                intent.SetAction(ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(Android.Net.Uri.Parse($"package:{packageName}"));
            }
        }

        #endregion

        protected override void OnResume()
        {
            base.OnResume();

            this.CancelAllAppNotifications();

            PollingService.StopRingtone();
            PollingService.TurnFlashlightNotificationsOff();
           
            if (int.Parse(Build.VERSION.Sdk) >= 26)
            {
                RegisterReceiver(_uiNotificationBroadcastReceiver, new IntentFilter(BroadcastingConstants.UiNotification));
            }
            
            CurrentUiApp.UpdateUi(null);
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (int.Parse(Build.VERSION.Sdk) >= 26)
            {
                UnregisterReceiver(_uiNotificationBroadcastReceiver);
            }
        }

        public async void ShowToast(string message)
        {
            try
            {
                var currentActivityContext = CrossCurrentActivity.Current.Activity;
                if (currentActivityContext != null && !currentActivityContext.IsFinishing)
                {
                    // Waiting for main context loading completion:
                    await Task.Delay(400);

                    Toast.MakeText(currentActivityContext, message, ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                this.LoggingService.Log(ex.Message, LogType.Warning);
            }
        }

        private void CancelAllAppNotifications()
        {
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager.CancelAll();
        }

        private bool IsForegroundServiceRunning()
        {
            var manager = (ActivityManager)this.GetSystemService(ActivityService);

#pragma warning disable CS0618
            var runningServices = manager.GetRunningServices(int.MaxValue).Select(_ => _.Service.ClassName)
                                                                          .ToList();
#pragma warning restore CS0618

            var isForegroundServiceRunning = runningServices.Any(_ => _.Contains(typeof(PollingService).Name));

            return isForegroundServiceRunning;
        }

        #region NetworkStatus

        private void CheckNetworkStatus(object sender, EventArgs e)
        {
            _networkStatusTimer.Stop();

            if (!this.CheckInternetConnection())
            {
                _isPreviousNetworkStateFailed = true;
                PollingService.IsNetworkAvailable = false;
                MessagingCenter.Send(CurrentUiApp.MainPage as MainPage, MessagingCenterConstants.ConnectionRefusedKey);
            }
            else
            {
                if (_isPreviousNetworkStateFailed)
                {
                    _isPreviousNetworkStateFailed = false;
                    PollingService.IsNetworkAvailable = true;
                    MessagingCenter.Send(CurrentUiApp.MainPage as MainPage, MessagingCenterConstants.ConnectionResumedKey);
                }
            }

            _networkStatusTimer.Start();
        }

        public bool CheckInternetConnection()
        {
            try
            {
                this.AwakeCpu();

                var pingRequest = (HttpWebRequest)WebRequest.Create(MainActivityConstants.NetworkPingUrl);

                pingRequest.Timeout = 5000;

                var pingResponse = pingRequest.GetResponse();

                pingResponse.Close();

                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        #endregion

        private void AwakeCpu()
        {
            try
            {
                var powerManager = (PowerManager)GetSystemService(PowerService);
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, MainActivityConstants.WakeLockTag);
                _wakeLock.Acquire();
            }
            catch (Exception ex)
            {
                this.LoggingService.Log(ex, LogType.Error);
            }
        }
    }
}  