using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hoogland
{
    public partial class Form1 : Form
    {
        private int _buttonWidth = 75, _buttonHeight = 75;

        public Form1()
        {
            InitializeComponent();
        }



        private void DrawBoard()
        {
            Board board = new Board();
            int buttonX = 0, buttonY = 0;
            for(int i = 0; i < board.GetWidth(); i++)
            {
                buttonX = 0;
                for (int j = 0; j < board.GetHeight(); j++)
                {
                    Button btn = new Button();
                    btn.Location = new System.Drawing.Point(buttonX, buttonY);
                    buttonX += 75;
                    btn.Size = new System.Drawing.Size(_buttonHeight, _buttonWidth);
                    if(i%2 == 0)
                    {
                        if (j % 2 == 0)
                        {
                            btn.BackColor = Color.White;
                        }
                        else
                        {
                            btn.BackColor = Color.DarkGray;
                        }
                    }
                    else
                    {
                        if (j % 2 == 0)
                        {
                            btn.BackColor = Color.DarkGray;
                        }
                        else
                        {
                            btn.BackColor = Color.White;
                        }
                    }
                    if(board.GetBoardMatrix()[i,j] == 1)
                    {
                        btn.BackgroundImage = System.Drawing.Image.FromFile(@".\Resources\Images\blackPiece.jpg");
                        btn.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                    else if (board.GetBoardMatrix()[i, j] == 3)
                    {
                        btn.BackgroundImage = System.Drawing.Image.FromFile(@".\Resources\Images\redPiece.png");
                        btn.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                    boardPanel.Controls.Add(btn);
                }
                buttonY += 75;
            }
        }
        private void ClearBoard()
        {
            boardPanel.Controls.Clear();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;
            DrawBoard();
            movesTextBox.Text += "hello";
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
    }
}
