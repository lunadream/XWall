using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Xml;
using System.Net;
using XWall.Properties;
namespace XWall
{
    /// <summary>
    /// FeedbackWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FeedbackWindow : Window
    {
        static Settings settings = Settings.Default;
        public FeedbackWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox_AppVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            TextBox_AppName.Text = Assembly.GetExecutingAssembly().GetName().Name.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
            this.IsEnabled = false;
            var url = new Uri(settings.OnlineVersionUrl);
            XmlDocument xmlDocRpt = new XmlDocument();
            var clientRpt = new WebClient();
            //client.Proxy = null;
            clientRpt.DownloadStringAsync(url);
            clientRpt.Encoding = System.Text.Encoding.UTF8;
            clientRpt.DownloadStringCompleted += (_sender, _e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_e.Error == null)
                    {
                        xmlDocRpt.LoadXml (_e.Result);
                        MainSetting.minVersion = xmlDocRpt.SelectSingleNode("XWall.UpdateXML/MinVersion").InnerText;
                        MainSetting.latestVersion = xmlDocRpt.SelectSingleNode("XWall.UpdateXML/LatestVersion").InnerText;

                        //get latest version download times
                        var checkUrl = new Uri(settings.UpdateReportUrl + "?v=" + MainSetting.latestVersion + "&r=1");
                        XmlDocument xmlDoc = new XmlDocument();
                        var client = new WebClient();
                        client.DownloadStringAsync(checkUrl);
                        client.Encoding = System.Text.Encoding.UTF8;
                        client.DownloadStringCompleted += (__sender, __e) =>
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                MainSetting.downloadTimes = __e.Result;
                            }));

                        };


    
                    }
                    
                }));
            };
        }
    }
}