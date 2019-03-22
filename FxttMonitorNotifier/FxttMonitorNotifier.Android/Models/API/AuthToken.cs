namespace FxttMonitorNotifier.Droid.Models.Api
{
    public class AuthToken
    {
        public AuthToken() { }

        public AuthToken(string token)
        {
            this.Token = token;
        }

        public string Token { get; set; }
    }
}