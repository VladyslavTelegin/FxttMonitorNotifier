namespace FxttMonitorNotifier.Droid.Models.Api
{
    using Newtonsoft.Json;

    using System;

    public class UpdateMessageStateReply : BaseApiResult
    {
        [JsonIgnore]
        public UpdateMessageStateReply Result => this;

        public static UpdateMessageStateReply Error(Exception exception) => Error(exception.Message);
        public static UpdateMessageStateReply Error(string errorMessage) => new UpdateMessageStateReply { IsSuccess = false, ErrorMessage = errorMessage };
    }
}