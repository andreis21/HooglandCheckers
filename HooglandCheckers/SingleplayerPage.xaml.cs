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
    /// Interaction logic for SingleplayerPage.xaml
    /// </summary>
    public partial class SingleplayerPage : Page
    {
        MainWindow _mainWindow;

        public SingleplayerPage(MainWindow window)
        {
            this._mainWindow = window;
            InitializeComponent();
        }

        private void easyBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Content = new GamePage(_mainWindow, "singleplayer", "easy", "");
        }

        private void mediumButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Content = new GamePage(_mainWindow, "singleplayer", "medium", "");
        }

        private void quitGameBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Close();
        }
    }
}
