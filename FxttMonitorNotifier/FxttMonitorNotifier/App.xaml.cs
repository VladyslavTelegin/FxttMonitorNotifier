using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace FxttMonitorNotifier
{
    using FxttMonitorNotifier.Droid.Services.Implementations.ForegroundServices;

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
            (MainPage as MainPage)?.UpdateUI(message);
        }

        protected override void OnStart()
        {
            PollingService.IsUiActivityVisible = true;
        }

        protected override void OnSleep()
        {
            PollingService.IsUiActivityVisible = false;
        }

        protected override void OnResume()
        {
            PollingService.IsUiActivityVisible = true;
        }
    }
}