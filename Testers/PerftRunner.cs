using static Lisa.Globals;
namespace Lisa
{
    public class PerftRunner
    {

        private readonly string _perftFen = "";
        private readonly int _perftDepth = 0;
        private readonly string _perftOutputFile;

        public PerftRunner(string fen, int depth, string outputFile)
        {
            _perftFen = fen;
            _perftDepth = depth;
            _perftOutputFile = outputFile;
        }


        public void DoPerft()
        {

            List<string> lines = new();
            Board perftBoard = new();
            perftBoard.InitialiseFromFEN(_perftFen);
            int depth = 0;

            do
            {

                long perftMovesAtDepth = 0;
                depth += 1;

                lines.Add("Perft @ depth " + depth.ToString());

                int startTicks = System.Environment.TickCount;
                Move[] moves;

                if (perftBoard.OnMove == WHITE)
                {
                    moves = perftBoard.GenerateAllMoves(WHITE);
                }
                else
                {
                    moves = perftBoard.GenerateAllMoves(BLACK);
                }

                Perft(moves, depth, perftBoard.OnMove, ref perftBoard, ref perftMovesAtDepth);

                int endTicks = System.Environment.TickCount;

                lines.Add("Moves: " + perftMovesAtDepth.ToString());
                lines.Add("Milliseconds: " + (endTicks - startTicks).ToString());
                lines.Add(" ");
                lines.Add(" ");

            } while (depth < _perftDepth);

            StreamWriter sw = File.CreateText(_perftOutputFile);
            foreach (string line in lines)
            {
                sw.WriteLine(line);
            }
            sw.Close();

        }


        private void Perft(Move[] moves, int depth, int onMove, ref Board theBoard, ref long totalMoveCount)
        {
            if (depth == 1)
            {
                foreach (Move M in moves)
                {
                    if (theBoard.MoveIsLegal(M, onMove))
                    {
                        totalMoveCount += 1;
                    }
                }
                return;
            }

            foreach (Move m in moves)
            {
                if (theBoard.MoveIsLegal(m, onMove))
                {
                    theBoard.MakeMove(m, onMove, false);
                    Move[] NewMoves = theBoard.GenerateAllMoves(onMove == WHITE ? BLACK : WHITE);
                    Perft(NewMoves, depth - 1, onMove == WHITE ? BLACK : WHITE, ref theBoard, ref totalMoveCount);
                    theBoard.UnmakeLastMove();
                }
            }
        }
    }
}
