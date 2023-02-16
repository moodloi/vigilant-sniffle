using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Predator
{
    /// <summary>
    ///     Interaction logic for auth.xaml
    /// </summary>
    public partial class auth : Window
    {
        public static string email = "";

        // private Timer right_slider_timer = new Timer();
        private readonly DispatcherTimer right_slider_timer = new DispatcherTimer();

        private bool _shown;


        private string[] auth_imges;
        private int auth_imges_index;

        private Thread img_thread;
        public bool showLauncher;

        public auth()
        {
            InitializeComponent();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e) //for moving form
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

            auth_Shown(this, null);
        }

        private void accounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            var box = (ComboBox)sender;
            var selected = box.SelectedValue.ToString();
            if (File.Exists(h.credientials))
                foreach (var line in File.ReadAllLines(h.credientials))
                    if (line.Contains(":"))
                    {
                        var email = line.Split(':')[0];
                        if (email == selected)
                        {
                            s_email.Text = email;
                            s_pass.Password = crypt.Decrypt(line.Split(':')[1]);
                            ls_pass.Visibility = Visibility.Hidden;
                        }
                    }
        }

        private void auth_Shown(object sender, EventArgs e)
        {
            if (File.Exists(h.credientials))
            {
                foreach (var line in File.ReadAllLines(h.credientials))
                    if (line.Contains(":"))
                    {
                        var email = line.Split(':')[0];
                        accounts.Items.Add(email);
                    }

                accounts.SelectionChanged += accounts_SelectedIndexChanged;
            }


            //s_email
            s_capcha_code.Content = ranStr();
            r_capcha_code.Content = ranStr();

            //   movable_form.ME = this;
            //    movable_form.draggable(new Control[] { this, right_slider, login_panel, registration_panel });


            // if (Directory.Exists("auth")) Directory.Delete("auth", true);
            img_thread = new Thread(() =>
            {
                try
                {
                    using (var web = new WebClient())
                    {
                        Directory.CreateDirectory(h.auth);
                        var data = web.DownloadString(h.slider);
                        var firsttime = true;
                        foreach (Match m in Regex.Matches(data, @"<a.*?href=""(.*?)"">", RegexOptions.Multiline))
                        {
                            var img = m.Groups[1].ToString();
                            if (img.Contains(".") && img.Contains("slider") && img.Contains("/"))
                            {
                                var filename = img.Split('/')[img.Split('/').Length - 1];
                                var path = $"{h.auth}/{filename}";
                                var url = h.slider + filename;
                                // web.DownloadFile(helper.slider + filename, $"auth/{filename}");
                                if (File.Exists(path) && new FileInfo(path).Length == Downloader.getFileSize(url))
                                {
                                    // Console.WriteLine("Found existing with same size");
                                }
                                else
                                {
                                    Downloader.Download(url, h.auth);
                                }

                                if (firsttime)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        left_slider.ImageSource = new BitmapImage(new Uri($"{h.auth}/{filename}"));
                                    }); //right_slider.ImageLocation = $"{helper.auth}/{filename}";
                                    firsttime = false;
                                }
                            }
                        }
                    }


                    // Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>{
                    right_slider_timer.Interval = new TimeSpan(0, 0, 4);
                    right_slider_timer.Tick += right_slider_timer_Tick;
                    right_slider_timer.Start();
                    //  }));
                }
                catch (Exception ex)
                {
                    h.log(ex);
                }
            });

            img_thread.Start();
        }

        private void right_slider_timer_Tick(object sender, EventArgs e)
        {
            if (Directory.GetFiles(h.auth).Length > 0)
            {
                auth_imges = Directory.GetFiles(h.auth);
                if (auth_imges_index > auth_imges.Length - 1) auth_imges_index = 0;
                //    left_slider.ImageLocation = auth_imges[auth_imges_index];
                left_slider.ImageSource = new BitmapImage(new Uri(auth_imges[auth_imges_index]));

                auth_imges_index++;
            }
        }


        private void login_panel_show_Click(object sender, RoutedEventArgs e)
        {
            //var previous_location = login_panel.Location;
            //login_panel.Location = registration_panel.Location;
            //registration_panel.Location = previous_location;
            //registration_panel.Visibility = Visibility.Hidden;
            login_panel.Visibility = Visibility.Visible;
        }

        private void signup_panel_show_Click(object sender, RoutedEventArgs e)
        {
            //var previous_location = registration_panel.Location;
            //registration_panel.Location = login_panel.Location;
            //login_panel.Location = previous_location;
            //registration_panel.Visibility = Visibility.Visible;
            login_panel.Visibility = Visibility.Hidden;

            //registration_panel.Visible = true;
            // login_panel.Visible = false;
        }

        public static string ranStr()
        {
            var ran = new Random();
            var b = "abcdefghijklmnopqrstuvwxyz";
            var length = 4;

            var random = "";
            for (var i = 0; i < length; i++)
            {
                var a = ran.Next(26);
                random = random + b.ElementAt(a);
            }

            return random.ToUpper();
        }

        private void r_signup_Click(object sender, RoutedEventArgs e)
        {
            var error = "";
            var email = r_email.Text.ToLower().Trim();
            var user = r_user.Text.ToLower().Trim();
            var pass1 = r_pass1.Password.Trim();
            var pass2 = r_pass2.Password.Trim();
            var agreed = r_agreed.IsChecked;

            if (!h.IsValidEmail(email)) error = "Please provide a valid email address";
            else if (email.Length > 50) error = "Please provide a valid email address";
            else if (user.Length > 16) error = "Username must be less than 16 characters";
            else if (!user.All(char.IsLetterOrDigit)) error = "Username must contain only letters and numbers";
            else if (user.Length > 16) error = "Username must be less than 16 characters";
            else if (user.Length < 4) error = "Username must be at least 4 characters";
            else if (pass1.Length > 16) error = "Password must be less than 16 characters";
            else if (pass1.Length < 4) error = "Password must be at least 6 characters";
            else if (!pass1.All(char.IsLetterOrDigit)) error = "Password must contain only letters and numbers";
            else if (pass1 != pass2) error = "Passwords do not match";
            else if (agreed == false) error = "Please, accept terms to continue";
            else if (r_capcha_code.Content.ToString() != r_recapcha.Text) error = "Invalid captcha";
            if (error != string.Empty)
            {
                h.Warn(error);
                r_capcha_code.Content = ranStr();
                return;
            }


            using (var web = new WebClient())
            {
                var hwdata = File.ReadAllText(h.hwdata);

                user = h.UrlEncode(user);
                email = h.UrlEncode(email);
                pass1 = h.UrlEncode(pass1);
                hwdata = h.UrlEncode(hwdata);
                var url =
                    $"{h.account}?action=registerUser&username={user}&email={email}&password={pass1}&hwdata={hwdata}";

                //Console.WriteLine(url);

                var response = web.DownloadString(url);

                if (response.Contains("ERROR:"))
                {
                    if (response.Contains("MISSING_PARAMETERS")) error = "Missing parameters";
                    else if (response.Contains("EMAIL_TOO_SHORT")) error = "Email is too short";
                    else if (response.Contains("PASSWORD_TOO_SHORT")) error = "Password is too short";
                    else if (response.Contains("USERNAME_TOO_SHORT")) error = "Username is too short";
                    else if (response.Contains("USERNAME_TOO_LONG")) error = "Username is too long";
                    else if (response.Contains("PASSWORD_TOO_LONG")) error = "Passowrd is too long";
                    else if (response.Contains("EMAIL_TAKEN")) error = "Email already exists. Please, login";
                    else if (response.Contains("USERNAME_TAKEN"))
                        error = "Username is already taken. Please, choose another";
                    r_response.Content = "Something went wrong";
                    r_response.Foreground = Brushes.Red;

                    if (error != string.Empty)
                    {
                        r_response.Content = error;
                        r_capcha_code.Content = ranStr();
                    }
                }
                else if (response.Contains("OK:DONE"))
                {
                    r_response.Content = "Your account has been created successfully";
                    r_response.Foreground = Brushes.Lime;
                }
            }
        }

        public void login_Click(object sender, RoutedEventArgs e)
        {
            email = s_email.Text.ToLower().Trim();
            var pass = s_pass.Password.Trim();
            var remember = s_remember.IsChecked;
            if (s_capcha_code.Content.ToString() != s_capcha.Text)
            {
                s_response.Content = "Invalid captcha";
                s_response.Foreground = Brushes.Red;
            }
            else if (h.connect(email, pass, remember == true, true, s_response))
            {
                showLauncher = true;
                Dispatcher.Invoke(() => { Close(); });
            }
            else
            {
                s_response.Content = "Something went wrong";
                s_response.Foreground = Brushes.Red;
            }

            s_capcha_code.Content = ranStr();
        }


        private void auth_FormClosed(object sender, EventArgs e)
        {
            if (img_thread != null) img_thread.Abort();
        }

        private void close_click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void minimize_click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        private void textbox_Focus(object sender, RoutedEventArgs e)
        {
            var box = (Control)sender;
            var border = (Border)FindName("b" + box.Name);

            if (box is PasswordBox)
            {
                var label = (TextBlock)FindName("l" + box.Name);
                label.Visibility = Visibility.Hidden;
            }


            border.BorderBrush = Brushes.Orange;
        }

        private void textbox_UnFocus(object sender, RoutedEventArgs e)
        {
            var box = (Control)sender;
            var border = (Border)FindName("b" + box.Name);
            if (box is PasswordBox)
            {
                var label = (TextBlock)FindName("l" + box.Name);
                var val = ((PasswordBox)box).Password;
                label.Visibility = val.Trim() != string.Empty ? Visibility.Hidden : Visibility.Visible;
            }

            //var label = (TextBlock)FindName("l" + box.Name);
            //var val = box is PasswordBox ? ((PasswordBox)box).Password : ((TextBox)box).Text;
            // label.Visibility = val.Trim() != string.Empty ? Visibility.Hidden : Visibility.Visible;            
            border.BorderBrush = Brushes.Gray;
        }
    }
}