namespace FxttMonitorNotifier
{
    using Android.App;
    using Android.Content;

    using FxttMonitorNotifier.Droid;
    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Extensions;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Models.Ui;
    using FxttMonitorNotifier.Droid.Services.Implementations.ForegroundServices;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Plugin.CurrentActivity;

    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Xamarin.Essentials;
    using Xamarin.Forms;

    using static Droid.Models.GlobalConstants;

    public partial class MainPage : ContentPage
    {
        private static readonly object _loginSyncLock = new object();

        private string _busyText;

        private long _totalMessagesCount;

        private bool _isActionBusy;
        private bool _isMessagesGridVisible;
        private bool _isLoginGridVisible;
        private bool _isListViewRefreshing;

        private Command _listViewRefreshCommand;

        private Command _acceptMessageCommand;
        private Command _rejectMessageCommand;

        private ObservableMessage _selectedMessage;

        private ILoggingService _loggingService;
        private ISettingsProvider _settingsProvider;
        private IMessagesApiProvider _messagesApiProvider;

        public MainPage(Message message = null)
        {
            this.InitializeComponent();

            this.InitializeBindingContext();
            this.InitializeDndSwitch();
            this.InitializeMessageGrid(message);

            this.Appearing += (sender, args) => 
            {
                PollingService.IsUiActivityVisible = true;
                this.InitializeNetworkStatusHandlers();
            };

            this.Disappearing += (sender, args) =>
            {
                PollingService.IsUiActivityVisible = false;

                MessagingCenter.Unsubscribe<MainPage>(this, MessagingCenterConstants.ConnectionRefusedKey);
                MessagingCenter.Unsubscribe<MainPage>(this, MessagingCenterConstants.ConnectionResumedKey);
            };

            this.InitializeGestureRecognizers();
        }

        #region CrossCurrent

        protected Activity Activity => CrossCurrentActivity.Current.Activity;

        protected Context AppContext => this.Activity.ApplicationContext;

        protected MainActivity MainActivity => this.Activity as MainActivity;

        #endregion

        #region ServicesAndProviders

        protected ILoggingService LoggingService => _loggingService ?? (_loggingService = DependencyService.Get<ILoggingService>());

        protected ISettingsProvider SettingsProvider => (_settingsProvider ?? (_settingsProvider = DependencyService.Get<ISettingsProvider>()));

        protected IMessagesApiProvider MessagesApiProvider => (_messagesApiProvider ?? (_messagesApiProvider = DependencyService.Get<IMessagesApiProvider>()));

        #endregion

        #region BindableProperties

        public ObservableCollection<ObservableMessage> Messages { get; } = new ObservableCollection<ObservableMessage>();

        public bool IsActionBusy
        {
            get { return _isActionBusy; }
            set
            {
                _isActionBusy = value;
                this.OnPropertyChanged(nameof(this.IsActionBusy));
            }
        }

        public string BusyText
        {
            get { return _busyText; }
            set
            {
                _busyText = value;
                this.OnPropertyChanged(nameof(this.BusyText));
            }
        }

        public bool IsMessagesGridVisible
        {
            get { return _isMessagesGridVisible; }
            set
            {
                _isMessagesGridVisible = value;
                this.OnPropertyChanged(nameof(this.IsMessagesGridVisible));
            }
        }

        public bool IsLoginGridVisible
        {
            get { return _isLoginGridVisible; }
            set
            {
                _isLoginGridVisible = value;
                this.OnPropertyChanged(nameof(this.IsLoginGridVisible));
            }
        }

        public long TotalMessagesCount
        {
            get { return _totalMessagesCount; }
            set
            {
                _totalMessagesCount = value;
                this.OnPropertyChanged(nameof(this.TotalMessagesCount));
            }
        }

        public bool IsListViewRefreshing
        {
            get { return _isListViewRefreshing; }
            set
            {
                _isListViewRefreshing = value;
                this.OnPropertyChanged(nameof(this.IsListViewRefreshing));
            }
        }

        public string TotalMessagesCountString => $"({this.TotalMessagesCount})";

        public ObservableMessage SelectedMessage
        {
            get { return _selectedMessage; }
            set
            {
                _selectedMessage = value;
                this.OnPropertyChanged(nameof(this.SelectedMessage));
            }
        }

        public Command ListViewRefreshCommand => _listViewRefreshCommand ?? (_listViewRefreshCommand = new Command(RefreshListView));

        public Command AcceptMessageCommand => _acceptMessageCommand ?? (_acceptMessageCommand = new Command(AcceptSelectedMessage));
        public Command RejectMessageCommand => _rejectMessageCommand ?? (_rejectMessageCommand = new Command(RejectSelectedMessage));

