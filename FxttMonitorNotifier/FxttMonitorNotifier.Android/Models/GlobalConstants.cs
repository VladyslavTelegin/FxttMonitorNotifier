namespace FxttMonitorNotifier.Droid.Models
{
    public static class GlobalConstants
    {
        public const string DefaultDateTimeFormat = "dd.MM.yyyy HH:mm:ss";

        public static class ControlNames
        {
            #region Grids

            public const string OptionsPopupGrid = "OptionsPopupGrid";
            public const string OptionsPopupGridOverlay = "OptionsPopupGridOverlay";

            public const string LogsPopupGrid = "LogsPopupGrid";
            public const string LogsPopupGridOverlay = "LogsPopupGridOverlay";

            public const string ConnectivityErrorPopup = "ConnectivityErrorPopup";

            #endregion

            #region ListViews

            public const string MessagesListView = "MessagesListView";

            #endregion

            #region Buttons

            public const string ShowOptionsButton = "ShowOptionsButton";

            #endregion

            #region Entries

            public const string UserNameEntry = "UserNameEntry";
            public const string PasswordEntry = "PasswordEntry";

            #endregion

            #region Switches

            public const string DndSwitch = "DndSwitch";

            #endregion

            #region Labels

            public const string LogsTextLabel = "LogsTextLabel";

            #endregion
        }

        public static class BroadcastingConstants
        {
            public const string UiNotification = "UiNotificationBroadcasting";
            public const string UiNotificationKey = "UiNotificationBroadcastingKey";

            public const string IsAfterBootCompleteExtraKey = "IsAfterBootComplete";
            public const string SucceessfulLogInfo = "Service started after boot complete.";
        }

        public static class MainActivityConstants
        {
            public const string DefaultServiceRunningToastMessage = "Polling service is running...";
            public const string DefaultServiceStoppedToastMessage = "Polling service was stopped.";

            public const string SerializedMessageModelExtraKey = "Serialized_Message_Model";

            public const string NetworkPingUrl = "http://www.google.com";

            public const string WakeLockTag = "fxtt.monitorNotifier:wakeLock";
        }

        public static class PollingServiceConstants
        {
            // TODO: Change Urls after API will be ready:
            public const string PollingSpecificUrl = "http://demo3.genie-solution.com/api/messages";
            public const string PollingAllUrl = "http://demo3.genie-solution.com/api/messages";

            public const string AcceptSpecificUrl = "";
            public const string RejectSpecificUrl = "";

            public const string EnvironmentNotificationTitle = "Environment Notification";
            public const string TapForMoreDetails = "Tap for more details.";
            public const string ServiceStartedMessage = "Polling service has been started.";
            public const string SingleCallReceivedFromMonitorMessage = "1 new call received from monitor.";
            public const string ServiceDestroyedLogMessage = "Polling service has been destroyed.";

            public const string CallMessageType = "Call";

            public const string SystemNotificationChannelId = "fxtt_monitor_notifier_system_channel";

            public const string CallsNotificationBasicChannelId = "fxtt_monitor_notifier_calls_basic_channel";
            public const string CallsNotificationDNDChannelId = "fxtt_monitor_notifier_calls_dnd_channel";

            public const int ServiceRunningNotificationId = 12210;
        }

        public static class BusyTexts
        {
            public const string Authentication = "Authentication...";
            public const string LoadingItems = "Loading Items...";
            public const string Updating = "Updating...";
        }

        public static class AuthenticationServiceConstants
        {
            public const string AuthStateFileName = "auth_state.data";
        }

        public static class SettingsProviderConstants
        {
            public const string SettingsFileName = "app_settings.config";
        }

        public static class ExceptionConstants
        {
            public const string ConnectionFailure = "Connection was lost or refused.";
            public const string ExceptionThrownFromStringSlice = "Exception thrown from";
            public const string UnspecifiedErrorOccurred = "Unspecified error occurred.";
            public const string ActionUrlIsUndefinedStringSlice = "Action Url is undefined.";
        }

        public static class LoggingServiceConstants
        {
            public const string LogsFolderName = "logs";
        }

        public static class MessagingCenterConstants
        {
            public const string ConnectionRefusedKey = "connection_refused";
            public const string ConnectionResumedKey = "connection_resumed";
        }

        public static class TransientFaultHandlerConstants
        {
            public const uint MaxAtteptsCount = 3;
        }

        public static class ToastTexts
        {
            public const string DndModeEnabled = "Don't disturb mode enabled.";
            public const string DndModeDisabled = "Don't disturb mode disabled.";
        }
    }
}