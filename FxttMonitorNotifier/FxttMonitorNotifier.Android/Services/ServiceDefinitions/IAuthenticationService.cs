using FxttMonitorNotifier.Droid.Models.Api;
using FxttMonitorNotifier.Droid.Services.Implementations;

using System.Threading.Tasks;

using Xamarin.Forms;

[assembly: Dependency(typeof(AuthenticationService))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }

        Task<AuthResult> LogIn(AuthCredentials authCredentials);

        void LogOut();
    }
}