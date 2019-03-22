namespace FxttMonitorNotifier.Droid.Extensions
{
    public static class StringExtensions
    {
        public static string UniformLifetimeHistory(this string text)
        {
            var rawText = text;

            var rawTextHistoryStartIndex = rawText.IndexOf($"{System.Environment.NewLine}\t");
            if (rawTextHistoryStartIndex != -1)
            {
                rawText = rawText.Insert(rawTextHistoryStartIndex, $"\r\n\r\nLIFETIME HISTORY:");
            }

            return rawText;
        }
    }
}