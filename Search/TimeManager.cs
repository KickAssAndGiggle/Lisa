using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lisa
{
    public sealed class TimeManager
    {
        public int MaxTimeToUse;

        //Sets MaxTimeToUse, the most milliseconds to spend on this move: a fixed amount when the clock is under
        //two minutes, otherwise a share of the time left that grows as pieces come off the board
        public TimeManager(int millisecondsLeft, Board theBoard)
        {
            if (millisecondsLeft < 500)
            {
                MaxTimeToUse = 50;
            }
            else if (millisecondsLeft < 1750)
            {
                MaxTimeToUse = 100;
            }
            else if (millisecondsLeft < 3500)
            {
                MaxTimeToUse = 150;
            }
            else if (millisecondsLeft < 5000)
            {
                MaxTimeToUse = 300;
            }
            else if (millisecondsLeft >= 5000 && millisecondsLeft < 15000)
            {
                MaxTimeToUse = 600;
            }
            else if (millisecondsLeft >= 15000 && millisecondsLeft < 30000)
            {
                MaxTimeToUse = 1200;
            }
            else if (millisecondsLeft >= 30000 && millisecondsLeft < 60000)
            {
                MaxTimeToUse = 5000;
            }
            else if (millisecondsLeft >= 60000 && millisecondsLeft < 120000)
            {
                MaxTimeToUse = 9000;
            }
            else
            {
                if (theBoard.PieceCount >= 28)
                {
                    MaxTimeToUse = Convert.ToInt32(millisecondsLeft * 0.04);
                }
                else if (theBoard.PieceCount < 28 && theBoard.PieceCount >= 20)
                {
                    MaxTimeToUse = Convert.ToInt32(millisecondsLeft * 0.05);
                }
                else if (theBoard.PieceCount < 20 && theBoard.PieceCount >= 14)
                {
                    MaxTimeToUse = Convert.ToInt32(millisecondsLeft * 0.10);
                }
                else
                {
                    MaxTimeToUse = Convert.ToInt32(millisecondsLeft * 0.12);
                }
            }
        }
    }
}
