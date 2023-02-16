using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Label = System.Windows.Controls.Label;

namespace Predator
{
    public class h
    {
        public static string account = "http://predator.ge/launcher/execute.php";
        public static string domain = "https://predator.ge/launcher/";
        public static string slider = "https://predator.ge/launcher/slider/";
        public static string credientials = env("%AppData%/predator/" + "helper.dll");
        public static string installed = env("%AppData%/predator/" + "installed_games.txt");
        public static string images = env("%AppData%/predator/" + "images");
        public static string auth = env("%AppData%/predator/" + "auth");
        public static string hwdata = env("%AppData%/predator/" + "hwdata.dll");
        public static string error_txt = env("%AppData%/predator/" + "error.txt");
        public static string box1 = "";
        public static string box2 = "";
        public static string box3 = "";
        public static Color orange = Color.FromArgb(255, 157, 0);

        public static string env(string input)
        {
            return Environment.ExpandEnvironmentVariables(input);
        }

        public static bool IsValidEmail(string strIn)
        {
            return Regex.IsMatch(strIn,
                @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public static bool connect(string email, string pass, bool remember, bool alert = true, Label response = null)
        {
            var error = "";
            if (email.Length > 16) error = "Username must be less than 16 characters";
            else if (email.Length < 4) error = "Username must be at least 4 characters";
            else if (pass.Length > 16) error = "Password must be less than 16 characters";
            else if (pass.Length < 4) error = "Please, specify a valid password";
            if (error != string.Empty)
            {
                if (response != null)
                {
                    response.Content = error;
                    response.Foreground = Brushes.Red;
                }
                else if (alert)
                {
                    Warn(error);
                }

                return false;
            }

            using (var web = new WebClient())
            {
                var hwdata = File.ReadAllText(h.hwdata);
                var url = $"{account}?action=accessAccount&username={UrlEncode(email)}&password={UrlEncode(pass)}";
                var r = web.DownloadString(url);
                if (r.Contains("ERROR:"))
                {
                    if (r.Contains("MISSING_PARAMETERS")) error = "Missing parameters";
                    else if (r.Contains("INVALID_CREDENTIALS")) error = "Credientials are invalid";
                    if (error != string.Empty)
                    {
                        if (response != null)
                        {
                            response.Content = error;
                            response.Foreground = Brushes.Red;
                        }
                        else if (alert)
                        {
                            Warn(error);
                        }

                        return false;
                    }
                }
                else if (r.Contains("OK:LOGGED_IN"))
                {
                    if (!File.Exists(credientials))
                    {
                        File.WriteAllText(credientials, $"{email}:{crypt.Encrypt(pass)}:1\n");
                    }
                    else if (remember)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(credientials));
                        var alreadyexists = false;
                        if (File.Exists(credientials))
                        {
                            var data = "";
                            var lines = File.ReadAllLines(credientials);
                            foreach (var line in lines)
                                if (line.Contains(":"))
                                {
                                    var email2 = line.Split(':')[0];
                                    var pass2 = line.Split(':')[1];
                                    if (email2 == email)
                                    {
                                        alreadyexists = true;
                                        data += $"{email2}:{pass2}:1\n";
                                    }
                                    else
                                    {
                                        data += $"{email2}:{pass2}\n";
                                    }
                                }

                            if (!alreadyexists)
                                File.WriteAllText(credientials, data + $"{email}:{crypt.Encrypt(pass)}:1\n");
                            else File.WriteAllText(credientials, data);
                        }
                        else
                        {
                            File.WriteAllText(credientials, $"{email}:{crypt.Encrypt(pass)}:1\n");
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public static void Warn(string msg)
        {
            MessageBox.Show(msg, "Warning", 0, MessageBoxIcon.Warning);
        }

        public static DialogResult assure(string msg)
        {
            return MessageBox.Show(msg, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        }

        public static int GetStatusCode(WebClient client)
        {
            var responseField =
                client.GetType().GetField("m_WebResponse", BindingFlags.Instance | BindingFlags.NonPublic);
            var statusDescription = "";
            if (responseField != null)
            {
                var response = responseField.GetValue(client) as HttpWebResponse;

                if (response != null)
                {
                    statusDescription = response.StatusDescription;
                    return (int)response.StatusCode;
                }
            }

            statusDescription = null;
            return 0;
        }

        public static void log(Exception ex)
        {
            File.WriteAllText(error_txt, ex.ToString());
        }

        public static string UrlEncode(string value)
        {
            var chars = value.ToCharArray();
            var encodedValue = new StringBuilder();
            foreach (var c in chars) encodedValue.Append("%" + ((int)c).ToString("X2"));
            return encodedValue.ToString();
        }

        public static string link2img(string link, int index, string media) //,int chunks)
        {
            var filename = link.Split('/')[link.Split('/').Length - 1];
            var filepath = Path.Combine(images, $"{index}/{media}/{filename}");

            Directory.CreateDirectory(images);
            if (File.Exists(filepath) && new FileInfo(filepath).Length == Downloader.getFileSize(link)) return filepath;
            var r = Downloader.Download(link, Path.GetDirectoryName(filepath));


            return r.FilePath;
        }

        public static BitmapImage resource2img(string name)
        {
            return new BitmapImage(new Uri(@"pack://application:,,,/images/" + name + ".png", UriKind.Absolute));
        }

        public static lst_parts line2list(string line)
        {
            var parts = line.Split('|');
            return new lst_parts
            {
                file_path = parts[0],
                file_size = long.Parse(parts[1]),
                file_hash = long.Parse(parts[2]),
                zip_file_size = long.Parse(parts[3]),
                zip_file_hash = long.Parse(parts[4])
            };
        }
    }

    public class set
    {
        public string Cover;
        public string Description = "";
        public string Executable = "";
        public string Icon;

        public string Name = "";
        public string Profile;
        public string URL = "";
    }

    public class mediaType
    {
        public static string icon = "icon";
        public static string profile = "profile";
        public static string cover = "cover";
    }

    public class lst_parts
    {
        public long file_hash;
        public string file_path = "";
        public long file_size;
        public long zip_file_hash;
        public long zip_file_size;
    }

    public class install
    {
        public string folder = null;
        public bool installed = false;
    }

    public class btn_state
    {
        public static int install = 0;
        public static int cancel = 1;
        public static int play = 2;
    }

    public class state
    {
        public int btn_install;
        public bool cancellation;
        public string filename = "";
        public int width = 0;
    }
}