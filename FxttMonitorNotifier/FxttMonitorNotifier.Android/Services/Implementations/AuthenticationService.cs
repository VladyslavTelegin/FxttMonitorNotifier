namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Extensions;
    using FxttMonitorNotifier.Droid.Models;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class AuthenticationService : IAuthenticationService
    {
        private readonly Guid TestGuid = Guid.Parse("71f84a98-cdeb-41c4-ae60-6bf428f464b8");

        protected string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), GlobalConstants.AuthenticationServiceConstants.AuthStateFileName);

        public bool IsAuthenticated
        {
            get
            {
                var result = false;

                if (File.Exists(FilePath))
                {
                    var fileText = File.ReadAllText(FilePath);

                    if (!string.IsNullOrEmpty(fileText))
                    {
                        AuthToken deserializedAuthTokenModel = null;

                        try
                        {
                            deserializedAuthTokenModel = JsonConvert.DeserializeObject<AuthToken>(fileText.Base64Decode());
                        }
                        catch (Exception)
                        {
                            /* Ignored. */
                        }

                        result = deserializedAuthTokenModel != null && deserializedAuthTokenModel.Token != Guid.Empty;
                    }
                }

                return result;
            }
        }

        public async Task<AuthResult> LogIn(AuthCredentials authCredentials)
        {
            // TODO: Retreive AuthToken from server here.

            await this.SaveAuthStateToStorageAsync(TestGuid);

            return new AuthResult(IsAuthenticated);
        }

        public void LogOut()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        private async Task SaveAuthStateToStorageAsync(Guid token)
        {
            using (var fileWriter = File.CreateText(FilePath))
            {
                var authTokenModel = new AuthToken(token);

                var serializedTokenModel = JsonConvert.SerializeObject(authTokenModel);

                await fileWriter.WriteLineAsync(serializedTokenModel.Base64Encode());
            }
        }
    }
}