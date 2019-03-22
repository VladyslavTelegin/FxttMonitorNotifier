namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using global::Firebase.Iid;

    using Android.App;
    using Android.Content;

    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Models;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Services.Implementations.Firebase;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using Plugin.CurrentActivity;

    using System;
    using System.Net;

    public class AuthenticationService :  IAuthenticationService
    {
        #region Constants

        private const string SharedPreferencesKey = "user_auth_data";

        #endregion

        #region ServiceProviders

        protected IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

        #endregion

        #region PublicProperties

        public AuthCredentials CurrentUser { get; private set; }

        public string CurrentAuthToken { get; private set; }

        #endregion

        #region CrossCurrent

        protected Activity Activity => CrossCurrentActivity.Current.Activity;

        protected Context AppContext => this.Activity.ApplicationContext;

        #endregion

        #region PublicMethods

        public bool IsAuthenticated
        {
            get
            {
                var result = false;

                try
                {
                    if (this.ServiceProvider.SharedPreferencesProvider.Exists(SharedPreferencesKey))
                    {
                        AuthToken deserializedAuthTokenModel = null;

                        try
                        {
                            deserializedAuthTokenModel = this.ServiceProvider.SharedPreferencesProvider.Get<AuthToken>(SharedPreferencesKey);
                            this.CurrentAuthToken = deserializedAuthTokenModel.Token;
                        }
                        catch (Exception ex)
                        {
                            this.ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
                        }

                        result = !(deserializedAuthTokenModel == null || string.IsNullOrEmpty(deserializedAuthTokenModel.Token));
                    }
                }
                catch (Exception ex)
                {
                    this.ServiceProvider.LoggingService.Log(ex, LogType.Warning);
                }

                return result;
            }
        }

        public AuthResult LogIn(AuthCredentials authCredentials)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    var response = webClient.UploadString($"{GlobalConstants.AuthenticationServiceConstants.ApiAuthUrl}?userName={authCredentials.UserName}&password={authCredentials.Password}", string.Empty);

                    var authModel = JsonConvert.DeserializeObject<AuthResult>(response);
                    if (authModel.IsSuccess)
                    {
                        this.ServiceProvider.SharedPreferencesProvider.Save(SharedPreferencesKey, new AuthToken(authModel.AuthToken));

                        this.CurrentUser = IsAuthenticated ? authCredentials : null;
                        this.CurrentAuthToken = authModel.AuthToken;

                        FirebaseIIdService.SendRefreshedTokenToServer(FirebaseInstanceId.Instance.Token);

                        return new AuthResult(IsAuthenticated);
                    }
                    else
                    {
                        this.CurrentUser = null;
                        this.CurrentAuthToken = null;

                        return new AuthResult(false);
                    }
                }
            }
            catch (Exception ex)
            {
                this.CurrentUser = null;
                this.CurrentAuthToken = null;

                this.ServiceProvider.LoggingService.Log(ex, LogType.Error);
            }
            
            return new AuthResult(false);
        }

        public void LogOut()
        {
            this.CurrentUser = null;
            this.CurrentAuthToken = null;

            if (this.ServiceProvider.SharedPreferencesProvider.Exists(SharedPreferencesKey))
            {
                this.ServiceProvider.SharedPreferencesProvider.Delete(SharedPreferencesKey);
            }

            this.ServiceProvider.SettingsProvider.DisableDnd();
        }

        #endregion
    }
}