namespace FxttMonitorNotifier.Droid.Models.Api
{
    public class AuthResult
    {
        public AuthResult(bool isSucceeded, string errorMessage = null)
        {
            this.IsSucceeded = isSucceeded;
            this.ErrorMessage = errorMessage;
        }

        public bool IsSucceeded { get; }

        public string ErrorMessage { get; }
    }
}