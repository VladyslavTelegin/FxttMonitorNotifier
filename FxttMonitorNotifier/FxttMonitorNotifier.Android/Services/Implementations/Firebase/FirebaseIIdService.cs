namespace FxttMonitorNotifier.Droid.Services.Implementations.Firebase
{
    using global::Firebase.Iid;

    using Android.App;
    using Android.Content;

    using FxttMonitorNotifier.Droid.Models;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;
    
    using System.Net;
    using System;

    using Newtonsoft.Json;

    using static System.Net.WebRequestMethods;

    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseIIdService : FirebaseInstanceIdService
    {
        #region Constants

        private const string ServiceTag = "fxttmonitornotifier_firebase_iid_service";

   

        #endregion

        #region ServiceProviders

        protected static IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

        #endregion

        #region PublicMethods

        public override void OnTokenRefresh()
        {
            var refreshedToken = FirebaseInstanceId.Instance.Token;

            SendRefreshedTokenToServer(refreshedToken);
        }

        #endregion

        #region PrivatMethods

        public static void SendRefreshedTokenToServer(string token)
        {
            try
            {
                var authService = ServiceProvider.AuthenticationService;

                var authCredenials = authService.CurrentUser;

                if (authService.IsAuthenticated)
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("AuthToken", authService.CurrentAuthToken);

                        var response = webClient.UploadString($"{GlobalConstants.FirebaseConstants.UpdateFirebaseTokenApiUrl}?token={token}", Http.Put, string.Empty);

                        var responseResult = JsonConvert.DeserializeObject<BaseApiResult>(response);
                        if (responseResult.IsSuccess)
                        {
                            if (!string.IsNullOrEmpty(token))
                            {
                                ServiceProvider.LoggingService.Log($"Firebase token successfully refreshed. New value = '{token}'.",
                                                               Enums.Logging.LogType.Info);
                            }
                        }
                        else
                        {
                            ServiceProvider.LoggingService.Log($"Cannot refresh Firebase token due to error: {responseResult.ErrorMessage}.",
                                                               Enums.Logging.LogType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceProvider.LoggingService.Log($"Cannot refresh Firebase token due to error: {ex.Message}.",
                                                   Enums.Logging.LogType.Error);
            }
        }

        #endregion
    }
}