        #endregion

        public void UpdateUI(Message message)
        {
            if (!ScreenLock.IsActive)
            {
                ScreenLock.RequestActive();
            }

            this.ShowMessagesGrid(message, false);

            if (message != null)
            {
                this.Vibrate();
            }
        }

        private void InitializeMessageGrid(Message message)
        {
            if (!this.MainActivity.AuthenticationService.IsAuthenticated)
            {
                this.IsLoginGridVisible = true;
            }
            else
            {
                this.MainActivity?.StartPollingService();

                this.ShowMessagesGrid(message);
            }
        }

        private void RefreshListView()
        {
            this.IsListViewRefreshing = true;
            this.ShowMessagesGrid(null, false);
            this.IsListViewRefreshing = false;
        }

        private void InitializeGestureRecognizers()
        {
            #region OptionsGrid

            var oneTapGestureRecognizer = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 1,
                Command = new Command(() =>
                {
                    this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
                    this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = true;
                })
            };

            var optionsPopupGridOverlay = this.GetControl<Grid>(ControlNames.OptionsPopupGridOverlay);
            optionsPopupGridOverlay.GestureRecognizers.Add(oneTapGestureRecognizer);

            #endregion

            #region LogsGrid

            var twoTapsGestureRecogniser = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 2,
                Command = new Command(() =>
                {
                    this.GetControl<Grid>(ControlNames.LogsPopupGrid).IsVisible = false;
                    this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = true;
                })
            };

            var logsPopupLabel = this.GetControl<Label>(ControlNames.LogsTextLabel);
            logsPopupLabel.GestureRecognizers.Add(twoTapsGestureRecogniser);

