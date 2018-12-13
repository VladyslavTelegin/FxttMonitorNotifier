namespace FxttMonitorNotifier.Droid.Models.Api
{
    using Newtonsoft.Json;

    using System;

    public class UpdateMessageStateReply
    {
        #region SerializableProperties

        public bool IsSucceeded { get; private set; } = true;

        public string ErrorMessage { get; private set; }

        #endregion

        [JsonIgnore]
        public UpdateMessageStateReply Result => this;

        public static UpdateMessageStateReply Error(Exception exception) => Error(exception.Message);
        public static UpdateMessageStateReply Error(string errorMessage) => new UpdateMessageStateReply { IsSucceeded = false, ErrorMessage = errorMessage };
    }
}