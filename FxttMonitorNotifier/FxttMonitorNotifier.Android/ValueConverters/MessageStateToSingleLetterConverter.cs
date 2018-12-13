namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using System;

    using Xamarin.Forms;

    public sealed class MessageStateToSingleLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value?.ToString()?[0];

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}