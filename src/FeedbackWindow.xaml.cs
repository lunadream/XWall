using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XWall
{
    /// <summary>
    /// FeedbackWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FeedbackWindow : Window
    {
        public FeedbackWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox_AppVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            TextBox_AppName.Text = Assembly.GetExecutingAssembly().GetName().Name.ToString();
        }
    }
}
