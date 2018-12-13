namespace FxttMonitorNotifier.Droid.Broadcasting
{
    using Android.App;
    using Android.Content;

    using FxttMonitorNotifier.Droid.Models;

    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootCompleteBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var activityIntent = new Intent(context, typeof(MainActivity));
            activityIntent.PutExtra(GlobalConstants.BroadcastingConstants.IsAfterBootCompleteExtraKey, true);

            context.StartActivity(activityIntent);
        }
    }
}