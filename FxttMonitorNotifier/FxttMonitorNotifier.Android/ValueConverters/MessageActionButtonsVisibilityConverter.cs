namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using System;
    using Xamarin.Forms;

    public class MessageActionButtonsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value != null && MainActivity.CheckInternetConnection();

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}