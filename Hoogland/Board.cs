using System;
using System.Collections.Generic;
using System.Text;

namespace Hoogland
{
    class Board
    {
        private int _width;
        private int _height;

        private int[,] _boardMatrix = {
            { 0, 1, 0, 1, 0, 1, 0, 1},
            { 1, 0, 1, 0, 1, 0, 1, 0},
            { 0, 1, 0, 1, 0, 1, 0, 1},
            { 0, 0, 0, 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0, 0, 0, 0},
            { 3, 0, 3, 0, 3, 0, 3, 0},
            { 0, 3, 0, 3, 0, 3, 0, 3},
            { 3, 0, 3, 0, 3, 0, 3, 0}
        };

        public Board()
        {
            this._height = 8;
            this._width = 8;
        }

        public int GetWidth()
        {
            return _width;
        }
        public int GetHeight()
        {
            return _height;
        }
        public int[,] GetBoardMatrix()
        {
            return _boardMatrix;
        }
        public void SwapPieces(int[,] poz1, int[,] poz2)
        {
            int[,] temp = poz1;
            poz1 = poz2;
            poz2 = temp;
        }
    }
}
