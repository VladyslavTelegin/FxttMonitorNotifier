namespace FxttMonitorNotifier.Droid.ValueConverters
{
    using System;

    using Xamarin.Forms;

    public sealed class MessagePriorityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int messagePriority)
            {
                Color? color = null;

                switch (messagePriority)
                {
                    case 1:
                        {
                            color = Color.Black;
                        }
                        break;
                    case 2:
                        {
                            color = Color.Yellow;
                        }
                        break;
                    case 3:
                        {
                            color = Color.DarkRed;
                        }
                        break;
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