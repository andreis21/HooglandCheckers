using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace Hoogland
{
    public partial class Form1 : Form
    {
        private int _buttonWidth = 75, _buttonHeight = 75;

        public Form1()
        {
            InitializeComponent();
        }


        static Board board = new Board();
        Button[,] btn = new Button[board.GetHeight(), board.GetWidth()];
        private void DrawBoard()
        {
            int buttonX = 0, buttonY = 0;
            for(int i = 0; i < board.GetWidth(); i++)
            {
                buttonX = 0;
                for (int j = 0; j < board.GetHeight(); j++)
                {
                    btn[i,j] = new Button();
                    btn[i,j].Location = new System.Drawing.Point(buttonX, buttonY);
                    buttonX += 75;
                    btn[i,j].Size = new System.Drawing.Size(_buttonHeight, _buttonWidth);
                    if(i%2 == 0)
                    {
                        if (j % 2 == 0)
                        {
                            btn[i,j].BackColor = Color.White;
                        }
                        else
                        {
                            btn[i,j].BackColor = Color.DarkGray;
                        }
                    }
                    else
                    {
                        if (j % 2 == 0)
                        {
                            btn[i,j].BackColor = Color.DarkGray;
                        }
                        else
                        {
                            btn[i,j].BackColor = Color.White;
                        }
                    }
                    if(board.GetBoardMatrix()[i,j] == 1)
                    {
                        btn[i,j].BackgroundImage = System.Drawing.Image.FromFile(@".\Resources\Images\blackPiece.jpg");
                    }
                    else if (board.GetBoardMatrix()[i,j] == 2) 
                    {
                        // TO-DO Black King Image
                    }
                    else if (board.GetBoardMatrix()[i, j] == 3)
                    {
                        btn[i,j].BackgroundImage = System.Drawing.Image.FromFile(@".\Resources\Images\redPiece.png");
                    }
                    else if(board.GetBoardMatrix()[i,j] == 4)
                    {
                        // TO-DO Red King Image
                    }
                    btn[i,j].Click += new EventHandler(cellButton_Click);
                    btn[i, j].BackgroundImageLayout = ImageLayout.Stretch;
                    btn[i, j].Name = i + "," + j;
                    boardPanel.Controls.Add(btn[i,j]);
                }
                buttonY += 75;
            }
        }
        private void ClearBoard()
        {
            boardPanel.Controls.Clear();
        }
        private void RestoreBoardColors()
        {
            for (int i = 0; i < board.GetWidth(); i++)
            {
                for (int j = 0; j < board.GetHeight(); j++)
                {
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
                        {
                            btn[i, j].BackColor = Color.White;
                        }
                        else
                        {
                            btn[i, j].BackColor = Color.DarkGray;
                        }
                    }
                    else
                    {
                        if (j % 2 == 0)
                        {
                            btn[i, j].BackColor = Color.DarkGray;
                        }
                        else
                        {
                            btn[i, j].BackColor = Color.White;
                        }
                    }
                }
            }
        }
        private void DrawPieces()
        {
            foreach(var button in btn)
            {
                button.BackgroundImage = null;
            }
            for (int i = 0; i < board.GetWidth(); i++)
            {
                for (int j = 0; j < board.GetHeight(); j++)
                {
                    if (board.GetBoardMatrix()[i, j] == 1)
                    {
                        btn[i, j].BackgroundImage = System.Drawing.Image.FromFile(@".\Resources\Images\blackPiece.jpg");
                    }
                    else if (board.GetBoardMatrix()[i, j] == 2)
                    {
                        // TO-DO Black King Image
                    }
                    else if (board.GetBoardMatrix()[i, j] == 3)
                    {
                        btn[i, j].BackgroundImage = System.Drawing.Image.FromFile(@".\Resources\Images\redPiece.png");
                    }
                    else if (board.GetBoardMatrix()[i, j] == 4)
                    {
                        // TO-DO Red King Image
                    }
                }
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;
            DrawBoard();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void movesTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void boardPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        bool pieceSelected = false;
        int[] selectedPiece = new int[2];
        int[] jumpedOverPiece = new int[2];
        private void cellButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            string[] positionsString = button.Name.Split(',');
            int[] positions = new int[2];
            positions[0] = Int32.Parse(positionsString[0]);
            positions[1] = Int32.Parse(positionsString[1]);
            if(button.BackColor == Color.LightYellow)
            {
                if(board.GetBoardMatrix()[jumpedOverPiece[0],jumpedOverPiece[1]] != 0)
                {
                    board.RemovePiece(jumpedOverPiece);
                    board.SwapPieces(selectedPiece, positions);
                    jumpedOverPiece = new int[2];
                }
                else
                {
                    board.SwapPieces(selectedPiece, positions);
                }
                RestoreBoardColors();
                DrawPieces();
                string moves = movesTextBox.Text;
                movesTextBox.Text = selectedPiece[0].ToString() + " " + selectedPiece[1].ToString() + " | " + positions[0].ToString() + " " + positions[1].ToString() + Environment.NewLine + moves;
            }
            else
            {
                if (pieceSelected)
                {
                    RestoreBoardColors();
                    CalculateLegalMoves(positions[0], positions[1]);
                }
                else
                {
                    CalculateLegalMoves(positions[0], positions[1]);
                }
            }
        }

        private void CalculateLegalMoves(int y, int x)
        {
            // TO-DO calculations for king piece
            selectedPiece[0] = y;
            selectedPiece[1] = x;
            int factor = 1;
            int pieceID = board.GetBoardMatrix()[y,x];
            if (pieceID == 1)
            {
                int opponentPieceID = 3;
                if (x == 0)
                {
                    if (board.GetBoardMatrix()[y + factor, x + 1] == pieceID) { }
                    else if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                    {
                        if (board.GetBoardMatrix()[y + 2 * factor, x + 2] != 0) { }
                        else
                        {
                            jumpedOverPiece[0] = y + factor;
                            jumpedOverPiece[1] = x + 1;
                            btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                        }
                    }
                    else btn[y + factor, x + 1].BackColor = Color.LightYellow;
                }
                else if (x == 7)
                {
                    if (board.GetBoardMatrix()[y + factor, x - 1] == pieceID) { }
                    else if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                    {
                        if (board.GetBoardMatrix()[y + 2 * factor, x - 2] != 0) { }
                        else
                        {
                            jumpedOverPiece[0] = y + factor;
                            jumpedOverPiece[1] = x - 1;
                            btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                        }
                    }
                    else btn[y + factor, x - 1].BackColor = Color.LightYellow;
                }
                else
                {
                    if (y <= 7)
                    {
                        if (board.GetBoardMatrix()[y + factor, x - 1] == pieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                            {
                                if (x + 2 <= 7)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x + 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x + 1;
                                        btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x + 1] == pieceID) { }
                            else btn[y + factor, x + 1].BackColor = Color.LightYellow;
                        }
                        else if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                            {
                                if (x + 2 <= 7)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x + 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x + 1;
                                        btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x + 1] == 0)
                            {
                                btn[y + factor, x + 1].BackColor = Color.LightYellow;
                            }
                            if (x - 2 >= 0)
                            {
                                if (board.GetBoardMatrix()[y + 2 * factor, x - 2] == 0)
                                {
                                    jumpedOverPiece[0] = y + factor;
                                    jumpedOverPiece[1] = x - 1;
                                    btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                                }
                            }
                        }
                        else if (board.GetBoardMatrix()[y + factor, x + 1] == pieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                            {
                                if (x - 2 >= 0)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x - 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x - 1;
                                        btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x - 1] == pieceID) { }
                            else btn[y + factor, x - 1].BackColor = Color.LightYellow;
                        }
                        else if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                        {
                            
                            if (x + 2 <= 7)
                            {
                                if (board.GetBoardMatrix()[y + 2 * factor, x + 2] == 0)
                                {
                                    jumpedOverPiece[0] = y + factor;
                                    jumpedOverPiece[1] = x + 1;
                                    btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                                }
                            }
                            if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                            {
                                if (x - 2 >= 0)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x - 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x - 1;
                                        btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x - 1] == 0)
                            {
                                jumpedOverPiece[0] = 0;
                                jumpedOverPiece[1] = 0;
                                btn[y + factor, x - 1].BackColor = Color.LightYellow;
                            }
                        }
                        else
                        {
                            btn[y + factor, x + 1].BackColor = Color.LightYellow;
                            btn[y + factor, x - 1].BackColor = Color.LightYellow;
                        }
                    }
                }
            }
            if (pieceID == 3)
            {
                factor = -1;
                int opponentPieceID = 1;
                if (x == 0)
                {
                    if (board.GetBoardMatrix()[y + factor, x + 1] == pieceID) { }
                    else if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                    {
                        if (board.GetBoardMatrix()[y + 2 * factor, x + 2] != 0) { }
                        else
                        {
                            jumpedOverPiece[0] = y + factor;
                            jumpedOverPiece[1] = x + 1;
                            btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                        }
                    }
                    else btn[y + factor, x + 1].BackColor = Color.LightYellow;
                }
                else if (x == 7)
                {
                    if (board.GetBoardMatrix()[y + factor, x - 1] == pieceID) { }
                    else if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                    {
                        if (board.GetBoardMatrix()[y + 2 * factor, x - 2] != 0) { }
                        else
                        {
                            jumpedOverPiece[0] = y + factor;
                            jumpedOverPiece[1] = x - 1;
                            btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                        }
                    }
                    else btn[y + factor, x - 1].BackColor = Color.LightYellow;
                }
                else
                {
                    if (y <= 7)
                    {
                        if (board.GetBoardMatrix()[y + factor, x - 1] == pieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                            {
                                if (x + 2 <= 7)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x + 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x + 1;
                                        btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if(board.GetBoardMatrix()[y + factor, x + 1] == pieceID) { }
                            else btn[y + factor, x + 1].BackColor = Color.LightYellow;
                        }
                        else if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                            {
                                if (x + 2 <= 7)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x + 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x + 1;
                                        btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x + 1] == 0)
                            {
                                btn[y + factor, x + 1].BackColor = Color.LightYellow;
                            }
                            if (x - 2 >= 0)
                            {
                                if (board.GetBoardMatrix()[y + 2 * factor, x - 2] == 0)
                                {
                                    jumpedOverPiece[0] = y + factor;
                                    jumpedOverPiece[1] = x - 1;
                                    btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                                }
                            }
                        }
                        else if (board.GetBoardMatrix()[y + factor, x + 1] == pieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                            {
                                if (x - 2 >= 0)
                                {
                                    if (board.GetBoardMatrix()[y + 2 * factor, x - 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x - 1;
                                        btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x - 1] == pieceID) { }
                            else btn[y + factor, x - 1].BackColor = Color.LightYellow;
                        }
                        else if (board.GetBoardMatrix()[y + factor, x + 1] == opponentPieceID)
                        {
                            if (board.GetBoardMatrix()[y + factor, x - 1] == opponentPieceID)
                            {
                                if (x - 2 >= 0)
                                {
                                    if (x - 2 >= 0 && board.GetBoardMatrix()[y + 2 * factor, x - 2] == 0)
                                    {
                                        jumpedOverPiece[0] = y + factor;
                                        jumpedOverPiece[1] = x - 1;
                                        btn[y + 2 * factor, x - 2].BackColor = Color.LightYellow;
                                    }
                                }
                            }
                            else if (board.GetBoardMatrix()[y + factor, x - 1] == 0)
                            {
                                btn[y + factor, x - 1].BackColor = Color.LightYellow;
                            }
                            if (x + 2 <= 7)
                            {
                                if (board.GetBoardMatrix()[y + 2 * factor, x + 2] == 0)
                                {
                                    jumpedOverPiece[0] = y + factor;
                                    jumpedOverPiece[1] = x + 1;
                                    btn[y + 2 * factor, x + 2].BackColor = Color.LightYellow;
                                }
                            }
                        }
                        else
                        {
                            btn[y + factor, x + 1].BackColor = Color.LightYellow;
                            btn[y + factor, x - 1].BackColor = Color.LightYellow;
                        }
                    }
                }
            }
            pieceSelected = true;
        }
    }
}
