using static Lisa.Globals;
namespace Lisa
{
    public static class Sorter
    {

        private const int MAX_DEPTH = 30;

        //Quiet move history, by side to move, from square and to square. Every update pulls the value towards +/-QUIET_HISTORY_MAX
        //in proportion to how far it still has to go, so values stay bounded and recent results can overturn old ones
        private const int QUIET_HISTORY_MAX = 16384;
        private const int QUIET_HISTORY_BONUS_CAP = 1200;

#pragma warning disable CA2211 // Non-constant fields should not be visible

        //This is faster than the "correct" practice, and I've tested it enough times
        //to be certain about it by now

        public static int[] QuietHistory = new int[2 * 64 * 64];
        public static int[] History;
        public static int[,] SingleMoveHistory;
        public static int[] CutCount;
        public static int[,] CutCountSingleMove;

        public static Move[] KillerOnes;
        public static Move[] KillerTwos;
        public static int[] FirstKillerPieces;
        public static int[] SecondKillerPieces;
        public static int[,,] KillerScores;

        public static Move[] Refutations;
        public static int[] RefutationPieces;

#pragma warning restore CA2211 // Non-constant fields should not be visible

        static Sorter()
        {
            History = new int[6363];
            SingleMoveHistory = new int[6363, 6363];
            CutCount = new int[6363];
            CutCountSingleMove = new int[6363, 6363];

            //Create a new array of Killer Move trackers, for each From Square/To Square/Depth
            KillerScores = new int[64, 64, MAX_DEPTH];
            KillerOnes = new Move[MAX_DEPTH];
            KillerTwos = new Move[MAX_DEPTH];
            FirstKillerPieces = new int[MAX_DEPTH];
            SecondKillerPieces = new int[MAX_DEPTH];

            Refutations = new Move[MAX_DEPTH];
            RefutationPieces = new int[MAX_DEPTH];
        }


        public static Move[] GetSortedMoves(ref Board theBoard, int rootMoveKey, bool capturesOnly, bool nonCapturesOnly, 
            bool lowDepth = false)
        {

            Move[] list;
            if (capturesOnly)
            {
                if (lowDepth)
                {
                    list = theBoard.GenerateCaptureMovesWithScore(theBoard.OnMove);
                    return list;
                }
                else
                {
                    list = theBoard.GenerateCaptureMoves(theBoard.OnMove);
                }
            }
            else if (nonCapturesOnly)
            {
                list = theBoard.GenerateNonCaptureMoves(theBoard.OnMove);
            }
            else
            {
                list = theBoard.GenerateAllMoves(theBoard.OnMove);
            }

            for (int nn = 0; nn < list.Length; nn++)
            {
                if (list[nn].IsCapture)
                {
                    if (theBoard.Piece[list[nn].To] != -1)
                    {
                        //No en-pasant
                        list[nn].Score = 4000 + theBoard.See(list[nn].To, list[nn].From);
                        if (list[nn].Score == 4000)
                        {
                            list[nn].Score += theBoard.Piece[list[nn].To];
                        }
                    }
                    else
                    {
                        //en-pasant: as a one-off chance, score it highly
                        list[nn].Score = 4750;
                    }
                }
                else if (USE_QUIET_HISTORY)
                {
                    list[nn].Score = QuietHistory[QuietHistoryIndex(theBoard.OnMove, list[nn])];
                }
                else
                {
                    //Previous ordering, kept so the two can be compared on the same build (QuietHistory=false in Settings.ini)
                    int moveKey = list[nn].From * 100 + list[nn].To;
                    list[nn].Score = History[moveKey];
                    list[nn].Score += SingleMoveHistory[rootMoveKey, moveKey];
                }
            }

            Array.Sort(list);
            return list;

        }


        //Called with the node's side to move and remaining depth: the quiet move that caused a cutoff gets a bonus, and every
        //quiet move searched before it in that node gets the same amount taken off
        public static void RewardQuietMove(int side, Move move, int depth)
        {
            UpdateQuietHistory(QuietHistoryIndex(side, move), Math.Min(depth * depth, QUIET_HISTORY_BONUS_CAP));
        }


        public static void PenaliseQuietMove(int side, Move move, int depth)
        {
            UpdateQuietHistory(QuietHistoryIndex(side, move), -Math.Min(depth * depth, QUIET_HISTORY_BONUS_CAP));
        }


