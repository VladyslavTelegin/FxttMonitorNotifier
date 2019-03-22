namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using Android.App;
    using Android.Content;

    using FxttMonitorNotifier.Droid.Extensions;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using Plugin.CurrentActivity;

    using System;

    public class SharedPreferencesProvider : ISharedPreferencesProvider
    {
        #region CrossCurrent

        protected Activity Activity => CrossCurrentActivity.Current.Activity;

        protected Context AppContext => this.Activity.ApplicationContext;

        #endregion

        #region ServiceProviders

        protected IBaseServiceProvider ServiceProvider => BaseServiceProvider.Instance;

        #endregion

        #region PublicMethods

        public T Get<T>(string key)
        {
            var result = default(T);

            try
            {
                var preferences = this.AppContext.GetSharedPreferences(this.AppContext.ApplicationInfo.PackageName, FileCreationMode.Private);

                var @string = preferences.GetString(key, string.Empty);

                var decodedString = @string.Base64Decode();

                result = JsonConvert.DeserializeObject<T>(decodedString);
            }
            catch (Exception ex)
            {
                this.ServiceProvider.LoggingService.Log(ex, Enums.Logging.LogType.Error);
            }

            return result;
        }

        public void Save<T>(string key, T value)
        {
            try
            {
                var preferences = this.AppContext.GetSharedPreferences(this.AppContext.ApplicationInfo.PackageName, FileCreationMode.Private);

                var preferencesEditor = preferences.Edit();

                var serializedValue = JsonConvert.SerializeObject(value);

                preferencesEditor.PutString(key, serializedValue.Base64Encode());
                preferencesEditor.Commit();
            }
            catch (Exception ex)
            {
                this.ServiceProvider.LoggingService.Log(ex, Enums.Logging.LogType.Error);
            }
        }

        public void Delete(string key)
        {
            try
            {
                var preferences = this.AppContext.GetSharedPreferences(this.AppContext.ApplicationInfo.PackageName, FileCreationMode.Private);

                var preferencesEditor = preferences.Edit();

                preferencesEditor.Remove(key);
                preferencesEditor.Commit();
            }
            catch (Exception ex)
            {
                this.ServiceProvider.LoggingService.Log(ex, Enums.Logging.LogType.Error);
            }
        }

        public bool Exists(string key)
        {
            string @string = null;

            try
            {
                var preferences = this.AppContext.GetSharedPreferences(this.AppContext.ApplicationInfo.PackageName, FileCreationMode.Private);

                @string = preferences.GetString(key, null);
            }
            catch (Exception ex)
            {
                this.ServiceProvider.LoggingService.Log(ex, Enums.Logging.LogType.Error);
            }

            return @string != null;
        }

        #endregion
    }
}