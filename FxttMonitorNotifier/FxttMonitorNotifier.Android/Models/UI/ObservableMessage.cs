namespace FxttMonitorNotifier.Droid.Models.Ui
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ObservableMessage : INotifyPropertyChanged
    {
        private bool _isVisible;

        public ObservableMessage(Api.Message apiMessage)
        {
            this.ApiMessage = apiMessage;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Api.Message ApiMessage { get; private set; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                this.RaisePropertyChanged();
            }
        }

        public void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}