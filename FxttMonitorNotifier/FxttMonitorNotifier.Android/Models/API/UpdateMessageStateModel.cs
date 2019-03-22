namespace FxttMonitorNotifier.Droid.Models.API
{
    using FxttMonitorNotifier.Droid.Enums;

    public class UpdateMessageStateModel
    {
        public int Id { get; set; }

        public NotificationState State { get; set; }
    }
}