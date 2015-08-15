using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reflection;
namespace XWall
{
    /// <summary>
    /// ShowUpdateLog.xaml 的交互逻辑
    /// </summary>
    public partial class ShowUpdateLog : Window
    {
        private Boolean allowClose = false;
        public ShowUpdateLog()
        {
            InitializeComponent();
        }

        private void Update_UpdateLogExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Height = 163;
        }

        private void Update_UpdateLogExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.Height = 270;
        }

        private void Update_Cancel_Click(object sender, RoutedEventArgs e)
        {
            MainSetting.isUpdateCancel = true;
            allowClose = true;
            this.Close();
        }

        private void Update_Confirm_Click(object sender, RoutedEventArgs e)
        {
            allowClose = true;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Update_LocalVerNum.Content = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (MainSetting.latestVersion != null)
            {
                Update_OnlineVerNum.Content = MainSetting.latestVersion;
                Update_UpdateLogBox.Text = MainSetting.updateLog;
                Update_DownloadTimes.Content = MainSetting.downloadTimes;
                var installedVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (installedVersion < Version.Parse(MainSetting.minVersion))
                {
                    Update_DownloadTimes.Foreground = Brushes.ForestGreen;
                    Update_DownloadTimes_Text.Foreground = Brushes.ForestGreen;
                }
            }
            Update_UpdateLogBox.IsReadOnly = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (allowClose == false)
            {
                e.Cancel = true;
            }
        }
    }
}
