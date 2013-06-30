using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using XWall.Properties;

namespace XWall {

    public class WebClientWithCookies : WebClient {
        public static readonly CookieContainer CookieContainer = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address) {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null) {
                webRequest.CookieContainer = CookieContainer;
            }
            return request;
        }
    }

    class GAESimulator {
        static Settings settings = Settings.Default;
        static ResourceDictionary resources = App.Current.Resources;

        bool stopped = false;

        static NameValueCollection initQuery(string html) {
            var query = HttpUtility.ParseQueryString("");

            var re = new Regex(@"<input type=""hidden""[^>]+name=""(.+?)""[^>]+value=[""'](.*?)[""']");
            var matches = re.Matches(html);

            foreach (Match match in matches) {
                query[HttpUtility.HtmlDecode(match.Groups[1].Value)] = HttpUtility.HtmlDecode(match.Groups[2].Value);
            }

            return query;
        }

        public class LoginCallbackData {
            public string[] AppIds;
            public int RemainingNumber;
        }
        public delegate void LoginCallback(bool success, LoginCallbackData data);
        public void Login(string email, string password, LoginCallback callback) {
            var client = new WebClientWithCookies();

            var loginHomeUrl = "https://accounts.google.com/ServiceLogin?service=ah&passive=true&continue=https://appengine.google.com/_ah/conflogin%3Fcontinue%3Dhttps://appengine.google.com/start&ltmpl=ae";
            var loginUrl = "https://accounts.google.com/ServiceLoginAuth";
            var userRe = new Regex(@"id=""ae-userinfo"">[\s\S]+?<strong>\s*([^\s]+?@[^\s]+?)\s*</strong>");
            
            client.DownloadStringAsync(new Uri(loginHomeUrl));
            client.DownloadStringCompleted += (sender, e) => {
                if (e.Error != null) {
                    callback(false, null);
                    return;
                }

                var processHtml = new Action<string>((h) => {
                    if (!userRe.IsMatch(h)) {
                        callback(false, null);
                        return;
                    }

                    int remaining;

                    var remainingMatch = new Regex(@"(\d+) applications remaining").Match(h);
                    if (remainingMatch.Success) {
                        remaining = int.Parse(remainingMatch.Groups[1].Value);
                    }
                    else {
                        remaining = 10;
                    }

                    var appIds = new List<string>();
                    var idMatches = new Regex(@"<tr[\s\S]+?&app_id=s~(.+?)""[\s\S]*?</tr>").Matches(h);
                    foreach (Match match in idMatches) {
                        if (!match.Value.Contains("<strong>Disabled")) {
                            Console.WriteLine(match.Groups[1].Value);
                            appIds.Add(match.Groups[1].Value);
                        }
                    }

                    callback(true, new LoginCallbackData() { AppIds = appIds.ToArray(), RemainingNumber = remaining });
                });

                var processLogin = new Action<string>((h) => {
                    var query = initQuery(h);

                    query["Email"] = email;
                    query["Passwd"] = password;

                    query["PersistentCookie"] = "yes";

                    var cl = new WebClientWithCookies();
                    cl.UploadValuesAsync(new Uri(loginUrl), query);
                    cl.UploadValuesCompleted += (s, o) => {
                        processHtml(Encoding.UTF8.GetString(o.Result));
                    };
                });

                var html = e.Result;

                var userMatch = userRe.Match(html);

                if (userMatch.Success) {
                    var user = userMatch.Groups[1].Value;
                    if (user.ToLower() == email.ToLower()) {
                        processHtml(html);
                    }
                    else {
                        loginHomeUrl = "https://appengine.google.com/_ah/logout?continue=https://www.google.com/accounts/Logout%3Fcontinue%3Dhttps://appengine.google.com/_ah/logout%253Fcontinue%253Dhttps://appengine.google.com/start%26service%3Dah";

                        var cl = new WebClientWithCookies();

                        cl.DownloadStringAsync(new Uri(loginHomeUrl));
                        cl.DownloadStringCompleted += (ss, ee) => {
                            if (ee.Error != null) {
                                callback(false, null);
                            }
                            else {
                                processLogin(ee.Result);
                            }
                        };
                    }
                }
                else {
                    processLogin(html);
                }
            };
        }

        public delegate void CreateAppsCallback(bool done, int i, string id, bool error = false);
        public void CreateApps(string prefix, int number, CreateAppsCallback callback) {
            var current = 0;
            var i = 0;

            NameValueCollection query = null;
            CheckAppIdCallback next = null;

            next = new CheckAppIdCallback((valid) => {
                current++;

                if (valid) {
                    i++;
                }

                if (i >= number) {
                    callback(true, i, null);
                    return;
                }

                var id = prefix + "-" + current;

                callback(false, i, id);

                CheckAppIdAvailability(id, (v) => {
                    if (v) {
                        query["app_id"] = id;
                        query["title"] = "GoAgent Server " + current;
                        query["tos_accept"] = "on";

                        var cl = new WebClientWithCookies();
                        cl.UploadValuesAsync(new Uri("https://appengine.google.com/start/createapp.do"), query);
                        cl.UploadValuesCompleted += (sender, e) => {
                            next(true);
                        };
                    }
                    else {
                        next(false);
                    }
                });
            });

            Action prefetch = null;
            prefetch = new Action(() => {
                var client = new WebClientWithCookies();
                client.DownloadStringAsync(new Uri("https://appengine.google.com/start/createapp"));
                client.DownloadStringCompleted += (sender, e) => {
                    if (e.Result.Contains("IdvPhoneType()")) {
                        var r = MessageBox.Show(resources["GaeBeforeVerifyMessage"] as string, resources["XWall"] as string, MessageBoxButton.OKCancel);

                        if (r == MessageBoxResult.OK) {
                            Process.Start("\"https://accounts.google.com/ServiceLogin?continue=https%3A%2F%2Faccounts.google.com%2Fb%2F0%2FIdvChallenge%3FidvContinueHandler%3DSERVICE%26service%3Dah\"");
                            r = MessageBox.Show(resources["GaeAfterVerifyMessage"] as string, resources["XWall"] as string, MessageBoxButton.OKCancel);

                            if (r == MessageBoxResult.OK) {
                                prefetch();
                            }
                            else {
                                callback(false, 0, null, true);
                            }
                        }
                        else {
                            callback(false, 0, null, true);
                        }
                    }
                    else {
                        query = initQuery(e.Result);
                        query["auth_config"] = "google";
                        next(false);
                    }
                };
                

            });

            prefetch();
        }

        public delegate void CheckAppIdCallback(bool valid);
        public void CheckAppIdAvailability(string id, CheckAppIdCallback callback) {
            var client = new WebClientWithCookies();
            var url = "https://appengine.google.com/start/checkappid?app_id=" + HttpUtility.UrlEncode(id);
            client.DownloadStringAsync(new Uri(url));
            client.DownloadStringCompleted += (sender, e) => {
                if (e.Error != null) {
                    callback(false);
                }
                else {
                    callback(!e.Result.Contains("invalid"));
                }
            };
        }

        public delegate void DeployChallback(bool done, int i, double currentProgress, bool error = false);
        public void Deploy(string email, string password, string[] appIds, DeployChallback callback) {
            new Action(() => {
                try {
                    File.Delete(App.AppDataDirectory + settings.GaUploaderCookieFileName);
                }
                catch {
                    callback(false, 0, 0, true);
                    return;
                }

                var i = 0;
                for (; i < appIds.Length;) {
                    if (stopped) {
                        return;
                    }

                    var info = new ProcessStartInfo(App.AppDataDirectory + settings.GaPython27FileName);
                    info.Arguments = "-u \"" + App.AppDataDirectory + settings.GaUploaderFileName + "\" " + email + " \"" + password.Replace("\"", "\"\"") + "\" " + appIds[i];

                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
                    info.RedirectStandardInput = true;
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;

                    var process = new Process();
                    callback(false, i, 0);

                    Thread.Sleep(1000);

                    var steps = 17;
                    var step = 0;

                    process.ErrorDataReceived += (sender, e) => {
                        step++;
                        if (e.Data != null && e.Data.StartsWith("Completed update of app:")) {
                            i++;
                        }
                        else {
                            callback(false, i, Math.Min(step * 1.0 / steps, 1));
                        }
                    };

                    process.StartInfo = info;
                    process.Start();

                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }

                callback(true, i, 0);
                try {
                    File.Delete(App.AppDataDirectory + settings.GaUploaderCookieFileName);
                }
                catch { }
            }).BeginInvoke(null, null);
        }

        internal void Stop() {
            stopped = true;
            Operation.KillProcess(App.AppDataDirectory + settings.GaPython27FileName);
        }
    }
}
