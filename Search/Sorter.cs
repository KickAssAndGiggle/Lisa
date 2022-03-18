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

        public static Move[] GetSortedMoves(ref Board TheBoard, int RootMoveKey, bool CapturesOnly, bool NonCapturesOnly, bool LowDepth = false)
        {

            Move[] List;
            if (CapturesOnly)
            {
                if (LowDepth)
                {
                    List = TheBoard.GenerateCaptureMovesWithScore(TheBoard.OnMove);
                    return List;
                }
                else
                {
                    List = TheBoard.GenerateCaptureMoves(TheBoard.OnMove);
                }
            }
            else if (NonCapturesOnly)
            {
                List = TheBoard.GenerateNonCaptureMoves(TheBoard.OnMove);
            }
            else
            {
                List = TheBoard.GenerateAllMoves(TheBoard.OnMove);
            }

            int TotalMaterial = (TheBoard.WhiteMaterial + TheBoard.BlackMaterial);
            float EarlyRatio = (float)TotalMaterial / (float)MaxMaterial; float LateRatio = 1 - EarlyRatio;

            for (int NN = 0; NN < List.Length; NN++)
            {
                if (!LowDepth)
                {
                    TheBoard.MakeMove(List[NN], TheBoard.OnMove, false);
                    if (TheBoard.IsInCheck(TheBoard.OnMove))
                    {
                        List[NN].Score = 4950;
                        TheBoard.UnmakeLastMove();
                        continue;
                    }
                    else
                    {
                        TheBoard.UnmakeLastMove();
                    }
                }
                if (List[NN].IsCapture)
                {
                    if (TheBoard.Piece[List[NN].To] != -1)
                    {
                        //No en-pasant
                        List[NN].Score = 4000 + TheBoard.See(List[NN].To);
                        if (List[NN].Score == 4000)
                        {
                            List[NN].Score += TheBoard.Piece[List[NN].To];
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
                    int piecePressure = Material[TheBoard.Piece[List[NN].From]];
                    if (piecePressure == 0)
                    {
                        piecePressure = 75;
                    }
                    if (TheBoard.OnMove == WHITE)
                    {
                        List[NN].Score += (((TheBoard.WhitePressureMap[List[NN].To] - piecePressure) - TheBoard.WhitePressureMap[List[NN].From]) / 2);
                    }
                    else
                    {
                        List[NN].Score += (((TheBoard.BlackPressureMap[List[NN].To] - piecePressure) - TheBoard.BlackPressureMap[List[NN].From]) / 2);
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
