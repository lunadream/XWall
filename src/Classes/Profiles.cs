using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using XWall.Properties;

namespace XWall {
    class Profile {
        static Settings settings = Settings.Default;

        public abstract class DefaultProfile {
            public string Name;
            public override string ToString() {
                return Name;
            }
            public abstract string ToSettingString();
        }

        public class SshProfilesCollection {
            public BindingList<DefaultProfile> Items;

            public SshProfilesCollection(string settingName) {
                Items = new BindingList<DefaultProfile>();

                var str = settings[settingName] as string;

                if (str != "") {
                    var profileStrs = str.Split('\n');
                    for (int i = 0; i < profileStrs.Length; i++) {
                        var profile = new SshProfile(profileStrs[i]);
                        Items.Add(profile);
                    }
                }

                Items.ListChanged += (sender, e) => {
                    var profileStrs = new List<string>();
                    for (int i = 0; i < Items.Count; i++) {
                        profileStrs.Add(Items[i].ToSettingString());
                    }
                    var newSetting = String.Join("\n", profileStrs.ToArray());

                    if (settings[settingName] as string != newSetting) {
                        settings[settingName] = newSetting;
                    }
                };
            }
        }

        public class SshProfile : DefaultProfile {
            public string Server;
            public int Port;
            public string Username;
            public string Password;
            public SshProfile(string info = "") {
                if (String.IsNullOrEmpty(info)) {
                    Name = "";
                    Server = "";
                    Port = 22;
                    Username = "";
                    Password = "";
                }
                else {
                    var infos = info.Split('\t');
                    Name = infos[0];
                    Server = infos[1];
                    Port = int.Parse(infos[2]);
                    Username = infos[3];
                    Password = infos[4];
                }
            }
            public SshProfile(SshProfile profile) {
                Name = profile.Name;
                Server = profile.Server;
                Port = profile.Port;
                Username = profile.Username;
                Password = profile.Password;
            }
            public override string ToSettingString() {
                return String.Join("\t", new string[] { Name, Server, Port.ToString(), Username, Password });
            }
        }
    }
}
