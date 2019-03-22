namespace FxttMonitorNotifier.Droid
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Widget;

    using Firebase.Iid;

    using FxttMonitorNotifier.Droid.Broadcasting;
    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Services.Implementations.Firebase;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using Plugin.CurrentActivity;

    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Xamarin.Forms;

    using static Android.OS.PowerManager;
    using static Android.Provider.Settings;
    using static Droid.Models.GlobalConstants;

    #region Aliases

    using ApiModels = Models.Api;

    #endregion

    [Activity(MainLauncher = true,
              Icon = "@mipmap/icon",
              Label = "FXTT Monitor Notifier",
              Theme = "@style/MainTheme",
              ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]

    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        #region PrivateFields

        private static bool _isPreviousNetworkStateFailed = false;

        private UiNotificationBroadcastReceiver _uiNotificationBroadcastReceiver;

        private static readonly global::System.Timers.Timer _networkStatusTimer = new global::System.Timers.Timer { Interval = 2500 };

        private static WakeLock _wakeLock;

        private ILoggingService _loggingService;
        private IAuthenticationService _authenticationService;

        #endregion

        #region Constructor

        public MainActivity() : base() { }

        #endregion

        #region ServicesAndProviders

        public IAuthenticationService AuthenticationService =>
            _authenticationService ?? (_authenticationService = DependencyService.Get<IAuthenticationService>());

        public ILoggingService LoggingService => _loggingService ?? (_loggingService = DependencyService.Get<ILoggingService>());

        #endregion

        #region PublicProperties

        public static App CurrentUiApp { get; private set; }

        #endregion

        #region Methods

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
#if DEBUG
            var refreshedToken = FirebaseInstanceId.Instance.Token;
            global::System.Diagnostics.Debug.WriteLine($"Refreshed token: {refreshedToken}");
#endif
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

        #region Actions

        protected override void OnStart()
        {
            base.OnStart();
            IsStarted = false;
        }

        protected override void OnStop()
        {
            base.OnStop();
            IsStarted = false;
        }

        public static bool IsStarted { get; set; }

        protected override void OnResume()
        {
            base.OnResume();
 
            this.CancelAllAppNotifications();

            FirebaseNotificationsService.StopRingtone();
            FirebaseNotificationsService.TurnFlashlightNotificationsOff();

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

        #endregion

        #region UI

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

        #endregion

        private void CancelAllAppNotifications()
        {
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager.CancelAll();
        }

        #region NetworkStatus

        private void CheckNetworkStatus(object sender, EventArgs e)
        {
            _networkStatusTimer.Stop();

            if (!CheckInternetConnection())
            {
                _isPreviousNetworkStateFailed = true;
                FirebaseNotificationsService.IsNetworkAvailable = false;
                MessagingCenter.Send(CurrentUiApp.MainPage as MainPage, MessagingCenterConstants.ConnectionRefusedKey);
            }
            else
            {
                if (_isPreviousNetworkStateFailed)
                {
                    _isPreviousNetworkStateFailed = false;
                    FirebaseNotificationsService.IsNetworkAvailable = true;
                    MessagingCenter.Send(CurrentUiApp.MainPage as MainPage, MessagingCenterConstants.ConnectionResumedKey);
                }
            }

            _networkStatusTimer.Start();
        }

        public static bool CheckInternetConnection()
        {
            try
            {
                AwakeCpu();

                var pingRequest = (HttpWebRequest)WebRequest.Create(MainActivityConstants.NetworkPingUrl);

                pingRequest.Timeout = 10000;

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

        #region Optimizations

        private static void AwakeCpu()
        {
            try
            {
                var powerManager = (PowerManager)PowerService;
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, MainActivityConstants.WakeLockTag);
                _wakeLock.Acquire();
            }
            catch (Exception ex)
            {
                /* Ignored. */
            }
        }

        private void DisableBatteryOptimizationsForIntent(Intent intent)
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

        #endregion
    }
}  