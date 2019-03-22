namespace FxttMonitorNotifier
{
    using Android.App;
    using Android.Content;

    using Firebase.Iid;

    using FxttMonitorNotifier.Droid;
    using FxttMonitorNotifier.Droid.Enums;
    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Extensions;
    using FxttMonitorNotifier.Droid.Models.Api;
    using FxttMonitorNotifier.Droid.Models.Ui;
    using FxttMonitorNotifier.Droid.Services.Implementations;
    using FxttMonitorNotifier.Droid.Services.Implementations.Firebase;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Microcharts;
    using Microcharts.Forms;

    using Plugin.CurrentActivity;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Xamarin.Essentials;
    using Xamarin.Forms;
   
    using static Droid.Models.GlobalConstants;

    #region Aliases

    using Entry = Xamarin.Forms.Entry;

    #endregion

    public partial class MainPage : ContentPage
    {
        #region PrivateFields

        private static readonly object LoginSyncLock = new object();

        private string _busyText;

        private long _totalMessagesCount;

        private bool _isActionBusy;
        private bool _isMessagesGridVisible;
        private bool _isLoginGridVisible;
        private bool _isListViewRefreshing;

        private Command _listViewRefreshCommand;

        private Command _acceptMessageCommand;
        private Command _rejectMessageCommand;

        private Command _acceptMessageActionCommand;
        private Command _cancelMessageActionCommand;

        private ObservableMessage _selectedMessage;

        #endregion

        #region Constructor

        public MainPage(Message message = null)
        {
            this.InitializeComponent();

            this.InitializeBindingContext();
            this.InitializeDndSwitch();
            this.InitializeMessageGrid(message);

            this.Appearing += (sender, args) => 
            {
                FirebaseNotificationsService.IsUiActivityVisible = true;
                this.InitializeNetworkStatusHandlers();
            };

            this.Disappearing += (sender, args) =>
            {
                FirebaseNotificationsService.IsUiActivityVisible = false;

                MessagingCenter.Unsubscribe<MainPage>(this, MessagingCenterConstants.ConnectionRefusedKey);
                MessagingCenter.Unsubscribe<MainPage>(this, MessagingCenterConstants.ConnectionResumedKey);
            };

            this.InitializeGestureRecognizers();
        }

        #endregion

        #region CrossCurrent

        protected Activity Activity => CrossCurrentActivity.Current.Activity;

        protected Context AppContext => this.Activity.ApplicationContext;

        protected MainActivity MainActivity => this.Activity as MainActivity;

        #endregion

        #region ServiceProviders

        protected IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

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

        public Command AcceptMessageCommand => _acceptMessageCommand ?? (_acceptMessageCommand = new Command(this.AcceptSelectedMessage));
        public Command RejectMessageCommand => _rejectMessageCommand ?? (_rejectMessageCommand = new Command(this.RejectSelectedMessage));

        public Command AcceptMessageActionCommand => _acceptMessageActionCommand ?? (_acceptMessageActionCommand = new Command(async () => await this.AcceptMessageAction()));
        public Command CancelMessageActionCommand => _cancelMessageActionCommand ?? (_cancelMessageActionCommand = new Command(this.CancelMessageAction));

        #endregion

        #region Methods

        public async void UpdateUi(Message message)
        {
            if (this.ServiceProvider.AuthenticationService.IsAuthenticated)
            {
                if (message != null && message.IsSilent)
                {
                    var existingObservableEntry = this.Messages.FirstOrDefault(_ => _.ApiMessage.Id == message.Id);
                    if (!(existingObservableEntry == null || existingObservableEntry.Equals(message)))
                    {
                        existingObservableEntry.ApiMessage = message;
                        existingObservableEntry.AcceptedUsersCount = message.AcceptedUsersCount.ToString();
                    }

                    return;
                }

                await this.ShowMessageGridAsync(message: message, 
                                                isUpdate: message != null);

                if (message != null)
                {
                    this.Vibrate();
                }
            }
        }

        private async void InitializeMessageGrid(Message message)
        {
            if (!this.MainActivity.AuthenticationService.IsAuthenticated)
            {
                this.IsLoginGridVisible = true;
            }
            else
            {
                await this.ShowMessageGridAsync(message);
            }
        }

        private async void RefreshListView()
        {
            this.IsListViewRefreshing = true;

            await this.ShowMessageGridAsync(null);

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

            var twoTapsGestureRecognizer_Logs = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 2,
                Command = new Command(() =>
                {
                    this.GetControl<Grid>(ControlNames.LogsPopupGrid).IsVisible = false;
                    this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
                    this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = true;
                })
            };


            var logsPopupLabel = this.GetControl<Label>(ControlNames.LogsTextLabel);
            logsPopupLabel.GestureRecognizers.Add(twoTapsGestureRecognizer_Logs);

            var twoTapsGestureRecognizer_Statistics = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 2,
                Command = new Command(async () =>
                {
                    await this.HideChart();
                    this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
                    this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = true;
                })
            };

            var innerChartsContainer = this.GetControl<Grid>("InnerChartsContainer");
            innerChartsContainer.GestureRecognizers.Add(twoTapsGestureRecognizer_Statistics);

            #endregion
        }

        private void Vibrate(bool isFromDndToggle = false)
        {
            if (!this.ServiceProvider.SettingsProvider.IsDndModeEnabled)
            {
                Vibration.Vibrate(250);
            }

            if (isFromDndToggle)
            {
                Vibration.Vibrate(750);
            }
        }

        private async Task ShowMessageGridAsync(Message message = null, bool showCustomPreloader = true, bool isUpdate = false)
        {
            if (showCustomPreloader)
            {
                this.IsMessagesGridVisible = false;
                this.IsLoginGridVisible = false;
                this.IsActionBusy = true;
                this.BusyText = isUpdate ? BusyTexts.Updating : BusyTexts.LoadingItems;

                await Task.Delay(500);
            }

            this.Messages.Clear();

            await UpdateMessagesCollectionAsync();

            this.TotalMessagesCount = this.Messages.Count;

            this.OnPropertyChanged(nameof(this.TotalMessagesCountString));

            if (showCustomPreloader)
            {
                await Task.Delay(250);

                this.IsActionBusy = false;
                this.IsMessagesGridVisible = true;
            }

            if (message != null)
            {
                var entryToSelect = this.Messages.FirstOrDefault(_ => _.ApiMessage.Id == message.Id);
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

        private async Task UpdateMessagesCollectionAsync()
        {
            if (!MainActivity.CheckInternetConnection())
            {
                var connectivityErrorPopup = this.GetControl<Grid>(ControlNames.ConnectivityErrorPopup);
                if (!connectivityErrorPopup.IsVisible)
                {
                    connectivityErrorPopup.IsVisible = true;
                    await connectivityErrorPopup.FadeTo(1, 100);
                }
            }

            var allMessages = this.ServiceProvider.MessagesApiProvider.RetreiveSpecificMessages().ToList();

            allMessages.Distinct()
                       .ToList()
                       .ForEach(_ => this.Messages.Add(new ObservableMessage(_)));

            await Task.CompletedTask;
        }

        private void InitializeDndSwitch()
        {
            var switchControl = this.GetControl<Switch>(ControlNames.DndSwitch);

            switchControl.Toggled -= this.OnDndSwitchToggle;
            switchControl.IsToggled = this.ServiceProvider.SettingsProvider.IsDndModeEnabled;
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
            try
            {
                if (e.Item is ObservableMessage currentItem)
                {
                    this.Messages.ToList().ForEach(_ =>
                    {
                        if (_ != currentItem)
                            _.IsVisible = false;
                    });

                    currentItem.IsVisible ^= true;

                    if (!currentItem.IsVisible)
                    {
                        this.GetControl<ListView>(ControlNames.MessagesListView).SelectedItem = null;
                    }
                }
            }
            catch (Exception ex)
            {
                this.ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
            }
        }

        private void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            this.ServiceProvider.AuthenticationService.LogOut();

            Task.Run(() => FirebaseInstanceId.Instance.DeleteInstanceId());

            this.IsMessagesGridVisible = false;

            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
            this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = true;

            this.IsLoginGridVisible = true;

            this.ServiceProvider.SettingsProvider.DisableDnd();
          
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
            if (authResult != null && authResult.IsSuccess)
            {
                await this.ShowMessageGridAsync(null, false);

                this.IsActionBusy = false;
                this.IsMessagesGridVisible = true;

                this.ResetCredentialFields();

                MainActivity.ShowToast("Authentication succeeded.");
            }
            else
            {
                this.IsActionBusy = false;
                this.BusyText = null;

                this.ResetCredentialFields();

                this.IsLoginGridVisible = true;

                MainActivity.ShowToast("Invalid credentials supplied. Please, try again.");
            }
        }

        private void ResetCredentialFields()
        {
            this.GetControl<Entry>(ControlNames.UserNameEntry).Text = null;
            this.GetControl<Entry>(ControlNames.PasswordEntry).Text = null;
        }

        private Task<AuthResult> LoginAsync(string userName, string password) => Task.Run(() => Login(userName, password));

        private AuthResult Login(string userName, string password)
        {
            lock (LoginSyncLock)
            {
                var authCredentials = new AuthCredentials(userName, password);

                var authResult = this.MainActivity?.AuthenticationService.LogIn(authCredentials);

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
            this.ServiceProvider.SettingsProvider.ToggleDndSettings();

            try
            {
                var message = string.Empty;

                if (this.ServiceProvider.SettingsProvider.IsDndModeEnabled)
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
                this.ServiceProvider.LoggingService.Log(ex, LogType.Suppress);
            }
        }

        private void OnViewLogsButtonClick(object sender, EventArgs e)
        {
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
            this.GetControl<Label>(ControlNames.LogsTextLabel).Text = this.ServiceProvider.LoggingService.GetTodayLogs();
            this.GetControl<Grid>(ControlNames.LogsPopupGrid).IsVisible = true;
        }

        private void OnClearAllLogsButtonClick(object sender, EventArgs e)
        {
            this.ServiceProvider.LoggingService.ClearLogs();

            this.GetControl<Grid>(ControlNames.LogsPopupGrid).IsVisible = false;
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = true;
        }

        private async void OnViewStatisticsButtonClicked(object sender, EventArgs e)
        {
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
            await ShowChart();
        }

        private void OnClearMessagesButtonClicked(object sender, EventArgs e)
        {
            this.ServiceProvider.PermanentCacheService.Invalidate(MessagesApiProvider.MessagesCacheKey);
            this.GetControl<Button>(ControlNames.ShowOptionsButton).IsVisible = true;
            this.GetControl<Grid>(ControlNames.OptionsPopupGrid).IsVisible = false;
            MainActivity.ShowToast("Notifications cache cleared.");
        }

        #endregion

        private void InitializeNetworkStatusHandlers()
        {
            MessagingCenter.Subscribe(this, MessagingCenterConstants.ConnectionRefusedKey, (Action<MainPage>)((sender) => {

                Device.BeginInvokeOnMainThread(async () =>
                {
                    this.DisableInetBoundControls();

                    var connectivityErrorPopup = this.GetControl<Grid>(ControlNames.ConnectivityErrorPopup);
                    if (!connectivityErrorPopup.IsVisible)
                    {
                        connectivityErrorPopup.IsVisible = true;
                        await connectivityErrorPopup.FadeTo(1, 100);
                    }
                });
            }));

            MessagingCenter.Subscribe(this, MessagingCenterConstants.ConnectionResumedKey, (Action<MainPage>)((sender) => {

                Device.BeginInvokeOnMainThread(async () => {

                    this.EnableInetBoundControls();

                    var connectivityErrorPopup = this.GetControl<Grid>(ControlNames.ConnectivityErrorPopup);
                    if (connectivityErrorPopup.IsVisible)
                    {
                        await connectivityErrorPopup.FadeTo(0, 100);
                        connectivityErrorPopup.IsVisible = false;

                        if (this.ServiceProvider.AuthenticationService.IsAuthenticated)
                        {
                            await this.ShowMessageGridAsync(showCustomPreloader: true);
                        }
                    }
                });
            }));
        }

        public void DisableInetBoundControls()
        {
            this.GetControl<Button>("RejectMessageButton").IsEnabled =
            this.GetControl<Button>("AcceptMessageButton").IsEnabled =
            this.GetControl<Button>("SubmitLoginFormButton").IsEnabled = 
            this.GetControl<Entry>("UserNameEntry").IsEnabled =
            this.GetControl<Entry>("PasswordEntry").IsEnabled = false;
        }

        public void EnableInetBoundControls()
        {
           this.GetControl<Button>("RejectMessageButton").IsEnabled =
           this.GetControl<Button>("AcceptMessageButton").IsEnabled =
           this.GetControl<Button>("SubmitLoginFormButton").IsEnabled =
           this.GetControl<Entry>("UserNameEntry").IsEnabled =
           this.GetControl<Entry>("PasswordEntry").IsEnabled = true;
        }

        #region UpdateMessageStateActions

        private MessageAction? _messageAction;

        private async void AcceptSelectedMessage()
        {
            Vibration.Vibrate(25);

            _messageAction = MessageAction.Acception;

            this.GetControl<Label>("CurrentActionText").Text = string.Format(MessageActionQuestionFormat, "accept");
            this.GetControl<Label>("ActionNameAliasText").Text = _messageAction.ToString();

            this.GetControl<Label>("ActionNameAliasText").BackgroundColor = Color.FromRgb(0, 142, 33);

            this.GetControl<Grid>("MessageActionConfirmationDialog").IsVisible = true;
            await this.GetControl<Grid>("MessageActionConfirmationDialog").FadeTo(1, 250);
        }

        private async void RejectSelectedMessage()
        {
            Vibration.Vibrate(25);

            _messageAction = MessageAction.Rejection;

            this.GetControl<Label>("CurrentActionText").Text = string.Format(MessageActionQuestionFormat, "reject");
            this.GetControl<Label>("ActionNameAliasText").Text = _messageAction.ToString();

            this.GetControl<Label>("ActionNameAliasText").BackgroundColor = Color.FromRgb(142, 142, 142);

            this.GetControl<Grid>("MessageActionConfirmationDialog").IsVisible = true;
            await this.GetControl<Grid>("MessageActionConfirmationDialog").FadeTo(1, 250);
        }

        private async Task AcceptMessageAction()
        {
            var selectedMessageId = this.SelectedMessage.ApiMessage.Id;

            if (_messageAction.HasValue)
            {
                var successToastText = 
                    $"Selected notification successfully {(_messageAction.Value == MessageAction.Acception ? "accepted" : "rejected")}.";

                if (_messageAction.Value == MessageAction.Acception)
                {
                    var acceptionResult = await this.ServiceProvider.MessagesApiProvider.AcceptMessageAsync(this.SelectedMessage?.ApiMessage);
                    if (acceptionResult.IsSuccess)
                    {
                        this.CancelMessageAction();

                        var messageCandidate = this.Messages.FirstOrDefault(_ => _.ApiMessage.Id == selectedMessageId).ApiMessage;

                        await this.ShowMessageGridAsync(message: messageCandidate, isUpdate: true);

                        await Task.Delay(500);

                        this.MainActivity.ShowToast(successToastText);
                    }
                    else
                    {
                        this.CancelMessageAction();
                        this.MainActivity.ShowToast(acceptionResult.ErrorMessage);
                    }
                }
                else
                {
                    var rejectionResult = await this.ServiceProvider.MessagesApiProvider.RejectMessageAsync(this.SelectedMessage?.ApiMessage);
                    if (rejectionResult.IsSuccess)
                    {
                        this.CancelMessageAction();

                        var messageCandidate = this.Messages.FirstOrDefault(_ => _.ApiMessage.Id == selectedMessageId).ApiMessage;

                        await this.ShowMessageGridAsync(message: messageCandidate, isUpdate: true);

                        await Task.Delay(500);

                        this.MainActivity.ShowToast(successToastText);
                    }
                    else
                    {
                        this.CancelMessageAction();
                        this.MainActivity.ShowToast(rejectionResult.ErrorMessage);
                    }
                }
            }
        }

        private async void CancelMessageAction()
        {
            await this.GetControl<Grid>("MessageActionConfirmationDialog").FadeTo(0, 250);
            this.GetControl<Grid>("MessageActionConfirmationDialog").IsVisible = false;

            this.GetControl<Label>("CurrentActionText").Text = null;

            _messageAction = null;
        }

        #endregion

        private void InitializeBindingContext() => this.BindingContext = this;

        #endregion

        #region Charts

        public async Task ShowChart()
        {
            var chartView = this.GetControl<ChartView>("MessagesStatisticsChart");

            var lastMonthDate = DateTime.UtcNow.AddMonths(-1);

            var allMessages = this.ServiceProvider.MessagesApiProvider.RetreiveAllMesages()
                                                                      .Where(_ => _.CreatedOn >= lastMonthDate)
                                                                      .ToList();

            var lowCount = allMessages.Count(_ => _.Priority == (int)Droid.Enums.NotificationPriority.Low);
            var mediumCount = allMessages.Count(_ => _.Priority == (int)Droid.Enums.NotificationPriority.Medium);
            var highCount = allMessages.Count(_ => _.Priority == (int)Droid.Enums.NotificationPriority.High);

            var chartEntries = new List<ChartEntry>
            {
                new ChartEntry(lowCount)
                {
                    Color = SkiaSharp.SKColor.Parse("FF1E90FF"),
                    Label = "L",
                    ValueLabel = lowCount.ToString()
                },
                new ChartEntry(mediumCount)
                {
                    Color = SkiaSharp.SKColor.Parse("0000FF"),
                    Label = "M",
                    ValueLabel = mediumCount.ToString()
                },
                new ChartEntry(highCount)
                {
                    Color = SkiaSharp.SKColor.Parse("FF8B0000"),
                    Label = "H",
                    ValueLabel = highCount.ToString()
                }
            };

            chartView.Chart = new RadarChart
            {
                Entries = chartEntries,
                LabelTextSize = 52,
                PointSize = 10,
                LineSize = 10,
                BackgroundColor = SkiaSharp.SKColor.FromHsl(0, 0, 0, 0)
            };

            this.GetControl<Grid>("ChartContainer").IsVisible = true;

            await Task.CompletedTask;
        }

        public async Task HideChart()
        {
            this.GetControl<Grid>("ChartContainer").IsVisible = false;

            await Task.CompletedTask;
        }

        #endregion
    }
}