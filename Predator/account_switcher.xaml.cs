using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Predator
{
    /// <summary>
    ///     Interaction logic for account_switcher.xaml
    /// </summary>
    public partial class account_switcher : Window
    {
        private bool _shown;

        public account_switcher()
        {
            InitializeComponent();
        }

        private void close_click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void minimize_click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown) return;
            _shown = true;

            Account_Switcher_Shown(this, null);
        }


        private void Account_Switcher_Shown(object sender, EventArgs e)
        {
            if (File.Exists(h.credientials))
                foreach (var line in File.ReadAllLines(h.credientials))
                    if (line.Contains(":"))
                    {
                        var email = line.Split(':')[0];
                        accounts.Items.Add(email);
                    }
        }

        private void install_btn_Click(object sender, RoutedEventArgs e)
        {
            var box = accounts;
            if (box.SelectedIndex == -1)
            {
                h.Warn("No account was selected.");
                return;
            }

            var selected = box.SelectedValue.ToString();
            if (File.Exists(h.credientials))
            {
                var lines = File.ReadAllLines(h.credientials);
                var data = "";
                var done = false;
                var error = false;
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Contains(":"))
                    {
                        var email = line.Split(':')[0];
                        var pass = line.Split(':')[1];
                        if (email == selected && !done)
                        {
                            done = true;
                            if (h.connect(email, crypt.Decrypt(pass), false, false))
                            {
                                data += $"{email}:{pass}:1\n";
                            }
                            else
                            {
                                h.Warn($"Invalid credientials for {email}");
                                error = true;
                                break;
                            }
                        }
                        else
                        {
                            data += $"{email}:{pass}\n";
                        }
                    }
                }

                if (!error)
                {
                    File.WriteAllText(h.credientials, data);
                    Close();
                }
            }
        }
    }
}