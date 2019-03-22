namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using System;

    using Xamarin.Forms;

    public sealed class MessageStateToDisplayStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var @string = value?.ToString();
            if (@string != null)
            {
                return @string.ToUpperInvariant();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}