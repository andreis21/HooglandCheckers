using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HooglandCheckers
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        MainWindow _mainWindow;

        public MainPage(MainWindow window)
        {
            this._mainWindow = window;
            InitializeComponent();
        }

        private void singleplayerBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Content = new SingleplayerPage(_mainWindow);
        }

        private void multiplayerBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Content = new MultiplayerPage(_mainWindow);
        }

        private void quitGameBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Close();
        }
    }
}
