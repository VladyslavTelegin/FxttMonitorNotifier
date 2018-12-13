namespace FxttMonitorNotifier.Droid.Services.Implementations.ForegroundServices
{
    using Android.App;
    using Android.Content;
    using Android.Media;
    using Android.OS;
    using Android.Runtime;
    using Android.Support.V4.App;

    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Extensions;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using Plugin.CurrentActivity;

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Xamarin.Essentials;
    using Xamarin.Forms;

    using ApiModels = Models.Api;

    using static Droid.Models.GlobalConstants;

    [Service]
    public class PollingService : Service
    {
        private Timer _serverPollingTimer;

        private static MediaPlayer _mediaPlayer;

        private static bool _isUiActivityVisible;
        private static bool _isNetworkAvailable = true;
        private static DateTime? _lastNetwokAvailableDate;

        private static readonly object _pollServerSyncLock = new object();
        private static readonly object _flashlightNotificationSyncLock = new object();

        private static readonly ConcurrentBag<ApiModels.Message> _cachedCallMessages = new ConcurrentBag<ApiModels.Message>();
        private static readonly ConcurrentBag<CancellationTokenSource> _flashlightCtsCollection = new ConcurrentBag<CancellationTokenSource>();

        private static readonly Random _identityRandomizer = new Random();

        private static ILoggingService _loggingService;
        private static ISettingsProvider _settingsProvider;

        private IMessagesApiProvider _messagesApiProvider;

        #region CrossCurrent

        protected Activity Activity => CrossCurrentActivity.Current.Activity;

        protected Context AppContext => this.Activity.ApplicationContext;

        #endregion

        #region ServicesAndProviders

        protected static ILoggingService LoggingService => _loggingService ?? (_loggingService = DependencyService.Get<ILoggingService>());

        protected static ISettingsProvider SettingsProvider => _settingsProvider ?? (_settingsProvider = DependencyService.Get<ISettingsProvider>());

        protected IMessagesApiProvider MessagesApiProvider => _messagesApiProvider ?? (_messagesApiProvider = DependencyService.Get<IMessagesApiProvider>());

        #endregion

        public static bool IsUiActivityVisible
        {
            get { return _isUiActivityVisible; }
            set
            {
                _isUiActivityVisible = value;

                if (value)
                {
                    CancelVibrate();
                }
            }
        }

        public static bool IsNetworkAvailable
        {
            get { return _isNetworkAvailable; }
            set
            {
                _isNetworkAvailable = value;
                if (_isNetworkAvailable)
                {
                    _lastNetwokAvailableDate = DateTime.UtcNow;
                }
            }
        }

        public static List<ApiModels.Message> CachedCallMessages => _cachedCallMessages.OrderByDescending(_ => _.CreatedOn).ToList();

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            base.OnStartCommand(intent, flags, startId);

            this.RunWithFaultHandler(() => this.ProcessColdStart());

            _serverPollingTimer = new Timer(callback => this.RunWithFaultHandler(() => this.PollServer()), null, 100, 3000);

            var notification = new Notification.Builder(this)

                .SetContentTitle(PollingServiceConstants.EnvironmentNotificationTitle)
                .SetContentText(PollingServiceConstants.ServiceStartedMessage)
                .SetSmallIcon(Resource.Mipmap.icon)
                .SetContentIntent(this.BuildIntentToShowMainActivity())
                .SetOngoing(true);

            var notificationChannel = this.CreateNotificationChannelIfNecessary(true);
            if (notificationChannel != null)
            {
                notification.SetChannelId(notificationChannel.Id);
            }

            this.StartForeground(PollingServiceConstants.ServiceRunningNotificationId, notification.Build());

            return StartCommandResult.Sticky;
        }

        private void RunWithFaultHandler(Action action)
        {
            try
            {
                var transientFaultHandler = DependencyService.Get<ITransientFaultHandler>();
                transientFaultHandler.Do(action, TimeSpan.FromSeconds(10));
            }
            catch (AggregateException aggregateException)
            {
                LoggingService.Log(aggregateException, LogType.Error);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            this._serverPollingTimer?.Dispose();

            _cachedCallMessages.Clear();

            this.CancellAllServiceNotifications();

            TurnFlashlightNotificationsOff();

            this.StopForeground(true);

            this.StopSelf();

            LoggingService.Log(PollingServiceConstants.ServiceDestroyedLogMessage, LogType.Info);
        }

        public override IBinder OnBind(Intent intent) => null;

        private void ProcessColdStart()
        {
            try
            {
                if (IsNetworkAvailable)
                {
                    _cachedCallMessages.Clear();

                    var allMessages = this.MessagesApiProvider.RetreiveAllMesages();
                    if (allMessages.Any())
                    {
                        _cachedCallMessages.Clear();

                        var callMessages = this.FilterCallMessages(allMessages);

                        callMessages.ToList()
                                    .ForEach(_ => _cachedCallMessages.Add(_));
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log(ex, LogType.Error);
                throw;
            }

            this.NotifyUi();
        }

        private void PollServer()
        {
            try
            {
                if (IsNetworkAvailable)
                {
                    lock (_pollServerSyncLock)
                    {
                        var specificMessages = this.MessagesApiProvider.RetreiveAllMesages();
                        if (specificMessages.Any())
                        {
                            this.ProcessMessages(specificMessages);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log(ex, LogType.Error);
                throw;
            }
        }

        private void ProcessMessages(IEnumerable<ApiModels.Message> messages)
        {
            var callMessages = this.FilterCallMessages(messages);

            var uniqueCallMessages = this.GetUniqueCallMessages(callMessages).ToList();

            /* Network-missed entries processing */
            if (_lastNetwokAvailableDate.HasValue)
            {
                var _networkMissedMessages = uniqueCallMessages.Where(_ => _.CreatedOn < _lastNetwokAvailableDate.Value)
                                                               .ToList();

                uniqueCallMessages = uniqueCallMessages.Except(_networkMissedMessages)
                                                       .ToList();
                if (_networkMissedMessages.Any())
                {
                    _networkMissedMessages.ForEach(_ => _cachedCallMessages.Add(_));
                    if (IsUiActivityVisible)
                    {
                        this.NotifyUi();
                    }
                }
            }

            /* Stable entries processing */
            if (uniqueCallMessages.Any())
            {
                var firstItem = uniqueCallMessages.First();

                if (_cachedCallMessages.FirstOrDefault(_ => firstItem.Id.Equals(_.Id)) == null)
                {
                    _cachedCallMessages.Add(firstItem);

                    if (!IsUiActivityVisible)
                    {
                        this.SendNotification(PollingServiceConstants.SingleCallReceivedFromMonitorMessage, uniqueCallMessages.First());
                    }
                    else
                    {
                        this.NotifyUi(firstItem);
                    }
                }
            }
        }

        private void NotifyUi(ApiModels.Message message = null)
        {
            var uiNotificationIntent = new Intent(BroadcastingConstants.UiNotification);

            if (message != null)
            {
                var serializedModel = JsonConvert.SerializeObject(message);

                uiNotificationIntent.PutExtra(BroadcastingConstants.UiNotificationKey, serializedModel);
            }

            SendBroadcast(uiNotificationIntent);
        }

        private IEnumerable<ApiModels.Message> FilterCallMessages(IEnumerable<ApiModels.Message> messages)
        {
            var callMessages = messages.Where(_ => _.Type.Equals(PollingServiceConstants.CallMessageType, StringComparison.OrdinalIgnoreCase) &&
                                                   DateTime.UtcNow.Date == _.CreatedOn.Date)
                                       .ToList();
            return callMessages;
        }

        private IEnumerable<ApiModels.Message> GetUniqueCallMessages(IEnumerable<ApiModels.Message> messages)
        {
            if (_cachedCallMessages.IsEmpty)
            {
                return messages;
            }

            return messages.ExceptUsingJsonComparer(_cachedCallMessages);
        }

        private void SendNotification(string monitorMessageDescription = "", ApiModels.Message message = null)
        {
            var id = _identityRandomizer.Next();

            var notification = new NotificationCompat.Builder(this)

                .SetSmallIcon(Resource.Mipmap.icon)
                .SetContentIntent(this.BuildIntentToShowMainActivity(id, message))
                .SetContentTitle(monitorMessageDescription)
                .SetAutoCancel(true)
                .SetContentText(PollingServiceConstants.TapForMoreDetails)
                .SetOnlyAlertOnce(false);

            if (SettingsProvider.IsDndModeEnabled)
            {
                notification.SetPriority((int)NotificationPriority.Low);
            }

            var notificationManager = this.GetSystemService(NotificationService) as NotificationManager;

            var notificationChannel = this.CreateNotificationChannelIfNecessary();
            if (notificationChannel != null)
            {
                notification.SetChannelId(notificationChannel.Id);
                notificationManager.CreateNotificationChannel(notificationChannel);
            }

            notificationManager.Notify(_identityRandomizer.Next(), notification.Build());

            this.RunVisualEffects();
        }

        private void RunVisualEffects()
        {
            ReleasePreviousFlashlights();

            var _flashLightCTS = new CancellationTokenSource();

            _flashlightCtsCollection.Add(_flashLightCTS);

            this.FireFlashlight(_flashLightCTS.Token);
            this.FireRingtone();

            if (!ScreenLock.IsActive)
            {
                ScreenLock.RequestActive();
            }
        }

        private void FireRingtone()
        {
            StopRingtone();
            Task.Run(() => this.PlayRingtone());
        }

        private void FireFlashlight(CancellationToken cancellationToken)
        {
            Task.Run(() => this.TurnFlashlightNotificationsOn(cancellationToken));
        }

        private async void PlayRingtone()
        {
            if (!SettingsProvider.IsDndModeEnabled)
            {
                var ringtone = RingtoneManager.GetActualDefaultRingtoneUri(this.AppContext, RingtoneType.Ringtone);

                _mediaPlayer = MediaPlayer.Create(this, ringtone);
                _mediaPlayer.Looping = true;
                _mediaPlayer.Start();

                await Task.Delay(30 * 1000).ContinueWith(_ => 
                {
                    StopRingtone(); 
                    TurnFlashlightNotificationsOff();
                });
            }
        }

        private NotificationChannel CreateNotificationChannelIfNecessary(bool isSystemChannel = false)
        {
            NotificationChannel notificationChannel = null;

            if (int.Parse(Build.VERSION.Sdk) >= 26)
            {
                var channelId = !SettingsProvider.IsDndModeEnabled ? PollingServiceConstants.CallsNotificationBasicChannelId 
                                                                   : PollingServiceConstants.CallsNotificationDNDChannelId;
                if (isSystemChannel)
                {
                    channelId = PollingServiceConstants.SystemNotificationChannelId;
                }

                var notificationImportance = (!SettingsProvider.IsDndModeEnabled) ? NotificationImportance.High : NotificationImportance.Low;

                notificationChannel = new NotificationChannel(channelId, channelId, notificationImportance);

                notificationChannel.SetBypassDnd(true);
                notificationChannel.LockscreenVisibility = NotificationVisibility.Public;
            }

            return notificationChannel;
        }

        private PendingIntent BuildIntentToShowMainActivity(int id = -1, ApiModels.Message message = null)
        {
            var notificationIntent = new Intent(this, typeof(MainActivity));

            notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);

            if (message != null)
            {
                var serializedMessage = JsonConvert.SerializeObject(message);
                notificationIntent.PutExtra(MainActivityConstants.SerializedMessageModelExtraKey, serializedMessage);
            }

            var pendingIntent = PendingIntent.GetActivity(this, id, notificationIntent, PendingIntentFlags.UpdateCurrent);

            return pendingIntent;
        }

        private void CancellAllServiceNotifications()
        {
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager.CancelAll();
        }

        private static void Vibrate(bool isWithFlashlight = false)
        {
            if (!SettingsProvider.IsDndModeEnabled)
            {
                Vibration.Vibrate(!isWithFlashlight ? 2000 : 1000);
            }
        }

        private static void CancelVibrate() => Vibration.Cancel();

        private static void ReleasePreviousFlashlights()
        {
            if (_flashlightCtsCollection.Any())
            {
                _flashlightCtsCollection.AsParallel()
                                        .ForAll(_ => _.Cancel());

                _flashlightCtsCollection.Clear();
            }
        }

        private async void TurnFlashlightNotificationsOn(CancellationToken cancellationToken)
        {
            if (!SettingsProvider.IsDndModeEnabled)
            {
                try
                {
                    try
                    {
                        CancelVibrate();
                        await Flashlight.TurnOffAsync();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log(ex, LogType.Suppress);
                    }

                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        Vibrate(true);

                        await Flashlight.TurnOnAsync();
                        await Task.Delay(1000);
                        await Flashlight.TurnOffAsync();
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Log(ex, LogType.Suppress);
                }
            }
        }

        public static void TurnFlashlightNotificationsOff()
        {
            try
            {
                _flashlightCtsCollection.AsParallel()
                                        .ForAll(_ => _.Cancel());
                CancelVibrate();
                Flashlight.TurnOffAsync();
            }
            catch (Exception ex)
            {
                LoggingService.Log(ex, LogType.Suppress);
            }
        }

        public static void StopRingtone()
        {
            try
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
            }
            catch (Exception ex)
            {
                LoggingService.Log(ex, LogType.Suppress);
            }
        }
    }
}