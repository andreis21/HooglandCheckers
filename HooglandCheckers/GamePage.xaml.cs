using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HooglandCheckers
{
    /// <summary>
    /// Interaction logic for GamePage.xaml
    /// </summary>
    public partial class GamePage : Page
    {
        private MainWindow _mainWindow;
        private string _gameType;
        private string _gameDifficulty;
        private Button[,] btnMatrix = new Button[8, 8];
        private Board gameBoard = null;
        private bool playerMoved = false;
        private string playerType;

        Socket listener = null;
        Socket handler = null;
        Socket client = null;

        public GamePage(MainWindow window, string gameType, string gameDifficulty, string playerType)
        {
            this._mainWindow = window;
            this._gameType = gameType;
            this._gameDifficulty = gameDifficulty;
            this.playerType = playerType;
            InitializeComponent();
            btnMatrix = new Button[8, 8];
            if (_gameType.Equals("singleplayer"))
            {
                gameBoard = new Board("Human", "Computer", _gameType);
            }
            else
            {
                if (playerType.Equals("server"))
                {
                    gameBoard = new Board("Player 1", "Player 2", _gameType);
                }
                else
                {
                    gameBoard = new Board("Player 1", "Player 2", _gameType, "black");
                }
            }
            InitializeGameBoard();
            movesTB.FontSize = 24;
            movesTB.IsReadOnly = true;
            if (_gameType.Equals("singleplayer"))
            {
                CompositionTarget.Rendering += GameLoopSingleplayer;
            }
            else if (_gameType.Equals("multiplayer"))
            {
                if (playerType.Equals("server"))
                {
                    // Establish the local endpoint for the socket.  
                    // Dns.GetHostName returns the name of the
                    // host running the application.  
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

                    // Create a TCP/IP socket.  
                    listener = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                    // Bind the socket to the local endpoint and
                    // listen for incoming connections.  
                    try
                    {
                        listener.Bind(localEndPoint);
                        listener.Listen(10);
                        handler = listener.Accept();
                        CompositionTarget.Rendering += MultiplayerServer;
                    }
                    catch (Exception e)
                    {
                    }
                }
                else
                {
                    // Connect to a remote device.  
                    try
                    {
                        // Establish the remote endpoint for the socket.  
                        // This example uses port 11000 on the local computer.  
                        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                        IPAddress ipAddress = ipHostInfo.AddressList[0];
                        IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                        // Create a TCP/IP  socket.  
                        client = new Socket(ipAddress.AddressFamily,
                            SocketType.Stream, ProtocolType.Tcp);

                        // Connect the socket to the remote endpoint. Catch any errors.  
                        try
                        {
                            client.Connect(remoteEP);
                            DisableMove();
                            Task.Run(GameLoopMultiplayer);
                            CompositionTarget.Rendering += MultiplayerClient;
                        }
                        catch (ArgumentNullException ane)
                        {
                            Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                        }
                        catch (SocketException se)
                        {
                            Console.WriteLine("SocketException : {0}", se.ToString());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unexpected exception : {0}", e.ToString());
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        private void MultiplayerServer(object sender, EventArgs e)
        {
            ClearBoard();
            DrawPieces();
            if (playerMoved)
            {
                DisableMove();
                Task.Run(GameLoopMultiplayer);
                playerMoved = false;
            }
        }

        private void MultiplayerClient(object sender, EventArgs e)
        {
            ClearBoard();
            DrawPieces();
            if (playerMoved)
            {
                DisableMove();
                Task.Run(GameLoopMultiplayer);
                playerMoved = false;
            }
        }

        private void GameLoopSingleplayer(object sender, EventArgs e)
        {
            ClearBoard();
            DrawPieces();
            if (playerMoved)
            {
                var computerLegalMoves = CalculateLegalMoves("black");
                if (computerLegalMoves.Count == 0)
                {
                    gameBoard.SetGameWinner("red");
                    ShowGameWinner();
                }
                else
                {
                    if (gameBoard.GetGameState())
                    {
                        ShowGameWinner();
                    }
                    Random rand = new Random();
                    ComputerMovePiece(computerLegalMoves[rand.Next(computerLegalMoves.Count)]);
                    ClearBoard();
                    DrawPieces();
                    playerMoved = false;
                }
            }
            else
            {
                if (CalculateLegalMoves("red").Count == 0)
                {
                    gameBoard.SetGameWinner("black");
                    ShowGameWinner();
                }
            }
        }

        private void GameLoopMultiplayer()
        {
            while (true)
            {
                byte[] bytes = new byte[1024];
                int bytesRec = 0;
                if (playerType.Equals("server"))
                {
                    bytesRec = handler.Receive(bytes);
                }
                else if (playerType.Equals("client"))
                {
                    bytesRec = client.Receive(bytes);
                }
                string test = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (bytesRec > 0)
                {
                    string[] objects = test.Split('?');
                    this.Dispatcher.Invoke(() =>
                    {
                        movesTB.Text = objects[1];
                    });
                    int[,] newMatrix = MakeMatrix(objects[0]);
                    gameBoard.setBoardMatrix(MatrixMirroring(newMatrix));
                    ClearBoard();
                    DrawPieces();
                    EnableMove();
                    playerMoved = false;
                }
            }
        }

        private void ComputerMovePiece(string move)
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            string[] positions = move.Split('|');
            string[] positionToMove = positions[0].Split(',');
            int[] intPositionToMove = { Int16.Parse(positionToMove[0]), Int16.Parse(positionToMove[1]) };
            string[] positionToBeMoved = positions[1].Split(',');
            int[] intPositionToBeMoved = { Int16.Parse(positionToBeMoved[0]), Int16.Parse(positionToBeMoved[1]) };
            if (gameBoard.movePiece(intPositionToMove, intPositionToBeMoved))
            {
                int[] jumpedOverPiece = new int[2];
                if (intPositionToMove[0] - intPositionToBeMoved[0] == -2)
                {
                    string text = movesTB.Text;
                    movesTB.Text = "";
                    jumpedOverPiece = new int[] { intPositionToMove[0] + 1, intPositionToMove[1] + 1 };
                    movesTB.Text += intPositionToMove[0] + "," + intPositionToMove[1] + "|" + intPositionToBeMoved[0] + "," + intPositionToBeMoved[1] + "|" + matrix[jumpedOverPiece[1], jumpedOverPiece[0]] + Environment.NewLine + text;
                    gameBoard.removePiece(intPositionToMove[0] + 1, intPositionToMove[1] + 1);
                }
                else
                {
                    string text = movesTB.Text;
                    movesTB.Text = "";
                    jumpedOverPiece = new int[] { intPositionToMove[0] - 1, intPositionToMove[1] + 1 };
                    movesTB.Text += intPositionToMove[0] + "," + intPositionToMove[1] + "|" + intPositionToBeMoved[0] + "," + intPositionToBeMoved[1] + "|" + matrix[jumpedOverPiece[1], jumpedOverPiece[0]] + Environment.NewLine + text;
                    gameBoard.removePiece(intPositionToMove[0] - 1, intPositionToMove[1] + 1);
                }
            }
        }

        private void InitializeGameBoard()
        {
            double btnHeigth = 75;
            double btnWidth = 75;


            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    btnMatrix[i, j] = new Button();
                    btnMatrix[i, j].Height = btnHeigth;
                    btnMatrix[i, j].Width = btnWidth;
                    string name = "cell" + j.ToString() + i.ToString();
                    btnMatrix[i, j].Name = name;
                    btnMatrix[i, j].Click += new RoutedEventHandler(cellBtnClick);
                    btnMatrix[i, j].VerticalAlignment = VerticalAlignment.Center;
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                        {
                            btnMatrix[i, j].Background = Brushes.LightYellow;
                        }
                        else
                        {
                            btnMatrix[i, j].Background = new SolidColorBrush(Colors.SandyBrown);
                        }
                    }
                    else
                    {
                        if (j % 2 == 0)
                        {
                            btnMatrix[i, j].Background = new SolidColorBrush(Colors.SandyBrown);
                        }
                        else
                        {
                            btnMatrix[i, j].Background = new SolidColorBrush(Colors.LightYellow);
                        }
                    }
                    gamePanel.Children.Add(btnMatrix[i, j]);
                }
            }

            DrawPieces();
        }

        private void RecolorBoard()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                        {
                            btnMatrix[i, j].Background = Brushes.LightYellow;
                        }
                        else
                        {
                            btnMatrix[i, j].Background = Brushes.SandyBrown;
                        }
                    }
                    else
                    {
                        if (j % 2 == 0)
                        {
                            btnMatrix[i, j].Background = Brushes.SandyBrown;
                        }
                        else
                        {
                            btnMatrix[i, j].Background = Brushes.LightYellow;
                        }
                    }
                }
            }
        }

        private void DrawPieces()
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (matrix[i, j] == 1)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            StackPanel sp = new StackPanel();
                            Image img = new Image();
                            BitmapImage BitImg = new BitmapImage(new Uri(@"D:\Proiecte\HooglandCheckers\HooglandCheckers\bin\Debug\netcoreapp3.1\Resources\Images\blackPiece.png"));
                            img.Source = BitImg;
                            btnMatrix[i, j].Content = img;
                        });
                    }
                    else if (matrix[i, j] == 2)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            StackPanel sp = new StackPanel();
                            Image img = new Image();
                            BitmapImage BitImg = new BitmapImage(new Uri(@"D:\Proiecte\HooglandCheckers\HooglandCheckers\bin\Debug\netcoreapp3.1\Resources\Images\blackKingPiece.png"));
                            img.Source = BitImg;
                            btnMatrix[i, j].Content = img;
                        });
                    }
                    else if (matrix[i, j] == 3)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            StackPanel sp = new StackPanel();
                            Image img = new Image();
                            BitmapImage BitImg = new BitmapImage(new Uri(@"D:\Proiecte\HooglandCheckers\HooglandCheckers\bin\Debug\netcoreapp3.1\Resources\Images\redPiece.png"));
                            img.Source = BitImg;
                            btnMatrix[i, j].Content = img;
                        });
                    }
                    else if (matrix[i, j] == 4)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            StackPanel sp = new StackPanel();
                            Image img = new Image();
                            BitmapImage BitImg = new BitmapImage(new Uri(@"D:\Proiecte\HooglandCheckers\HooglandCheckers\bin\Debug\netcoreapp3.1\Resources\Images\redKingPiece.png"));
                            img.Source = BitImg;
                            btnMatrix[i, j].Content = img;
                        });
                    }
                }
            }
        }

        private void ClearBoard()
        {
            foreach (var btn in btnMatrix)
            {
                if (_gameType.Equals("singleplayer"))
                {
                    btn.Content = "";
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        btn.Content = "";
                    });
                }
            }
        }

        bool pieceSelected = false;
        Button selectedPiece = new Button();

        private void cellBtnClick(object sender, EventArgs e)
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            Button btnClicked = (Button)sender;
            string name = btnClicked.Name.Replace("cell", "");
            int[] coords = { Int16.Parse(name[0].ToString()), Int16.Parse(name[1].ToString()) };

            if (pieceSelected)
            {
                if (btnClicked.Background == Brushes.Aqua)
                {
                    playerMoved = true;
                    string text = movesTB.Text;
                    movesTB.Text = "";
                    string pieceToBeMoved = selectedPiece.Name.Replace("cell", "");
                    int[] coordsPieceToBeMoved = { Int16.Parse(pieceToBeMoved[0].ToString()), Int16.Parse(pieceToBeMoved[1].ToString()) };

                    if (gameBoard.movePiece(coordsPieceToBeMoved, coords))
                    {
                        int[] jumpedOverPiece = new int[2];
                        if (coordsPieceToBeMoved[0] - coords[0] == -2)
                        {
                            if (coordsPieceToBeMoved[1] - coords[1] == -2)
                            {
                                jumpedOverPiece = new int[] { coordsPieceToBeMoved[0] + 1, coordsPieceToBeMoved[1] + 1 };
                                movesTB.Text += coordsPieceToBeMoved[0] + "," + coordsPieceToBeMoved[1] + "|" + coords[0] + "," + coords[1] + "|" + matrix[jumpedOverPiece[1], jumpedOverPiece[0]] + Environment.NewLine + text;
                                gameBoard.removePiece(coordsPieceToBeMoved[0] + 1, coordsPieceToBeMoved[1] + 1);
                            }
                            else if (coordsPieceToBeMoved[1] - coords[1] == 2)
                            {
                                jumpedOverPiece = new int[] { coordsPieceToBeMoved[0] + 1, coordsPieceToBeMoved[1] - 1 };
                                movesTB.Text += coordsPieceToBeMoved[0] + "," + coordsPieceToBeMoved[1] + "|" + coords[0] + "," + coords[1] + "|" + matrix[jumpedOverPiece[1], jumpedOverPiece[0]] + Environment.NewLine + text;
                                gameBoard.removePiece(coordsPieceToBeMoved[0] + 1, coordsPieceToBeMoved[1] - 1);
                            }
                        }
                        else
                        {
                            if (coordsPieceToBeMoved[1] - coords[1] == -2)
                            {
                                jumpedOverPiece = new int[] { coordsPieceToBeMoved[0] - 1, coordsPieceToBeMoved[1] + 1 };
                                movesTB.Text += coordsPieceToBeMoved[0] + "," + coordsPieceToBeMoved[1] + "|" + coords[0] + "," + coords[1] + "|" + matrix[jumpedOverPiece[1], jumpedOverPiece[0]] + Environment.NewLine + text;
                                gameBoard.removePiece(coordsPieceToBeMoved[0] - 1, coordsPieceToBeMoved[1] + 1);
                            }
                            else if (coordsPieceToBeMoved[1] - coords[1] == 2)
                            {
                                jumpedOverPiece = new int[] { coordsPieceToBeMoved[0] - 1, coordsPieceToBeMoved[1] - 1 };
                                movesTB.Text += coordsPieceToBeMoved[0] + "," + coordsPieceToBeMoved[1] + "|" + coords[0] + "," + coords[1] + "|" + matrix[jumpedOverPiece[1], jumpedOverPiece[0]] + Environment.NewLine + text;
                                gameBoard.removePiece(coordsPieceToBeMoved[0] - 1, coordsPieceToBeMoved[1] - 1);
                            }
                        }
                    }
                    else
                    {
                        movesTB.Text += coordsPieceToBeMoved[0] + "," + coordsPieceToBeMoved[1] + "|" + coords[0] + "," + coords[1] + "|0" + Environment.NewLine + text;
                    }
                    RecolorBoard();
                    ClearBoard();
                    DrawPieces();
                    pieceSelected = false;
                    selectedPiece = null;
                    if (playerType.Equals("client"))
                    {
                        byte[] bytes = new byte[1024];
                        int[,] data = gameBoard.getBoardMatrix();
                        string stringMatrix = StringifyMatrix(data) + "?" + movesTB.Text;
                        byte[] msg = Encoding.ASCII.GetBytes(stringMatrix);
                        client.Send(msg);
                    }
                    else if(playerType.Equals("server"))
                    {
                        byte[] bytes = new byte[1024];
                        int[,] data = gameBoard.getBoardMatrix();
                        string stringMatrix = StringifyMatrix(data) + "?" + movesTB.Text;
                        byte[] msg = Encoding.ASCII.GetBytes(stringMatrix);
                        handler.Send(msg);
                    }
                    if (gameBoard.GetGameState())
                    {
                        ShowGameWinner();
                    }
                }
                else
                {
                    pieceSelected = false;
                    selectedPiece = null;
                    RecolorBoard();
                }
            }
            else
            {
                List<string> allMoves = new List<string>();
                if (_gameType.Equals("singleplayer"))
                {
                    if (matrix[coords[1], coords[0]] == 1 || matrix[coords[1], coords[0]] == 2)
                    {
                        allMoves = CalculateLegalMoves("black");
                        selectedPiece = btnClicked;
                        pieceSelected = true;
                    }
                    else if (matrix[coords[1], coords[0]] == 3 || matrix[coords[1], coords[0]] == 4)
                    {
                        allMoves = CalculateLegalMoves("red");
                        selectedPiece = btnClicked;
                        pieceSelected = true;
                    }
                }
                else
                {
                    if (playerType.Equals("server"))
                    {
                        allMoves = CalculateLegalMoves("red");
                    }
                    else if (playerType.Equals("client"))
                    {
                        allMoves = CalculateLegalMoves("black");
                    }
                    selectedPiece = btnClicked;
                    pieceSelected = true;
                }

                List<string> buttonsToColor = new List<string>();

                foreach (var move in allMoves)
                {
                    string[] temp = move.Split('|');
                    string[] positionsToMove = temp[0].Split(',');
                    if (Int16.Parse(positionsToMove[0]) == coords[0] && Int16.Parse(positionsToMove[1]) == coords[1])
                    {
                        buttonsToColor.Add(temp[1]);
                    }
                }

                foreach (var btn in buttonsToColor)
                {
                    string[] positionsToMove = btn.Split(',');
                    btnMatrix[Int16.Parse(positionsToMove[1]), Int16.Parse(positionsToMove[0])].Background = Brushes.Aqua;
                }
            }
        }

        private string StringifyMatrix(int[,] data)
        {
            string result = string.Empty;
            int[,] matrix = gameBoard.getBoardMatrix();
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    result += matrix[i, j] + " ";
                }
                result += "|";
            }
            return result;
        }

        private List<string> CalculateLegalMoves(string color)
        {
            List<string> moves = new List<string>();
            int[,] matrix = gameBoard.getBoardMatrix();
            int factor;
            if (_gameType.Equals("singleplayer"))
            {
                if (color.Equals("red"))
                {
                    factor = -1;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (matrix[i, j] == 3)
                            {
                                if (j == 0)
                                {
                                    if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i > 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i > 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                }
                            }
                            if (matrix[i, j] == 4)
                            {
                                if (j == 0)
                                {
                                    if (i == 0)
                                    {
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if(i == 0)
                                    {
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (color.Equals("black"))
                {
                    factor = 1;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (matrix[i, j] == 1)
                            {
                                if (j == 0)
                                {
                                    if (i == 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i < 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i < 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                }
                            }
                            if (matrix[i, j] == 2)
                            {
                                if (j == 0)
                                {
                                    if (i == 0)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 0)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    if (i == 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    if (i == 7)
                                    {
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        if (i == 7)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            else
            {
                if (color.Equals("red"))
                {
                    factor = -1;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (matrix[i, j] == 3)
                            {
                                if (j == 0)
                                {
                                    if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i > 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i > 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (j == 6)
                                        {
                                            if (i == 1)
                                            {
                                                if (checkLeft(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                                }
                                                if (checkRight(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                                }
                                            }
                                            else if (i > 1)
                                            {
                                                if (checkLeft(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                                }
                                                if (checkRight(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                                }
                                                if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (matrix[i, j] == 4)
                            {
                                if (j == 0)
                                {
                                    if (i == 0)
                                    {
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 0)
                                    {

                                    }
                                    else if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "red"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    factor = -1;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (matrix[i, j] == 1)
                            {
                                if (j == 0)
                                {
                                    if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i > 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                    }
                                    else if (i > 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        else if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                        }
                                        else if (i > 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (j == 6)
                                        {
                                            if (i == 1)
                                            {
                                                if (checkLeft(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                                }
                                                if (checkRight(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                                }
                                            }
                                            else if (i > 1)
                                            {
                                                if (checkLeft(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                                }
                                                if (checkRight(j, i, factor))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                                }
                                                if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                                {
                                                    moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (matrix[i, j] == 2)
                            {
                                if (j == 0)
                                {
                                    if (i == 0)
                                    {
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 6)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkRight(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkRight(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentRightAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else if (j == 7)
                                {
                                    if (i == 0)
                                    {
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 1)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i > 1 && i < 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 6)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkLeft(j, i, -1 * factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                        }
                                    }
                                    else if (i == 7)
                                    {
                                        if (checkLeft(j, i, factor))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                        }
                                        if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                        {
                                            moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                        }
                                    }
                                }
                                else
                                {
                                    if (j == 1)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else if (j > 1 && j < 6)
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentRightAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (i == 0)
                                        {
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 1)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i > 1 && i < 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, -1 * factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + -2 * factor));
                                            }
                                        }
                                        else if (i == 6)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkLeft(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, -1 * factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + -1 * factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                        else if (i == 7)
                                        {
                                            if (checkLeft(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 1) + "," + (i + factor));
                                            }
                                            if (checkRight(j, i, factor))
                                            {
                                                moves.Add(j + "," + i + "|" + (j + 1) + "," + (i + factor));
                                            }
                                            if (checkOpponentLeftAndJump(j, i, factor, "black"))
                                            {
                                                moves.Add(j + "," + i + "|" + (j - 2) + "," + (i + 2 * factor));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return moves;
        }

        private bool checkLeft(int x, int y, int factor)
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            if (matrix[y + factor, x - 1] == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool checkOpponentLeftAndJump(int x, int y, int factor, string color)
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            bool legal = false;
            if (color.Equals("red"))
            {
                if ((matrix[y + factor, x - 1] == 1 || matrix[y + factor, x - 1] == 2) && matrix[y + 2 * factor, x - 2] == 0)
                {
                    legal = true;
                }
            }
            else
            {
                if ((matrix[y + factor, x - 1] == 3 || matrix[y + factor, x - 1] == 4) && matrix[y + 2 * factor, x - 2] == 0)
                {
                    legal = true;
                }
            }
            return legal;
        }

        private bool checkRight(int x, int y, int factor)
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            if (matrix[y + factor, x + 1] == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool checkOpponentRightAndJump(int x, int y, int factor, string color)
        {
            int[,] matrix = gameBoard.getBoardMatrix();
            bool legal = false;
            if (color.Equals("red"))
            {
                if ((matrix[y + factor, x + 1] == 1 || matrix[y + factor, x + 1] == 2) && matrix[y + 2 * factor, x + 2] == 0)
                {
                    legal = true;
                }
            }
            else
            {
                if ((matrix[y + factor, x + 1] == 3 || matrix[y + factor, x + 1] == 4) && matrix[y + 2 * factor, x + 2] == 0)
                {
                    legal = true;
                }
            }
            return legal;
        }

        private void ShowGameWinner()
        {
            if (_gameType.Equals("singleplayer"))
            {
                MessageBox.Show("The " + gameBoard.GetGameWinner() + " won the Game!");
            }
            _mainWindow.Close();
        }

        private int[,] MatrixMirroring(int[,] matrix)
        {
            int[,] result = new int[8, 8];

            for (int i = 7; i >= 0; i--)
            {
                for (int j = 7; j >= 0; j--)
                {
                    result[7 - i, 7 - j] = matrix[i, j];
                }
            }

            return result;
        }

        private int[,] MakeMatrix(string data)
        {
            int[,] result = new int[8, 8];
            string[] stringRows = data.TrimEnd('|').Split("|");
            List<List<string>> stringMatrix = new List<List<string>>();
            foreach(var row in stringRows)
            {
                string trimedRow = row.TrimEnd();
                List<string> listRow = new List<string>();
                foreach(var cell in trimedRow.Split(" "))
                {
                    listRow.Add(cell);
                }
                stringMatrix.Add(listRow);
            }
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    result[i, j] = Int16.Parse(stringMatrix[i][j]);
                }
            }
            return result;
        }

        private void DisableMove()
        {
            foreach(var btn in btnMatrix)
            {
                this.Dispatcher.Invoke(() =>
                {
                    btn.IsEnabled = false;
                });
            }
        }

        private void EnableMove()
        {
            foreach(var btn in btnMatrix)
            {
                this.Dispatcher.Invoke(() =>
                {
                    btn.IsEnabled = true;
                });
            }
        }
    }
}
