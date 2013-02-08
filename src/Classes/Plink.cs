using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using XWall.Properties;

namespace XWall {
    class Plink {
        static Settings settings = Settings.Default;
        static ResourceDictionary resources = App.Current.Resources;
        Process process;
        int reconnectPeriod;
        bool isLastSuccess;
        bool toReconnect;
        int portCloseCount;
        public bool IsReconnecting = false;
        public bool IsConnecting = false;
        public bool IsConnected = false;
        public bool IsNormallyStopped = false;
        Action stopReconnect;

        public Plink() {
            Operation.KillProcess(settings.PlinkFileName);
            App.Current.Exit += (sender, e) => {
                Stop();
            };
        }

        void startProcess() {
            startProcess(false);
        }

        public static bool CheckSettings() {
            return
                settings.SshServer != "" &&
                settings.SshPort > 0 &&
                settings.SshUsername != "" &&
                settings.SshPassword != "" &&
                settings.SshSocksPort > 0;
        }

        void startProcess(bool isReconnect) {
            if (Environment.HasShutdownStarted) return;

            Thread.Sleep(10);

            isLastSuccess = false;
            toReconnect = true;
            IsConnecting = true;
            IsNormallyStopped = false;
            portCloseCount = 0;

            Started();
            process = new Process();

            var si = process.StartInfo;
            si.FileName = settings.SshUsePlonk ? settings.PlonkFileName : settings.PlinkFileName;
            si.Arguments = String.Format(
                "-v -x -a -T -N{0}{1} -l {2} -pw {3} -P {4} -D {5} {6}",
                settings.SshCompression ? " -C" : "",
                settings.SshUsePlonk ? " -z" + (settings.SshPlonkKeyword.Trim() != "" ? " -Z " + settings.SshPlonkKeyword : "") : "",
                settings.SshUsername,
                settings.SshPassword,
                settings.SshPort,
                "127.0.0.1:" + settings.SshSocksPort,
                settings.SshServer
            );

            si.RedirectStandardOutput = true;
            si.RedirectStandardInput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;
            si.UseShellExecute = false;

            process.OutputDataReceived += onOutputDataReceived;
            process.ErrorDataReceived += onErrorDataReceived;

            Error = null;
            try {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(settings.PlinkConnectTimeout * 1000);
                if (process != null && !process.HasExited) {
                    if (IsConnected) {
                        process.WaitForExit();
                    }
                    else {
                        process.Kill();
                    }
                }
            }
            catch { }

            IsConnected = false;
            IsConnecting = false;
            Disconnected(isLastSuccess, isReconnect);

            if (toReconnect) {
                var stopReconnectingHandler = reconnect();
                if (stopReconnectingHandler != null) {
                    stopReconnect = stopReconnectingHandler;
                }
            }
        }

        public void Start() {
            reconnectPeriod = 1;
            Stop();
            if (!CheckSettings()) return;
            new Action(() => {
                Thread.Sleep(100);
                startProcess();
            }).BeginInvoke(null, null);
        }

        public void Stop(bool toReconnect = false) {
            IsNormallyStopped = true;
            this.toReconnect = toReconnect;

            if (IsReconnecting) {
                stopReconnect();
            }

            if (process != null && !process.HasExited) {
                process.Kill();
            }
        }

        Action reconnect() {
            if (IsReconnecting) {
                //new Action(() => {
                //    Thread.Sleep(1000);
                //    reconnect();
                //}).BeginInvoke(null, null);
                return null;
            }


            if (settings.SshAutoReconnect && (settings.SshReconnectAnyCondition || Error == null)) {
                var stopReconnect = false;
                IsReconnecting = true;
                //StopReconnect = false;

                new Action(() => {
                    Thread.Sleep(500);

                    if (stopReconnect) {
                        return;
                    }

                    var time = reconnectPeriod;

                    if (reconnectPeriod < settings.MaxReconnectPeriod)
                        reconnectPeriod *= 2;

                    while (time > 0) {
                        ReconnectCountingDown(time--);
                        Thread.Sleep(1000);
                        if (stopReconnect) {
                            return;
                        }
                        //if started by user or something elses while counting.
                        if (process != null && !process.HasExited || !toReconnect) {
                            IsReconnecting = false;
                            return;
                        }
                    }

                    IsReconnecting = false;

                    if (!CheckSettings()) {
                        Disconnected(false, true);
                        return;
                    }

                    startProcess(true);
                }).BeginInvoke(null, null);

                return () => {
                    stopReconnect = true;
                    IsReconnecting = false;
                    Disconnected(false, true);
                };
            }
            else {
                return null;
            }
        }

        void onOutputDataReceived(object sender, DataReceivedEventArgs e) {
            Console.WriteLine("D: " + e.Data);
        }

        void onErrorDataReceived(object sender, DataReceivedEventArgs e) {
            var line = e.Data;
            if (line == null) return;

            Console.WriteLine("E: " + line);

            if (new Regex(@"^Local port .+ SOCKS dynamic forwarding").IsMatch(line)) {
                reconnectPeriod = 1;
                isLastSuccess = true;
                IsConnected = true;
                IsConnecting = false;
                Connected();
            }
            else if (line.StartsWith("Nothing left to send, closing channel"))
                portCloseCount = Math.Min(1, portCloseCount + 1);
            else if (line.StartsWith("Forwarded port closed")) {
                if (--portCloseCount < -settings.AbortionBeforeReconnect && settings.SshAutoReconnect)
                    Stop(true);
            }
            else if (line.StartsWith("The server's host key is not cached in the registry."))
                process.StandardInput.WriteLine("y");
            else if (line.StartsWith("Password authentication failed")) {
                Error = resources["PlinkAuthFailed"] as string;
                Stop(settings.SshReconnectAnyCondition);
            }
            //else if (line.StartsWith("FATAL ERROR:")) {
            //    Stop(settings.SshReconnectAnyCondition);
            //}
        }

        public string Error;

        public event Action Started = () => { };
        public event Action Connected = () => { };

        public delegate void DisconnectHandler(bool isLastSuccess, bool isReconnect);
        public event DisconnectHandler Disconnected = (isLastSuccess, isReconnect) => { };

        public delegate void CountingDownHandler(int seconds);
        public event CountingDownHandler ReconnectCountingDown = (seconds) => { };
    }
}
