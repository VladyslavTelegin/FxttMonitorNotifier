namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using FxttMonitorNotifier.Droid.Enums;

    using System;

    using Xamarin.Forms;

    public sealed class MessageStateToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string messageState)
            {
                Color? color = null;

                if (messageState.Equals(NotificationState.Pending.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    color = Color.FromRgb(0, 114, 198);
                }
                else if (messageState.Equals(NotificationState.Accepted.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    color = Color.FromRgb(0, 142, 33);
                }
                else if (messageState.Equals(NotificationState.Rejected.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    color = Color.FromRgb(142, 142, 142);
                }

                if (color.HasValue)
                {
                    return color;
                }
            }

            return default(System.Drawing.Color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}