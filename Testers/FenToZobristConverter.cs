using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lisa
{
    public sealed class FenToZobristConverter
    {

        public void ConvertFenToZobrist(string Fen, string OutputFile)
        {

            Board TheBoard = new();
            TheBoard.InitialiseFromFEN(Fen);
            string ZobString = TheBoard.CurrentZobrist.ToString();

            StreamWriter SW = File.CreateText(OutputFile);
            SW.WriteLine("Fen: " + Fen);
            SW.WriteLine("Zob: " + ZobString);
            SW.Close();

        }

    }
}
