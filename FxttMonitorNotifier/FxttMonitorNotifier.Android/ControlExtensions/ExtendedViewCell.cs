namespace FxttMonitorNotifier.Droid.ControlExtensions
{
    using FxttMonitorNotifier.Droid.Extensions;

    using Xamarin.Forms;

    public class ExtendedViewCell : ViewCell
    {
        public static readonly BindableProperty SelectedBackgroundColorProperty = BindableProperty.Create(nameof(SelectedBackgroundColor),
                                                                                                          typeof(string),
                                                                                                          typeof(ExtendedViewCell),
                                                                                                          Color.Default.GetHexString());
        public string SelectedBackgroundColor
        {
            get  { return this.GetValue(SelectedBackgroundColorProperty) as string; }
            set
            {
                SetValue(SelectedBackgroundColorProperty, value);
            }
        }
    }
}