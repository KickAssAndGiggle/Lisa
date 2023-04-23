using static Lisa.Globals;
namespace Lisa
{
    public sealed class TranspositionTable
    {

        private TTMove[] _primaryMoveList;
        private TTScore[] _scoreList;
        private TTPawnAnalysis[] _pawnStructList;

        private readonly int _ttSize;
        private readonly int _scoreSize;
        private readonly int _passedPawnSize;

        public TranspositionTable()
        {

            int ttByteSize;
            int scoreByteSize;
            int pawnByteSize;

            unsafe
            {
                ttByteSize = sizeof(TTMove);
                scoreByteSize = sizeof(TTScore);
                pawnByteSize = sizeof(TTPawnAnalysis);
            }

            int totalAllowed_ttSize = TT_HASH_SIZE_MB * 1024 * 1024;
            int totalAllowed_scoreSize = POSITION_SCORE_HASH_SIZE_MB * 1024 * 1024;
            int totalAllowed_passedPawnSize = PAWN_STRUCTURE_HASH_SIZE_MB * 1024 * 1024;

            _ttSize = totalAllowed_ttSize / ttByteSize;
            _scoreSize = totalAllowed_scoreSize / scoreByteSize;
            _passedPawnSize = totalAllowed_passedPawnSize / pawnByteSize;

            _primaryMoveList = new TTMove[_ttSize];
            _scoreList = new TTScore[_scoreSize];
            _pawnStructList = new TTPawnAnalysis[_passedPawnSize];

        }


        private int MakeTTHash(long zobrist)
        {
            return (int)Math.Abs(zobrist % _ttSize);
        }

        private int MakeScoreTTHash(long zobrist)
        {
            return (int)Math.Abs(zobrist % _scoreSize);
        }

        private int MakePawnTTHash(long pawnOnlyZobrist)
        {
            return (int)Math.Abs(pawnOnlyZobrist % _passedPawnSize);
        }

        public void AddToTranstable(long zobrist, int posScore, byte posDepth, Move bestResponse, byte posType)
        {

            int ttHash = MakeTTHash(zobrist);
            byte primaryCurrentDepth;
            if (_primaryMoveList[ttHash].PositionFlag >= 200)
            {
                primaryCurrentDepth = (byte)(_primaryMoveList[ttHash].PositionFlag - 200);
            }
            else if (_primaryMoveList[ttHash].PositionFlag >= 100)
            {
                primaryCurrentDepth = (byte)(_primaryMoveList[ttHash].PositionFlag - 100);
            }
            else
            {
                primaryCurrentDepth = (byte)(_primaryMoveList[ttHash].PositionFlag);
            }

            if (primaryCurrentDepth < posDepth)
            {
                _primaryMoveList[ttHash].PositionFlag = (byte)((posType * 100) + posDepth);
                _primaryMoveList[ttHash].BestResponseFrom = bestResponse.From;
                _primaryMoveList[ttHash].BestResponseTo = bestResponse.To;
                _primaryMoveList[ttHash].Score = posScore;
                _primaryMoveList[ttHash].Zobrist = zobrist;
                _primaryMoveList[ttHash].MoveAttributeFlag = (byte)((bestResponse.IsCapture ? 100 : 0) + bestResponse.PromotionPiece);
            }

        }


        public void AddPawnStructureToTransTable(long pawnOnlyZobrist, int whitePPScore, int blackPPScore, int whiteBWPScore,
            int blackBWPScore, int whiteChainScore, int blackChainPawn, int whiteDblPawnScore, int blackDblPawnScore,
            int whiteIsoPawnScore, int blackIsoPawnScore)
        {

            int ttPPHash = MakePawnTTHash(pawnOnlyZobrist);
            _pawnStructList[ttPPHash].WhitePassedPawnScore = whitePPScore;
            _pawnStructList[ttPPHash].BlackPassedPawnScore = blackPPScore;
            _pawnStructList[ttPPHash].WhiteBackwardsPawnScore = whiteBWPScore;
            _pawnStructList[ttPPHash].BlackBackwardPawnScore = blackBWPScore;
            _pawnStructList[ttPPHash].WhitePawnChainScore = whiteChainScore;
            _pawnStructList[ttPPHash].BlackPawnChainScore = blackChainPawn;
            _pawnStructList[ttPPHash].WhiteDoubledPawnScore = whiteDblPawnScore;
            _pawnStructList[ttPPHash].BlackDoubledPawnScore = blackDblPawnScore;
            _pawnStructList[ttPPHash].WhiteIsolatedPawnScore = whiteIsoPawnScore;
            _pawnStructList[ttPPHash].BlackIsolatedPawnScore = blackIsoPawnScore;

            _pawnStructList[ttPPHash].Zobrist = pawnOnlyZobrist;

        }


        public bool LookupPPScore(long zobrist, out TTPawnAnalysis posScore)
        {

            int ttHash = MakePawnTTHash(zobrist);
            if (_pawnStructList[ttHash].Zobrist == zobrist)
            {
                posScore = _pawnStructList[ttHash];
                return true;
            }
            else
            {
                posScore = new TTPawnAnalysis();
                return false;
            }
        }


        public void AddScoreToTransTable(long zobrist, int whiteScore, int blackScore)
        {

            int TTHash = MakeScoreTTHash(zobrist);
            _scoreList[TTHash].WhiteScore = whiteScore;
            _scoreList[TTHash].BlackScore = blackScore;
            _scoreList[TTHash].Zobrist = zobrist;

        }


        public bool LookupScore(long zobrist, out TTScore posScore)
        {

            int ttHash = MakeScoreTTHash(zobrist);
            if (_scoreList[ttHash].Zobrist == zobrist)
            {
                posScore = _scoreList[ttHash];
                return true;
            }
            else
            {
                posScore = new TTScore();
                return false;
            }

        }

        public bool LookupPos(long zobrist, out TTMove posEntry)
        {

            int ttHash = MakeTTHash(zobrist);
            if (_primaryMoveList[ttHash].Zobrist == zobrist)
            {
                posEntry = _primaryMoveList[ttHash];
                return true;
            }
            else
            {
                posEntry = new TTMove();
                return false;
            }

        }

        public void Clear()
        {
            _primaryMoveList = new TTMove[_ttSize];
            _scoreList = new TTScore[_scoreSize];
            _pawnStructList = new TTPawnAnalysis[_passedPawnSize];
            GC.Collect();
        }

    }
}
