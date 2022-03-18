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

        public TimeManager(int TotalTimeLeftForGame, Board B)
        {

            if (TotalTimeLeftForGame < 500)
            {
                MaxTimeToUse = 50;
            }
            else if (TotalTimeLeftForGame < 1750)
            {
                MaxTimeToUse = 100;
            }
            else if (TotalTimeLeftForGame < 3500)
            {
                MaxTimeToUse = 150;
            }
            else if (TotalTimeLeftForGame < 5000)
            {
                MaxTimeToUse = 300;
            }
            else if (TotalTimeLeftForGame >= 5000 && TotalTimeLeftForGame < 15000)
            {
                MaxTimeToUse = 600;
            }
            else if (TotalTimeLeftForGame >= 15000 && TotalTimeLeftForGame < 30000)
            {
                MaxTimeToUse = 1200;
            }
            else if (TotalTimeLeftForGame >= 30000 && TotalTimeLeftForGame < 60000)
            {
                MaxTimeToUse = 5000;
            }
            else if (TotalTimeLeftForGame >= 60000 && TotalTimeLeftForGame < 120000)
            {
                MaxTimeToUse = 9000;
            }
            else
            {
                if (B.PieceCount >= 28)
                {
                    MaxTimeToUse = Convert.ToInt32(TotalTimeLeftForGame * 0.04);
                }
                else if (B.PieceCount < 28 && B.PieceCount >= 20)
                {
                    MaxTimeToUse = Convert.ToInt32(TotalTimeLeftForGame * 0.05);
                }
                else if (B.PieceCount < 20 && B.PieceCount >= 14)
                {
                    MaxTimeToUse = Convert.ToInt32(TotalTimeLeftForGame * 0.10);
                }
                else
                {
                    MaxTimeToUse = Convert.ToInt32(TotalTimeLeftForGame * 0.12);
                }
            }

        }




    }
}
