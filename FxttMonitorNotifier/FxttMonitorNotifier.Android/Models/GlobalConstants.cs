namespace FxttMonitorNotifier.Droid.Models
{
    public static class GlobalConstants
    {
        public const string DefaultDateTimeFormat = "dd.MM.yyyy HH:mm:ss";

        public const string MessageActionQuestionFormat = "Do you really want to {0} selected notification?";

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
            public const string DefaultServiceRunningToastMessage = "Notification service is running...";
            public const string DefaultServiceStoppedToastMessage = "Notification service was stopped.";

            public const string SerializedMessageModelExtraKey = "Serialized_Message_Model";

            public const string NetworkPingUrl = "http://www.google.com";

            public const string WakeLockTag = "fxtt.monitorNotifier:wakeLock";
        }

        public static class FirebaseConstants
        {
            public const string TopicId = "call_messages";

            public const string PollingSpecificUrl = "http://monitor.fxtoptech.com/api/notifier/notifications/actual";
            public const string PollingAllUrl = "http://monitor.fxtoptech.com/api/notifier/notifications/all";
            public const string UpdateFirebaseTokenApiUrl = "http://monitor.fxtoptech.com/api/notifier/firebase/updatetoken";
            public const string UpdateMessageStateApiUrl = "http://monitor.fxtoptech.com/api/notifier/state";

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
            public const string CallsNotificationDndChannelId = "fxtt_monitor_notifier_calls_dnd_channel";

            public const int ServiceRunningNotificationId = 12210;
        }

        public static class BusyTexts
        {
            public const string Authentication = "Authentication...";
            public const string LoadingItems = "Synchronizing Feed...";
            public const string Updating = "Updating Feed...";
        }

        public static class AuthenticationServiceConstants
        {
            public const string AuthStateFileName = "auth_state.data";

            public const string ApiAuthUrl = "http://monitor.fxtoptech.com/api/notifier/auth";
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