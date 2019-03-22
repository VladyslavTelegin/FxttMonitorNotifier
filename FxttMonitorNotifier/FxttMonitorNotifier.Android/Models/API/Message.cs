namespace FxttMonitorNotifier.Droid.Models.Api
{
    using FxttMonitorNotifier.Droid.Enums;
    using FxttMonitorNotifier.Droid.Extensions;

    using System;

    public class Message : IEquatable<Message>
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string Type { get; set; }

        public string ServerInfo { get; set; }

        public DateTime CreatedOn { get; set; }

        public string State { get; set; }

        public int Priority { get; set; }

        public bool IsSilent { get; set; }

        public uint AcceptedUsersCount { get; set; }

        public bool Equals(Message other)
        {
            if (other == null) return false;

            return this.Id == other.Id &&
                   this.Text.Equals(other.Text) &&
                   this.Priority == other.Priority &&
                   this.ServerInfo.Equals(other.ServerInfo) &&
                   this.CreatedOn == other.CreatedOn &&
                   this.State.Equals(other.State) &&
                   this.AcceptedUsersCount == other.AcceptedUsersCount;
        }
    }

    public class MonitorApiMessageWrapper
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string ServerInfo { get; set; }

        public string CreatedOn { get; set; }

        public NotificationPriority Priority { get; set; }

        public NotificationState State { get; set; }

        public uint AcceptedUsersCount { get; set; }

        public Message MapToDomainMessage()
        {
            return new Message
            {
                Id = this.Id,
                Text = this.Text.UniformLifetimeHistory(),
                Type = GlobalConstants.FirebaseConstants.CallMessageType,
                CreatedOn = DateTime.ParseExact(this.CreatedOn, "dd.MM.yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture),
                Priority = (int)this.Priority,
                State = this.State.ToString(),
                ServerInfo = this.ServerInfo,
                AcceptedUsersCount = this.AcceptedUsersCount
            };
        }
    }
}