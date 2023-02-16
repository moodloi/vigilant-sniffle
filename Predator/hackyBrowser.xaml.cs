using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace Predator
{
    /// <summary>
    ///     Interaction logic for hackyBrowser.xaml
    /// </summary>
    public partial class hackyBrowser : Window
    {
        public hackyBrowser()
        {
            InitializeComponent();
            Loaded += On_Load;
        }

        public void changeWidth(double Width, double Left)
        {
            var bW_board = new Storyboard();
            /*
            var bW_animator = new DoubleAnimation();
            bW_animator.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            //bW_animator.From = From;
            bW_animator.To = Width;
            bW_board.Children.Add(bW_animator);
            Storyboard.SetTargetProperty(bW_animator, new PropertyPath(hackyBrowser.WidthProperty));
            Storyboard.SetTarget(bW_animator, this);
            */

            var bL_animator = new DoubleAnimation();
            bL_animator.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            //bL_animator.From = From2;
            bL_animator.To = Left;
            bW_board.Children.Add(bL_animator);
            Storyboard.SetTargetProperty(bL_animator, new PropertyPath(LeftProperty));
            Storyboard.SetTarget(bL_animator, this);

            BeginStoryboard(bW_board, HandoffBehavior.SnapshotAndReplace, false);
        }


        private void On_Load(object sender, RoutedEventArgs e)
        {
            box1.Navigating += Navigating;
            box2.Navigating += Navigating;
            box3.Navigating += Navigating;

            box1.LoadCompleted += LoadCompleted;
            box2.LoadCompleted += LoadCompleted;
            box3.LoadCompleted += LoadCompleted;

            if (h.box1.Contains("http")) box1.Source = new Uri(h.box1);
            if (h.box2.Contains("http")) box2.Source = new Uri(h.box2);
            if (h.box3.Contains("http")) box3.Source = new Uri(h.box3);
        }


        private void LoadCompleted(object sender, NavigationEventArgs e)
        {
            var script = "document.documentElement.style.overflow ='hidden'";
            var wb = (WebBrowser)sender;
            wb.InvokeScript("execScript", script, "JavaScript");
            wb.Visibility = Visibility.Visible;
        }

        private void Navigating(object sender, NavigatingCancelEventArgs e)
        {
            var browser = (WebBrowser)sender;
            dynamic activeX = browser.GetType().InvokeMember("ActiveXInstance",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, browser,
                new object[] { });
            activeX.Silent = true;
        }
    }
}