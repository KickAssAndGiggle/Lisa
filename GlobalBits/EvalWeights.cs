namespace Lisa
{
    //Evaluation weights: the piece square tables and the evaluation parameters. TexelTuner and TexelPSTTuner find and
    //write these by name on Globals, so they must stay public static fields of Globals (this is a partial of it)
    public static partial class Globals
    {

        #region Default Piece Square Table


        public static int[] WhiteKnightEarlyPST = new int[64]
        {
            -161, -2, -2, 12, 12, 0, 0, -36,
            -46, -17, 40, 53, 58, 115, 2, 51,
            -30, 4, 61, 91, 136, 140, 96, 56,
            -8, -11, 28, 66, 20, 71, 19, 68,
            -28, 0, 8, 9, 32, 24, 47, 6,
            -46, -40, -27, 14, 21, -16, -9, -12,
            -90, -83, -44, -28, -29, -36, -37, -24,
            -139, -61, -83, -56, -42, -33, -66, -53
        };
        public static int[] BlackKnightEarlyPST = new int[64];


        public static int[] WhiteKnightLatePST = new int[64]
        {
            -91, -18, 5, 4, 17, 8, -3, -64,
            -7, -10, -3, 16, 6, -15, 2, -14,
            -3, 18, 16, 11, -6, 17, -4, 9,
            -13, 9, 21, 29, 39, 34, 19, -14,
            -27, -12, 21, 23, 24, 17, -9, -20,
            -77, -34, -19, -5, -5, -34, -24, -54,
            -72, -21, -42, -36, -27, -19, -34, -72,
            -114, -107, -47, -49, -52, -84, -82, -111
        };
        public static int[] BlackKnightLatePST = new int[64];


        public static int[] WhiteBishopEarlyPST = new int[64]
        {
            -10, 2, 2, 4, 2, 0, 0, -3,
            -22, -9, -17, 36, 31, 51, 30, -58,
            -12, 28, 14, 56, 101, 168, 119, 45,
            -15, -18, 36, 67, 30, 65, -26, 3,
            -13, 8, -6, 25, 26, -21, -3, -9,
            -22, 6, -1, 0, -7, -6, -8, 3,
            0, -18, 33, -21, -9, -3, 7, -23,
            -12, 43, -18, -40, -19, -23, 15, -27
        };
        public static int[] BlackBishopEarlyPST = new int[64];


        public static int[] WhiteBishopLatePST = new int[64]
        {
            21, 25, 30, 28, 37, 14, 24, 10,
            10, 30, 31, 16, 13, 21, 10, 15,
            22, 27, 28, 20, 12, 1, 19, 21,
            24, 37, 16, 12, 20, 24, 51, 13,
            25, 8, 36, 19, 10, 18, 19, -2,
            11, 15, 10, 3, 14, 2, 1, -5,
            14, -5, -14, 8, -3, 3, -12, -21,
            -25, -15, -22, 13, -6, 1, -16, -28
        };
        public static int[] BlackBishopLatePST = new int[64];


        public static int[] WhiteRookEarlyPST = new int[64]
        {
            43, 51, 52, 92, 124, 44, 49, 153,
            16, 22, 58, 84, 111, 119, 61, 134,
            -4, 28, 43, 83, 130, 159, 126, 95,
            -32, -10, -10, 35, 35, 61, 81, 35,
            -66, -60, -39, -16, -13, -49, -2, -24,
            -69, -66, -60, -33, -26, -30, 6, -31,
            -85, -60, -40, -30, -32, -23, -5, -39,
            -67, -47, -34, -28, -25, -37, 6, -55
        };
        public static int[] BlackRookEarlyPST = new int[64];


        public static int[] WhiteRookLatePST = new int[64]
        {
            31, 42, 33, 16, 17, 51, 59, 9,
            42, 42, 30, 12, 18, 19, 43, 9,
            57, 45, 35, 16, 8, 13, 18, 18,
            56, 50, 46, 26, 25, 31, 23, 33,
            44, 40, 39, 18, 18, 57, 25, 18,
            15, 23, 32, 5, 5, 12, -12, -7,
            14, 6, -1, -16, -8, -30, -25, -16,
            10, -2, -2, -16, -15, -8, -35, -8
        };
        public static int[] BlackRookLatePST = new int[64];


        public static int[] WhiteKingEarlyPST = new int[64]
        {
            -9, -9, -9, -9, -9, -9, -9, -9,
            -9, 6, -3, -9, -9, -5, 2, 3,
            -7, -2, -8, -18, -4, -7, 7, -7,
            -8, -6, -18, -14, -23, 4, -6, 3,
            3, -8, -20, -49, -43, -26, -13, -48,
            41, 59, 1, -55, -43, -21, 40, 52,
            -5, -23, 22, 8, -5, 1, 15, -4,
            -66, -17, -31, -32, -12, -17, 17, -3
        };
        public static int[] BlackKingEarlyPST = new int[64];


        public static int[] WhiteKingLatePST = new int[64]
        {
            30, 59, 48, 47, 51, 48, 59, 20,
            35, 71, 68, 47, 40, 61, 76, 51,
            50, 63, 44, 41, 30, 54, 65, 36,
            30, 45, 39, 34, 34, 31, 37, 25,
            14, 19, 21, 25, 18, 14, 6, 5,
            -15, -11, -3, 6, 2, -5, -27, -43,
            -39, -18, -7, -6, -1, -2, -27, -37,
            -60, -44, -31, -9, -36, -20, -56, -91
        };
        public static int[] BlackKingLatePST = new int[64];


        public static int[] WhiteQueenEarlyPST = new int[64]
        {
            -18, 20, 11, 35, 60, 42, 64, 113,
            -28, -60, 7, 20, 37, 73, 38, 92,
            -29, -9, -16, 22, 53, 82, 100, 44,
            -32, -31, -20, 3, 9, 8, 37, 16,
            -35, -37, -15, -15, 1, -14, 9, 5,
            -43, -31, -12, -15, -10, -5, -1, 14,
            -29, -9, -3, 8, 2, 4, -11, -6,
            -5, -18, -6, -5, 10, -34, -76, 8
        };
        public static int[] BlackQueenEarlyPST = new int[64];


        public static int[] WhiteQueenLatePST = new int[64]
        {
            27, 27, 63, 46, 49, 56, 67, 22,
            39, 65, 54, 52, 66, 54, 62, 35,
            35, 28, 45, 35, 62, 60, 58, 50,
            32, 45, 58, 33, 40, 71, 64, 63,
            7, 42, -5, 30, 5, 35, 14, 41,
            -15, -6, -19, -35, -26, -18, -24, -30,
            -32, -58, -68, -82, -76, -111, -92, -43,
            -58, -74, -85, -71, -126, -104, -80, -70
        };
        public static int[] BlackQueenLatePST = new int[64];


        public static int[] WhitePawnEarlyPST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            169, 151, 213, 209, 178, 137, 14, 34,
            26, 37, 61, 77, 114, 110, 95, 34,
            -7, -3, -4, 16, 45, 50, 31, 0,
            -31, -27, -21, -15, 2, 6, 11, -24,
            -50, -50, -62, -58, -46, -25, -23, -39,
            -47, -48, -61, -70, -57, -22, -4, -54,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public static int[] BlackPawnEarlyPST = new int[64];


        public static int[] WhitePawnLatePST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            37, 85, 68, 68, 83, 84, 94, 56,
            30, 48, 37, 23, 15, 42, 39, 30,
            25, 41, 26, 6, 15, 21, 36, 17,
            18, 35, 16, 6, 14, 23, 24, 10,
            10, 26, 27, 25, 27, 27, 18, 1,
            18, 33, 46, 28, 56, 36, 22, 8,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public static int[] BlackPawnLatePST = new int[64];


        #endregion

        #region Evaluation parameters

        public static int KNIGHT_VALUE = 426;
        public static int BISHOP_VALUE = 396;
        public static int ROOK_VALUE = 624;
        public static int QUEEN_VALUE = 1177;

        public static int BISHOP_PAIR_BONUS_VALUE = 27;

        public static int FLANK_BACKWARD_PAWN_PENALTY = 3;
        public static int CENTER_BACKWARD_PAWN_PENALTY = 8;

        public static int ROOK_ON_SEVENTH_BONUS = 9;

        public static int BISHOP_PAWN_COLOR_PENALTY = 6;

        public static int KSIDE_CASTLE_BONUS = 45;
        public static int QSIDE_CASTLE_BONUS = 90;
        public static int KSIDE_CASTLE_RIGHTS = 46;
        public static int QSIDE_CASTLE_RIGHTS = 24;

        public static int DOUBLED_PAWN_PENALTY = 30;
        public static int DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 0;
        public static int TRIPLED_PAWN_PENALTY = 119;
        public static int TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 0;

        public static int EDGE_PAWN_ISOLATED_PENALTY = 5;
        public static int CENTER_PAWN_ISOLATED_PENALTY = 22;

        public static int ROOK_ON_SEMI_OPEN_FILE_BONUS = 17;
        public static int ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = 14;

        public static int PASSED_PAWN_BONUS = 0;
        public static int PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = 75;
        public static int PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = 79;
        public static int PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = 39;
        public static int PASSED_PAWN_BLOCKADED_KEEP_PERCENT = 52;
        public static int PASSED_PAWN_OWN_KING_DISTANCE_PENALTY = 4;
        public static int PASSED_PAWN_ENEMY_KING_DISTANCE_BONUS = 9;
        public static int PASSED_PAWN_FREE_PATH_BONUS = 11;
        public static int PASSED_PAWN_SAFE_STEP_BONUS = 7;
        public static int PASSED_PAWN_ROOK_BEHIND_BONUS = 29;
        public static int PASSED_PAWN_ENEMY_ROOK_BEHIND_PENALTY = 76;

        public static int PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = 23;

        public static int BISHOP_ATTACKS_QUEEN_BONUS = 14;
        public static int BISHOP_ATTACKS_ROOK_BONUS = 3;
        public static int BISHOP_ATTACKS_KNIGHT_BONUS = 17;

        public static int KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = 133;
        public static int KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = 0;
        public static int KING_STEPPED_UP_EARLY_PENALTY = 0;

        public static int KING_FILE_SHELTER_PAWN_BONUS = 11;
        public static int KING_FILE_NO_SHELTER_PAWN_PENALTY = 16;
        public static int SIDE_FILE_SHELTER_PAWN_BONUS = 1;
        public static int SIDE_FILE_NO_SHELTER_PAWN_PENALTY = 11;

        public static int KING_NEAR_HALF_OPEN_FILE_PENALTY = 31;
        public static int KING_NEAR_OPEN_FILE_ADDITIONAL_PENALTY = 25;

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

        public static int OPENING_MOBILITY_PER_KNIGHT_MOVE_BONUS = 2;
        public static int OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS = 6;
        public static int OPENING_MOBILITY_PER_ROOK_MOVE_BONUS = 5;
        public static int OPENING_MOBILITY_PER_QUEEN_MOVE_BONUS = 1;
        public static int ENDGAME_MOBILITY_PER_KNIGHT_MOVE_BONUS = 1;
        public static int ENDGAME_MOBILITY_PER_BISHOP_MOVE_BONUS = 7;
        public static int ENDGAME_MOBILITY_PER_ROOK_MOVE_BONUS = 6;
        public static int ENDGAME_MOBILITY_PER_QUEEN_MOVE_BONUS = 8;
        public static int OPENING_MINOR_PIECE_INFLUENCES_CENTER = 1;

        public static int ROOK_REDUNDANCY_PENALTY = 113;
        public static int EXCHANGE_IMBALANCE_BONUS = 56;
        public static int KNIGHT_REDUNDANCY_PENALTY = 10;
        public static int SEMI_OPEN_FILE_TWO_ROOKS_BONUS = 11;
        public static int SEMI_OPEN_FILE_ONE_ROOK_BONUS = 0;

        public static int TRAPPED_ROOK_PENALTY = 73;
        public static int TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = 0;
        public static int TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = 0;

        public static int BLOCKADING_PASSED_PAWN_BONUS = 54;
        public static int KNIGHT_OUTPOST_MINOR_BONUS = 2;
        public static int KNIGHT_OUTPOST_MAJOR_BONUS = 2;

        public static int PAWN_CHAIN_BONUS = 13;
        public static int KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = 16;
        public static int TEMPO_BONUS = 4;
        public static int CONNECTED_PASSED_PAWN_BONUS = 26;

        #endregion

        #region Tables built from the evaluation parameters

        public static int[] MATERIAL;
        public static int[] SEE_MATERIAL;
        public static int[] KING_ATTACK_UNITS;
        public static int[] KING_DEFENCE_UNITS;
        public static int[] OPENING_MOBILITY_BONUS;
        public static int[] ENDGAME_MOBILITY_BONUS;
        public static int MAX_MATERIAL;

        #endregion


        public static void InitialiseScorer()
        {

            MATERIAL = new int[6] { 100, KNIGHT_VALUE, BISHOP_VALUE, ROOK_VALUE, QUEEN_VALUE, 0 };
            MAX_MATERIAL = (800 + (MATERIAL[KNIGHT] * 2) + (MATERIAL[BISHOP] * 2) + (MATERIAL[ROOK] * 2) +
                MATERIAL[QUEEN]) * 2;
            SEE_MATERIAL = new int[6] { 100, 300, 300, 500, 900, 0 };
            KING_ATTACK_UNITS = new int[6] { 0, KING_ATTACK_KNIGHT_UNITS, KING_ATTACK_BISHOP_UNITS,
                KING_ATTACK_ROOK_UNITS, KING_ATTACK_QUEEN_UNITS, 0 };
            KING_DEFENCE_UNITS = new int[6] { 0, KING_DEFENCE_KNIGHT_UNITS, KING_DEFENCE_BISHOP_UNITS,
                KING_DEFENCE_ROOK_UNITS, KING_DEFENCE_QUEEN_UNITS, 0 };
            OPENING_MOBILITY_BONUS = new int[6] { 0, OPENING_MOBILITY_PER_KNIGHT_MOVE_BONUS,
                OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS, OPENING_MOBILITY_PER_ROOK_MOVE_BONUS,
                OPENING_MOBILITY_PER_QUEEN_MOVE_BONUS, 0 };
            ENDGAME_MOBILITY_BONUS = new int[6] { 0, ENDGAME_MOBILITY_PER_KNIGHT_MOVE_BONUS,
                ENDGAME_MOBILITY_PER_BISHOP_MOVE_BONUS, ENDGAME_MOBILITY_PER_ROOK_MOVE_BONUS,
                ENDGAME_MOBILITY_PER_QUEEN_MOVE_BONUS, 0 };

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

    }
}
