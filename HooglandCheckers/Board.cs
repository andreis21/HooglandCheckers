using System;
using System.Collections.Generic;
using System.Text;

namespace HooglandCheckers
{
    internal class Board
    {
        public readonly int _boardWidth;
        public readonly int _boardHeigth;

        private string blackPlayer;
        private string redPlayer;

        private string playerColor;

        private string gameType;

        private bool gameOver;

        private string gameWinner;

        private bool blackKingOnTable;
        private bool redKingOnTable;

        private int[,] _boardMatrix;

        public Board(string redPlayer, string blackPlayer, string gameType, string playerColor = "red")
        {
            _boardWidth = 8;
            _boardHeigth = 8;
            this.gameType = gameType;
            this.playerColor = playerColor;
            gameOver = false;
            gameWinner = "";
            blackKingOnTable = false;
            redKingOnTable = false;
            this.redPlayer = redPlayer;
            this.blackPlayer = blackPlayer;

            if (gameType.Equals("multiplayer"))
            {
                if (playerColor.Equals("red"))
                {
                    _boardMatrix = new int[,]{
                        { 0, 1, 0, 1, 0, 1, 0, 1},
                        { 1, 0, 1, 0, 1, 0, 1, 0},
                        { 0, 1, 0, 1, 0, 1, 0, 1},
                        { 0, 0, 0, 0, 0, 0, 0, 0},
                        { 0, 0, 0, 0, 0, 0, 0, 0},
                        { 3, 0, 3, 0, 3, 0, 3, 0},
                        { 0, 3, 0, 3, 0, 3, 0, 3},
                        { 3, 0, 3, 0, 3, 0, 3, 0}
                    };
                }
                else
                {
                    _boardMatrix = new int[,]{
                        { 0, 3, 0, 3, 0, 3, 0, 3},
                        { 3, 0, 3, 0, 3, 0, 3, 0},
                        { 0, 3, 0, 3, 0, 3, 0, 3},
                        { 0, 0, 0, 0, 0, 0, 0, 0},
                        { 0, 0, 0, 0, 0, 0, 0, 0},
                        { 1, 0, 1, 0, 1, 0, 1, 0},
                        { 0, 1, 0, 1, 0, 1, 0, 1},
                        { 1, 0, 1, 0, 1, 0, 1, 0}
                    };
                }
            }
            else
            {
                _boardMatrix = new int[,]{
                    { 0, 1, 0, 1, 0, 1, 0, 1},
                    { 1, 0, 1, 0, 1, 0, 1, 0},
                    { 0, 1, 0, 1, 0, 1, 0, 1},
                    { 0, 0, 0, 0, 0, 0, 0, 0},
                    { 0, 0, 0, 0, 0, 0, 0, 0},
                    { 3, 0, 3, 0, 3, 0, 3, 0},
                    { 0, 3, 0, 3, 0, 3, 0, 3},
                    { 3, 0, 3, 0, 3, 0, 3, 0}
                };
            }
        }

        public int[,] getBoardMatrix()
        {
            return _boardMatrix;
        }

        public void setBoardMatrix(int[,] matrix)
        {
            _boardMatrix = matrix;
        }

        public bool movePiece(int[] pieceToBeMoved, int[] positionToMove)
        {
            _boardMatrix[positionToMove[1]/*y*/, positionToMove[0]/*x*/] = _boardMatrix[pieceToBeMoved[1]/*y*/, pieceToBeMoved[0]/*x*/];
            _boardMatrix[pieceToBeMoved[1]/*y*/, pieceToBeMoved[0]/*x*/] = 0;

            if (gameType.Equals("singleplayer"))
            {
                for (int i = 0; i < _boardWidth; i++)
                {
                    if (_boardMatrix[0, i] == 3 && redKingOnTable == false)
                    {
                        _boardMatrix[0, i]++;
                        redKingOnTable = true;
                        break;
                    }
                }
                for (int i = 0; i < _boardWidth; i++)
                {
                    if (_boardMatrix[7, i] == 1 && blackKingOnTable == false)
                    {
                        _boardMatrix[7, i]++;
                        blackKingOnTable = true;
                        break;
                    }
                }
            }
            else
            {
                if (playerColor.Equals("red"))
                {
                    for (int i = 0; i < _boardWidth; i++)
                    {
                        if (_boardMatrix[0, i] == 3 && redKingOnTable == false)
                        {
                            _boardMatrix[0, i]++;
                            redKingOnTable = true;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _boardWidth; i++)
                    {
                        if (_boardMatrix[0, i] == 3 && blackKingOnTable == false)
                        {
                            _boardMatrix[0, i]++;
                            blackKingOnTable = true;
                            break;
                        }
                    }
                }
            }
            

            if (Math.Abs(positionToMove[0]-pieceToBeMoved[0]) == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public void removePiece(int x, int y)
        {
            if(_boardMatrix[y, x] == 2)
            {
                blackKingOnTable = false;
            }
            else if(_boardMatrix[y, x] == 4)
            {
                redKingOnTable = false;
            }
            _boardMatrix[y, x] = 0;
            if(CountPieces("black") == 0)
            {
                gameOver = true;
                gameWinner = redPlayer;
            }
            else if(CountPieces("red") == 0)
            {
                gameOver = true;
                gameWinner = blackPlayer;
            }
        }

        public bool GetGameState()
        {
            return gameOver;
        }

        public void SetGameState(bool state)
        {
            gameOver = state;
        }

        public string GetGameWinner()
        {
            return gameWinner;
        }

        public void SetGameWinner(string winner)
        {
            if(winner == "red")
            {
                gameWinner = redPlayer;
            }
            else
            {
                gameWinner = blackPlayer;
            }
        }

        public int CountPieces(string color)
        {
            int numberOfPieces = 0;
            if (color.Equals("black"))
            {
                foreach(var piece in _boardMatrix)
                {
                    if(piece == 1 || piece == 2)
                    {
                        numberOfPieces++;
                    }
                }
            }
            else
            {
                foreach (var piece in _boardMatrix)
                {
                    if (piece == 3 || piece == 4)
                    {
                        numberOfPieces++;
                    }
                }
            }
            return numberOfPieces;
        }
    }
}
