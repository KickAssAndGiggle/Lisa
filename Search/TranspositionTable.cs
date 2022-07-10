using static Lisa.Globals;
namespace Lisa
{
    public sealed class TranspositionTable
    {

        private TTMove[] PrimaryMoveList;
        private TTScore[] ScoreList;
        private TTPawnAnalysis[] PawnStructList;

        private readonly int TTSize;
        private readonly int ScoreSize;
        private readonly int PassedPawnSize;

        public TranspositionTable()
        {

            int TTByteSize;
            int ScoreByteSize;
            int PawnByteSize;

            unsafe
            {
                TTByteSize = sizeof(TTMove);
                ScoreByteSize = sizeof(TTScore);
                PawnByteSize = sizeof(TTPawnAnalysis);
            }

            int TotalAllowedTTSize = TTHashSizeMB * 1024 * 1024;
            int TotalAllowedScoreSize = PositionScoreHashSizeMB * 1024 * 1024;
            int TotalAllowedPassedPawnSize = PawnStructureHashSizeMB * 1024 * 1024;

            TTSize = TotalAllowedTTSize / TTByteSize;
            ScoreSize = TotalAllowedScoreSize / ScoreByteSize;
            PassedPawnSize = TotalAllowedPassedPawnSize / PawnByteSize;

            PrimaryMoveList = new TTMove[TTSize];
            ScoreList = new TTScore[ScoreSize];
            PawnStructList = new TTPawnAnalysis[PassedPawnSize];

        }


        private int MakeTTHash(long Zobrist)
        {
            return (int)Math.Abs(Zobrist % TTSize);
        }

        private int MakeScoreTTHash(long Zobrist)
        {
            return (int)Math.Abs(Zobrist % ScoreSize);
        }

        private int MakePawnTTHash(long PawnOnlyZobrist)
        {
            return (int)Math.Abs(PawnOnlyZobrist % PassedPawnSize);
        }

        public void AddToTranstable(long Zobrist, int PosScore, byte PosDepth, Move PosBestResponse, byte PosType)
        {

            int TTHash = MakeTTHash(Zobrist);
            byte PrimaryCurrentDepth;
            if (PrimaryMoveList[TTHash].PositionFlag >= 200)
            {
                PrimaryCurrentDepth = (byte)(PrimaryMoveList[TTHash].PositionFlag - 200);
            }
            else if (PrimaryMoveList[TTHash].PositionFlag >= 100)
            {
                PrimaryCurrentDepth = (byte)(PrimaryMoveList[TTHash].PositionFlag - 100);
            }
            else
            {
                PrimaryCurrentDepth = (byte)(PrimaryMoveList[TTHash].PositionFlag);
            }

            if (PrimaryCurrentDepth < PosDepth)
            {
                PrimaryMoveList[TTHash].PositionFlag = (byte)((PosType * 100) + PosDepth);
                PrimaryMoveList[TTHash].BestResponseFrom = PosBestResponse.From;
                PrimaryMoveList[TTHash].BestResponseTo = PosBestResponse.To;
                PrimaryMoveList[TTHash].Score = PosScore;
                PrimaryMoveList[TTHash].Zobrist = Zobrist;
                PrimaryMoveList[TTHash].MoveAttributeFlag = (byte)((PosBestResponse.IsCapture ? 100 : 0) + PosBestResponse.PromotionPiece);
            }

        }


        public void AddPawnStructureToTransTable(long PawnOnlyZobrist, int WhitePPScore, int BlackPPScore, int WhiteBWPScore,
            int BlackBWPScore, int WhiteChainScore, int BlackChainPawn, int WhiteDblPawnScore, int BlackDblPawnScore,
            int WhiteIsoPawnScore, int BlackIsoPawnScore)
        {

            int TTPPHash = MakePawnTTHash(PawnOnlyZobrist);
            PawnStructList[TTPPHash].WhitePassedPawnScore = WhitePPScore;
            PawnStructList[TTPPHash].BlackPassedPawnScore = BlackPPScore;
            PawnStructList[TTPPHash].WhiteBackwardsPawnScore = WhiteBWPScore;
            PawnStructList[TTPPHash].BlackBackwardPawnScore = BlackBWPScore;
            PawnStructList[TTPPHash].WhitePawnChainScore = WhiteChainScore;
            PawnStructList[TTPPHash].BlackPawnChainScore = BlackChainPawn;
            PawnStructList[TTPPHash].WhiteDoubledPawnScore = WhiteDblPawnScore;
            PawnStructList[TTPPHash].BlackDoubledPawnScore = BlackDblPawnScore;
            PawnStructList[TTPPHash].WhiteIsolatedPawnScore = WhiteIsoPawnScore;
            PawnStructList[TTPPHash].BlackIsolatedPawnScore = BlackIsoPawnScore;

            PawnStructList[TTPPHash].Zobrist = PawnOnlyZobrist;

        }


        public bool LookupPPScore(long Zobrist, out TTPawnAnalysis PosScore)
        {

            int TTHash = MakePawnTTHash(Zobrist);
            if (PawnStructList[TTHash].Zobrist == Zobrist)
            {
                PosScore = PawnStructList[TTHash];
                return true;
            }
            else
            {
                PosScore = new TTPawnAnalysis();
                return false;
            }
        }


        public void AddScoreToTransTable(long Zobrist, int WhiteScore, int BlackScore)
        {

            int TTHash = MakeScoreTTHash(Zobrist);
            ScoreList[TTHash].WhiteScore = WhiteScore;
            ScoreList[TTHash].BlackScore = BlackScore;
            ScoreList[TTHash].Zobrist = Zobrist;

        }


        public bool LookupScore(long Zobrist, out TTScore PosScore)
        {

            int TTHash = MakeScoreTTHash(Zobrist);
            if (ScoreList[TTHash].Zobrist == Zobrist)
            {
                PosScore = ScoreList[TTHash];
                return true;
            }
            else
            {
                PosScore = new TTScore();
                return false;
            }

        }

        public bool LookupPos(long Zobrist, out TTMove PosEntry)
        {

            int TTHash = MakeTTHash(Zobrist);
            if (PrimaryMoveList[TTHash].Zobrist == Zobrist)
            {
                PosEntry = PrimaryMoveList[TTHash];
                return true;
            }
            else
            {
                PosEntry = new TTMove();
                return false;
            }

        }

        public void Clear()
        {
            PrimaryMoveList = new TTMove[TTSize];
            ScoreList = new TTScore[ScoreSize];
            PawnStructList = new TTPawnAnalysis[PassedPawnSize];
            GC.Collect();
        }

    }
}
