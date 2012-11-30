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
        Timer connectTimer;
        int portCloseCount;
        public bool IsReconnecting = false;
        public bool IsConnecting = false;
        public bool IsConnected = false;
        public bool StopReconnect = false;
        public bool IsNormallyStopped = false;

        //public enum Status {
        //    Connected, ConnectedAutomatically, ConnectedByReconnect, ConnectedByUser,
        //    Disconnected, DisconnectedByUser, DisconnectedByError
        //}

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
            Thread.Sleep(10);
            isLastSuccess = false;
            toReconnect = true;
            IsConnecting = true;
            IsNormallyStopped = false;
            portCloseCount = 0;

            Started();
            process = new Process();

            var si = process.StartInfo;
            si.FileName = settings.PlinkFileName;
            si.Arguments = String.Format(
                "-v -x -a -T -N{0} -l {1} -pw {2} -P {3} -D {4} {5}",
                settings.SshCompression ? " -C" : "",
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
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            connectTimer = new Timer((o) => {
                process.Kill();
            }, null, settings.SshConnectTimeout, Timeout.Infinite);

            process.WaitForExit();

            if (connectTimer != null)
                connectTimer.Dispose();

            IsConnected = false;
            IsConnecting = false;
            Disconnected(isLastSuccess, isReconnect);

            if (toReconnect)
                reconnect();
        }

        public void Start() {
            if (!CheckSettings()) return;
            reconnectPeriod = 1;
            Stop();
            new Action(() => {
                Thread.Sleep(100);
                startProcess();
            }).BeginInvoke(null, null);
        }

        public void Stop(bool toReconnect = false) {
            IsNormallyStopped = true;
            this.toReconnect = toReconnect;

            if (process != null && !process.HasExited)
                process.Kill();
        }

        void reconnect() {
            if (IsReconnecting) {
                new Action(() => {
                    Thread.Sleep(1000);
                    reconnect();
                }).BeginInvoke(null, null);
                return;
            }

            IsReconnecting = true;

            if (settings.SshAutoReconnect && Error == null) {
                StopReconnect = false;

                new Action(() => {
                    Thread.Sleep(500);

                    var time = reconnectPeriod;

                    if (reconnectPeriod < settings.MaxReconnectPeriod)
                        reconnectPeriod *= 2;

                    while (time > 0) {
                        if (StopReconnect) {
                            IsReconnecting = false;
                            Disconnected(false, true);
                            return;
                        }

                        ReconnectCountingDown(time--);
                        Thread.Sleep(1000);
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
            }
        }

        void onOutputDataReceived(object sender, DataReceivedEventArgs e) {
            //Console.WriteLine("D: " + e.Data);
        }

        void onErrorDataReceived(object sender, DataReceivedEventArgs e) {
            var line = e.Data;
            if (line == null) return;

            //Console.WriteLine("E: " + line);

            if (new Regex(@"^Local port .+ SOCKS dynamic forwarding").IsMatch(line)) {
                reconnectPeriod = 1;
                isLastSuccess = true;
                IsConnected = true;
                IsConnecting = false;
                if (connectTimer != null)
                    connectTimer.Dispose();
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
                Stop();
            }
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
