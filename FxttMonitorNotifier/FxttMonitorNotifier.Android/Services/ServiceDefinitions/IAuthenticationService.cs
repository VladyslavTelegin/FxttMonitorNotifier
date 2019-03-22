using FxttMonitorNotifier.Droid.Models.Api;
using FxttMonitorNotifier.Droid.Services.Implementations;

using Xamarin.Forms;

[assembly: Dependency(typeof(AuthenticationService))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }

        AuthCredentials CurrentUser { get; }

        string CurrentAuthToken { get; }

        AuthResult LogIn(AuthCredentials authCredentials);

        void LogOut();
    }
}