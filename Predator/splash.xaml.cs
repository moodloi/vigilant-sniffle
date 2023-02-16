using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using zip;

namespace Predator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class splash : Window
    {
        private static readonly string updater = h.env("%temp%/updater.exe");
        private bool _shown;

        public bool splash_shown;

        public splash()
        {
            var args = Environment.GetCommandLineArgs();
            var u = args.Length > 2 && Directory.Exists(args[1]) && args[2].Contains("http")
                ? update(args[1], args[2])
                : update();
            if (u == r.updating || u == r.updated) Close();

            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown) return;

            _shown = true;


            var showAuth = true;
            var email = "";
            new Thread(() =>
            {
                //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var r_join = HardwareInfo.GetHDDSerialNo() + "||" + HardwareInfo.CPU() + "||" +
                             HardwareInfo.DiskSize() + "||" + HardwareInfo.VideoAdapter() + "||" +
                             HardwareInfo.ResolutionH() + "||" + HardwareInfo.RefreshRate() + "||" +
                             HardwareInfo.GetMACAddress() + "||" + HardwareInfo.GetPhysicalMemory() + "||" +
                             HardwareInfo.GetComputerName() + "||" + HardwareInfo.OSname();
                Directory.CreateDirectory(Path.GetDirectoryName(h.hwdata));
                File.WriteAllText(h.hwdata, r_join);

                if (File.Exists(h.credientials))
                    foreach (var item in File.ReadAllLines(h.credientials))
                    {
                        if (!item.Contains(":")) continue;
                        var data = item.Split(':');
                        email = data[0];
                        var pass = crypt.Decrypt(data[1]);
                        if (data.Length < 3 || !h.IsValidEmail(email) || email.Length > 50 || pass.Length > 25 ||
                            pass.Length < 5 || !h.connect(email, pass, false, false))
                        {
                        }
                        else
                        {
                            showAuth = false;
                            break;
                        }
                    }
            }).Start();


            var t = new Thread(() =>
            {
                Thread.Sleep(3000);
                splash_shown = true;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    ShowInTaskbar = false;
                    Hide();
                    if (showAuth) auth(email);
                    else launcher(email);
                    Close();
                }));
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private static void auth(string email)
        {
            var Auth = new auth();
            Auth.ShowDialog();
            if (Auth.showLauncher) launcher(email);
        }

        private static void launcher(string email)
        {
            var Launcher = new launcher();
            Launcher.email_lbl.Text = email.Length > Launcher.maxEmailLength
                ? email.Substring(0, Launcher.maxEmailLength - 1) + "..."
                : email;
            Launcher.ShowDialog();
            if (Launcher.showAuth) auth(email);
        }

        private static int update(string directory = null, string link = null)
        {
            using (var web = new WebClient())
            {
                if (directory != null && link != null)
                {
                    using (var stream = new MemoryStream(web.DownloadData(link)))
                    {
                        var extractor = ZipStorer.Open(stream, FileAccess.Read); // stream-oriented version
                        foreach (var entry in extractor.ReadCentralDir())
                        {
                            var file = Path.Combine(directory, entry.FilenameInZip);
                            if (File.Exists(file)) File.Delete(file);
                            extractor.ExtractFile(entry, file);
                            if (file.EndsWith(".exe")) Process.Start(file);
                        }
                    }

                    return r.updated;
                }

                var executable = Assembly.GetEntryAssembly().Location;
                var current_md5 = MD5(File.ReadAllBytes(executable));
                var update_md5 = current_md5;
                var mandatory = false;
                var zip = "";
                var xml = new XmlDocument();
                xml.LoadXml(web.DownloadString("https://predator.ge/launcher/launcher.xml"));
                foreach (XmlNode main in xml.GetElementsByTagName("launcher"))
                foreach (XmlNode node in main.ChildNodes)
                {
                    var name = "";
                    var value = "";
                    foreach (XmlAttribute attr in node.Attributes)
                        if (attr.Name == "name") name = attr.Value.ToLower();
                        else if (attr.Name == "value") value = attr.Value.ToLower();
                    if (name == "md5") update_md5 = value;
                    else if (name == "zip") zip = value;
                    else if (name == "mandatory") mandatory = value == "yes";
                }

                if (current_md5 != update_md5)
                {
                    var signal = MessageBoxResult.Yes;
                    if (!mandatory)
                        signal = MessageBox.Show("An update is available. Would you like to install ?",
                            "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (signal == MessageBoxResult.Yes)
                    {
                        File.Copy(executable, updater, true);
                        Process.Start(updater, $"\"{Environment.CurrentDirectory}\" \"{zip}\"");
                        return r.updating;
                    }
                }
            }

            return r.no_update;
        }


        public static string MD5(byte[] input)
        {
            using (var provider = System.Security.Cryptography.MD5.Create())
            {
                var builder = new StringBuilder();
                foreach (var b in provider.ComputeHash(input)) builder.Append(b.ToString("x2").ToLower());
                return builder.ToString();
            }
        }

        public class r
        {
            public static int no_update = 0;
            public static int updating = 1;
            public static int updated = 2;
        }
    }
}