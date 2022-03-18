using static Lisa.Globals;
namespace Lisa
{
    public class PerftRunner
    {

        private readonly string PerftFen = "";
        private readonly int PerftDepth = 0;
        private readonly string PerftOutputFile;

        public PerftRunner(string Fen, int Depth, string OutputFile)
        {
            PerftFen = Fen;
            PerftDepth = Depth;
            PerftOutputFile = OutputFile;
        }


        public void DoPerft()
        {

            List<string> Lines = new();
            Board PerftBoard = new();
            PerftBoard.InitialiseFromFEN(PerftFen);
            int Depth = 0;

            do
            {

                long PerftMovesAtDepth = 0;
                Depth += 1;

                Lines.Add("Perft @ depth " + Depth.ToString());

                int StartTicks = System.Environment.TickCount;
                Move[] Moves;
                if (PerftBoard.OnMove == WHITE)
                {
                    Moves = PerftBoard.GenerateAllMoves(WHITE);
                }
                else
                {
                    Moves = PerftBoard.GenerateAllMoves(BLACK);
                }
                Perft(Moves, Depth, PerftBoard.OnMove, ref PerftBoard, ref PerftMovesAtDepth);
                int EndTicks = System.Environment.TickCount;
                Lines.Add("Moves: " + PerftMovesAtDepth.ToString());
                Lines.Add("Milliseconds: " + (EndTicks - StartTicks).ToString());
                Lines.Add(" ");
                Lines.Add(" ");

            } while (Depth < PerftDepth);

            StreamWriter SW = File.CreateText(PerftOutputFile);
            foreach (string L in Lines)
            {
                SW.WriteLine(L);
            }
            SW.Close();

        }


        private void Perft(Move[] Moves, int Depth, int OnMove, ref Board B, ref long TotalMoveCount)
        {

            if (Depth == 1)
            {
                foreach (Move M in Moves)
                {
                    if (B.MoveIsLegal(M, OnMove))
                    {
                        TotalMoveCount += 1;
                    }
                }
                return;
            }

            foreach (Move M in Moves)
            {
                if (B.MoveIsLegal(M, OnMove))
                {
                    long StartZobrist = B.CurrentZobrist;
                    B.MakeMove(M, OnMove, false);
                    Move[] NewMoves = B.GenerateAllMoves(OnMove == WHITE ? BLACK : WHITE);
                    Perft(NewMoves, Depth - 1, OnMove == WHITE ? BLACK : WHITE, ref B, ref TotalMoveCount);
                    B.UnmakeLastMove();
                    long EndZobrist = B.CurrentZobrist;
                }
            }

        }
    }
}
