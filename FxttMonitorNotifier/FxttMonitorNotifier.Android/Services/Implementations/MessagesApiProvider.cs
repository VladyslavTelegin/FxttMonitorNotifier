namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Enums;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using static FxttMonitorNotifier.Droid.Models.GlobalConstants;
    using static System.Net.WebRequestMethods;

    public class MessagesApiProvider : IMessagesApiProvider
    {
        public IEnumerable<Message> RetreiveAllMesages() => this.PollServerByMode(PollServerMode.All);
        public IEnumerable<Message> RetreiveSpecificMessages() => this.PollServerByMode(PollServerMode.Specific);

        public Task<UpdateMessageStateReply> AcceptMessageAsync(Message message) => Task.Run(() => this.UpdateMessageState(message, MessageState.Accepted));
        public Task<UpdateMessageStateReply> RejectMessageAsync(Message message) => Task.Run(() => this.UpdateMessageState(message, MessageState.Rejected));

        private IEnumerable<Message> PollServerByMode(PollServerMode mode)
        {
            var pollingUrl = (mode == PollServerMode.Specific) ? PollingServiceConstants.PollingSpecificUrl
                                                               : PollingServiceConstants.PollingAllUrl;
            using (var webClient = new WebClient())
            {
                var responseContent = webClient.DownloadString(pollingUrl);

                if (!string.IsNullOrEmpty(responseContent))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<Message>>(responseContent);
                }
            }

            return new List<Message>();
        }

        private UpdateMessageStateReply UpdateMessageState(Message message, MessageState newState)
        {
            try
            {
                message.State = newState.ToString();

                var serializedMessageModel = JsonConvert.SerializeObject(message);

                var actionUrl = string.Empty;

                if (newState == MessageState.Accepted)
                {
                    actionUrl = PollingServiceConstants.AcceptSpecificUrl;
                }
                else if (newState == MessageState.Rejected)
                {
                    actionUrl = PollingServiceConstants.RejectSpecificUrl;
                }

                if (!string.IsNullOrEmpty(actionUrl))
                {
                    using (var webClient = new WebClient())
                    {
                        var replyResultContent = webClient.UploadString(actionUrl, Http.Put, serializedMessageModel);

                        if (!string.IsNullOrEmpty(replyResultContent))
                        {
                            var replyResultModel = JsonConvert.DeserializeObject<UpdateMessageStateReply>(replyResultContent);
                            if (replyResultModel != null)
                            {
                                return replyResultModel;
                            }
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"{nameof(this.UpdateMessageState)}: {ExceptionConstants.ActionUrlIsUndefinedStringSlice}");
                }
            }
            catch (Exception ex)
            {
                return UpdateMessageStateReply.Error(ex);
            }

            return UpdateMessageStateReply.Error(ExceptionConstants.UnspecifiedErrorOccurred);
        }
    }
}