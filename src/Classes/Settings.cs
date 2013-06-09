using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
namespace XWall.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {

        public bool AutoSave { get; set; }
        
        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //

            AutoSave = true;

            this.PropertyChanged += (sender, e) => {
                if (AutoSave) {
                    Settings.Default.Save();
                }
            };
        }

        public bool Export(string path) {
            try {
                File.Copy(GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal), path, true);
                return true;
            }
            catch {
                return false;
            }
        }

        public bool Import(string path) {
            AutoSave = false;
            try {
                File.Copy(path, GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal), true);
                return true;
            }
            catch {
                return false;
            }
        }

        public static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel) {
            try {
                var UserConfig = ConfigurationManager.OpenExeConfiguration(userLevel);
                return UserConfig.FilePath;
            }
            catch (ConfigurationException e) {
                return e.Filename;
            }
        }
        
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Add code to handle the SettingChangingEvent event here.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }
    }
}