        private static void UpdateQuietHistory(int index, int bonus)
        {
            QuietHistory[index] += bonus - QuietHistory[index] * Math.Abs(bonus) / QUIET_HISTORY_MAX;
        }


        private static int QuietHistoryIndex(int side, Move move)
        {
            return (side == WHITE ? 0 : 4096) + move.From * 64 + move.To;
        }


        public static void UpdateHistoryStandard(int rootMoveKey, int moveKey, int depth, bool wasCut)
        {
            History[moveKey] += (depth * depth * depth);
            SingleMoveHistory[rootMoveKey, moveKey] += (depth * depth * depth * depth);
            if (wasCut)
            {
                CutCount[moveKey] += 1;
                CutCountSingleMove[rootMoveKey, moveKey] += 1;
            }
        }


        public static void UpdateHistoryAggressive(int rootMoveKey, int moveKey, int depth, bool wasCut)
        {
            History[moveKey] += (depth * depth * depth * depth);
            SingleMoveHistory[rootMoveKey, moveKey] += (depth * depth * depth * depth * depth);
            if (wasCut)
            {
                CutCount[moveKey] += 1;
                CutCountSingleMove[rootMoveKey, moveKey] += 1;
            }
        }

        public static void ReduceHistory(int rootMoveKey, int moveKey, int depth)
        {
            History[moveKey] -= (depth * depth);
            SingleMoveHistory[rootMoveKey, moveKey] -= depth;
        }


        public static void IncreaseKillerScore(int fullDepth, int depth, Move kMove, int piece, int score)
        {
            KillerScores[kMove.From, kMove.To, fullDepth - depth] += score;
            ReplaceKillerIfAppropriate(kMove, fullDepth - depth, piece);
        }


        private static void ReplaceKillerIfAppropriate(Move kMove, int killerDepth, int killerPiece)
        {

            if ((kMove.From == KillerOnes[killerDepth].From && kMove.To == KillerOnes[killerDepth].To) ||
                (kMove.From == KillerTwos[killerDepth].From && kMove.To == KillerTwos[killerDepth].To))
            {
                return;
            }

            if (KillerOnes[killerDepth].From == 0 && KillerOnes[killerDepth].To == 0)
            {
                KillerOnes[killerDepth] = kMove;
                FirstKillerPieces[killerDepth] = killerPiece;
            }
            else
            {
                if (KillerScores[kMove.From, kMove.To, killerDepth] > KillerScores[KillerOnes[killerDepth].From, KillerOnes[killerDepth].To, killerDepth])
                {
                    KillerTwos[killerDepth] = KillerOnes[killerDepth];
                    SecondKillerPieces[killerDepth] = FirstKillerPieces[killerDepth];
                    KillerOnes[killerDepth] = kMove;
                    FirstKillerPieces[killerDepth] = killerPiece;
                }
                else
                {
                    if (KillerTwos[killerDepth].From == 0 && KillerTwos[killerDepth].To == 0)
                    {
                        KillerTwos[killerDepth] = kMove;
                        SecondKillerPieces[killerDepth] = killerPiece;
                    }
                    else
                    {
                        if (KillerScores[kMove.From, kMove.To, killerDepth] > KillerScores[KillerTwos[killerDepth].From, KillerTwos[killerDepth].To, killerDepth])
                        {
                            KillerTwos[killerDepth] = kMove;
                            SecondKillerPieces[killerDepth] = killerPiece;
                        }
                    }
                }
            }

        }


        public static void SetRefutationMove(int fullDepth, int refutationDepth, Move rMove, int fromSquarePiece)
        {
            Refutations[fullDepth - refutationDepth] = rMove;
            RefutationPieces[fullDepth - refutationDepth] = fromSquarePiece;
        }


        public static void Reset()
        {
            Array.Clear(QuietHistory);
            History = new int[6363];
            SingleMoveHistory = new int[6363, 6363];
            CutCount = new int[6363];
            CutCountSingleMove = new int[6363, 6363];
            KillerScores = new int[64, 64, MAX_DEPTH];
            KillerOnes = new Move[MAX_DEPTH];
            KillerTwos = new Move[MAX_DEPTH];
            FirstKillerPieces = new int[MAX_DEPTH];
            SecondKillerPieces = new int[MAX_DEPTH];
            Refutations = new Move[MAX_DEPTH];
            RefutationPieces = new int[MAX_DEPTH];
        }

    }

}
