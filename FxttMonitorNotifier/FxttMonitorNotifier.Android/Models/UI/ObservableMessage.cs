namespace FxttMonitorNotifier.Droid.Models.Ui
{
    using FxttMonitorNotifier.Droid.Enums;

    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ObservableMessage : INotifyPropertyChanged
    {
        private bool _isVisible;

        private Api.Message _apiMessage;

        public ObservableMessage(Api.Message apiMessage)
        {
            this.ApiMessage = apiMessage;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Api.Message ApiMessage
        {
            get { return _apiMessage; }
            set
            {
                _apiMessage = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                this.RaisePropertyChanged();
            }
        }

        public string AcceptedUsersCount
        {
            get
            {
                if (this.ApiMessage.State.Equals(NotificationState.Accepted.ToString()))
                {
                    return this.ApiMessage.AcceptedUsersCount.ToString();
                }

                return string.Empty;
            }
            set
            {
                this.ApiMessage.AcceptedUsersCount = uint.Parse(value);
                this.RaisePropertyChanged();
            }
        }

        public void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}