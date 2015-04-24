using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using XWall.Properties;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Windows;
using System.Diagnostics;

namespace XWall
{
    public class AppSetting
    {
        static Settings mysettings = XWall.Properties.Settings.Default;
        static string[] userSettings = {"ProxyType","SshServer","SshPort","SshUsername","SshPassword","HttpServer","HttpPort","AutoStart","ListenToLocalOnly","ProxyPort","SshCompression","SshSocksPort","CustomRulesAddSubdomains","UpgradeRequired","SshAutoReconnect","OnlineRulesLastUpdateTime","UseOnlineRules","CustomRules","FirstRun","OriginalProxies","DismissedUpdateVersion","SshNotification","SubmitNewRule","SubmitNewRuleAsked","SshUsePlonk","SshPlonkKeyword","SshReconnectAnyCondition","SshProfiles","SshSelectedProfileIndex","ForwardAll","SetProxyAutomatically","SocksServer","SocksPort","UseIntranetProxy","IntranetProxyServer","IntranetProxyPort","GaAppIds","GaPort","GaProfile","ToUseGoAgent","GaLastServerVersion" };
        readonly static string confFile = Environment.CurrentDirectory + "\\Application.conf";
        public static void SettingUpgrade(){
            XElement xElement = new XElement(new XElement("XWall.SettingsXML"));
            foreach (string sp in userSettings)
            {
                xElement.Add(new XElement(sp, Settings.Default[sp].ToString()));
            }
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = ASCIIEncoding.UTF8;
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(confFile, settings);
            xElement.Save(writer);
            writer.Flush();
            writer.Close();
            Process.Start(System.Windows.Forms.Application.ExecutablePath, "restart");
            App.Current.Shutdown();
        }
        public static string ReadSetting(string settingName)
        {
            if (IsFileExist())
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(confFile);
                try
                {
                    return xmlDoc.SelectSingleNode("Xwall.SettingsXML//" + settingName).InnerText;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            
        }
        public static bool IsFileExist()
         {
             if (System.IO.File.Exists(confFile))
             {
                 return true;
             }
             else
             {
                 return false;
             }
         }
        public static bool WriteSetting(string settingName, string settingValue)
         {
             if (IsFileExist())
             {
                 try
                 {
                     XmlDocument xmlDoc = new XmlDocument();
                     xmlDoc.Load(confFile);
                     XmlElement element = (XmlElement)xmlDoc.SelectSingleNode("Xwall.SettingsXML//" + settingName);
                     element.InnerText = settingValue;
                     xmlDoc.Save(confFile);
                 }
                 catch
                 {
                     return false;
                 }
                 return true;
             }
             else
             {
                 return false;
             }
         }
         public static void MoveOldConfig()
         {
             string groinupDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Groinup";
             if (System.IO.Directory.Exists(groinupDir))
             {
                 if (!File.Exists(groinupDir + "\\Updated"))
                 {
                     DirectoryInfo directoryInfo = new DirectoryInfo(groinupDir);
                     DirectoryInfo[] arrayDir = directoryInfo.GetDirectories();
                     SortAsFolderModifyTime(ref arrayDir);
                     string lastversionAppdir = groinupDir + "\\" + arrayDir[0].Name;
                     directoryInfo = new DirectoryInfo(lastversionAppdir);
                     arrayDir = directoryInfo.GetDirectories();
                     SortAsFolderModifyTime(ref arrayDir);
                     string lastversionCFGdir = lastversionAppdir + "\\" + arrayDir[0].Name;
                     var result = mysettings.Import(lastversionCFGdir + "\\user.config");
                     if (!result)
                     {
                         MessageBox.Show(App.Current.Resources["FailedImportSettings"] as string);
                     }
                     else
                     {
                         File.WriteAllText(groinupDir + "\\Updated", null);
                         MessageBox.Show(App.Current.Resources["UpdateSuccessDetails"] as string, App.Current.Resources["UpdateSuccessTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Information);
                         Process.Start(System.Windows.Forms.Application.ExecutablePath, "restart");
                         App.Current.Shutdown();
                     }
                 }
             }
         }
         private static void SortAsFolderModifyTime(ref DirectoryInfo[] dirs)
         {
             Array.Sort(dirs, delegate(DirectoryInfo x, DirectoryInfo y) { return y.LastWriteTime.CompareTo(x.LastWriteTime); });
         }
    }
}
