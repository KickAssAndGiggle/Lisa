namespace Lisa
{
    //The structs and enums. They are nested in Globals, so code reaches them through using static Lisa.Globals
    public static partial class Globals
    {

        #region Public Global Enums And Structs

        public struct TTMove
        {
            public byte BestResponseFrom;
            public byte BestResponseTo;
            public int Score;
            public long Zobrist;
            public byte PositionFlag;
            public byte MoveAttributeFlag;
        }

        public struct TTScore
        {
            public int WhiteScore;
            public int BlackScore;
            public long Zobrist;
        }


        public struct TTPawnAnalysis
        {
            public int WhitePassedPawnScore;
            public int BlackPassedPawnScore;
            public int WhiteBackwardsPawnScore;
            public int BlackBackwardPawnScore;
            public int WhitePawnChainScore;
            public int BlackPawnChainScore;
            public int WhiteIsolatedPawnScore;
            public int BlackIsolatedPawnScore;
            public int WhiteDoubledPawnScore;
            public int BlackDoubledPawnScore;
            public ulong WhitePassedPawns;
            public ulong BlackPassedPawns;
            public long Zobrist;
        }

        public struct BookMove
        {
            public byte From;
            public byte To;
            public int Weight;
        }

        public struct Move : IComparable<Move>
        {
            public byte From;
            public byte To;
            public bool IsCapture;
            public byte PromotionPiece;
            public int Score;
            public int CompareTo(Move obj)
            {
                if (obj.Score == Score)
                {
                    return 0;
                }
                else
                {
                    return obj.Score < Score ? -1 : 1;
                }
            }
        }

        public enum ProgramMode
        {
            Unknown = 0,
            UCI = 1,
            Perft = 2,
            MultiFen = 3,
            FenToZobrist = 4,
            EPD = 5,
            MakeTexelFile = 6,
            TexelTune = 7,
            TexelTunePST = 8,
            MakeBook = 9
        }

        public enum UCILogLevels
        {
            None = 0,
            Standard = 1,
            Heavy = 2
        }

        public enum NodeTypes
        {
            All = 0,
            Cut = 1,
            PV = 2,
            Unknown = 3
        };

        #endregion

    }
}
