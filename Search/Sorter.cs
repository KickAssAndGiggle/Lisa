using static Lisa.Globals;
namespace Lisa
{
    public static class Sorter
    {

        public static int[] History;
        public static int[,] SingleMoveHistory;

        public static Move[] KillerOnes;
        public static Move[] KillerTwos;
        public static int[] FirstKillerPieces;
        public static int[] SecondKillerPieces;
        public static int[,,] KillerScores;

        public static Move[] Refutations;
        public static int[] RefutationPieces;


        public static void InitialiseSorter(byte MaxDepth)
        {

            History = new int[6363];
            SingleMoveHistory = new int[6363, 6363];

            //Create a new array of Killer Move trackers, for each From Square/To Square/Depth
            KillerScores = new int[64, 64, MaxDepth];
            KillerOnes = new Move[MaxDepth];
            KillerTwos = new Move[MaxDepth];
            FirstKillerPieces = new int[MaxDepth];
            SecondKillerPieces = new int[MaxDepth];

            Refutations = new Move[MaxDepth];
            RefutationPieces = new int[MaxDepth];

        }

        public static Move[] GetSortedMoves(ref Board theBoard, int RootMoveKey, bool CapturesOnly, bool NonCapturesOnly, bool LowDepth = false)
        {

            Move[] List;
            if (CapturesOnly)
            {
                if (LowDepth)
                {
                    List = theBoard.GenerateCaptureMovesWithScore(theBoard.OnMove);
                    return List;
                }
                else
                {
                    List = theBoard.GenerateCaptureMoves(theBoard.OnMove);
                }
            }
            else if (NonCapturesOnly)
            {
                List = theBoard.GenerateNonCaptureMoves(theBoard.OnMove);
            }
            else
            {
                List = theBoard.GenerateAllMoves(theBoard.OnMove);
            }

            int TotalMaterial = (theBoard.WhiteMaterial + theBoard.BlackMaterial);
            float EarlyRatio = (float)TotalMaterial / (float)MaxMaterial; float LateRatio = 1 - EarlyRatio;

            for (int NN = 0; NN < List.Length; NN++)
            {
                if (!LowDepth)
                {
                    theBoard.MakeMove(List[NN], theBoard.OnMove, false);
                    if (theBoard.IsInCheck(theBoard.OnMove))
                    {
                        List[NN].Score = 4950;
                        theBoard.UnmakeLastMove();
                        continue;
                    }
                    else
                    {
                        theBoard.UnmakeLastMove();
                    }
                }
                if (List[NN].IsCapture)
                {
                    if (theBoard.Piece[List[NN].To] != -1)
                    {
                        //No en-pasant
                        List[NN].Score = 4000 + theBoard.See(List[NN].To);
                        if (List[NN].Score == 4000)
                        {
                            List[NN].Score += theBoard.Piece[List[NN].To];
                        }
                    }
                    else
                    {
                        //en-pasant: as a one-off chance, score it highly
                        List[NN].Score = 4750;
                    }
                }
                else
                {

                    int MoveKey = List[NN].From * 100 + List[NN].To;
                    List[NN].Score = History[MoveKey];
                    List[NN].Score += (SingleMoveHistory[RootMoveKey, MoveKey]);
                    int piecePressure = Material[theBoard.Piece[List[NN].From]];
                    if (piecePressure == 0)
                    {
                        piecePressure = 75;
                    }
                    if (theBoard.OnMove == WHITE)
                    {
                        List[NN].Score += (((theBoard.WhitePressureMap[List[NN].To] - piecePressure) - theBoard.WhitePressureMap[List[NN].From]) / 2);
                    }
                    else
                    {
                        List[NN].Score += (((theBoard.BlackPressureMap[List[NN].To] - piecePressure) - theBoard.BlackPressureMap[List[NN].From]) / 2);
                    }

                }
            }

            Array.Sort(List);
            return List;

        }


        public static void UpdateHistoryStandard(int RootMoveKey, int MoveKey, int Depth)
        {
            History[MoveKey] += (Depth * Depth * Depth);
            SingleMoveHistory[RootMoveKey, MoveKey] += (Depth * Depth * Depth * Depth);
        }


        public static void UpdateHistoryAggressive(int RootMoveKey, int MoveKey, int Depth)
        {
            History[MoveKey] += (Depth * Depth * Depth * Depth);
            SingleMoveHistory[RootMoveKey, MoveKey] += (Depth * Depth * Depth * Depth * Depth);
        }


        public static void IncreaseKillerScore(int FullDepth, int Depth, Move M, int Piece, int Score)
        {
            KillerScores[M.From, M.To, FullDepth - Depth] += Score;
            ReplaceKillerIfAppropriate(M, FullDepth - Depth, Piece);
        }


        private static void ReplaceKillerIfAppropriate(Move M, int KillerDepth, int KillerPiece)
        {

            if ((M.From == KillerOnes[KillerDepth].From && M.To == KillerOnes[KillerDepth].To) ||
                (M.From == KillerTwos[KillerDepth].From && M.To == KillerTwos[KillerDepth].To))
            {
                return;
            }

            if (KillerOnes[KillerDepth].From == 0 && KillerOnes[KillerDepth].To == 0)
            {
                KillerOnes[KillerDepth] = M;
                FirstKillerPieces[KillerDepth] = KillerPiece;
            }
            else
            {
                if (KillerScores[M.From, M.To, KillerDepth] > KillerScores[KillerOnes[KillerDepth].From, KillerOnes[KillerDepth].To, KillerDepth])
                {
                    KillerTwos[KillerDepth] = KillerOnes[KillerDepth];
                    SecondKillerPieces[KillerDepth] = FirstKillerPieces[KillerDepth];
                    KillerOnes[KillerDepth] = M;
                    FirstKillerPieces[KillerDepth] = KillerPiece;
                }
                else
                {
                    if (KillerTwos[KillerDepth].From == 0 && KillerTwos[KillerDepth].To == 0)
                    {
                        KillerTwos[KillerDepth] = M;
                        SecondKillerPieces[KillerDepth] = KillerPiece;
                    }
                    else
                    {
                        if (KillerScores[M.From, M.To, KillerDepth] > KillerScores[KillerTwos[KillerDepth].From, KillerTwos[KillerDepth].To, KillerDepth])
                        {
                            KillerTwos[KillerDepth] = M;
                            SecondKillerPieces[KillerDepth] = KillerPiece;
                        }
                    }
                }
            }

        }


        public static void SetRefutationMove(int FullDepth, int RefutationDepth, Move M, int FromSquarePiece)
        {
            Refutations[FullDepth - RefutationDepth] = M;
            RefutationPieces[FullDepth - RefutationDepth] = FromSquarePiece;
        }


    }
}
