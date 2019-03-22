namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Enums;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Models.API;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using static FxttMonitorNotifier.Droid.Models.GlobalConstants;
    using static System.Net.WebRequestMethods;

    public class MessagesApiProvider : IMessagesApiProvider
    {
        #region Constants

        public const string MessagesCacheKey = "cached_messages";

        #endregion

        #region ServiceProviders

        protected IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

        #endregion

        public IEnumerable<Message> RetreiveAllMesages() => this.PollServerByMode(PollServerMode.All);
        public IEnumerable<Message> RetreiveSpecificMessages() => this.PollServerByMode(PollServerMode.Specific);

        public Task<UpdateMessageStateReply> AcceptMessageAsync(Message message) => Task.Run(() => this.UpdateMessageState(message.Id, NotificationState.Accepted));
        public Task<UpdateMessageStateReply> RejectMessageAsync(Message message) => Task.Run(() => this.UpdateMessageState(message.Id, NotificationState.Rejected));

        private IEnumerable<Message> PollServerByMode(PollServerMode mode)
        {
            if (this.ServiceProvider.AuthenticationService.IsAuthenticated)
            {
                if (MainActivity.CheckInternetConnection())
                {
                    var pollingUrl = (mode == PollServerMode.Specific) ? FirebaseConstants.PollingSpecificUrl
                                                                       : FirebaseConstants.PollingAllUrl;
                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("AuthToken", this.ServiceProvider.AuthenticationService.CurrentAuthToken);

                        var responseContent = webClient.DownloadString(pollingUrl);

                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            var filteredMessages = JsonConvert.DeserializeObject<IEnumerable<MonitorApiMessageWrapper>>(responseContent)
                               .Select(_ => _.MapToDomainMessage())
                               .OrderByDescending(_ => _.CreatedOn)
                               .ToList();

                            if (mode == PollServerMode.Specific)
                            {
                                Task.Run(() => this.ServiceProvider.PermanentCacheService.Set(MessagesCacheKey, filteredMessages));
                            }

                            return filteredMessages;
                        }
                    }
                }
                else if (mode == PollServerMode.Specific)
                {
                    if (this.ServiceProvider.PermanentCacheService.IsSet(MessagesCacheKey))
                    {
                        return this.ServiceProvider.PermanentCacheService.Get<List<Message>>(MessagesCacheKey);
                    }
                }
            }

            return new List<Message>();
        }

        private UpdateMessageStateReply UpdateMessageState(int messageId, NotificationState newState)
        {
            try
            {
                if (this.ServiceProvider.AuthenticationService.IsAuthenticated)
                {
                    var apiModel = new UpdateMessageStateModel
                    {
                        Id = messageId,
                        State = newState
                    };

                    var serializedMessageModel = JsonConvert.SerializeObject(apiModel);

                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("AuthToken", this.ServiceProvider.AuthenticationService.CurrentAuthToken);
                        webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                        var replyResultContent = webClient.UploadString(FirebaseConstants.UpdateMessageStateApiUrl, Http.Put, serializedMessageModel);

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
            }
            catch (Exception ex)
            {
                return UpdateMessageStateReply.Error(ex);
            }

            return UpdateMessageStateReply.Error(ExceptionConstants.UnspecifiedErrorOccurred);
        }
    }
}