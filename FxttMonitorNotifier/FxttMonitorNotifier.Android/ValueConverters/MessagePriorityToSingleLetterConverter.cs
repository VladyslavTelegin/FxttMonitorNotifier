namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using FxttMonitorNotifier.Droid.Enums;
    using System;
    using Xamarin.Forms;

    public sealed class MessagePriorityToSingleLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = string.Empty;

            if (value is int messagePriority)
            {
                var @enum = (NotificationPriority)messagePriority;

                value = @enum.ToString()?.ToUpperInvariant()?[0];
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}