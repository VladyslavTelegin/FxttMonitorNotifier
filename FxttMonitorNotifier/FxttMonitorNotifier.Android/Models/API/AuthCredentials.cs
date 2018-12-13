namespace FxttMonitorNotifier.Droid.Models.Api
{
    public class AuthCredentials
    {
        public AuthCredentials(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        public string UserName { get; }

        public string Password { get; }
    }
}