namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Enums.Logging;
    using FxttMonitorNotifier.Droid.Models;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using Newtonsoft.Json;

    using System;
    using System.IO;

    using Xamarin.Forms;

    public class SettingsProvider : ISettingsProvider
    {
        protected string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), GlobalConstants.SettingsProviderConstants.SettingsFileName);

        public bool IsDndModeEnabled
        {
            get
            {
                var result = false;

                if (File.Exists(FilePath))
                {
                    var fileText = File.ReadAllText(FilePath);

                    var settingsModel = JsonConvert.DeserializeObject<SettingsModel>(fileText);

                    result = settingsModel.IsDndModeEnabled;
                }
                else
                {
                    this.CreateSettingsFileWithStructureIfNecessary();
                }

                return result;
            }
        }

        public void DisableDnd()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        public void ToggleDndSettings()
        {
            try
            {
                this.CreateSettingsFileWithStructureIfNecessary();

                var fileText = File.ReadAllText(FilePath);

                var settingsModel = JsonConvert.DeserializeObject<SettingsModel>(fileText);

                settingsModel.IsDndModeEnabled ^= true;

                var serializedModel = JsonConvert.SerializeObject(settingsModel);

                File.WriteAllText(FilePath, serializedModel);
            }
            catch (Exception ex)
            {
                DependencyService.Get<ILoggingService>().Log(ex, LogType.Error);
            }
        }

        private void CreateSettingsFileWithStructureIfNecessary()
        {
            if (!File.Exists(this.FilePath))
            {
                var emptySerializedModel = JsonConvert.SerializeObject(new SettingsModel());
                File.WriteAllText(this.FilePath, emptySerializedModel);
            }
        }
    }
}