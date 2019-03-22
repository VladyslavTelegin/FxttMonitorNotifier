namespace FxttMonitorNotifier.Droid.Services.Implementations.Firebase
{
    using global::Firebase.Messaging;

    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Android.App;
    using Android.Content;
    using Android.Media;
    using Android.OS;
    using Android.Support.V4.App;

    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;
    
    using Newtonsoft.Json;

    using Xamarin.Essentials;
    using Xamarin.Forms;

    using static Droid.Models.GlobalConstants;

    #region Aliases

    using ApiModels = Models.Api;
    using FxttMonitorNotifier.Droid.Extensions;

    #endregion

    [Service(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class FirebaseNotificationsService : FirebaseMessagingService
    {
        #region Constants

        private const string ServiceTag = "fxttmonitornotifier_firebase_notifications_service";

        #endregion

        #region PrivateFields

        private static readonly ConcurrentBag<CancellationTokenSource> FlashlightCtsCollection = new ConcurrentBag<CancellationTokenSource>();

        private static readonly object FlashlightNotificationSyncLock = new object();
        private static readonly object CommonVisualEffectsLock = new object();

        private static MediaPlayer _mediaPlayer;

        private static readonly Random IdentityRandomizer = new Random();

        private static bool _isUiActivityVisible;
        private static bool _isNetworkAvailable = true;
        private static DateTime? _lastNetworkAvailableDate;
      
        public static volatile bool IsVisualEffectsRunning;

        #endregion

        #region PublicProperties

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
                    _lastNetworkAvailableDate = DateTime.UtcNow;
                }
            }
        }

        #endregion

        #region ServiceProviders

        protected static IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

        #endregion

        #region PublicMethods

        public override void OnCreate()
        {
            base.OnCreate();

            try
            {
                if (!Forms.IsInitialized)
                {
                    Forms.Init(this, null);
                }

                Platform.Init(this.Application);
            }
            catch (Exception ex)
            {
                ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
            }
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            var serializedModel = message.Data["MessageBody"];
            var messageApiModel = JsonConvert.DeserializeObject<ApiModels.Message>(serializedModel);

            if (messageApiModel.IsSilent)
            {
                messageApiModel.Text = messageApiModel.Text.UniformLifetimeHistory();

                NotifyUi(messageApiModel);

                return;
            }

            if (!IsUiActivityVisible || this.IsScreenLocked())
            {
                Device.BeginInvokeOnMainThread(
                    () => this.SendNotification(FirebaseConstants.SingleCallReceivedFromMonitorMessage, messageApiModel));
            }
            else
            {
                NotifyUi(messageApiModel);
            }
        }

        #endregion

        #region Notifications

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

        private void SendNotification(string monitorMessageDescription = "", ApiModels.Message message = null)
        {
            var id = IdentityRandomizer.Next();

            var notification = new NotificationCompat.Builder(this)

                .SetSmallIcon(Resource.Mipmap.icon)
                .SetContentIntent(this.BuildIntentToShowMainActivity(id, message))
                .SetContentTitle(monitorMessageDescription)
                .SetAutoCancel(true)
                .SetContentText(FirebaseConstants.TapForMoreDetails)
                .SetOnlyAlertOnce(false);

            var notificationPriority = ServiceProvider.SettingsProvider.IsDndModeEnabled ? (int)NotificationPriority.Low :
                                                                                           (int)NotificationPriority.High;
            notification.SetPriority(notificationPriority);

            var notificationManager = this.GetSystemService(NotificationService) as NotificationManager;

            var notificationChannel = this.CreateNotificationChannelIfNecessary();
            if (notificationChannel != null)
            {
                notification.SetChannelId(notificationChannel.Id);
                notificationManager.CreateNotificationChannel(notificationChannel);
            }

            notificationManager.Notify(IdentityRandomizer.Next(), notification.Build());

            if (message != null && 
                message.Priority == (int)Enums.NotificationPriority.High &&
                !this.IsCallActive())
            {
                this.RunVisualEffects();
            }
        }

        private NotificationChannel CreateNotificationChannelIfNecessary(bool isSystemChannel = false)
        {
            NotificationChannel notificationChannel = null;

            var sdkVersion = int.Parse(Build.VERSION.Sdk);
            if (sdkVersion >= 26)
            {
                var channelId = !ServiceProvider.SettingsProvider.IsDndModeEnabled ? FirebaseConstants.CallsNotificationBasicChannelId
                                                                                   : FirebaseConstants.CallsNotificationDndChannelId;
                if (isSystemChannel)
                {
                    channelId = FirebaseConstants.SystemNotificationChannelId;
                }

                var notificationImportance = NotificationImportance.High;

                notificationChannel = new NotificationChannel(channelId, channelId, notificationImportance);

                notificationChannel.SetBypassDnd(true);
                notificationChannel.LockscreenVisibility = NotificationVisibility.Public;

                if (sdkVersion >= 27)
                {
                    var notificationService = GetSystemService(NotificationService) as NotificationManager;
                    notificationService.CreateNotificationChannel(notificationChannel);
                }
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

        #endregion

        #region VisualEffectsControlling

        private void RunVisualEffects()
        {
            try
            {
                Monitor.Enter(CommonVisualEffectsLock);

                if (!IsVisualEffectsRunning)
                {
                    IsVisualEffectsRunning = true;

                    ReleasePreviousFlashlights();

                    var _flashLightCTS = new CancellationTokenSource();

                    FlashlightCtsCollection.Add(_flashLightCTS);

                    this.FireFlashlight(_flashLightCTS.Token);
                    this.FireRingtone();
                }
            }
            catch (Exception ex)
            {
                ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
            }
            finally
            {
                Monitor.Exit(CommonVisualEffectsLock);
            }
        }

        #region VibrationControlling

        private static void Vibrate(bool isWithFlashlight = false)
        {
            if (!ServiceProvider.SettingsProvider.IsDndModeEnabled)
            {
                Vibration.Vibrate(!isWithFlashlight ? 2000 : 1000);
            }
        }

        private static void CancelVibrate() => Vibration.Cancel();

        #endregion

        #region Ringtone

        private void FireRingtone()
        {
            StopRingtone();
            Task.Run(() => this.PlayRingtone());
        }

        private async void PlayRingtone()
        {
            if (!ServiceProvider.SettingsProvider.IsDndModeEnabled)
            {
                try
                {
                    var ringtone = RingtoneManager.GetActualDefaultRingtoneUri(this, RingtoneType.Ringtone);

                    _mediaPlayer = new MediaPlayer();

                    _mediaPlayer.SetDataSource(this, ringtone);

                    if (int.Parse(Build.VERSION.Sdk) >= 21)
                    {
                        var customAudioAttributes = new AudioAttributes.Builder()

                                                .SetUsage(AudioUsageKind.NotificationRingtone)
                                                .SetContentType(AudioContentType.Sonification)
                                                .Build();

                        _mediaPlayer.SetAudioAttributes(customAudioAttributes);
                    }
                    else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        _mediaPlayer.SetAudioStreamType(Stream.Ring);
#pragma warning restore CS0618 // Type or member is obsolete
                    }

                    _mediaPlayer.Looping = true;

                    _mediaPlayer.Prepare();
                    _mediaPlayer.Start();

                    await Task.Delay(30 * 1000).ContinueWith(_ =>
                    {
                        StopRingtone();
                        TurnFlashlightNotificationsOff();
                    });
                }
                catch (Exception ex)
                {
                    ServiceProvider.LoggingService.Log(ex, LogType.Error);
                }
            }
        }

        public static void StopRingtone()
        {
            try
            {
                IsVisualEffectsRunning = false;

                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
            }
            catch (Exception ex)
            {
                ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
            }
        }

        #endregion

        #region Flashlight

        private void FireFlashlight(CancellationToken cancellationToken)
        {
            Task.Run(() => this.TurnFlashlightNotificationsOn(cancellationToken));
        }

        private static void ReleasePreviousFlashlights()
        {
            if (FlashlightCtsCollection.Any())
            {
                FlashlightCtsCollection.AsParallel()
                                        .ForAll(_ => _.Cancel());

                FlashlightCtsCollection.Clear();
            }
        }

        private async void TurnFlashlightNotificationsOn(CancellationToken cancellationToken)
        {
            if (!ServiceProvider.SettingsProvider.IsDndModeEnabled)
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
                        ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
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
                    ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
                }
            }
        }

        public static void TurnFlashlightNotificationsOff()
        {
            try
            {
                IsVisualEffectsRunning = false;

                FlashlightCtsCollection.AsParallel()
                                        .ForAll(_ => _.Cancel());
                CancelVibrate();
                Flashlight.TurnOffAsync();
            }
            catch (Exception ex)
            {
                ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
            }
        }

        #endregion

        private bool IsScreenLocked()
        {
            var keyManager = (KeyguardManager)base.ApplicationContext.GetSystemService(KeyguardService);
            return keyManager.InKeyguardRestrictedInputMode();
        }

        public bool IsCallActive()
        {
            var audioManager = (AudioManager)base.ApplicationContext.GetSystemService(AudioService);
            return audioManager.Mode == Mode.InCall;
        }

        #endregion
    }
}