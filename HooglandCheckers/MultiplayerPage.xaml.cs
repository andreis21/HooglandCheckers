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
    /// Interaction logic for MultiplayerPage.xaml
    /// </summary>
    public partial class MultiplayerPage : Page
    {
        MainWindow _mainWindow;

        public MultiplayerPage(MainWindow window)
        {
            this._mainWindow = window;
            InitializeComponent();
        }

        private void quitGameBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Close();
        }

        private void serverBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Content = new GamePage(_mainWindow, "multiplayer", "", "server");
        }

        private void clientBtn_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Content = new GamePage(_mainWindow, "multiplayer", "", "client");
        }
    }
}
