using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace FxttMonitorNotifier
{
    using FxttMonitorNotifier.Droid.Services.Implementations.Firebase;

    using Xamarin.Forms;

    public partial class App : Application
    {
        public App(Droid.Models.Api.Message messageModel = null)
        {
            InitializeComponent();

            MainPage = new MainPage(messageModel);
        }

        public void UpdateUi(Droid.Models.Api.Message message)
        {
            (MainPage as MainPage)?.UpdateUi(message);
        }

        protected override void OnStart()
        {
            FirebaseNotificationsService.IsUiActivityVisible = true;
        }

        protected override void OnSleep()
        {
            FirebaseNotificationsService.IsUiActivityVisible = false;
        }

        protected override void OnResume()
        {
            FirebaseNotificationsService.IsUiActivityVisible = true;
        }
    }
}