namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using System;

    using Xamarin.Forms;

    public sealed class NullToBoolInversionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value != null;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}