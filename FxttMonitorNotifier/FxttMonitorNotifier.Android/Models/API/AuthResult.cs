namespace FxttMonitorNotifier.Droid.Models.Api
{
    public class AuthResult : BaseApiResult
    {
        public AuthResult() : base() { }

        public AuthResult(bool isSuccess, 
                          string errorMessage = null, 
                          string authToken = null) : base(isSuccess, errorMessage)
        {
            this.AuthToken = authToken;
        }

        public string AuthToken { get; set; }
    }
}