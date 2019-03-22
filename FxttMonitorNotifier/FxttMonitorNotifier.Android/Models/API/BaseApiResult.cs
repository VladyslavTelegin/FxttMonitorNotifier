namespace FxttMonitorNotifier.Droid.Models.Api
{
    public class BaseApiResult
    {
        public BaseApiResult() { }

        public BaseApiResult(bool isSuccess, string errorMessage = null)
        {
            this.IsSuccess = isSuccess;
            this.ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }
    }
}