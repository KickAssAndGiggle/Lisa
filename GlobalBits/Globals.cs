using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lisa
{
    public static class Globals
    {

        #region Default PST


        public static int[] WhiteKnightEarlyPST = new int[64]
        {
            -60, 0, 0, 0, 0, 0, 0, 0,
            -34, 24, 54, 74, 60, 122, 2, 29,
            -22, 18, 60, 64, 124, 143, 55, 6,
            -14, -4, 25, 33, 10, 33, 14, 43,
            -14, 0, 2, 3, 19, 12, 33, -7,
            -38, -16, 0, 14, 8, 3, 3, -42,
            -56, -31, -28, -1, -7, -20, -42, -11,
            -99, -30, -66, -64, -29, -19, -61, -81

        };
        public static int[] BlackKnightEarlyPST = new int[64];


        public static int[] WhiteKnightLatePST = new int[64]
        {
            -21, -3, 10, 16, 16, 10, -3, -21,
            -7, 12, 25, 31, 31, 25, 12, -7,
            -2, 17, 30, 36, 36, 30, 17, -2,
            -7, 12, 25, 31, 31, 25, 12, -7,
            -22, -3, 10, 16, 16, 10, -3, -22,
            -46, -27, -15, -9, -9, -15, -27, -46,
            -81, -62, -49, -43, -43, -49, -62, -81,
            -99, -99, -94, -88, -88, -94, -99, -99
        };
        public static int[] BlackKnightLatePST = new int[64];


        public static int[] WhiteBishopEarlyPST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            -24, -23, 30, 58, 65, 61, 69, 11,
            7, 27, 20, 56, 91, 108, 53, 44,
            -1, 16, 29, 27, 37, 27, 17, 4,
            1, 5, 23, 32, 21, 8, 17, 4,
            5, 12, 14, 13, 10, -1, 3, 4,
            15, 5, 13, -10, 1, 2, 0, 15,
            -7, 12, -8, -37, -31, -8, -45, -67

        };
        public static int[] BlackBishopEarlyPST = new int[64];


        public static int[] WhiteBishopLatePST = new int[64]
        {
            -2, 4, 8, 10, 10, 8, 4, -2,
            8, 14, 18, 20, 20, 18, 14, 8,
            13, 19, 23, 25, 25, 23, 19, 13,
            14, 20, 24, 26, 26, 24, 20, 14,
            11, 17, 21, 23, 23, 21, 17, 11,
            2, 8, 12, 14, 14, 12, 8, 2,
            -10, -4, 0, 2, 2, 0, -4, -10,
            -27, -21, -17, -15, -15, -17, -21, -27

        };
        public static int[] BlackBishopLatePST = new int[64];


        public static int[] WhiteRookEarlyPST = new int[64]
        {
            84, 0, 0, 37, 124, 0, 0, 153,
            46, 33, 64, 62, 91, 89, 70, 104,
            24, 83, 54, 75, 134, 144, 85, 75,
            19, 33, 46, 57, 53, 39, 53, 16,
            -9, -5, 8, 14, 18, -17, 13, -13,
            -16, 0, 3, -3, 8, -1, 12, 3,
            -26, -6, 2, -2, 2, -10, -1, -29,
            -2, -1, 3, 1, 2, 1, 4, -8

        };
        public static int[] BlackRookEarlyPST = new int[64];


        public static int[] WhiteRookLatePST = new int[64]
        {
            16, 17, 18, 19, 19, 18, 17, 16,
            27, 28, 29, 30, 30, 29, 28, 27,
            25, 27, 28, 28, 28, 28, 27, 25,
            15, 17, 18, 18, 18, 18, 17, 15,
            1, 2, 3, 4, 4, 3, 2, 1,
            -15, -13, -12, -12, -12, -12, -13, -15,
            -27, -25, -24, -24, -24, -24, -25, -27,
            -32, -31, -30, -29, -29, -30, -31, -32

        };
        public static int[] BlackRookLatePST = new int[64];


        public static int[] WhiteKingEarlyPST = new int[64]
        {
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -9, -9, -9, -9, -9, -9, -9,
            0, 0, 0, -9, 0, -9, 25, 0

        };
        public static int[] BlackKingEarlyPST = new int[64];


        public static int[] WhiteKingLatePST = new int[64]
        {
            42, 46, 48, 50, 50, 48, 46, 42,
            38, 41, 44, 45, 45, 44, 41, 38,
            31, 34, 37, 38, 38, 37, 34, 31,
            22, 26, 28, 29, 29, 28, 26, 22,
            11, 15, 17, 18, 18, 17, 15, 11,
            -2, 2, 4, 5, 5, 4, 2, -2,
            -17, -13, -11, -10, -10, -11, -13, -17,
            -34, -30, -28, -27, -27, -28, -30, -34

        };
        public static int[] BlackKingLatePST = new int[64];


        public static int[] WhiteQueenEarlyPST = new int[64]
        {
            -13, 6, -42, 0, 29, 0, 0, 102,
            1, 11, 35, 0, 16, 55, 39, 57,
            -16, 10, 13, 25, 37, 30, 15, 26,
            -6, 0, 15, 25, 32, 9, 26, 12,
            -9, 5, 7, 9, 18, 17, 26, 4,
            -11, 0, 12, 2, 8, 11, 7, -6,
            -7, 3, 2, 5, -1, -10, -7, -2,
            1, -10, -11, 3, -15, -51, -83, -13

        };
        public static int[] BlackQueenEarlyPST = new int[64];


        public static int[] WhiteQueenLatePST = new int[64]
        {
            12, 17, 21, 23, 23, 21, 17, 12,
            21, 26, 30, 31, 31, 30, 26, 21,
            23, 28, 32, 34, 34, 32, 28, 23,
            19, 24, 28, 30, 30, 28, 24, 19,
            9, 14, 17, 19, 19, 17, 14, 9,
            -8, -3, 1, 3, 3, 1, -3, -8,
            -31, -26, -22, -21, -21, -22, -26, -31,
            -61, -55, -52, -50, -50, -52, -55, -61

        };
        public static int[] BlackQueenLatePST = new int[64];


        public static int[] WhitePawnEarlyPST = new int[64]
        {
             0, 0, 0, 0, 0, 0, 0, 0,
             118, 121, 173, 168, 107, 82, -16, 22,
             21, 54, 72, 56, 77, 95, 71, 11,
             9, 30, 23, 31, 31, 23, 17, 11,
             1, 14, 8, 4, 5, 4, 10, 7,
             1, 1, -6, -19, -6, -7, -4, 10,
             -1, -7, -11, -35, -13, 5, 3, -5,
             0, 0, 0, 0, 0, 0, 0, 0

        };
        public static int[] BlackPawnEarlyPST = new int[64];


        public static int[] WhitePawnLatePST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            82, 82, 82, 82, 82, 82, 82, 82,
            55, 55, 55, 55, 55, 55, 55, 55,
            16, 16, 16, 16, 16, 16, 16, 16,
            -7, -7, -7, -7, -7, -7, -7, -7,
            -11, -11, -11, -11, -11, -11, -11, -11,
            -17, -17, -17, -17, -17, -17, -17, -17,
            0, 0, 0, 0, 0, 0, 0, 0

        };
        public static int[] BlackPawnLatePST = new int[64];


        #endregion

        #region Evaluation parameters

        public static int KNIGHT_VALUE = 349;
        public static int BISHOP_VALUE = 335;
        public static int ROOK_VALUE = 573;
        public static int QUEEN_VALUE = 1082;

        public static int BISHOP_PAIR_BONUS_VALUE = 47;

        public static int FLANK_BACKWARD_PAWN_PENALTY = 18;
        public static int CENTER_BACKWARD_PAWN_PENALTY = 19;

        public static int ROOK_ON_SEVENTH_BONUS = 36;

        public static int BISHOP_PAWN_COLOR_PENALTY = 9;

        public static int KSIDE_CASTLE_BONUS = 61;
        public static int QSIDE_CASTLE_BONUS = 60;
        public static int KSIDE_CASTLE_RIGHTS = 25;
        public static int QSIDE_CASTLE_RIGHTS = 19;

        public static int DOUBLED_PAWN_PENALTY = 40;
        public static int DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 32;
        public static int TRIPLED_PAWN_PENALTY = 82;
        public static int TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 29;

        public static int EDGE_PAWN_ISOLATED_PENALTY = 26;
        public static int CENTER_PAWN_ISOLATED_PENALTY = 31;

        public static int ROOK_ON_SEMI_OPEN_FILE_BONUS = 40;
        public static int ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = 38;

        public static int PASSED_PAWN_BONUS = 54;
        public static int PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = 206;
        public static int PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = 144;
        public static int PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = 84;

        public static int PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = 23;

        public static int BISHOP_ATTACKS_KING_BONUS = 33;
        public static int BISHOP_ATTACKS_QUEEN_BONUS = 31;
        public static int BISHOP_ATTACKS_ROOK_BONUS = 40;
        public static int BISHOP_ATTACKS_KNIGHT_BONUS = 37;

        public static int FIANCETTO_WITHOUT_BISHOP_PENALTY = 31;
        public static int FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY = 36;
        public static int FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY = 43;
        public static int FIANCETTO_IS_GOOD_BONUS = 22;

        public static int BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY = 210;
        public static int BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY = 223;

        public static int KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = 149;
        public static int KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = 120;
        public static int KING_STEPPED_UP_EARLY_PENALTY = 85;
        public static int KING_STEPPED_UP_PAWN_SHIELD_MITIGATION = 28;
        public static int KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION = 22;
        public static int KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION = 17;
        public static int KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION = 23;

        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS = 19;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY = 24;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = 23;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = 24;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = 23;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = 29;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = 27;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = 19;
        public static int KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = 30;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = 25;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = 30;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = 26;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = 25;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = 24;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = 20;
        public static int KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = 28;

        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS = 25;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY = 22;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = 30;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = 29;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = 24;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = 25;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = 21;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = 23;
        public static int KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = 27;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = 24;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = 30;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = 21;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = 18;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = 23;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = 26;
        public static int KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = 16;

        public static int OPENING_MOBILITY_PER_MOVE_BONUS = 3;
        public static int OPENING_MINOR_PIECE_INFLUENCES_CENTER = 10;

        public static int ROOK_REDUNDANCY_PENALTY = 40;
        public static int KNIGHT_REDUNDANCY_PENALTY = 29;
        public static int SEMI_OPEN_FILE_TWO_ROOKS_BONUS = 7;
        public static int SEMI_OPEN_FILE_ONE_ROOK_BONUS = 6;

        public static int TRAPPED_ROOK_PENALTY = 36;
        public static int TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = 11;
        public static int TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = 14;

        public static int BLOCKADING_PASSED_PAWN_BONUS = 36;
        public static int KNIGHT_OUTPOST_MINOR_BONUS = 35;
        public static int KNIGHT_OUTPOST_MAJOR_BONUS = 37;

        public static int PAWN_CHAIN_BONUS = 25;
        public static int KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = 44;
        public static int TEMPO_BONUS = 19;
        public static int CONNECTED_PASSED_PAWN_BONUS = 71;

        public static int CENTRAL_PRESSURE_BONUS = 25;

        #endregion

        #region Search parameters

        //public static int DEPTH2_FUTILITY_MARGIN = 369;
        //public static int DEPTH3_FUTILITY_MARGIN = 420;
        //public static int DEPTH4_FUTILITY_MARGIN = 779;
        //public static int DEPTH2_REVERSE_FUTILITY_MARGIN = 303;
        //public static int DEPTH3_REVERSE_FUTILITY_MARGIN = 417;
        //public static int DEPTH4_REVERSE_FUTILITY_MARGIN = 838;

        public static int DEPTH2_FUTILITY_MARGIN = 155;
        public static int DEPTH3_FUTILITY_MARGIN = 375;
        public static int DEPTH4_FUTILITY_MARGIN = 605;
        public static int DEPTH2_REVERSE_FUTILITY_MARGIN = 110;
        public static int DEPTH3_REVERSE_FUTILITY_MARGIN = 270;
        public static int DEPTH4_REVERSE_FUTILITY_MARGIN = 475;

        public static int DEPTH5_FUTILITY_MARGIN = 670;
        public static int DEPTH6_FUTILITY_MARGIN = 930;
        public static int DEPTH5_REVERSE_FUTILITY_MARGIN = 600;
        public static int DEPTH6_REVERSE_FUTILITY_MARGIN = 800;

        public static int DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF = 370;
        public static int DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF = 360;
        public static int DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF = 220;
        public static int DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF = 175;
        public static int DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF = 160;
        public static int DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF = 130;

        public static int DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF = 470;
        public static int DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF = 460;
        public static int DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF = 270;
        public static int DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF = 250;
        public static int DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF = 220;
        public static int DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF = 190;

        public static int LAZY_EVAL_QUEENS_OFF_MARGIN = 1143;
        public static int LAZY_EVAL_QUEENS_ON_MARGIN = 1348;

        #endregion


        public static bool UseTablebase;

        public static int[] Material;
        public static int[] SeeMaterial;
        public static int MaxMaterial;

        public static OpeningBook Book;

        public const byte TransTablePosTypeExact = 0;
        public const byte TransTablePosTypeAlphaNotExceeded = 1;
        public const byte TransTablePosTypeBetaCutoff = 2;

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
            public long Zobrist;
        }

        public enum NodeTypes
        {
            All = 0,
            Cut = 1,
            PV = 2
        };


        public struct BookMove
        {
            public byte From;
            public byte To;
        }

        public struct BookPosition
        {
            public long Zobrist;
            public BookMove[] Moves;
        }

        public const byte EMPTY = 0;
        public const byte WHITE = 1;
        public const byte BLACK = 2;

        public const byte PAWN = 0;
        public const byte KNIGHT = 1;
        public const byte BISHOP = 2;
        public const byte ROOK = 3;
        public const byte QUEEN = 4;
        public const byte KING = 5;

        public const byte DARKSQUARE = 0;
        public const byte LIGHTSQUARE = 1;

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
            EPD = 5
        }

        public enum UCILogLevels
        {
            None = 0,
            Standard = 1,
            Heavy = 2
        }

        public static bool Initialised = false;
        public static ProgramMode Mode = ProgramMode.Unknown;
        public static string PerftFen = "";
        public static int PerftDepth = 0;
        public static byte MultiFenDepth = 0;
        public static string OutputFile = "";
        public static string[] FensToTest;
        public static string EPDInput = "";
        public static string EPDOutput = "";

        public static int TTHashSizeMB = 128;
        public static int PawnStructureHashSizeMB = 64;
        public static int PositionScoreHashSizeMB = 64;

        public static string BookFile = "";

        public static void Initialise()
        {
            LoadSettings();
            Initialised = true;
        }


        public static void ReconfigureAfterOptions()
        {

            Material = new int[6] { 100, KNIGHT_VALUE, BISHOP_VALUE, ROOK_VALUE, QUEEN_VALUE, 0 };
            MaxMaterial = (800 + (Material[KNIGHT] * 2) + (Material[BISHOP] * 2) + (Material[ROOK] * 2) + Material[QUEEN]) * 2;
            SeeMaterial = new int[6] { 100, 300, 300, 500, 900, 0 };

            MirrorPSTsForBlack();

        }


        private static void MirrorPSTsForBlack()
        {

            for (int NN = 0; NN <= 7; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[56 + NN];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[56 + NN];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[56 + NN];
                BlackKingLatePST[NN] = WhiteKingLatePST[56 + NN];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[56 + NN];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[56 + NN];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[56 + NN];
                BlackPawnLatePST[NN] = WhitePawnLatePST[56 + NN];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[56 + NN];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[56 + NN];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[56 + NN];
                BlackRookLatePST[NN] = WhiteRookLatePST[56 + NN];
            }

            for (int NN = 8; NN <= 15; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[48 + NN - 8];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[48 + NN - 8];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[48 + NN - 8];
                BlackKingLatePST[NN] = WhiteKingLatePST[48 + NN - 8];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[48 + NN - 8];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[48 + NN - 8];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[48 + NN - 8];
                BlackPawnLatePST[NN] = WhitePawnLatePST[48 + NN - 8];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[48 + NN - 8];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[48 + NN - 8];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[48 + NN - 8];
                BlackRookLatePST[NN] = WhiteRookLatePST[48 + NN - 8];
            }

            for (int NN = 16; NN <= 23; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[40 + NN - 16];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[40 + NN - 16];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[40 + NN - 16];
                BlackKingLatePST[NN] = WhiteKingLatePST[40 + NN - 16];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[40 + NN - 16];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[40 + NN - 16];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[40 + NN - 16];
                BlackPawnLatePST[NN] = WhitePawnLatePST[40 + NN - 16];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[40 + NN - 16];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[40 + NN - 16];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[40 + NN - 16];
                BlackRookLatePST[NN] = WhiteRookLatePST[40 + NN - 16];
            }

            for (int NN = 24; NN <= 31; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[32 + NN - 24];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[32 + NN - 24];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[32 + NN - 24];
                BlackKingLatePST[NN] = WhiteKingLatePST[32 + NN - 24];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[32 + NN - 24];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[32 + NN - 24];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[32 + NN - 24];
                BlackPawnLatePST[NN] = WhitePawnLatePST[32 + NN - 24];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[32 + NN - 24];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[32 + NN - 24];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[32 + NN - 24];
                BlackRookLatePST[NN] = WhiteRookLatePST[32 + NN - 24];
            }

            for (int NN = 32; NN <= 39; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[24 + NN - 32];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[24 + NN - 32];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[24 + NN - 32];
                BlackKingLatePST[NN] = WhiteKingLatePST[24 + NN - 32];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[24 + NN - 32];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[24 + NN - 32];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[24 + NN - 32];
                BlackPawnLatePST[NN] = WhitePawnLatePST[24 + NN - 32];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[24 + NN - 32];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[24 + NN - 32];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[24 + NN - 32];
                BlackRookLatePST[NN] = WhiteRookLatePST[24 + NN - 32];
            }

            for (int NN = 40; NN <= 47; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[16 + NN - 40];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[16 + NN - 40];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[16 + NN - 40];
                BlackKingLatePST[NN] = WhiteKingLatePST[16 + NN - 40];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[16 + NN - 40];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[16 + NN - 40];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[16 + NN - 40];
                BlackPawnLatePST[NN] = WhitePawnLatePST[16 + NN - 40];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[16 + NN - 40];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[16 + NN - 40];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[16 + NN - 40];
                BlackRookLatePST[NN] = WhiteRookLatePST[16 + NN - 40];
            }

            for (int NN = 48; NN <= 55; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[8 + NN - 48];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[8 + NN - 48];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[8 + NN - 48];
                BlackKingLatePST[NN] = WhiteKingLatePST[8 + NN - 48];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[8 + NN - 48];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[8 + NN - 48];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[8 + NN - 48];
                BlackPawnLatePST[NN] = WhitePawnLatePST[8 + NN - 48];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[8 + NN - 48];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[8 + NN - 48];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[8 + NN - 48];
                BlackRookLatePST[NN] = WhiteRookLatePST[8 + NN - 48];
            }

            for (int NN = 56; NN <= 63; NN++)
            {
                BlackBishopEarlyPST[NN] = WhiteBishopEarlyPST[NN - 56];
                BlackBishopLatePST[NN] = WhiteBishopLatePST[NN - 56];
                BlackKingEarlyPST[NN] = WhiteKingEarlyPST[NN - 56];
                BlackKingLatePST[NN] = WhiteKingLatePST[NN - 56];
                BlackKnightEarlyPST[NN] = WhiteKnightEarlyPST[NN - 56];
                BlackKnightLatePST[NN] = WhiteKnightLatePST[NN - 56];
                BlackPawnEarlyPST[NN] = WhitePawnEarlyPST[NN - 56];
                BlackPawnLatePST[NN] = WhitePawnLatePST[NN - 56];
                BlackQueenEarlyPST[NN] = WhiteQueenEarlyPST[NN - 56];
                BlackQueenLatePST[NN] = WhiteQueenLatePST[NN - 56];
                BlackRookEarlyPST[NN] = WhiteRookEarlyPST[NN - 56];
                BlackRookLatePST[NN] = WhiteRookLatePST[NN - 56];
            }


        }


        private static void LoadSettings()
        {

            string AppFolder = AppDomain.CurrentDomain.BaseDirectory;
            if (!AppFolder.EndsWith("\\"))
            {
                AppFolder += "\\";
            }

            string SettingsFile = AppFolder + "Settings.ini";
            if (!File.Exists(SettingsFile))
            {
                SettingsFile = AppFolder + "GlobalBits\\Settings.ini";
            }

            if (File.Exists(AppFolder + "Book.ini"))
            {
                BookFile = AppFolder + "Book.ini";
            }
            else if (File.Exists(AppFolder + "GlobalBits\\Book.ini"))
            {
                BookFile = AppFolder + "GlobalBits\\Book.ini";
            }

            if (BookFile != "")
            {
                Book = new OpeningBook(BookFile);
            }

            List<string> MultiFenList = new();
            if (File.Exists(SettingsFile))
            {

                string[] Lines = File.ReadAllLines(SettingsFile);
                foreach (string Line in Lines)
                {
                    string L = Line.Trim();
                    if (!L.StartsWith("#"))
                    {
                        if (L.ToLower().StartsWith("mode="))
                        {
                            if (L.ToLower().Contains("=uci"))
                            {
                                Mode = ProgramMode.UCI;
                            }
                            else if (L.ToLower().Contains("=perftfen"))
                            {
                                Mode = ProgramMode.Perft;
                            }
                            else if (L.ToLower().Contains("=multifen"))
                            {
                                Mode = ProgramMode.MultiFen;
                            }
                            else if (L.ToLower().Contains("=fentozobrist"))
                            {
                                Mode = ProgramMode.FenToZobrist;
                            }
                            else if (L.ToLower().Contains("=epd"))
                            {
                                Mode = ProgramMode.EPD;
                            }
                        }
                        else if (L.ToLower().StartsWith("usetablebase"))
                        {
                            UseTablebase = (L.ToLower().Contains("true"));
                        }
                    }
                }

                foreach (string Line in Lines)
                {
                    string L = Line.Trim();
                    if (Mode == ProgramMode.Perft)
                    {
                        if (L.ToLower().StartsWith("fen="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            PerftFen = Splits[1];
                        }
                        if (L.ToLower().StartsWith("perftdepth="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            PerftDepth = Convert.ToInt32(Splits[1]);
                        }
                    }
                    else if (Mode == ProgramMode.MultiFen)
                    {
                        if (L.ToLower().StartsWith("multifen_"))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            MultiFenList.Add(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("multifendepth="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            MultiFenDepth = Convert.ToByte(Splits[1]);
                        }
                    }
                    else if (Mode == ProgramMode.FenToZobrist)
                    {
                        if (L.ToLower().StartsWith("fen="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            PerftFen = Splits[1];
                        }
                    }
                    else if (Mode == ProgramMode.EPD)
                    {
                        if (L.ToLower().StartsWith("epdinputfile="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            EPDInput = Splits[1];
                        }
                        if (L.ToLower().StartsWith("epdoutputfile="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            EPDOutput = Splits[1];
                        }
                        if (L.ToLower().StartsWith("multifendepth="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            MultiFenDepth = Convert.ToByte(Splits[1]);
                        }
                    }
                    if (L.ToLower().Contains("outputfile") && !L.ToLower().Contains("epdoutputfile"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        OutputFile = Splits[1];
                    }
                    if (L.ToLower().Contains("transtablehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        TTHashSizeMB = Convert.ToInt32(Splits[1]);
                    }
                    if (L.ToLower().Contains("pawnstructurehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        PawnStructureHashSizeMB = Convert.ToInt32(Splits[1]);
                    }
                    if (L.ToLower().Contains("positionscorehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        PositionScoreHashSizeMB = Convert.ToInt32(Splits[1]);
                    }
                }
            }

            FensToTest = MultiFenList.ToArray();

        }


        #region Helper functions used in many places


        /// <summary>
        /// Gets numeric column by its letter
        /// </summary>
        public static int GetXByColumn(string Col)
        {

            switch (Col.ToLower())
            {
                case "a":
                    return 0;
                case "b":
                    return 1;
                case "c":
                    return 2;
                case "d":
                    return 3;
                case "e":
                    return 4;
                case "f":
                    return 5;
                case "g":
                    return 6;
                case "h":
                    return 7;
                default:
                    break;
            }

            return -1;

        }


        public static string ConvertMoveToAlgebraic(Move Made, ref Board TheBoard)
        {


            string PieceOnSquare = ""; string ToRank = ""; string ToFile = ""; bool IsEP = false;

            if (TheBoard.Piece[Made.From] == KNIGHT)
            {
                PieceOnSquare = "N";
            }
            else if (TheBoard.Piece[Made.From] == BISHOP)
            {
                PieceOnSquare = "B";
            }
            else if (TheBoard.Piece[Made.From] == ROOK)
            {
                PieceOnSquare = "R";
            }
            else if (TheBoard.Piece[Made.From] == QUEEN)
            {
                PieceOnSquare = "Q";
            }
            else if (TheBoard.Piece[Made.From] == KING)
            {
                if (Made.To == Made.From - 2)
                {
                    return "O-O-O";
                }
                else if (Made.To == Made.From + 2)
                {
                    return "O-O";
                }
                else
                {
                    PieceOnSquare = "K";
                }
            }
            else if (TheBoard.Piece[Made.From] == PAWN && Made.IsCapture)
            {
                if (Made.From == 0 || Made.From % 8 == 0)
                {
                    PieceOnSquare = "a";
                }
                else if (Made.From == 1 || Made.From % 8 == 1)
                {
                    PieceOnSquare = "b";
                }
                else if (Made.From == 2 || Made.From % 8 == 2)
                {
                    PieceOnSquare = "c";
                }
                else if (Made.From == 3 || Made.From % 8 == 3)
                {
                    PieceOnSquare = "d";
                }
                else if (Made.From == 4 || Made.From % 8 == 4)
                {
                    PieceOnSquare = "e";
                }
                else if (Made.From == 5 || Made.From % 8 == 5)
                {
                    PieceOnSquare = "f";
                }
                else if (Made.From == 6 || Made.From % 8 == 6)
                {
                    PieceOnSquare = "g";
                }
                else if (Made.From == 7 || Made.From % 8 == 7)
                {
                    PieceOnSquare = "h";
                }
                if (Made.To == TheBoard.EnPasantCapSquare)
                {
                    IsEP = true;
                }
            }


            if (Made.To == 0 || Made.To % 8 == 0)
            {
                ToFile = "a";
            }
            else if (Made.To == 1 || Made.To % 8 == 1)
            {
                ToFile = "b";
            }
            else if (Made.To == 2 || Made.To % 8 == 2)
            {
                ToFile = "c";
            }
            else if (Made.To == 3 || Made.To % 8 == 3)
            {
                ToFile = "d";
            }
            else if (Made.To == 4 || Made.To % 8 == 4)
            {
                ToFile = "e";
            }
            else if (Made.To == 5 || Made.To % 8 == 5)
            {
                ToFile = "f";
            }
            else if (Made.To == 6 || Made.To % 8 == 6)
            {
                ToFile = "g";
            }
            else if (Made.To == 7 || Made.To % 8 == 7)
            {
                ToFile = "h";
            }

            if (Made.To <= 7)
            {
                ToRank = "8";
            }
            else if (Made.To >= 8 && Made.To <= 15)
            {
                ToRank = "7";
            }
            else if (Made.To >= 16 && Made.To <= 23)
            {
                ToRank = "6";
            }
            else if (Made.To >= 24 && Made.To <= 31)
            {
                ToRank = "5";
            }
            else if (Made.To >= 32 && Made.To <= 39)
            {
                ToRank = "4";
            }
            else if (Made.To >= 40 && Made.To <= 47)
            {
                ToRank = "3";
            }
            else if (Made.To >= 48 && Made.To <= 55)
            {
                ToRank = "2";
            }
            else if (Made.To >= 56 && Made.To <= 63)
            {
                ToRank = "1";
            }

            string Ret = PieceOnSquare + (Made.IsCapture ? "x" : "") + ToFile + ToRank + (IsEP ? "ep" : "");
            return Ret;

        }


        public static string ConvertMoveToString(Move Made)
        {

            string RetVal;
            string FromFile = "";
            string ToFile = "";

            if (Made.From == 0 || Made.From % 8 == 0)
            {
                FromFile = "a";
            }
            else if (Made.From == 1 || Made.From % 8 == 1)
            {
                FromFile = "b";
            }
            else if (Made.From == 2 || Made.From % 8 == 2)
            {
                FromFile = "c";
            }
            else if (Made.From == 3 || Made.From % 8 == 3)
            {
                FromFile = "d";
            }
            else if (Made.From == 4 || Made.From % 8 == 4)
            {
                FromFile = "e";
            }
            else if (Made.From == 5 || Made.From % 8 == 5)
            {
                FromFile = "f";
            }
            else if (Made.From == 6 || Made.From % 8 == 6)
            {
                FromFile = "g";
            }
            else if (Made.From == 7 || Made.From % 8 == 7)
            {
                FromFile = "h";
            }

            if (Made.To == 0 || Made.To % 8 == 0)
            {
                ToFile = "a";
            }
            else if (Made.To == 1 || Made.To % 8 == 1)
            {
                ToFile = "b";
            }
            else if (Made.To == 2 || Made.To % 8 == 2)
            {
                ToFile = "c";
            }
            else if (Made.To == 3 || Made.To % 8 == 3)
            {
                ToFile = "d";
            }
            else if (Made.To == 4 || Made.To % 8 == 4)
            {
                ToFile = "e";
            }
            else if (Made.To == 5 || Made.To % 8 == 5)
            {
                ToFile = "f";
            }
            else if (Made.To == 6 || Made.To % 8 == 6)
            {
                ToFile = "g";
            }
            else if (Made.To == 7 || Made.To % 8 == 7)
            {
                ToFile = "h";
            }

            string FromRank = ""; string ToRank = "";

            if (Made.From <= 7)
            {
                FromRank = "8";
            }
            else if (Made.From >= 8 && Made.From <= 15)
            {
                FromRank = "7";
            }
            else if (Made.From >= 16 && Made.From <= 23)
            {
                FromRank = "6";
            }
            else if (Made.From >= 24 && Made.From <= 31)
            {
                FromRank = "5";
            }
            else if (Made.From >= 32 && Made.From <= 39)
            {
                FromRank = "4";
            }
            else if (Made.From >= 40 && Made.From <= 47)
            {
                FromRank = "3";
            }
            else if (Made.From >= 48 && Made.From <= 55)
            {
                FromRank = "2";
            }
            else if (Made.From >= 56 && Made.From <= 63)
            {
                FromRank = "1";
            }

            if (Made.To <= 7)
            {
                ToRank = "8";
            }
            else if (Made.To >= 8 && Made.To <= 15)
            {
                ToRank = "7";
            }
            else if (Made.To >= 16 && Made.To <= 23)
            {
                ToRank = "6";
            }
            else if (Made.To >= 24 && Made.To <= 31)
            {
                ToRank = "5";
            }
            else if (Made.To >= 32 && Made.To <= 39)
            {
                ToRank = "4";
            }
            else if (Made.To >= 40 && Made.To <= 47)
            {
                ToRank = "3";
            }
            else if (Made.To >= 48 && Made.To <= 55)
            {
                ToRank = "2";
            }
            else if (Made.To >= 56 && Made.To <= 63)
            {
                ToRank = "1";
            }

            RetVal = FromFile + FromRank + ToFile + ToRank;

            if (Made.PromotionPiece == KNIGHT)
            {
                RetVal += "n";
            }
            else if (Made.PromotionPiece == BISHOP)
            {
                RetVal += "b";
            }
            else if (Made.PromotionPiece == ROOK)
            {
                RetVal += "r";
            }
            else if (Made.PromotionPiece == QUEEN)
            {
                RetVal += "q";
            }

            return RetVal;

        }


        #endregion


    }
}
