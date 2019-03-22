namespace FxttMonitorNotifier.StressTest.ApiModels
{
    internal class Message
    {
        public string Text { get; set; }

        public string Type { get; set; }

        public string ServerInfo { get; set; }

        public string State { get; set; }

        public int Priority { get; set; }
    }
}