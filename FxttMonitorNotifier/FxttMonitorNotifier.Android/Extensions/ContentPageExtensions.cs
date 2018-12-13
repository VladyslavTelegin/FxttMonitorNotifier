namespace FxttMonitorNotifier.Droid.Extensions
{
    using Xamarin.Forms;

    public static class ContentPageExtensions
    {
        public static T GetControl<T>(this ContentPage page, string controlName) where T : class => page.FindByName<T>(controlName);
    }
}