using static Lisa.Globals;
namespace Lisa
{
    //Builds the PV sections of the EPD and MultiFen output files. Only the testers call this, after the
    //search has finished, so the UCI engine never pays for the replay or the static evaluations.
    public static class PVReport
    {

        //The top root moves, best first, each with its score and PV. Every PV move is followed by Lisa's
        //static eval of the position it reaches, from the root side-to-move's point of view (the same
        //point of view as Score).
        public static List<string> TopMoveLines(Searcher search, string fen, int maxMoves)
        {
            List<string> lines = new();
            lines.Add("Top moves:");

            //ScorePosition needs a table to run; this one is separate from the Searcher's and holds one entry
            TranspositionTable evalTable = new(true);

            Board rootBoard = new();
            rootBoard.InitialiseFromFEN(fen);
            lines.Add("Static eval at root: " + FormatEval(StaticEval(ref rootBoard, ref evalTable, rootBoard.OnMove)));

            if (search.RootMoves == null || search.RootPVs == null)
            {
                return lines;
            }

            Move[] sortedRootMoves = (Move[])search.RootMoves.Clone();
            Array.Sort(sortedRootMoves);
            for (int nn = 0; nn < sortedRootMoves.Length && nn < maxMoves; nn++)
            {
                int moveKey = sortedRootMoves[nn].From * 100 + sortedRootMoves[nn].To;
                Move[] movePV = search.RootPVs.ContainsKey(moveKey) ? search.RootPVs[moveKey]
                    : new Move[] { sortedRootMoves[nn] };
                string plainPV = ConvertPVToSAN(movePV, fen, null);
                string evalPV = ConvertPVToSAN(movePV, fen, evalTable);
                string firstMove = plainPV.Split(' ')[0];
                lines.Add((nn + 1).ToString() + ") " + firstMove + "  Score: " + sortedRootMoves[nn].Score.ToString() +
                    "  PV: " + evalPV);
            }

            return lines;
        }


        //Converts a PV (stored root move last) to SAN by replaying it on a board set up from the FEN.
        //Each move is matched against the legal moves of the replayed position, and the PV stops at the
        //first one that doesn't match: stored PVs can end with a repeated move.
        //With an evalTable, each move is followed by the static eval of the position after it, in brackets.
        public static string ConvertPVToSAN(Move[]? pv, string fen, TranspositionTable? evalTable)
        {
            if (pv == null)
            {
                return "";
            }

            Board pvBoard = new();
            pvBoard.InitialiseFromFEN(fen);
            int rootSide = pvBoard.OnMove;
            string pvString = "";

            for (int nn = pv.Length - 1; nn >= 0; nn--)
            {
                if (pv[nn].From == 0 && pv[nn].To == 0)
                {
                    break;
                }
                if (!TryFindLegalMove(pv[nn], ref pvBoard, out Move legalMove))
                {
                    break;
                }
                pvString += ConvertMoveToSAN(legalMove, ref pvBoard);
                pvBoard.MakeMove(legalMove, pvBoard.OnMove, false);
                if (evalTable != null)
                {
                    TranspositionTable table = evalTable;
                    pvString += " (" + FormatEval(StaticEval(ref pvBoard, ref table, rootSide)) + ")";
                }
                pvString += " ";
            }

            return pvString.Trim();
        }


        //Full static eval (no lazy cut-off) of the position, from rootSide's point of view.
        //ScorePosition scores for the side not on move, tempo included, so flip to White first.
        private static int StaticEval(ref Board theBoard, ref TranspositionTable evalTable, int rootSide)
        {
            int score = Scorer.ScorePosition(ref theBoard, ref evalTable, -5000, 5000, false, 0);
            int whitePOV = theBoard.OnMove == BLACK ? score : -score;
            return rootSide == WHITE ? whitePOV : -whitePOV;
        }


        private static string FormatEval(int eval)
        {
            return eval.ToString("+0;-0;0");
        }


        private static bool TryFindLegalMove(Move wanted, ref Board theBoard, out Move found)
        {
            Move[] allMoves = theBoard.GenerateAllMoves(theBoard.OnMove);
            foreach (Move m in allMoves)
            {
                if (m.From == wanted.From && m.To == wanted.To && m.PromotionPiece == wanted.PromotionPiece &&
                    theBoard.MoveIsLegal(m, theBoard.OnMove))
                {
                    found = m;
                    return true;
                }
            }
            found = new Move();
            return false;
        }


        //Full SAN for a move in the position before it is played: adds disambiguation (Nbd7, R1e2),
        //promotion (e8=Q) and check or mate (+, #) to ConvertMoveToAlgebraic's output
        private static string ConvertMoveToSAN(Move theMove, ref Board theBoard)
        {
            string san = ConvertMoveToAlgebraic(theMove, ref theBoard).Replace("ep", "");

            int movingPiece = theBoard.Piece[theMove.From];
            if (movingPiece != PAWN && movingPiece != KING)
            {
                san = san.Substring(0, 1) + Disambiguation(theMove, ref theBoard) + san.Substring(1);
            }

            if (theMove.PromotionPiece != 0)
            {
                san += "=" + "NBRQ"[theMove.PromotionPiece - 1];
            }

            theBoard.MakeMove(theMove, theBoard.OnMove, false);
            if (theBoard.IsInCheck(theBoard.OnMove))
            {
                san += HasLegalMove(ref theBoard) ? "+" : "#";
            }
            theBoard.UnmakeLastMove();

            return san;
        }


        //The from-file, from-rank, or both, needed when another piece of the same type can also reach the target square
        private static string Disambiguation(Move theMove, ref Board theBoard)
        {
            bool clash = false; bool sameFile = false; bool sameRank = false;

            Move[] allMoves = theBoard.GenerateAllMoves(theBoard.OnMove);
            foreach (Move m in allMoves)
            {
                if (m.To == theMove.To && m.From != theMove.From &&
                    theBoard.Piece[m.From] == theBoard.Piece[theMove.From] && theBoard.MoveIsLegal(m, theBoard.OnMove))
                {
                    clash = true;
                    if (m.From % 8 == theMove.From % 8)
                    {
                        sameFile = true;
                    }
                    if (m.From / 8 == theMove.From / 8)
                    {
                        sameRank = true;
                    }
                }
            }

            if (!clash)
            {
                return "";
            }

            string fromFile = ((char)('a' + theMove.From % 8)).ToString();
            string fromRank = (8 - theMove.From / 8).ToString();
            if (!sameFile)
            {
                return fromFile;
            }
            if (!sameRank)
            {
                return fromRank;
            }
            return fromFile + fromRank;
        }


        private static bool HasLegalMove(ref Board theBoard)
        {
            Move[] allMoves = theBoard.GenerateAllMoves(theBoard.OnMove);
            foreach (Move m in allMoves)
            {
                if (theBoard.MoveIsLegal(m, theBoard.OnMove))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
