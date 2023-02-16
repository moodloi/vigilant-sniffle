using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using FlowDirection = System.Windows.FlowDirection;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using Label = System.Windows.Controls.Label;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Predator
{
    /// <summary>
    ///     Interaction logic for launcher.xaml
    /// </summary>
    public partial class launcher : Window
    {
        private readonly Dictionary<int, set> games = new Dictionary<int, set>();
        private readonly int increase_top = 96 / 2 + 40;

        private readonly List<BitmapSource> inverted = new List<BitmapSource>();


        private readonly Dictionary<int, state> states = new Dictionary<int, state>();
        private bool _shown;
        private hackyBrowser browser;


        private bool inProgress;
        public int maxEmailLength = 20;

        private int radius = 10;
        private double righ_panel_w;

        private int selected;

        public bool showAuth;


        private bool shown;


        private Thread thread1;
        private Thread thread2;
        private Thread thread3;
        private int top = 96 * 2;

        public launcher()
        {
            InitializeComponent();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown) return;
            _shown = true;

            launcher_Shown(this, null);
        }


        private void close_click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void minimize_click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
            if (browser != null) browser.Visibility = Visibility.Hidden;
        }

        private void launcher_Shown(object sender, EventArgs e)
        {
            thread1 = new Thread(() =>
            {
                using (var web = new WebClient())
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(web.DownloadString($"{h.domain}gamelist/Gamelist.xml"));
                    var games_xml = xml.GetElementsByTagName("game");
                    var i = 0;

                    thread2 = new Thread(() =>
                    {
                        var firstime = true;
                        foreach (XmlNode game_xml in games_xml)
                        {
                            var id = int.Parse(game_xml.Attributes[0].Value);
                            var game_name = game_xml.Attributes[1].Value;
                            var nodes = game_xml.ChildNodes;
                            var set = new set();
                            set.Name = game_name;

                            foreach (XmlNode node in game_xml.ChildNodes)
                            {
                                var name_att = node.Attributes[0];
                                var value_att = node.Attributes[1];

                                var name = name_att.Name.ToLower();
                                var value = name_att.Value.ToLower();
                                if (name == "name" && value == "icon")
                                    set.Icon = h.link2img(value_att.Value, i, mediaType.icon);
                                else if (name == "name" && value == "background image")
                                    set.Cover = h.link2img(value_att.Value, i, mediaType.cover);
                                else if (name == "name" && value == "smaller image")
                                    set.Profile = h.link2img(value_att.Value, i, mediaType.profile);
                                else if (name == "name" && value == "description") set.Description = value_att.Value;
                                else if (name == "name" && value == "url") set.URL = value_att.Value;
                                else if (name == "name" && value == "executable") set.Executable = value_att.Value;
                            }

                            games.Add(i, set);


                            Dispatcher.Invoke(() =>
                            {
                                var icon = new Button();
                                icon.Name = "icon" + i;
                                icon.Width = 96 / 2;
                                icon.Height = 96 / 2;
                                icon.Cursor = Cursors.Hand;
                                icon.Margin = new Thickness((left_panel.Width - icon.Width) / 2, top, 0, 0);
                                //  icon.Style = Resources["roundHoverableBtn"] as Style;
                                icon.Style = (Style)Application.Current.Resources["roundHoverableBtn"];
                                icon.HorizontalAlignment = HorizontalAlignment.Left;
                                icon.VerticalAlignment = VerticalAlignment.Top;
                                icon.Click += OnIconSelect;
                                icon.MouseEnter += icon_enter;
                                icon.MouseLeave += icon_leave;

                                var panel = new StackPanel();
                                panel.Name = "panel" + i;
                                panel.Width = 30;
                                panel.Height = 30;
                                panel.VerticalAlignment = VerticalAlignment.Center;
                                panel.HorizontalAlignment = HorizontalAlignment.Center;


                                var img = new Image();
                                img.Name = "box" + i;
                                img.Source = new BitmapImage(new Uri(set.Icon));
                                //    img.MouseLeftButtonDown += OnIconSelect;

                                panel.Children.Add(img);
                                icon.Content = panel;

                                main.Children.Add(icon);
                                top += increase_top;

                                var image = new BitmapImage(new Uri(set.Icon));

                                inverted.Add(Invert(image));
                                if (firstime)
                                {
                                    OnIconSelect(icon, null);
                                    firstime = false;
                                }
                            });

                            i++;
                        }
                    });
                    thread2.Start();
                }
            });
            thread1.Start();

            if (h.box1.Contains("http") || h.box2.Contains("http") || h.box3.Contains("http"))
            {
                var window = GetWindow(this);
                var wih = new WindowInteropHelper(window);
                var hWnd = wih.Handle;

                browser = new hackyBrowser(); // the WPF window instance
                var helper = new WindowInteropHelper(browser);
                helper.Owner = hWnd;

                browser.ShowInTaskbar = false; // hide from taskbar and alt-tab list
                Launcher_LocationChanged(null, null);
                LocationChanged += Launcher_LocationChanged;
                browser.Show();
            }
        }

        private Point pos()
        {
            return PointToScreen(Mouse.GetPosition(this));
        }


        private PointF GetWindowPosition()
        {
            if (WindowState == WindowState.Maximized)
            {
                var left = typeof(Window).GetField("_actualLeft", BindingFlags.NonPublic | BindingFlags.Instance);
                var top = typeof(Window).GetField("_actualTop", BindingFlags.NonPublic | BindingFlags.Instance);
                return new PointF((float)left.GetValue(this), (float)top.GetValue(this));
            }

            return new PointF((float)Left, (float)Top);
        }

        private void Launcher_LocationChanged(object sender, EventArgs e)
        {
            if (browser != null)
            {
                var position = GetWindowPosition();
                var left = position.X;
                var top = position.Y;

                browser.Width = Width - left_panel.Width - (right_panel.Width == 0 ? 200 : right_panel.Width);
                if (right_panel.Width == 0)
                    browser.Left = left + left_panel.Width + (Width - left_panel.Width - browser.Width) / 2;
                else
                    browser.Left = left + left_panel.Width;
                browser.Top = top + Height - browser.Height;
            }
        }


        private void icon_leave(object sender, MouseEventArgs e)
        {
            var icon = (Button)sender;
            var index = int.Parse(icon.Name.Replace("icon", ""));


            // var panel=     (System.Windows.Controls.StackPanel)icon.FindName("panel" + index);
            var panel = (StackPanel)icon.Content;
            // var img = (System.Windows.Controls.Image)panel.FindName("box" + index);
            var img = (Image)panel.Children[0];
            Dispatcher.Invoke(() => { img.Source = new BitmapImage(new Uri(games[index].Icon)); });
        }

        private void icon_enter(object sender, MouseEventArgs e)
        {
            var icon = (Button)sender;
            var index = int.Parse(icon.Name.Replace("icon", ""));


            // var panel=     (System.Windows.Controls.StackPanel)icon.FindName("panel" + index);
            var panel = (StackPanel)icon.Content;
            // var img = (System.Windows.Controls.Image)panel.FindName("box" + index);
            var img = (Image)panel.Children[0];
            Dispatcher.Invoke(() => { img.Source = inverted[index]; });
        }


        private void OnIconSelect(object sender, RoutedEventArgs e)
        {
            if (inProgress) return;
            var control = (Control)sender;
            selected = int.Parse(control.Name.Replace("icon", "").Replace("box", ""));
            string[] installed_games = { };
            if (File.Exists(h.installed)) installed_games = File.ReadAllLines(h.installed);
            var game = games[selected];
            //    int intervals = 3;
            //     var color = gameName.Foreground;


            /*
            new Thread(() =>
            {
                inProgress = true;
                //  Image cover=null;
                //  Invoke((MethodInvoker)delegate { cover= BackgroundImage; });

                /*if (BackgroundImage != null)
                {
                    var w = BackgroundImage.Width;
                    var h = BackgroundImage.Height;
                    var rect_cover_old = new Rectangle(new Point(0, 0), new Size(w,h));
                    var scaleW_old = w / intervals;
                    var scaleH_old = h / intervals;
                    index = 0;
                    while (rect_cover_old.Size.Height > 0 && rect_cover_old.Size.Width > 0)
                    {
                        index++;
                        Invoke((MethodInvoker)delegate {
                            //BackgroundImage = cropImage(cover, rect_cover_old);
                             CropBitmap((Bitmap)BackgroundImage, rect_cover_old);

                        });
                        if (index > speed)
                        {
                            Thread.Sleep(1);
                            index = 0;
                        }
                        rect_cover_old.Size = new Size(rect_cover_old.Size.Width - scaleW_old, rect_cover_old.Size.Height - scaleH_old);
                    }
                   
                } 

                var cover = new System.Windows.Media.Imaging.BitmapImage(new Uri(game.Cover));


                 
                var rect_cover = new Int32Rect(0, 0, 1, 1);
                var scaleW_cover = (int)(cover.Width / intervals);
                var scaleH_cover = (int)( cover.Height / intervals);
                while (rect_cover.Height < cover.Height && rect_cover.Width < cover.Width)
                {
                    Dispatcher.Invoke(() => {

                        ImageBrush brush = new ImageBrush();
                        // brush.ImageSource = CropBitmap(cover, rect_cover);


                        brush.ImageSource = new CroppedBitmap(new System.Windows.Media.Imaging.BitmapImage(new Uri(game.Cover)), rect_cover);
                        main.Background = brush;
                    });
                    cover = new System.Windows.Media.Imaging.BitmapImage(new Uri(game.Cover));
                    Thread.Sleep(1);
                    rect_cover.Width = rect_cover.Width - 1 + scaleW_cover;
                    rect_cover.Width = rect_cover.Height - 1 + scaleH_cover;
                }

            }).Start();
        */


            /*
             <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation
                            BeginTime="0:0:0"
                            Duration="0:0:0.5"
                            From="1"
                            To="0"
                            Storyboard.TargetProperty="(Image.Opacity)"
                            />
                        </Storyboard>
                    </BeginStoryboard>             
             */


            // public DoubleAnimation(double fromValue, double toValue, Duration duration, FillBehavior fillBehavior);

            //  var animation = new DoubleAnimation(1,0, new TimeSpan(0, 0, 0, 5), FillBehavior.HoldEnd);
            //  animation.BeginTime = new TimeSpan(0, 0, 0);
            //   animation.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("(UIElement.Opacity)"));


            //  var board =new Storyboard();
            //    Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
            //    board.Children.Add(animation);


            //    var begin =new BeginStoryboard( );
            //    begin.Storyboard = board;

            var cover = new BitmapImage(new Uri(game.Cover));


            back_img.ImageSource = cover;

            var animater = new DoubleAnimation();
            animater.From = 0.0;
            animater.To = 1.0;
            animater.Duration = new Duration(TimeSpan.FromSeconds(1));
            animater.BeginTime = new TimeSpan(0, 0, 0);
            // animater.AutoReverse = true;
            //  myDoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

            var board = new Storyboard();
            board.Children.Add(animater);
            Storyboard.SetTargetProperty(animater, new PropertyPath(OpacityProperty));


            Storyboard.SetTargetName(animater, back_border.Name);
            board.Begin(back_border);

            Storyboard.SetTargetName(animater, install_btn.Name);
            board.Begin(install_btn);

            Storyboard.SetTargetName(animater, uninstall_btn.Name);
            board.Begin(uninstall_btn);

            Storyboard.SetTargetName(animater, gameName.Name);
            board.Begin(gameName);

            // Use the Loaded event to start the Storyboard.
            //   myRectangle.Loaded += new RoutedEventHandler(myRectangleLoaded);


            // beginner.
            // animater.Begin();
            //   back_img.child


            //{

            //}

            //  var cover = new System.Windows.Media.Imaging.BitmapImage(new Uri(game.Cover));
            //  ImageBrush brush = new ImageBrush();
            //  brush.ImageSource = cover;// new CroppedBitmap(new System.Windows.Media.Imaging.BitmapImage(new Uri(game.Cover)), rect_cover);
            //  main_border.Background = brush;


            gameName.Content = game.Name;
            // gameDescription.Text = game.Description;
            gameName.Visibility = Visibility.Visible;
            //gameDescription.Visible = true;


            install_btn.Visibility = Visibility.Visible;
            uninstall_btn.Visibility = Visibility.Hidden;
            install_lbl.Content = "Install";
            install_img.Source = h.resource2img("install");


            if (states.ContainsKey(selected))
            {
                install_lbl.Content = "Cancel";
                install_img.Source = h.resource2img("cancel");

                filename.Content = states[selected].filename;
                download_progress.Width = states[selected].width;
            }
            else
            {
                filename.Content = "";
                download_progress.Width = 0;
            }


            foreach (var line in installed_games)
            {
                if (!line.Contains(",")) continue;
                var name = line.Split(',')[0];
                var folder = line.Split(',')[1];
                var exe = line.Split(',')[2];
                if (gameName.Content.ToString() == name)
                {
                    install_lbl.Content = "Play";
                    install_img.Source = h.resource2img("play");
                    uninstall_btn.Visibility = Visibility.Visible;
                    break;
                }
            }

            inProgress = false;
        }


        public static BitmapSource Invert(BitmapSource source)
        {
            // Calculate stride of source
            var stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;

            // Create data array to hold source pixel data
            var length = stride * source.PixelHeight;
            var data = new byte[length];

            // Copy source image pixels to the data array
            source.CopyPixels(data, stride, 0);

            // Change this loop for other formats
            for (var i = 0; i < length; i += 4)
            {
                data[i] = (byte)(255 - data[i]); //R
                data[i + 1] = (byte)(255 - data[i + 1]); //G
                data[i + 2] = (byte)(255 - data[i + 2]); //B
                //data[i + 3] = (byte)(255 - data[i + 3]); //A
            }

            // Create a new BitmapSource from the inverted pixel buffer
            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY, source.Format,
                null, data, stride);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (thread1 != null && thread1.IsAlive) thread1.Abort();
            if (thread2 != null && thread2.IsAlive) thread2.Abort();
            if (thread3 != null && thread3.IsAlive) thread3.Abort();
            if (browser != null) browser.Close();
        }


        private bool cancel_download(object sender, int index)
        {
            if (!states.ContainsKey(index)) return false;
            if (index == selected && install_lbl.Content.ToString().ToLower() == "cancel")
            {
                // Downloader.cancel = true;
                states[index].cancellation = true;
                install_lbl.Content = "Install";
                install_img.Source = h.resource2img("install");
                download_progress.Width = 0;
                filename.Content = "";
                Downloader.cancel = true;
                return true;
            }

            Downloader.cancel = false;
            return false;
        }


        private install is_installed(int index)
        {
            var install = new install();

            if (File.Exists(h.installed))
                foreach (var line in File.ReadAllLines(h.installed))
                {
                    if (!line.Contains(",")) continue;
                    var name = line.Split(',')[0];
                    var folder = line.Split(',')[1];
                    var exe = line.Split(',')[2];

                    if (gameName.Content.ToString() != name) continue;


                    install.installed = true;
                    //for (int i = 0; i < games.Count; i++)
                    //{
                    if (games[index].Name != name) continue;

                    install.folder = folder;
                    var url = games[index].URL;
                    var executable = games[index].Executable;
                    using (var web = new WebClient())
                    {
                        if (!url.EndsWith("/")) url = url + "/";
                        var critical_url = $"{url}critical.list";
                        var lines = web.DownloadString(critical_url).Split('\n');

                        for (var l = 0; l < lines.Length; l++)
                        {
                            if (!lines[l].Contains("|")) continue;
                            var lst = h.line2list(lines[l]);
                            var file = Path.Combine(folder, lst.file_path);

                            if (!(File.Exists(file) && new FileInfo(file).Length == lst.file_size))
                            {
                                install.installed = false;
                                return install;
                                //return false;
                            }
                        }

                        foreach (var item in File.ReadAllLines(h.credientials))
                            try
                            {
                                if (!(item.Contains(":") && item.Split(':').Length > 2)) continue;
                                var data = item.Split(':');
                                var game_exe = new Process();
                                game_exe.StartInfo.FileName = Path.Combine(folder, executable);
                                game_exe.StartInfo.Arguments = $"account={data[0]} password={crypt.Decrypt(data[1])}";
                                game_exe.Start();
                                break;
                            }
                            catch (Win32Exception ex)
                            {
                            }
                    }

                    break;
                    //}
                }

            return install;
        }

        private void download(int index, string game_folder = null)
        {
            if (game_folder == null)
                using (var folder = new FolderBrowserDialog())
                {
                    if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        game_folder = folder.SelectedPath;
                    else return;
                }


            righ_panel_w = Width - left_panel.Width - right_panel.Width;
            install_lbl.Content = "Cancel";
            install_img.Source = h.resource2img("cancel");
            //install_btn.Enabled = true;
            //install_btn.BackColor = Color.Red;
            filename.Visibility = Visibility.Visible;

            if (states.ContainsKey(index))
                states[index] = new state { btn_install = btn_state.cancel, cancellation = false };
            else states.Add(index, new state { btn_install = btn_state.cancel, cancellation = false });

            thread3 = new Thread(() =>
            {
                try
                {
                    var name = "";

                    Dispatcher.Invoke(() => { name = gameName.Content.ToString(); });

                    //for (int i = 0; i < games.Count; i++){
                    if (games[index].Name == name)
                    {
                        var url = games[index].URL;
                        if (!url.EndsWith("/")) url = url + "/";

                        var full_url = $"{url}full.list";

                        using (var web = new WebClient())
                        {
                            var lines = web.DownloadString(full_url).Split('\n');

                            var total = 0.0;
                            var current = 0.0;
                            for (var l = 0; l < lines.Length; l++)
                            {
                                if (!lines[l].Contains("|")) continue;
                                total += h.line2list(lines[l]).zip_file_size;
                            }

                            for (var l = 0; l < lines.Length; l++)
                            {
                                if (index == selected && states[index].cancellation)
                                {
                                    states.Remove(index);
                                    Dispatcher.Invoke(() =>
                                    {
                                        install_lbl.Content = "Install";
                                        install_img.Source = h.resource2img("install");
                                        //   install_btn.BackColor = helper.orange;
                                    });
                                    return;
                                }


                                if (!lines[l].Contains("|")) continue;
                                var lst = h.line2list(lines[l]);


                                var zip_url = $"{url}{lst.file_path}.zip";
                                var path = Path.Combine(game_folder, $"{lst.file_path}");
                                var zip = $"{path}.zip";

                                Directory.CreateDirectory(Path.GetDirectoryName(zip));
                                new Thread(() =>
                                {
                                    if (selected == index)
                                    {
                                        var per = current / total;
                                        var file = $"{Math.Round(per * 100.0, 2)}% {lst.file_path}";
                                        Dispatcher.Invoke(() =>
                                        {
                                            filename.Content = file;
                                            download_progress.Width = (int)(per * righ_panel_w);
                                        });
                                        states[index].filename = file;
                                        states[index].width = (int)(per * righ_panel_w);
                                    }
                                }).Start();

                                current += lst.zip_file_size;
                                while (true)
                                    try
                                    {
                                        if ((File.Exists(zip) && new FileInfo(zip).Length == lst.zip_file_size) ||
                                            (File.Exists(path) && new FileInfo(path).Length == lst.file_size))
                                        {
                                            // Console.WriteLine("Found existing with same size");
                                        }
                                        else
                                        {
                                            Downloader.Download(zip_url, Path.GetDirectoryName(zip));
                                        }

                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        //Console.WriteLine("Exception on Downloader: " + ex.ToString());
                                        h.log(ex);
                                    }
                            }
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(h.installed));
                        if (!File.Exists(h.installed)) File.WriteAllText(h.installed, "");
                        File.AppendAllText(h.installed,
                            $"{games[index].Name},{game_folder},{games[index].Executable}\n");
                        while (Downloader.extractInProgress) Thread.Sleep(1000);
                    }


                    if (selected == index)
                        Dispatcher.Invoke(() =>
                        {
                            states.Remove(index);
                            filename.Content = "";
                            install_btn.IsEnabled = true;
                            install_lbl.Content = "Play";
                            install_img.Source = h.resource2img("play");
                            // install_btn.BackColor = Color.Lime;
                            uninstall_btn.Visibility = Visibility.Visible;
                        });
                }
                catch (Exception ex)
                {
                    h.log(ex);
                }
            });
            thread3.Start();
        }

        private void install_click(object sender, MouseButtonEventArgs e)
        {
            if (cancel_download(sender, selected)) return;
            var installed = is_installed(selected);
            if (!installed.installed) download(selected, installed.folder);
        }


        private void uninstall_btn_Click(object sender, MouseButtonEventArgs e)
        {
            if (h.assure($"You are going to uninstall {gameName.Content}. Do you want to continue ?") ==
                System.Windows.Forms.DialogResult.Yes && File.Exists(h.installed))
            {
                var lines = File.ReadAllText(h.installed);
                foreach (var line in lines.Split('\n'))
                {
                    if (!line.Contains(",")) continue;
                    var name = line.Split(',')[0];
                    var folder = line.Split(',')[1];
                    var exe = line.Split(',')[2];
                    if (gameName.Content.ToString() == name)
                    {
                        Directory.Delete(folder, true);
                        uninstall_btn.Visibility = Visibility.Hidden;
                        //install_btn.BackColor = helper.orange;
                        install_lbl.Content = "Install";
                        install_img.Source = h.resource2img("install");
                        lines = lines.Replace(line, "");
                        File.WriteAllText(h.installed, lines);
                        break;
                    }
                }
            }
        }

        private void auth_dialog()
        {
            var auth = new auth();
            //auth.registration_panel.Location = auth.login_panel.Location;
            shown = true;
            auth.ShowDialog();
        }

        private void sign_out_Click(object sender, MouseButtonEventArgs e)
        {
            if (File.Exists(h.credientials)) File.Delete(h.credientials);
            showAuth = true;
            Close();
        }

        private void add_another_Click(object sender, MouseButtonEventArgs e)
        {
            showAuth = true;
            Close();
        }


        private void switch_account_Click(object sender, MouseButtonEventArgs e)
        {
            new account_switcher().ShowDialog();

            if (File.Exists(h.credientials))
                foreach (var item in File.ReadAllLines(h.credientials))
                {
                    if (!item.Contains(":")) continue;
                    var data = item.Split(':');
                    var email = data[0];
                    var pass = crypt.Decrypt(data[1]);
                    if (data.Length > 2 && data[2].Trim() == "1")
                    {
                        if (email.Length > maxEmailLength) email = email.Substring(0, maxEmailLength - 1) + "...";
                        email_lbl.Text = email;
                        //var W = MeasureString(email_lbl).Width;
                        //var left = Width - right_panel.Width + ((right_panel.Width - W) / 2);
                        //email_lbl.Margin = new Thickness(left, email_lbl.Margin.Top, 0, 0);
                    }
                }
        }

        private void Account_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (File.Exists(h.credientials))
                foreach (var item in File.ReadAllLines(h.credientials))
                {
                    if (!item.Contains(":")) continue;
                    var data = item.Split(':');
                    var email = data[0];
                    var pass = crypt.Decrypt(data[1]);
                    if (data.Length > 2 && data[2].Trim() == "1")
                    {
                        if (email.Length > maxEmailLength) email = email.Substring(0, maxEmailLength - 1) + "...";
                        email_lbl.Text = email;
                        //var W = MeasureString(email_lbl).Width;
                        //var left = Width - right_panel.Width + ((right_panel.Width - W) / 2);
                        //email_lbl.Margin = new Thickness(left, email_lbl.Margin.Top, 0, 0);
                    }
                }

            var expand = right_panel.Width == 0;
            var animater = new DoubleAnimation();
            animater.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            animater.From = expand ? 0 : 200;
            animater.To = expand ? 200 : 0;

            var labels = new[] { email_lbl, Add_another, switch_account, sign_out };
            foreach (var lbl in labels) lbl.Visibility = Visibility.Hidden;

            var board = new Storyboard();
            board.Children.Add(animater);
            Storyboard.SetTargetProperty(animater, new PropertyPath(WidthProperty));
            Storyboard.SetTargetName(animater, right_panel.Name);
            board.Completed += Board_Completed;
            board.Begin(right_panel);


            if (browser != null)
            {
                var position = GetWindowPosition();
                var left = position.X;
                var top = position.Y;


                var W = Width - left_panel.Width - 200;
                var L = !expand
                    ? left + left_panel.Width + (Width - left_panel.Width - W) / 2
                    : left + left_panel.Width;


                browser.changeWidth(
                    // 0,//  Width,
                    W, //      Width - left_panel.Width - (!expand ? 50 : 200),
                    // 0,//   left +browser.Left,
                    L //   left + left_panel.Width + (!expand ? ((Width - left_panel.Width - browser.Width) / 2):0)
                );

                /*
                var bW_animator = new DoubleAnimation();
                bW_animator.Duration = new Duration(TimeSpan.FromMilliseconds(200));
                bW_animator.From = Width;
                bW_animator.To = Width - left_panel.Width - (bW_animator.From == 200 ? 50 : 200);
                var bW_board = new Storyboard();
                bW_board.Children.Add(bW_animator);
                Storyboard.SetTargetProperty(bW_animator, new PropertyPath(hackyBrowser.WidthProperty));
                Storyboard.SetTargetName(bW_animator, browser.Name);
                bW_board.Begin(browser);
                */
                /*
                                var bH_animator = new DoubleAnimation();
                                bH_animator.Duration = new Duration(TimeSpan.FromMilliseconds(200));
                                bH_animator.From = browser.Left;
                                bH_animator.To = browser.Left = left + left_panel.Width + (animater.From == 200? ((Width - left_panel.Width - browser.Width) / 2):0);
                                var bH_board = new Storyboard();
                                bH_board.Children.Add(bH_animator);
                                Storyboard.SetTargetProperty(bH_animator, new PropertyPath(hackyBrowser.LeftProperty));
                                Storyboard.SetTargetName(bH_animator, browser.Name);
                                //bH_board.Completed += Board_Completed;
                                bH_board.Begin(browser);
                                */


                /*
                if (animater.From == 200)
                {
                    browser.Left = left + left_panel.Width + ((Width - left_panel.Width - browser.Width) / 2);
                }
                else
                {
                    browser.Left = left + left_panel.Width;
                }*/
                browser.Top = top + Height - browser.Height;
            }
        }

        private Size MeasureString(Label lbl)
        {
            var formattedText = new FormattedText(
                lbl.Content.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(lbl.FontFamily, lbl.FontStyle, lbl.FontWeight, lbl.FontStretch),
                lbl.FontSize,
                lbl.Foreground,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            return new Size(formattedText.Width, formattedText.Height);
        }


        private void Board_Completed(object s, EventArgs e)
        {
            var clock = (ClockGroup)s;
            var board = clock.Timeline as Storyboard;
            var animator = (DoubleAnimation)board.Children[0];
            if (animator.From == 0.0 && animator.To == 200.0)
            {
                // var W = MeasureString(email_lbl).Width;

                //  var left = Width - right_panel.Width + ((right_panel.Width - W) / 2);
                //  email_lbl.Margin = new Thickness(left, email_lbl.Margin.Top, 0, 0);
                email_lbl.Visibility = Visibility.Visible;
                Add_another.Visibility = Visibility.Visible;
                switch_account.Visibility = Visibility.Visible;
                sign_out.Visibility = Visibility.Visible;
            }
        }
    }
}