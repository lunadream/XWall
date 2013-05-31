using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using XWall.Properties;

namespace XWall {
    static class Operation {
        static Settings settings = Settings.Default;

        public static void KillProcess(string fileName) {
            var processes = GetProcesses(fileName);
            foreach (var process in processes)
                process.Kill();
        }

        public static Process[] GetProcesses(string fileName) {
            var path = Path.GetFullPath(fileName).ToLower();
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fileName));
            var results = new List<Process>();
            var current = Process.GetCurrentProcess();
            foreach (var process in processes) {
                if (process.Id != current.Id && Path.GetFullPath(process.MainModule.FileName).ToLower() == path)
                    results.Add(process);
            }
            return results.ToArray();
        }

        public static void SetAvailablePorts() {
            var process = new Process();
            var si = process.StartInfo;
            si.FileName = "netstat";
            si.Arguments = "-an";
            si.RedirectStandardOutput = true;
            si.CreateNoWindow = true;
            si.UseShellExecute = false;

            process.Start();
            var portsStr = process.StandardOutput.ReadToEnd();

            if (process != null && !process.HasExited)
                process.Kill();

            var hash = new HashSet<int>();

            foreach (Match match in new Regex(@"^\s*\w+\s+[^\s]+:(\d+)", RegexOptions.Multiline).Matches(portsStr))
                hash.Add(int.Parse(match.Groups[1].Value));

            settings.ProxyPort = findAvailablePort(hash, settings.ProxyPort);
            hash.Add(settings.ProxyPort);
            settings.SshSocksPort = findAvailablePort(hash, settings.SshSocksPort);
        }

        static int findAvailablePort(HashSet<int> hash, int start) {
            var port = start;
            while (hash.Contains(port))
                port++;
            return port;
        }

        public static bool SetAutoStart(bool autoStart) {
            var dir = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            var valueName = "X-Wall";
            try {
                var reg = Registry.CurrentUser.CreateSubKey(dir);

                if (autoStart) {
                    var value = "\"" + System.Windows.Forms.Application.ExecutablePath + "\"";
                    reg.SetValue(valueName, value);
                }
                else if (reg.GetValue(valueName) != null) {
                    reg.DeleteValue(valueName);
                }

                return autoStart;
            }
            catch {
                return false;
            }
        }

        //public static bool RegisterXWallProtocol(bool toggle) {
        //    var root = Registry.ClassesRoot;

        //    try {
        //        if (toggle) {
        //            var xwallKey = root.CreateSubKey("xwall");
        //            xwallKey.SetValue("URL Protocol", "");
        //            var commandKey = xwallKey.CreateSubKey(@"shell\open\command");
        //            var value = "\"" + System.Windows.Forms.Application.ExecutablePath + "\" \"%1\"";
        //            commandKey.SetValue("", value);
        //        }
        //        else {
        //            root.DeleteSubKeyTree("xwall");
        //        }
        //        return toggle;
        //    }
        //    catch {
        //        return false;
        //    }
        //}

        // Created by Joel 'Jaykul' Bennett
        // http://huddledmasses.org/setting-windows-internet-connection-proxy-from-c/
        // Modified by VILIC VANE to support setting proxies of all connections, and restoring proxy settings
        public static class Proxies {
            public static string DefaultProxy { get; private set; }

            static class OriginalProxies {
                static Dictionary<string, ProxyInfo> list = new Dictionary<string, ProxyInfo>();
                public static ProxyInfo[] List;

                static bool initialized = false;

                public static void Initialize() {
                    if (initialized) return;
                    initialized = true;

                    var xwallProxy = "127.0.0.1:" + settings.ProxyPort;

                    var cachedList = new Dictionary<string, ProxyInfo>();
                    
                    var proxiesStr = settings.OriginalProxies;
                    if (!string.IsNullOrEmpty(proxiesStr)) {
                        var proxyStrs = proxiesStr.Split('\n');
                        foreach (var proxyStr in proxyStrs) {
                            var infos = proxyStr.Split('\t');
                            cachedList.Add(infos[0], new ProxyInfo() {
                                ConnectionName = infos[0],
                                Flags = int.Parse(infos[1]),
                                Proxy = infos[2]
                            });
                        }
                    }

                    var connections = getConnections();
                    foreach (var connection in connections) {
                        var info = GetSingleProxy(connection);
                        //If it seems that the proxy is set by x-wall.
                        if (info.Proxy == xwallProxy && info.Flags == (int)(PerConnFlags.PROXY_TYPE_DIRECT | PerConnFlags.PROXY_TYPE_PROXY)) {
                            if (cachedList.ContainsKey(connection)) {
                                var cachedInfo = cachedList[connection];
                                info.Proxy = cachedInfo.Proxy;
                                info.Flags = cachedInfo.Flags;
                            }
                            else {
                                info.Proxy = null;
                                info.Flags = 0;
                            }
                        }
                        list.Add(connection, info);
                    }

                    List = list.Values.ToArray();

                    var defaultInfo = List[0];

                    if ((defaultInfo.Flags & (int)PerConnFlags.PROXY_TYPE_PROXY) != 0) {
                        var match = new Regex(@"^(?:http=)?(.+?)(?=;|$)").Match(defaultInfo.Proxy);
                        var defaultProxy = match.Groups[1].Value;
                        if (defaultProxy != xwallProxy)
                            DefaultProxy = defaultProxy;
                        System.Windows.Forms.MessageBox.Show(defaultInfo.Proxy);
                        System.Windows.Forms.MessageBox.Show(defaultInfo.Flags.ToString());
                    }

                    var strs = new List<string>();
                    foreach (var item in list)
                        strs.Add(item.Value.ConnectionName + "\t" + item.Value.Flags + "\t" + item.Value.Proxy);
                    settings.OriginalProxies = String.Join("\n", strs.ToArray());
                }
            }

            static bool SetSingleProxy(string connection, string proxy = "", int flags = 0) {
                var list = new InternetPerConnOptionList();

                var options = new InternetConnectionOption[2];
                // USE a proxy server ...
                options[0] = new InternetConnectionOption();
                options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS;
                options[0].m_Value.m_Int = flags != 0 ? flags : (int)(String.IsNullOrEmpty(proxy) ? PerConnFlags.PROXY_TYPE_DIRECT : (PerConnFlags.PROXY_TYPE_DIRECT | PerConnFlags.PROXY_TYPE_PROXY));
                // use THIS proxy server
                options[1] = new InternetConnectionOption();
                options[1].m_Option = PerConnOption.INTERNET_PER_CONN_PROXY_SERVER;
                options[1].m_Value.m_StringPtr = Marshal.StringToHGlobalAuto(proxy);

                // default stuff
                list.dwSize = Marshal.SizeOf(list);
                list.pszConnection = Marshal.StringToHGlobalAuto(connection);
                list.dwOptionCount = options.Length;
                list.dwOptionError = 0;

                var optSize = Marshal.SizeOf(typeof(InternetConnectionOption));
                // make a pointer out of all that ...
                var optionsPtr = Marshal.AllocCoTaskMem(optSize * options.Length);
                // copy the array over into that spot in memory ...
                for (var i = 0; i < options.Length; i++) {
                    var opt = new IntPtr(optionsPtr.ToInt64() + (i * optSize));
                    Marshal.StructureToPtr(options[i], opt, false);
                }

                list.pOptions = optionsPtr;

                // and then make a pointer out of the whole list
                var ipcoListPtr = Marshal.AllocCoTaskMem((Int32)list.dwSize);
                Marshal.StructureToPtr(list, ipcoListPtr, false);

                // and finally, call the API method!
                var success = NativeMethods.InternetSetOption(IntPtr.Zero, (int)InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION, ipcoListPtr, list.dwSize);

                // FREE the data ASAP
                Marshal.FreeCoTaskMem(optionsPtr);
                Marshal.FreeCoTaskMem(ipcoListPtr);

                return success;
            }

            static ProxyInfo GetSingleProxy(string connection) {
                var list = new InternetPerConnOptionList();

                var options = new InternetConnectionOption[]{
                    new InternetConnectionOption(){
                        m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS
                    },
                    new InternetConnectionOption(){
                        m_Option = PerConnOption.INTERNET_PER_CONN_PROXY_SERVER
                    }
                };

                // default stuff
                list.dwSize = Marshal.SizeOf(list);
                if (!string.IsNullOrEmpty(connection))
                    list.pszConnection = Marshal.StringToHGlobalAuto(connection);
                list.dwOptionCount = options.Length;
                list.dwOptionError = 0;

                var optSize = Marshal.SizeOf(typeof(InternetConnectionOption));
                var optionsPtr = Marshal.AllocCoTaskMem(optSize * options.Length);
                // copy the array over into that spot in memory ...
                for (var i = 0; i < options.Length; i++) {
                    var opt = new IntPtr(optionsPtr.ToInt32() + (i * optSize));
                    Marshal.StructureToPtr(options[i], opt, false);
                }

                list.pOptions = optionsPtr;

                // and then make a pointer out of the whole list
                var ipcoListPtr = Marshal.AllocCoTaskMem(list.dwSize);
                Marshal.StructureToPtr(list, ipcoListPtr, false);

                // and finally, call the API method!
                var success = NativeMethods.InternetQueryOption(IntPtr.Zero, (int)InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION, ipcoListPtr, ref list.dwSize);

                Marshal.PtrToStructure(ipcoListPtr, list);
                Marshal.PtrToStructure(list.pOptions, options[0]);
                Marshal.PtrToStructure(new IntPtr(list.pOptions.ToInt32() + optSize), options[1]);

                var proxyInfo = new ProxyInfo() {
                    ConnectionName = connection,
                    Flags = options[0].m_Value.m_Int,
                    Proxy = Marshal.PtrToStringAuto(options[1].m_Value.m_StringPtr)
                };

                // FREE the data ASAP
                Marshal.FreeCoTaskMem(optionsPtr);
                Marshal.FreeCoTaskMem(ipcoListPtr);

                return proxyInfo;
            }

            public static void SetXWallProxy() {
                SetProxy("127.0.0.1:" + settings.ProxyPort);
            }

            public static bool SetProxy(string proxy) {
                OriginalProxies.Initialize();

                var connections = getConnections();
                var success = true;

                foreach (var connection in connections) {
                    if (!SetSingleProxy(connection, proxy))
                        success = false;
                }

                NativeMethods.InternetSetOption(IntPtr.Zero, (int)InternetOption.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                NativeMethods.InternetSetOption(IntPtr.Zero, (int)InternetOption.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

                return success;
            }

            public static bool RestoreProxy() {
                OriginalProxies.Initialize();
                if (OriginalProxies.List.Length == 0) return true;

                var success = true;
                var connections = getConnections();

                foreach (var info in OriginalProxies.List) {
                    if (connections.Contains(info.ConnectionName) && !SetSingleProxy(info.ConnectionName, info.Proxy, info.Flags))
                        success = false;
                }

                return success;
            }

            static string[] getConnections() {
                //var dir = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";
                //var key = Registry.CurrentUser.CreateSubKey(dir);
                //return key.GetValueNames();
                int lpNames = 1;
                int entryNameSize = 0;
                int lpSize = 0;

                RasEntryName[] names = null;

                entryNameSize = Marshal.SizeOf(typeof(RasEntryName));
                lpSize = lpNames * entryNameSize;

                names = new RasEntryName[lpNames];
                names[0].dwSize = entryNameSize;

                uint retval = NativeMethods.RasEnumEntries(null, null, names, ref lpSize, out lpNames);

                //if we have more than one connection, we need to do it again
                if (lpNames > 1) {
                    names = new RasEntryName[lpNames];
                    for (int i = 0; i < names.Length; i++) {
                        names[i].dwSize = entryNameSize;
                    }
                    retval = NativeMethods.RasEnumEntries(null, null, names, ref lpSize, out lpNames);
                }

                var connections = new List<string>() { "" };

                var length = lpNames > 0 ? names.Length : 0;

                for (int i = 0; i < length; i++) {
                    connections.Add(names[i].szEntryName);
                }

                return connections.ToArray();
            }

            class ProxyInfo {
                public string ConnectionName;
                public int Flags;
                public string Proxy;
            }
        }

        #region WinINet structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct RasEntryName      //define the struct to receive the entry name
        {
            public int dwSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256 + 1)]
            public string szEntryName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class InternetPerConnOptionList {
            public int dwSize;               // size of the INTERNET_PER_CONN_OPTION_LIST struct
            public IntPtr pszConnection;         // connection name to set/query options
            public int dwOptionCount;        // number of options to set/query
            public int dwOptionError;           // on error, which option failed
            public IntPtr pOptions;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class InternetConnectionOption {
            static readonly int Size;
            public PerConnOption m_Option;
            public InternetConnectionOptionValue m_Value;
            static InternetConnectionOption() {
                InternetConnectionOption.Size = Marshal.SizeOf(typeof(InternetConnectionOption));
            }

            // Nested Types
            [StructLayout(LayoutKind.Explicit)]
            public struct InternetConnectionOptionValue {
                // Fields
                [FieldOffset(0)]
                public System.Runtime.InteropServices.ComTypes.FILETIME m_FileTime;
                [FieldOffset(0)]
                public int m_Int;
                [FieldOffset(0)]
                public IntPtr m_StringPtr;
            }
        }
        #endregion

        #region WinINet enums
        //
        // options manifests for Internet{Query|Set}Option
        //
        public enum InternetOption : uint {
            INTERNET_OPTION_REFRESH = 37,
            INTERNET_OPTION_SETTINGS_CHANGED = 39,
            INTERNET_OPTION_PER_CONNECTION_OPTION = 75
        }

        //
        // Options used in INTERNET_PER_CONN_OPTON struct
        //
        public enum PerConnOption {
            INTERNET_PER_CONN_FLAGS = 1, // Sets or retrieves the connection type. The Value member will contain one or more of the values from PerConnFlags 
            INTERNET_PER_CONN_PROXY_SERVER = 2, // Sets or retrieves a string containing the proxy servers.  
            INTERNET_PER_CONN_PROXY_BYPASS = 3, // Sets or retrieves a string containing the URLs that do not use the proxy server.  
            INTERNET_PER_CONN_AUTOCONFIG_URL = 4//, // Sets or retrieves a string containing the URL to the automatic configuration script.
        }

        //
        // PER_CONN_FLAGS
        //
        [Flags]
        public enum PerConnFlags : int {
            PROXY_TYPE_DIRECT = 1 << 0,  // direct to net
            PROXY_TYPE_PROXY = 1 << 1,  // via named proxy
            PROXY_TYPE_AUTO_PROXY_URL = 1 << 2,  // autoproxy URL
            PROXY_TYPE_AUTO_DETECT = 1 << 3   // use autoproxy detection
        }
        #endregion

        internal static class NativeMethods {
            [DllImport("WinINet.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

            [DllImport("WinINet.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool InternetQueryOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, ref int lpdwBufferLength);

            [DllImport("rasapi32.dll", CharSet = CharSet.Auto)]
            public static extern uint RasEnumEntries(string reserved, string lpszPhonebook, [In, Out]RasEntryName[] lprasentryname, ref int lpcb, out int lpcEntries);
        }
    }
}