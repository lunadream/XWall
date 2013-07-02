using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using XWall.Properties;

namespace XWall {
    class GoAgent {
        static Settings settings = Settings.Default;
        static ResourceDictionary resources = App.Current.Resources;
        Process process;
        bool stop;
        static string configTpl;
        bool startPending = false;

        public GoAgent() {
            Operation.KillProcess(App.AppDataDirectory + settings.GaPython33FileName);
            App.Current.Exit += (sender, e) => {
                Stop();
            };

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "ProxyType": break;
                    case "GaPort": break;
                    case "GaProfile": break;
                    case "GaAppIds": break;
                    default: return;
                }

                if (settings.ProxyType == "GA") {
                    GenerateConfigFile();
                }
            };

            if (settings.ProxyType == "GA") {
                GenerateConfigFile();
            }
        }

        public static void GenerateConfigFile() {
            if (String.IsNullOrEmpty(configTpl)) {
                configTpl = File.ReadAllText(App.AppDataDirectory + settings.GaConfigTemplateFileName);
            }

            var configText =
                configTpl.Replace("$port$", settings.GaPort.ToString())
                .Replace("$profile$", settings.GaProfile)
                .Replace("$app-ids$", settings.GaAppIds);

            File.WriteAllText(App.AppDataDirectory + settings.GaConfigFileName, configText);
            Operation.GrantAccessControl(App.AppDataDirectory + settings.GaConfigFileName);
        }

        public event Action Started = () => { };
        public event Action Stopped = () => { };
        public event Action RequireRunas = () => { };

        void startProcess() {
            if (Environment.HasShutdownStarted) return;

            stop = false;
            process = new Process();

            var si = process.StartInfo;

            si.FileName = App.AppDataDirectory + settings.GaPython33FileName;
            si.Arguments = '"' + App.AppDataDirectory + settings.GaScriptFileName + '"';
            si.CreateNoWindow = true;
            si.UseShellExecute = false;

            process.Start();

            Started();
            
            process.WaitForExit();

            Stopped();

            if (!stop) {
                new Action(() => {
                    Thread.Sleep(2000);
                    if ((process == null || process.HasExited) && !startPending) {
                        startProcess();
                    }
                }).BeginInvoke(null, null);
            }
        }

        public void Start() {
            Stop();
            if (settings.GaAppIds != "" && settings.GaAppIds != "goagent") {
                new Action(() => {
                    startPending = true;
                    Thread.Sleep(100);
                    startProcess();
                    startPending = false;
                }).BeginInvoke(null, null);
            }
        }

        public void Stop() {
            stop = true;
            try {
                if (process != null && !process.HasExited) {
                    process.Kill();
                }
            }
            catch { }
        }



    }
}
