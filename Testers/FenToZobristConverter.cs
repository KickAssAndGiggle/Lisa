using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lisa
{
    public sealed class FenToZobristConverter
    {
        //Writes the FEN and its Zobrist key to the output file
        public void ConvertFenToZobrist(string Fen, string OutputFile)
        {
            Board theBoard = new();
            theBoard.InitialiseFromFEN(Fen);
            string ZobString = theBoard.CurrentZobrist.ToString();

            StreamWriter SW = File.CreateText(OutputFile);
            SW.WriteLine("Fen: " + Fen);
            SW.WriteLine("Zob: " + ZobString);
            SW.Close();
        }
    }
}
