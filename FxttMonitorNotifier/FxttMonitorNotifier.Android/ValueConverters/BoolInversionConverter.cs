namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using System;

    public sealed class BoolInversionConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => !(bool)value;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}