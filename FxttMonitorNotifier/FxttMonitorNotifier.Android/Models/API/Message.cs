namespace FxttMonitorNotifier.Droid.Models.Api
{
    using System;

    public class Message
    {
        public Guid Id { get; set; }

        public string Text { get; set; }

        public string Type { get; set; }

        public string ServerInfo { get; set; }

        public DateTime CreatedOn { get; set; }

        public string State { get; set; }

        public int Priority { get; set; }
    }
}