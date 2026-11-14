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


        //Everything the evaluation takes from the pawns alone. The scores above the passed pawn masks are already
        //weighted. The masks have one bit per square (bit 0 is a8). The counts below them are unweighted, and the
        //StormFiles come from Scorer.PawnStormFiles. Filled is 1 in every stored entry, so an empty slot never matches
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
            public ulong WhiteHoles;
            public ulong BlackHoles;
            public ulong WhitePawnAttacks;
            public ulong BlackPawnAttacks;
            public ushort WhiteStormFiles;
            public ushort BlackStormFiles;
            public byte WhitePhalanxPawns;
            public byte BlackPhalanxPawns;
            public byte WhiteSpace;
            public byte BlackSpace;
            public byte WhiteUnopposedIsolatedPawns;
            public byte BlackUnopposedIsolatedPawns;
            public byte WhiteCandidatePassedPawns;
            public byte BlackCandidatePassedPawns;
            public byte WhitePawnIslands;
            public byte BlackPawnIslands;
            public byte WhiteAdvancedFlankPawns;
            public byte BlackAdvancedFlankPawns;
            public byte WhitePawnlessFiles;
            public byte BlackPawnlessFiles;
            public byte Filled;
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
            //Orders moves by score, highest first, so sorting puts the best scored moves at the front
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

        //One side's pieces as bit masks, one bit per square (bit 0 is a8): every square it occupies, its king, where
        //its knights, bishops, rooks and queens stand, and the squares each of those kinds bears on. Filled by
        //Board.GeneratePieceAttacksWithoutKing
        public struct PieceAttackMasks
        {
            public ulong Occupied;
            public ulong Kings;
            public ulong PieceSquares;
            public ulong Knight;
            public ulong Bishop;
            public ulong Rook;
            public ulong Queen;
        }

        //One knight, bishop, rook or queen: the squares it bears on (bit 0 is a8), where it stands and what it is
        public struct PieceAttacks
        {
            public ulong Attacks;
            public byte Square;
            public byte Piece;
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
            MakeBook = 9,
            VerifyBook = 10
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
