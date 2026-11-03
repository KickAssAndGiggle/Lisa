using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lisa
{
    public static class Globals
    {

        #region Default Piece Square Table


        public static int[] WhiteKnightEarlyPST = new int[64]
        {
            -80, -2, 0, 2, 2, 0, 0, -6,
            -38, 11, 47, 68, 58, 120, 2, 34,
            -26, 10, 58, 78, 125, 143, 69, 24,
            -13, -7, 17, 49, 5, 59, 24, 59,
            -27, 0, 11, 10, 34, 27, 46, 7,
            -49, -35, -23, 19, 27, -10, -3, -21,
            -73, -51, -37, -18, -19, -24, -34, -20,
            -110, -50, -66, -48, -30, -24, -56, -76
        };
        public static int[] BlackKnightEarlyPST = new int[64];


        public static int[] WhiteKnightLatePST = new int[64]
        {
            -37, -7, 8, 14, 17, 10, -3, -29,
            -7, 1, 14, 25, 23, 13, 10, -7,
            -3, 17, 23, 26, 19, 25, 14, 5,
            -10, 9, 23, 35, 41, 36, 17, -6,
            -27, -8, 18, 22, 22, 15, -3, -20,
            -61, -35, -21, -7, -7, -37, -27, -47,
            -82, -53, -48, -42, -35, -35, -51, -81,
            -103, -107, -77, -70, -72, -94, -96, -101
        };
        public static int[] BlackKnightLatePST = new int[64];


        public static int[] WhiteBishopEarlyPST = new int[64]
        {
            0, 2, 2, 2, 2, 0, 0, 0,
            -23, -17, 15, 52, 55, 60, 61, -8,
            -2, 31, 18, 58, 95, 130, 77, 51,
            -6, -11, 35, 57, 37, 51, -15, 6,
            -4, 8, -2, 33, 26, -14, 4, 0,
            -14, 12, 7, 3, 0, -4, -2, 6,
            11, -10, 36, -14, -4, 5, 10, -9,
            -7, 42, -9, -34, -17, -18, -16, -55
        };
        public static int[] BlackBishopEarlyPST = new int[64];


        public static int[] WhiteBishopLatePST = new int[64]
        {
            4, 11, 16, 17, 21, 10, 10, 0,
            10, 24, 19, 15, 13, 19, 9, 6,
            14, 26, 26, 23, 20, 21, 27, 18,
            18, 28, 19, 22, 21, 31, 37, 14,
            15, 11, 30, 17, 13, 15, 15, 4,
            4, 11, 7, 3, 11, 3, 2, -2,
            -3, -10, -12, 5, -7, 0, -13, -18,
            -27, -15, -25, 3, -9, -7, -13, -24
        };
        public static int[] BlackBishopLatePST = new int[64];


        public static int[] WhiteRookEarlyPST = new int[64]
        {
            75, 21, 19, 54, 124, 14, 16, 153,
            39, 36, 66, 69, 98, 98, 70, 112,
            22, 65, 55, 83, 133, 151, 98, 83,
            6, 21, 24, 53, 53, 58, 70, 32,
            -29, -23, -7, 8, 10, -17, 17, -8,
            -36, -28, -24, -8, 0, -3, 23, -7,
            -52, -27, -14, -6, -7, -4, 8, -26,
            -39, -20, -7, -1, 1, -11, 22, -27
        };
        public static int[] BlackRookEarlyPST = new int[64];


        public static int[] WhiteRookLatePST = new int[64]
        {
            13, 38, 33, 24, 14, 38, 42, 8,
            27, 30, 22, 17, 23, 25, 30, 17,
            40, 25, 28, 20, 15, 22, 26, 22,
            31, 28, 23, 16, 16, 28, 24, 26,
            15, 13, 15, 2, 3, 28, 11, 7,
            -12, -7, 1, -13, -14, -7, -15, -18,
            -16, -21, -21, -32, -26, -36, -27, -26,
            -13, -23, -21, -35, -33, -27, -39, -27
        };
        public static int[] BlackRookLatePST = new int[64];


        public static int[] WhiteKingEarlyPST = new int[64]
        {
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, -5, -7, -9, -9, -7, -6, -7,
            -7, -6, -8, -9, -8, -7, -4, -7,
            -8, -6, -8, -9, -11, -6, -6, -7,
            -7, -8, -10, -17, -17, -13, -12, -20,
            0, 7, -9, -26, -24, -18, 9, 8,
            -7, -14, 5, -3, -10, -4, 16, -1,
            -37, -3, -21, -19, 4, -16, 22, 1
        };
        public static int[] BlackKingEarlyPST = new int[64];


        public static int[] WhiteKingLatePST = new int[64]
        {
            40, 49, 48, 50, 51, 48, 49, 38,
            38, 51, 53, 47, 44, 49, 52, 42,
            38, 49, 43, 42, 38, 48, 50, 34,
            27, 40, 38, 34, 34, 34, 36, 25,
            13, 19, 20, 19, 14, 14, 9, 2,
            -4, 4, 0, -1, -3, -4, -13, -22,
            -26, -17, -2, -3, 0, 1, -22, -32,
            -53, -40, -30, -16, -36, -18, -50, -82
        };
        public static int[] BlackKingLatePST = new int[64];


        public static int[] WhiteQueenEarlyPST = new int[64]
        {
            -10, 13, -22, 13, 40, 12, 17, 107,
            -10, -20, 29, 10, 28, 65, 44, 75,
            -20, 3, 1, 25, 48, 57, 53, 49,
            -18, -17, 1, 14, 25, 20, 46, 29,
            -29, -17, -12, -3, 7, 2, 18, 17,
            -34, -27, -6, -14, -6, -1, 3, 11,
            -18, -6, -2, 10, 3, -6, -19, -2,
            3, -14, -3, 1, 4, -45, -82, -7
        };
        public static int[] BlackQueenEarlyPST = new int[64];


        public static int[] WhiteQueenLatePST = new int[64]
        {
            17, 22, 40, 34, 34, 32, 32, 17,
            24, 28, 34, 40, 45, 39, 35, 29,
            25, 27, 33, 35, 45, 49, 46, 36,
            20, 27, 33, 27, 30, 43, 40, 35,
            7, 17, 6, 21, 11, 21, 14, 22,
            -13, -3, -10, -16, -11, -6, -8, -8,
            -32, -37, -43, -51, -49, -50, -44, -33,
            -58, -60, -61, -57, -71, -64, -60, -61
        };
        public static int[] BlackQueenLatePST = new int[64];


        public static int[] WhitePawnEarlyPST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            132, 131, 184, 181, 126, 96, -8, 24,
            30, 48, 70, 77, 102, 108, 84, 33,
            2, 11, 7, 29, 59, 58, 42, 9,
            -20, -14, -6, -3, 16, 15, 20, -14,
            -36, -33, -47, -42, -27, -9, -3, -22,
            -35, -35, -48, -50, -38, -7, 12, -39,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public static int[] BlackPawnEarlyPST = new int[64];


        public static int[] WhitePawnLatePST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            65, 86, 79, 79, 89, 86, 87, 72,
            43, 48, 45, 38, 37, 49, 48, 45,
            21, 23, 15, -7, 4, 12, 20, 14,
            10, 15, 1, -8, 1, 12, 7, 4,
            1, 3, 11, 10, 13, 15, -1, -5,
            10, 12, 31, -2, 34, 25, 3, 1,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public static int[] BlackPawnLatePST = new int[64];


        #endregion

        #region Evaluation parameters

        public static int KNIGHT_VALUE = 428;
        public static int BISHOP_VALUE = 400;
        public static int ROOK_VALUE = 630;
        public static int QUEEN_VALUE = 1190;

        public static int BISHOP_PAIR_BONUS_VALUE = 27;

        public static int FLANK_BACKWARD_PAWN_PENALTY = 2;
        public static int CENTER_BACKWARD_PAWN_PENALTY = 8;

        public static int ROOK_ON_SEVENTH_BONUS = 8;

        public static int BISHOP_PAWN_COLOR_PENALTY = 7;

        public static int KSIDE_CASTLE_BONUS = 41;
        public static int QSIDE_CASTLE_BONUS = 86;
        public static int KSIDE_CASTLE_RIGHTS = 29;
        public static int QSIDE_CASTLE_RIGHTS = 22;

        public static int DOUBLED_PAWN_PENALTY = 24;
        public static int DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 0;
        public static int TRIPLED_PAWN_PENALTY = 107;
        public static int TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 0;

        public static int EDGE_PAWN_ISOLATED_PENALTY = 14;
        public static int CENTER_PAWN_ISOLATED_PENALTY = 24;

        public static int ROOK_ON_SEMI_OPEN_FILE_BONUS = 15;
        public static int ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = 18;

        public static int PASSED_PAWN_BONUS = 7;
        public static int PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = 75;
        public static int PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = 69;
        public static int PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = 38;
        public static int PASSED_PAWN_BLOCKADED_KEEP_PERCENT = 49;
        public static int PASSED_PAWN_OWN_KING_DISTANCE_PENALTY = 4;
        public static int PASSED_PAWN_ENEMY_KING_DISTANCE_BONUS = 9;
        public static int PASSED_PAWN_FREE_PATH_BONUS = 11;
        public static int PASSED_PAWN_SAFE_STEP_BONUS = 8;
        public static int PASSED_PAWN_ROOK_BEHIND_BONUS = 29;
        public static int PASSED_PAWN_ENEMY_ROOK_BEHIND_PENALTY = 80;

        public static int PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = 23;

        public static int BISHOP_ATTACKS_QUEEN_BONUS = 15;
        public static int BISHOP_ATTACKS_ROOK_BONUS = 3;
        public static int BISHOP_ATTACKS_KNIGHT_BONUS = 16;

        public static int KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = 116;
        public static int KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = 0;
        public static int KING_STEPPED_UP_EARLY_PENALTY = 0;

        public static int KING_FILE_SHELTER_PAWN_BONUS = 12;
        public static int KING_FILE_NO_SHELTER_PAWN_PENALTY = 9;
        public static int SIDE_FILE_SHELTER_PAWN_BONUS = 0;
        public static int SIDE_FILE_NO_SHELTER_PAWN_PENALTY = 7;

        public static int KING_NEAR_HALF_OPEN_FILE_PENALTY = 36;
        public static int KING_NEAR_OPEN_FILE_ADDITIONAL_PENALTY = 26;

        public static int KING_ATTACK_KNIGHT_UNITS = 2;
        public static int KING_ATTACK_BISHOP_UNITS = 2;
        public static int KING_ATTACK_ROOK_UNITS = 3;
        public static int KING_ATTACK_QUEEN_UNITS = 5;
        public static int KING_ATTACK_XRAY_SQUARES = 2;

        public static int FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY = 0;
        public static int KING_ATTACK_MAX_SQUARES_PER_PIECE = 3;

        public static int KING_DEFENCE_KNIGHT_UNITS = 5;
        public static int KING_DEFENCE_BISHOP_UNITS = 3;
        public static int KING_DEFENCE_ROOK_UNITS = 2;
        public static int KING_DEFENCE_QUEEN_UNITS = 2;

        //Penalty for the defending king by attack units: 10 for the first unit, then 5 more per unit, capped at 250
        public static int[] KING_ATTACK_PENALTY = new int[100]
        {
              0,  10,  15,  20,  25,  30,  35,  40,  45,  50,
             55,  60,  65,  70,  75,  80,  85,  90,  95, 100,
            105, 110, 115, 120, 125, 130, 135, 140, 145, 150,
            155, 160, 165, 170, 175, 180, 185, 190, 195, 200,
            205, 210, 215, 220, 225, 230, 235, 240, 245, 250,
            250, 250, 250, 250, 250, 250, 250, 250, 250, 250,
            250, 250, 250, 250, 250, 250, 250, 250, 250, 250,
            250, 250, 250, 250, 250, 250, 250, 250, 250, 250,
            250, 250, 250, 250, 250, 250, 250, 250, 250, 250,
            250, 250, 250, 250, 250, 250, 250, 250, 250, 250
        };

        public static int OPENING_MOBILITY_PER_KNIGHT_MOVE_BONUS = 3;
        public static int OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS = 6;
        public static int OPENING_MOBILITY_PER_ROOK_MOVE_BONUS = 5;
        public static int OPENING_MOBILITY_PER_QUEEN_MOVE_BONUS = 1;
        public static int ENDGAME_MOBILITY_PER_KNIGHT_MOVE_BONUS = 1;
        public static int ENDGAME_MOBILITY_PER_BISHOP_MOVE_BONUS = 7;
        public static int ENDGAME_MOBILITY_PER_ROOK_MOVE_BONUS = 6;
        public static int ENDGAME_MOBILITY_PER_QUEEN_MOVE_BONUS = 8;
        public static int OPENING_MINOR_PIECE_INFLUENCES_CENTER = 0;

        public static int ROOK_REDUNDANCY_PENALTY = 128;
        public static int EXCHANGE_IMBALANCE_BONUS = 59;
        public static int KNIGHT_REDUNDANCY_PENALTY = 11;
        public static int SEMI_OPEN_FILE_TWO_ROOKS_BONUS = 14;
        public static int SEMI_OPEN_FILE_ONE_ROOK_BONUS = 0;

        public static int TRAPPED_ROOK_PENALTY = 88;
        public static int TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = 0;
        public static int TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = 0;

        public static int BLOCKADING_PASSED_PAWN_BONUS = 51;
        public static int KNIGHT_OUTPOST_MINOR_BONUS = 10;
        public static int KNIGHT_OUTPOST_MAJOR_BONUS = 13;

        public static int PAWN_CHAIN_BONUS = 10;
        public static int KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = 12;
        public static int TEMPO_BONUS = 4;
        public static int CONNECTED_PASSED_PAWN_BONUS = 25;

        #endregion

        #region Search parameters

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
        }

        public struct BookPosition
        {
            public long Zobrist;
            public BookMove[] Moves;
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
            TexelTunePST = 8
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

        public static bool USE_TABLEBASE;

        public static int[] MATERIAL;
        public static int[] SEE_MATERIAL;
        public static int[] KING_ATTACK_UNITS;
        public static int[] KING_DEFENCE_UNITS;
        public static int[] OPENING_MOBILITY_BONUS;
        public static int[] ENDGAME_MOBILITY_BONUS;
        public static int MAX_MATERIAL;

        public static OpeningBook OPENING_BOOK;

        public const byte TT_POS_TYPE_EXACT = 0;
        public const byte TT_POS_TYPE_ALPHA_NOT_BEATEN = 1;
        public const byte TT_POS_TYPE_BETA_CUT = 2;

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

        public static ProgramMode RUN_MODE = ProgramMode.Unknown;

        public static string PERFT_FEN = "";
        public static int PERFT_DEPTH = 0;
        public static byte MULTI_FEN_DEPTH = 0;
        public static string OUTPUT_FILE = "";
        public static string[]? FENS_TO_TEST;
        public static string EPD_INPUT = "";
        public static string EPD_OUTPUT = "";

        public static string TEXEL_PGN_INPUT = "";
        public static string TEXEL_EPD_OUTPUT = "";
        public static int TEXEL_POSITIONS_PER_GAME = 10;
        public static int TEXEL_SKIP_PLIES = 16;
        public static int TEXEL_MAX_GAMES = 100000;
        public static int TEXEL_GAME_STRIDE = 25;

        public static string TEXEL_TUNE_INPUT = "";
        public static string TEXEL_TUNE_RESULTS = "";
        public static int TEXEL_TUNE_MAX_POSITIONS = 0;
        public static int TEXEL_TUNE_THREADS = 0;
        public static string TEXEL_PST_RESULTS = "";
        public static int TEXEL_PST_EPOCHS = 2000;

        public static int TT_HASH_SIZE_MB = 128;
        public static int PAWN_STRUCTURE_HASH_SIZE_MB = 64;
        public static int POSITION_SCORE_HASH_SIZE_MB = 64;

        private static string _bookFile = "";

        static Globals()
        {

            LoadSettings();

            MATERIAL = Array.Empty<int>();
            SEE_MATERIAL = Array.Empty<int>();
            KING_ATTACK_UNITS = Array.Empty<int>();
            KING_DEFENCE_UNITS = Array.Empty<int>();

            OPENING_BOOK = new OpeningBook(_bookFile);

        }


        public static void InitialiseScorer()
        {

            MATERIAL = new int[6] { 100, KNIGHT_VALUE, BISHOP_VALUE, ROOK_VALUE, QUEEN_VALUE, 0 };
            MAX_MATERIAL = (800 + (MATERIAL[KNIGHT] * 2) + (MATERIAL[BISHOP] * 2) + (MATERIAL[ROOK] * 2) + MATERIAL[QUEEN]) * 2;
            SEE_MATERIAL = new int[6] { 100, 300, 300, 500, 900, 0 };
            KING_ATTACK_UNITS = new int[6] { 0, KING_ATTACK_KNIGHT_UNITS, KING_ATTACK_BISHOP_UNITS, KING_ATTACK_ROOK_UNITS, KING_ATTACK_QUEEN_UNITS, 0 };
            KING_DEFENCE_UNITS = new int[6] { 0, KING_DEFENCE_KNIGHT_UNITS, KING_DEFENCE_BISHOP_UNITS, KING_DEFENCE_ROOK_UNITS, KING_DEFENCE_QUEEN_UNITS, 0 };
            OPENING_MOBILITY_BONUS = new int[6] { 0, OPENING_MOBILITY_PER_KNIGHT_MOVE_BONUS, OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS, OPENING_MOBILITY_PER_ROOK_MOVE_BONUS, OPENING_MOBILITY_PER_QUEEN_MOVE_BONUS, 0 };
            ENDGAME_MOBILITY_BONUS = new int[6] { 0, ENDGAME_MOBILITY_PER_KNIGHT_MOVE_BONUS, ENDGAME_MOBILITY_PER_BISHOP_MOVE_BONUS, ENDGAME_MOBILITY_PER_ROOK_MOVE_BONUS, ENDGAME_MOBILITY_PER_QUEEN_MOVE_BONUS, 0 };

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
                _bookFile = AppFolder + "Book.ini";
            }
            else if (File.Exists(AppFolder + "GlobalBits\\Book.ini"))
            {
                _bookFile = AppFolder + "GlobalBits\\Book.ini";
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
                                RUN_MODE = ProgramMode.UCI;
                            }
                            else if (L.ToLower().Contains("=perftfen"))
                            {
                                RUN_MODE = ProgramMode.Perft;
                            }
                            else if (L.ToLower().Contains("=multifen"))
                            {
                                RUN_MODE = ProgramMode.MultiFen;
                            }
                            else if (L.ToLower().Contains("=fentozobrist"))
                            {
                                RUN_MODE = ProgramMode.FenToZobrist;
                            }
                            else if (L.ToLower().Contains("=epd"))
                            {
                                RUN_MODE = ProgramMode.EPD;
                            }
                            else if (L.ToLower().Contains("=maketexelfile"))
                            {
                                RUN_MODE = ProgramMode.MakeTexelFile;
                            }
                            else if (L.ToLower().Contains("=texeltunepst"))
                            {
                                RUN_MODE = ProgramMode.TexelTunePST;
                            }
                            else if (L.ToLower().Contains("=texeltune"))
                            {
                                RUN_MODE = ProgramMode.TexelTune;
                            }
                        }
                        else if (L.ToLower().StartsWith("usetablebase"))
                        {
                            USE_TABLEBASE = (L.ToLower().Contains("true"));
                        }
                    }
                }

                foreach (string Line in Lines)
                {
                    string L = Line.Trim();
                    if (RUN_MODE == ProgramMode.Perft)
                    {
                        if (L.ToLower().StartsWith("fen="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            PERFT_FEN = Splits[1];
                        }
                        if (L.ToLower().StartsWith("perftdepth="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            PERFT_DEPTH = Convert.ToInt32(Splits[1]);
                        }
                    }
                    else if (RUN_MODE == ProgramMode.MultiFen)
                    {
                        if (L.ToLower().StartsWith("multifen_"))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            MultiFenList.Add(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("multifendepth="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            MULTI_FEN_DEPTH = Convert.ToByte(Splits[1]);
                        }
                    }
                    else if (RUN_MODE == ProgramMode.FenToZobrist)
                    {
                        if (L.ToLower().StartsWith("fen="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            PERFT_FEN = Splits[1];
                        }
                    }
                    else if (RUN_MODE == ProgramMode.EPD)
                    {
                        if (L.ToLower().StartsWith("epdinputfile="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            EPD_INPUT = Splits[1];
                        }
                        if (L.ToLower().StartsWith("epdoutputfile="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            EPD_OUTPUT = Splits[1];
                        }
                        if (L.ToLower().StartsWith("multifendepth="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            MULTI_FEN_DEPTH = Convert.ToByte(Splits[1]);
                        }
                    }
                    else if (RUN_MODE == ProgramMode.MakeTexelFile)
                    {
                        if (L.ToLower().StartsWith("texelpgninput="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_PGN_INPUT = Splits[1];
                        }
                        if (L.ToLower().StartsWith("texelepdoutput="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_EPD_OUTPUT = Splits[1];
                        }
                        if (L.ToLower().StartsWith("texelpositionspergame="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_POSITIONS_PER_GAME = Convert.ToInt32(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("texelskipplies="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_SKIP_PLIES = Convert.ToInt32(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("texelmaxgames="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_MAX_GAMES = Convert.ToInt32(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("texelgamestride="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_GAME_STRIDE = Convert.ToInt32(Splits[1]);
                        }
                    }
                    else if (RUN_MODE == ProgramMode.TexelTune)
                    {
                        if (L.ToLower().StartsWith("texeltuneinput="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_INPUT = Splits[1];
                        }
                        if (L.ToLower().StartsWith("texeltuneresults="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_RESULTS = Splits[1];
                        }
                        if (L.ToLower().StartsWith("texeltunemaxpositions="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_MAX_POSITIONS = Convert.ToInt32(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("texeltunethreads="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_THREADS = Convert.ToInt32(Splits[1]);
                        }
                    }
                    else if (RUN_MODE == ProgramMode.TexelTunePST)
                    {
                        if (L.ToLower().StartsWith("texeltuneinput="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_INPUT = Splits[1];
                        }
                        if (L.ToLower().StartsWith("texeltunemaxpositions="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_MAX_POSITIONS = Convert.ToInt32(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("texeltunethreads="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_TUNE_THREADS = Convert.ToInt32(Splits[1]);
                        }
                        if (L.ToLower().StartsWith("texelpstresults="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_PST_RESULTS = Splits[1];
                        }
                        if (L.ToLower().StartsWith("texelpstepochs="))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            TEXEL_PST_EPOCHS = Convert.ToInt32(Splits[1]);
                        }
                    }
                    if (L.ToLower().Contains("outputfile") && !L.ToLower().Contains("epdoutputfile"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        OUTPUT_FILE = Splits[1];
                    }
                    if (L.ToLower().Contains("transtablehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        TT_HASH_SIZE_MB = Convert.ToInt32(Splits[1]);
                    }
                    if (L.ToLower().Contains("pawnstructurehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        PAWN_STRUCTURE_HASH_SIZE_MB = Convert.ToInt32(Splits[1]);
                    }
                    if (L.ToLower().Contains("positionscorehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        POSITION_SCORE_HASH_SIZE_MB = Convert.ToInt32(Splits[1]);
                    }
                }
            }

            FENS_TO_TEST = MultiFenList.ToArray();

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


        public static string ConvertMoveToAlgebraic(Move Made, ref Board theBoard)
        {


            string PieceOnSquare = ""; string ToRank = ""; string ToFile = ""; bool IsEP = false;

            if (theBoard.Piece[Made.From] == KNIGHT)
            {
                PieceOnSquare = "N";
            }
            else if (theBoard.Piece[Made.From] == BISHOP)
            {
                PieceOnSquare = "B";
            }
            else if (theBoard.Piece[Made.From] == ROOK)
            {
                PieceOnSquare = "R";
            }
            else if (theBoard.Piece[Made.From] == QUEEN)
            {
                PieceOnSquare = "Q";
            }
            else if (theBoard.Piece[Made.From] == KING)
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
            else if (theBoard.Piece[Made.From] == PAWN && Made.IsCapture)
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
                if (Made.To == theBoard.EnPasantCapSquare)
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
