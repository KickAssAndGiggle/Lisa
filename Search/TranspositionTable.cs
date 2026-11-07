using static Lisa.Globals;
namespace Lisa
{
    //Shared by every search thread, without locks. Two threads can write the same entry at once, or one read it while
    //another writes it, so each entry is built whole, with its Zobrist XORed with a check value made from its own
    //data. A reader copies the entry before checking it: a torn mix of two writes no longer matches and reads as a miss
    public sealed class TranspositionTable
    {

        private TTMove[] _primaryMoveList;
        private TTScore[] _scoreList;
        private TTPawnAnalysis[] _pawnStructList;

        //The move table is in buckets of two entries. The first keeps the deepest search of any position hashing to
        //the bucket, the second always takes the newest store, so a crowd of deep entries can't lock out fresh results
        private readonly int _ttSize;
        private readonly int _ttBuckets;
        private readonly int _scoreSize;
        private readonly int _passedPawnSize;

        //instrumentationOnly gives every table a single entry, for callers outside search that only need
        //ScorePosition to run. Lookups check the full Zobrist, so a tiny table just means misses, which recalculate.
        public TranspositionTable(bool instrumentationOnly = false)
        {

            if (instrumentationOnly)
            {
                _ttSize = 1;
                _scoreSize = 1;
                _passedPawnSize = 1;
            }
            else
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
            }

            _ttBuckets = Math.Max(1, _ttSize / 2);
            _primaryMoveList = new TTMove[_ttBuckets * 2];
            _scoreList = new TTScore[_scoreSize];
            _pawnStructList = new TTPawnAnalysis[_passedPawnSize];

        }


        //Index of the bucket's first entry
        private int MakeTTBucket(long zobrist)
        {
            return (int)Math.Abs(zobrist % _ttBuckets) * 2;
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

            int bucket = MakeTTBucket(zobrist);
            byte deepestDepth;
            if (_primaryMoveList[bucket].PositionFlag >= 200)
            {
                deepestDepth = (byte)(_primaryMoveList[bucket].PositionFlag - 200);
            }
            else if (_primaryMoveList[bucket].PositionFlag >= 100)
            {
                deepestDepth = (byte)(_primaryMoveList[bucket].PositionFlag - 100);
            }
            else
            {
                deepestDepth = (byte)(_primaryMoveList[bucket].PositionFlag);
            }

            TTMove entry = new()
            {
                PositionFlag = (byte)((posType * 100) + posDepth),
                BestResponseFrom = bestResponse.From,
                BestResponseTo = bestResponse.To,
                Score = posScore,
                MoveAttributeFlag = (byte)((bestResponse.IsCapture ? 100 : 0) + bestResponse.PromotionPiece)
            };
            entry.Zobrist = zobrist ^ MoveEntryCheck(entry);

            if (deepestDepth < posDepth)
            {
                _primaryMoveList[bucket] = entry;
            }
            else
            {
                _primaryMoveList[bucket + 1] = entry;
            }

        }


        public void AddPawnStructureToTransTable(long pawnOnlyZobrist, int whitePPScore, int blackPPScore,
            int whiteBWPScore,
            int blackBWPScore, int whiteChainScore, int blackChainPawn, int whiteDblPawnScore, int blackDblPawnScore,
            int whiteIsoPawnScore, int blackIsoPawnScore, ulong whitePassedPawns, ulong blackPassedPawns)
        {

            int ttPPHash = MakePawnTTHash(pawnOnlyZobrist);
            TTPawnAnalysis entry = new()
            {
                WhitePassedPawnScore = whitePPScore,
                BlackPassedPawnScore = blackPPScore,
                WhiteBackwardsPawnScore = whiteBWPScore,
                BlackBackwardPawnScore = blackBWPScore,
                WhitePawnChainScore = whiteChainScore,
                BlackPawnChainScore = blackChainPawn,
                WhiteDoubledPawnScore = whiteDblPawnScore,
                BlackDoubledPawnScore = blackDblPawnScore,
                WhiteIsolatedPawnScore = whiteIsoPawnScore,
                BlackIsolatedPawnScore = blackIsoPawnScore,
                WhitePassedPawns = whitePassedPawns,
                BlackPassedPawns = blackPassedPawns
            };
            entry.Zobrist = pawnOnlyZobrist ^ PawnEntryCheck(entry);
            _pawnStructList[ttPPHash] = entry;

        }


        public bool LookupPPScore(long zobrist, out TTPawnAnalysis posScore)
        {

            int ttHash = MakePawnTTHash(zobrist);
            posScore = _pawnStructList[ttHash];
            if ((posScore.Zobrist ^ PawnEntryCheck(posScore)) == zobrist)
            {
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
            TTScore entry = new()
            {
                WhiteScore = whiteScore,
                BlackScore = blackScore
            };
            entry.Zobrist = zobrist ^ ScoreEntryCheck(entry);
            _scoreList[TTHash] = entry;

        }


        public bool LookupScore(long zobrist, out TTScore posScore)
        {

            int ttHash = MakeScoreTTHash(zobrist);
            posScore = _scoreList[ttHash];
            if ((posScore.Zobrist ^ ScoreEntryCheck(posScore)) == zobrist)
            {
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

            int bucket = MakeTTBucket(zobrist);
            posEntry = _primaryMoveList[bucket];
            if ((posEntry.Zobrist ^ MoveEntryCheck(posEntry)) == zobrist)
            {
                return true;
            }
            posEntry = _primaryMoveList[bucket + 1];
            if ((posEntry.Zobrist ^ MoveEntryCheck(posEntry)) == zobrist)
            {
                return true;
            }
            posEntry = new TTMove();
            return false;

        }


        private static long MoveEntryCheck(TTMove entry)
        {
            return (uint)entry.Score | ((long)entry.BestResponseFrom << 32) | ((long)entry.BestResponseTo << 40) |
                ((long)entry.PositionFlag << 48) | ((long)entry.MoveAttributeFlag << 56);
        }


        private static long ScoreEntryCheck(TTScore entry)
        {
            return (uint)entry.WhiteScore | ((long)entry.BlackScore << 32);
        }


        //Mixes every field, so any torn mix of two writes gives a different value
        private static long PawnEntryCheck(TTPawnAnalysis entry)
        {
            ulong check = (ulong)entry.WhitePassedPawns;
            check = MixIn(check, entry.BlackPassedPawns);
            check = MixIn(check, Pair(entry.WhitePassedPawnScore, entry.BlackPassedPawnScore));
            check = MixIn(check, Pair(entry.WhiteBackwardsPawnScore, entry.BlackBackwardPawnScore));
            check = MixIn(check, Pair(entry.WhitePawnChainScore, entry.BlackPawnChainScore));
            check = MixIn(check, Pair(entry.WhiteDoubledPawnScore, entry.BlackDoubledPawnScore));
            check = MixIn(check, Pair(entry.WhiteIsolatedPawnScore, entry.BlackIsolatedPawnScore));
            return (long)check;
        }


        private static ulong Pair(int low, int high)
        {
            return (uint)low | ((ulong)(uint)high << 32);
        }


        private static ulong MixIn(ulong check, ulong value)
        {
            return (check ^ value) * 0x9E3779B97F4A7C15UL + (check >> 29);
        }


        public void Clear()
        {
            _primaryMoveList = new TTMove[_ttBuckets * 2];
            _scoreList = new TTScore[_scoreSize];
            _pawnStructList = new TTPawnAnalysis[_passedPawnSize];
            GC.Collect();
        }

    }
}