            #endregion
        }

        private void Vibrate(bool isFromDndToggle = false)
        {
            if (!this.SettingsProvider.IsDndModeEnabled)
            {
                Vibration.Vibrate(250);
            }

            if (isFromDndToggle)
            {
                Vibration.Vibrate(750);
            }
        }

        private async void ShowMessagesGrid(Message message = null, bool showCustomPreloader = true)
        {
            if (showCustomPreloader)
            {
                this.IsMessagesGridVisible = false;
                this.IsLoginGridVisible = false;
                this.IsActionBusy = true;
                this.BusyText = BusyTexts.LoadingItems;

                await Task.Delay(500);
            }

            await UpdateMessagesCollectionAsync();

            this.TotalMessagesCount = this.Messages.Count;

            this.OnPropertyChanged(nameof(this.TotalMessagesCountString));

            if (showCustomPreloader)
            {
                this.IsActionBusy = false;
                this.IsMessagesGridVisible = true;
            }

            if (message != null)
            {
                var entryToSelect = this.Messages.FirstOrDefault(_ => _.ApiMessage.Id.Equals(message.Id));
                if (entryToSelect != null)
                {
                    entryToSelect.IsVisible = true;

                    await Task.Delay(200);

                    this.GetControl<ListView>(ControlNames.MessagesListView).SelectedItem = entryToSelect;

                    this.SelectedMessage = entryToSelect;
                }
            }
            else
            {
                var latestMessage = this.Messages.FirstOrDefault();
                if (latestMessage != null)
                {
                    latestMessage.IsVisible = true;

                    await Task.Delay(200);

                    this.GetControl<ListView>(ControlNames.MessagesListView).SelectedItem = latestMessage;

                    this.SelectedMessage = latestMessage;
                }
            }
        }

        private Task UpdateMessagesCollectionAsync() => Task.Run(() => this.UpdateMessagesCollection());

        private void UpdateMessagesCollection()
        {
            this.Messages.Clear();

            PollingService.CachedCallMessages.ForEach(_ => this.Messages.Add(new ObservableMessage(_)));
        }

        private void InitializeDndSwitch()
        {
            var switchControl = this.GetControl<Switch>(ControlNames.DndSwitch);

            switchControl.Toggled -= this.OnDndSwitchToggle;
            switchControl.IsToggled = this.SettingsProvider.IsDndModeEnabled;
            switchControl.Toggled += this.OnDndSwitchToggle;
        }

        private void TurnOffDndSwitch()
        {
            var switchControl = this.GetControl<Switch>(ControlNames.DndSwitch);

            switchControl.Toggled -= this.OnDndSwitchToggle;
            switchControl.IsToggled = false;
            switchControl.Toggled += this.OnDndSwitchToggle;
        }

        #region EventHandlers

        private void OnMessagesListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is ObservableMessage currentItem)
            {
                foreach (var message in this.Messages)
                {
                    if (message != currentItem)
                    {
                        message.IsVisible = false;
                    }
                }

                currentItem.IsVisible ^= true;

                if (!currentItem.IsVisible)
                {
                    this.GetControl<ListView>(ControlNames.MessagesListView).SelectedItem = null;
                }
            }
        }

        private void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            this.MainActivity.AuthenticationService.LogOut();

            this.IsMessagesGridVisible = false;

            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
            this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = true;

            this.IsLoginGridVisible = true;

            MainActivity?.StopPollingService();

            this.SettingsProvider.DisableDnd();
          
            this.TurnOffDndSwitch();
        }

        private async void OnSubmitLoginFormButtonClicked(object sender, EventArgs e)
        {
            this.BusyText = BusyTexts.Authentication;
            this.IsActionBusy = true;
            this.IsLoginGridVisible = false;
            this.IsMessagesGridVisible = false;

            var userName = this.GetControl<Entry>(ControlNames.UserNameEntry).Text;
            var password = this.GetControl<Entry>(ControlNames.PasswordEntry).Text;

            var authResult = await LoginAsync(userName, password);
            if (authResult != null && authResult.IsSucceeded)
            {
                this.MainActivity?.StartPollingService();
                this.ShowMessagesGrid(null, false);

                this.IsActionBusy = false;
                this.IsMessagesGridVisible = true;
            }
            else
            {
                // TODO: Implement here.
            }
        }

        private Task<AuthResult> LoginAsync(string userName, string password) => Task.Run(() => Login(userName, password));

        private AuthResult Login(string userName, string password)
        {
            lock (_loginSyncLock)
            {
                var authCredentials = new AuthCredentials(userName, password);

                var authResult = this.MainActivity?.AuthenticationService.LogIn(authCredentials)?.Result;

                return authResult;
            }
        }

        private void OnShowOptionsButtonClick(object sender, EventArgs e)
        {
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = true;
            this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = false;
        }

        private void OnDndSwitchToggle(object sender, ToggledEventArgs e)
        {
            this.SettingsProvider.ToggleDndSettings();

            try
            {
                var message = string.Empty;

                if (this.SettingsProvider.IsDndModeEnabled)
                {
                    this.Vibrate(true);
                    message = ToastTexts.DndModeEnabled;
                }
                else
                {
                    message = ToastTexts.DndModeDisabled;
                }

                this.MainActivity.ShowToast(message);
            }
            catch (Exception ex)
            {
                this.LoggingService.Log(ex, LogType.Suppress);
            }
        }

        private void OnViewLogsButtonClick(object sender, EventArgs e)
        {
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
            this.GetControl<Label>(ControlNames.LogsTextLabel).Text = this.LoggingService.GetTodayLogs();
            this.GetControl<Grid>(ControlNames.LogsPopupGrid).IsVisible = true;
        }

        private void OnClearAllLogsButtonClick(object sender, EventArgs e)
        {
            this.LoggingService.ClearLogs();

            this.GetControl<Grid>(ControlNames.LogsPopupGrid).IsVisible = false;
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = true;
        }

        #endregion

        private void InitializeNetworkStatusHandlers()
        {
            MessagingCenter.Subscribe<MainPage>(this, MessagingCenterConstants.ConnectionRefusedKey, (sender) => {

                Device.BeginInvokeOnMainThread(() =>
                {
                    var connectivityErrorPopup = this.GetControl<Grid>(ControlNames.ConnectivityErrorPopup);
                    if (!connectivityErrorPopup.IsVisible)
                    {
                        connectivityErrorPopup.IsVisible = true;
                    }
                });
            });

            MessagingCenter.Subscribe<MainPage>(this, MessagingCenterConstants.ConnectionResumedKey, (sender) => {

                Device.BeginInvokeOnMainThread(() => {

                    var connectivityErrorPopup = this.GetControl<Grid>(ControlNames.ConnectivityErrorPopup);
                    if (connectivityErrorPopup.IsVisible)
                    {
                        connectivityErrorPopup.IsVisible = false;
                    }
                });
            });
        }

        #region UpdateMessageStateActions

        private async void AcceptSelectedMessage()
        {
            await this.MessagesApiProvider.AcceptMessageAsync(this.SelectedMessage?.ApiMessage);
        }

        private async void RejectSelectedMessage()
        {
            await this.MessagesApiProvider.RejectMessageAsync(this.SelectedMessage?.ApiMessage);
        }

        #endregion

        private void InitializeBindingContext() => this.BindingContext = this;
    }
}