using static Lisa.Globals;
namespace Lisa
{
    public static class Sorter
    {

        private const int MAX_DEPTH = 30;

#pragma warning disable CA2211 // Non-constant fields should not be visible

        //This is faster than the "correct" practice, and I've tested it enough times
        //to be certain about it by now

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


        public static Move[] GetSortedMoves(ref Board theBoard, int rootMoveKey, bool capturesOnly, bool nonCapturesOnly, bool lowDepth = false)
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
                if (!lowDepth)
                {
                    theBoard.MakeMove(list[nn], theBoard.OnMove, false);
                    if (theBoard.IsInCheck(theBoard.OnMove))
                    {
                        list[nn].Score = 4950;
                        theBoard.UnmakeLastMove();
                        continue;
                    }
                    else
                    {
                        theBoard.UnmakeLastMove();
                    }
                }
                if (list[nn].IsCapture)
                {
                    if (theBoard.Piece[list[nn].To] != -1)
                    {
                        //No en-pasant
                        list[nn].Score = 4000 + theBoard.See(list[nn].To);
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
                else
                {

                    int moveKey = list[nn].From * 100 + list[nn].To;
                    list[nn].Score = History[moveKey];
                    list[nn].Score += (SingleMoveHistory[rootMoveKey, moveKey]);

                }
            }

            Array.Sort(list);
            return list;

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
