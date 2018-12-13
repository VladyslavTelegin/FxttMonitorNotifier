namespace FxttMonitorNotifier.Droid.Broadcasting
{
    using Android.App;
    using Android.Content;

    using FxttMonitorNotifier.Droid.Models;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using System;

    using Xamarin.Forms;

    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { GlobalConstants.BroadcastingConstants.UiNotification })]
    public class UiNotificationBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var serializedMessage = intent?.Extras?.GetString(GlobalConstants.BroadcastingConstants.UiNotificationKey);

            Models.Api.Message message = null;

            if (!string.IsNullOrEmpty(GlobalConstants.BroadcastingConstants.UiNotification) && serializedMessage != null)
            {
                try
                {
                    message = JsonConvert.DeserializeObject<Models.Api.Message>(serializedMessage);
                }
                catch (Exception ex)
                {
                    DependencyService.Get<ILoggingService>().Log(ex, Enums.Logging.LogType.Error);
                }
            }

            MainActivity.CurrentUiApp.UpdateUi(message);
        }
    }
}