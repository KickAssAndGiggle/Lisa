namespace Lisa
{
    //Evaluation weights: the piece square tables and the evaluation parameters. TexelTuner and TexelPSTTuner find and
    //write these by name on Globals, so they must stay public static fields of Globals (this is a partial of it)
    public static partial class Globals
    {

        #region Default Piece Square Table


        public static int[] WhiteKnightEarlyPST = new int[64]
        {
            -81, -1, -1, 6, 6, 0, 0, -18,
            -23, -9, 20, 27, 29, 58, 1, 26,
            -15, 2, 31, 46, 68, 70, 48, 28,
            -4, -6, 14, 33, 10, 36, 10, 34,
            -14, 0, 4, 5, 16, 12, 24, 3,
            -23, -20, -14, 7, 11, -8, -5, -6,
            -45, -42, -22, -14, -15, -18, -19, -12,
            -70, -31, -42, -28, -21, -17, -33, -27
        };
        public static int[] BlackKnightEarlyPST = new int[64];


        public static int[] WhiteKnightLatePST = new int[64]
        {
            -46, -9, 3, 2, 9, 4, -2, -32,
            -4, -5, -2, 8, 3, -8, 1, -7,
            -2, 9, 8, 6, -3, 9, -2, 5,
            -7, 5, 11, 15, 20, 17, 10, -7,
            -14, -6, 11, 12, 12, 9, -5, -10,
            -39, -17, -10, -3, -3, -17, -12, -27,
            -36, -11, -21, -18, -14, -10, -17, -36,
            -57, -54, -24, -25, -26, -42, -41, -56
        };
        public static int[] BlackKnightLatePST = new int[64];


        public static int[] WhiteBishopEarlyPST = new int[64]
        {
            -5, 1, 1, 2, 1, 0, 0, -2,
            -11, -5, -9, 18, 16, 26, 15, -29,
            -6, 14, 7, 28, 51, 84, 60, 23,
            -8, -9, 18, 34, 15, 33, -13, 2,
            -7, 4, -3, 13, 13, -11, -2, -5,
            -11, 3, -1, 0, -4, -3, -4, 2,
            0, -9, 17, -11, -5, -2, 4, -12,
            -6, 22, -9, -20, -10, -12, 8, -14
        };
        public static int[] BlackBishopEarlyPST = new int[64];


        public static int[] WhiteBishopLatePST = new int[64]
        {
            11, 13, 15, 14, 19, 7, 12, 5,
            5, 15, 16, 8, 7, 11, 5, 8,
            11, 14, 14, 10, 6, 1, 10, 11,
            12, 19, 8, 6, 10, 12, 26, 7,
            13, 4, 18, 10, 5, 9, 10, -1,
            6, 8, 5, 2, 7, 1, 1, -3,
            7, -3, -7, 4, -2, 2, -6, -11,
            -13, -8, -11, 7, -3, 1, -8, -14
        };
        public static int[] BlackBishopLatePST = new int[64];


        public static int[] WhiteRookEarlyPST = new int[64]
        {
            22, 26, 26, 46, 62, 22, 25, 77,
            8, 11, 29, 42, 56, 60, 31, 67,
            -2, 14, 22, 42, 65, 80, 63, 48,
            -16, -5, -5, 18, 18, 31, 41, 18,
            -33, -30, -20, -8, -7, -25, -1, -12,
            -35, -33, -30, -17, -13, -15, 3, -16,
            -43, -30, -20, -15, -16, -12, -3, -20,
            -34, -24, -17, -14, -13, -19, 3, -28
        };
        public static int[] BlackRookEarlyPST = new int[64];


        public static int[] WhiteRookLatePST = new int[64]
        {
            16, 21, 17, 8, 9, 26, 30, 5,
            21, 21, 15, 6, 9, 10, 22, 5,
            29, 23, 18, 8, 4, 7, 9, 9,
            28, 25, 23, 13, 13, 16, 12, 17,
            22, 20, 20, 9, 9, 29, 13, 9,
            8, 12, 16, 3, 3, 6, -6, -4,
            7, 3, -1, -8, -4, -15, -13, -8,
            5, -1, -1, -8, -8, -4, -18, -4
        };
        public static int[] BlackRookLatePST = new int[64];


        public static int[] WhiteKingEarlyPST = new int[64]
        {
            -5, -5, -5, -5, -5, -5, -5, -5,
            -5, 3, -2, -5, -5, -3, 1, 2,
            -4, -1, -4, -9, -2, -4, 4, -4,
            -4, -3, -9, -7, -12, 2, -3, 2,
            2, -4, -10, -25, -22, -13, -7, -24,
            21, 30, 1, -28, -22, -11, 20, 26,
            -3, -12, 11, 4, -3, 1, 8, -2,
            -33, -9, -16, -16, -6, -9, 9, -2
        };
        public static int[] BlackKingEarlyPST = new int[64];


        public static int[] WhiteKingLatePST = new int[64]
        {
            15, 30, 24, 24, 26, 24, 30, 10,
            18, 36, 34, 24, 20, 31, 38, 26,
            25, 32, 22, 21, 15, 27, 33, 18,
            15, 23, 20, 17, 17, 16, 19, 13,
            7, 10, 11, 13, 9, 7, 3, 3,
            -8, -6, -2, 3, 1, -3, -14, -22,
            -20, -9, -4, -3, -1, -1, -14, -19,
            -30, -22, -16, -5, -18, -10, -28, -46
        };
        public static int[] BlackKingLatePST = new int[64];


        public static int[] WhiteQueenEarlyPST = new int[64]
        {
            -9, 10, 6, 18, 30, 21, 32, 57,
            -14, -30, 4, 10, 19, 37, 19, 46,
            -15, -5, -8, 11, 27, 41, 50, 22,
            -16, -16, -10, 2, 5, 4, 19, 8,
            -18, -19, -8, -8, 1, -7, 5, 3,
            -22, -16, -6, -8, -5, -3, -1, 7,
            -15, -5, -2, 4, 1, 2, -6, -3,
            -3, -9, -3, -3, 5, -17, -38, 4
        };
        public static int[] BlackQueenEarlyPST = new int[64];


        public static int[] WhiteQueenLatePST = new int[64]
        {
            14, 14, 32, 23, 25, 28, 34, 11,
            20, 33, 27, 26, 33, 27, 31, 18,
            18, 14, 23, 18, 31, 30, 29, 25,
            16, 23, 29, 17, 20, 36, 32, 32,
            4, 21, -3, 15, 3, 18, 7, 21,
            -8, -3, -10, -18, -13, -9, -12, -15,
            -16, -29, -34, -41, -38, -56, -46, -22,
            -29, -37, -43, -36, -63, -52, -40, -35
        };
        public static int[] BlackQueenLatePST = new int[64];


        public static int[] WhitePawnEarlyPST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            85, 76, 107, 105, 89, 69, 7, 17,
            13, 19, 31, 39, 57, 55, 48, 17,
            -4, -2, -2, 8, 23, 25, 16, 0,
            -16, -14, -11, -8, 1, 3, 6, -12,
            -25, -25, -31, -29, -23, -13, -12, -20,
            -24, -24, -31, -35, -29, -11, -2, -27,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public static int[] BlackPawnEarlyPST = new int[64];


        public static int[] WhitePawnLatePST = new int[64]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            19, 43, 34, 34, 42, 42, 47, 28,
            15, 24, 19, 12, 8, 21, 20, 15,
            13, 21, 13, 3, 8, 11, 18, 9,
            9, 18, 8, 3, 7, 12, 12, 5,
            5, 13, 14, 13, 14, 14, 9, 1,
            9, 17, 23, 14, 28, 18, 11, 4,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        public static int[] BlackPawnLatePST = new int[64];


        #endregion

        #region Evaluation parameters

        public static int KNIGHT_VALUE = 383;
        public static int BISHOP_VALUE = 380;
        public static int ROOK_VALUE = 591;
        public static int QUEEN_VALUE = 1155;

        public static int BISHOP_PAIR_BONUS_VALUE = 26;

        public static int FLANK_BACKWARD_PAWN_PENALTY = 0;
        public static int CENTER_BACKWARD_PAWN_PENALTY = 0;

        public static int ROOK_ON_SEVENTH_BONUS = 25;

        public static int BISHOP_PAWN_COLOR_PENALTY = 6;

        public static int KSIDE_CASTLE_BONUS = 17;
        public static int QSIDE_CASTLE_BONUS = 53;
        public static int KSIDE_CASTLE_RIGHTS = 12;
        public static int QSIDE_CASTLE_RIGHTS = 10;

        public static int DOUBLED_PAWN_PENALTY = 21;
        public static int DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 0;
        public static int TRIPLED_PAWN_PENALTY = 94;
        public static int TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = 7;

        public static int EDGE_PAWN_ISOLATED_PENALTY = 0;
        public static int CENTER_PAWN_ISOLATED_PENALTY = 10;

        public static int ROOK_ON_SEMI_OPEN_FILE_BONUS = 10;
        public static int ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = 20;

        public static int PASSED_PAWN_BONUS = 0;
        public static int PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = 75;
        public static int PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = 79;
        public static int PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = 39;
        public static int PASSED_PAWN_BLOCKADED_KEEP_PERCENT = 73;
        public static int PASSED_PAWN_OWN_KING_DISTANCE_PENALTY = 5;
        public static int PASSED_PAWN_ENEMY_KING_DISTANCE_BONUS = 9;
        public static int PASSED_PAWN_FREE_PATH_BONUS = 10;
        public static int PASSED_PAWN_SAFE_STEP_BONUS = 10;
        public static int PASSED_PAWN_ROOK_BEHIND_BONUS = 21;
        public static int PASSED_PAWN_ENEMY_ROOK_BEHIND_PENALTY = 84;

        public static int PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = 23;

        public static int BISHOP_ATTACKS_QUEEN_BONUS = 17;
        public static int BISHOP_ATTACKS_ROOK_BONUS = 9;
        public static int BISHOP_ATTACKS_KNIGHT_BONUS = 16;

        public static int KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = 75;
        public static int KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = 0;
        public static int KING_STEPPED_UP_EARLY_PENALTY = 0;

        public static int KING_FILE_SHELTER_PAWN_BONUS = 0;
        public static int KING_FILE_NO_SHELTER_PAWN_PENALTY = 26;
        public static int SIDE_FILE_SHELTER_PAWN_BONUS = 13;
        public static int SIDE_FILE_NO_SHELTER_PAWN_PENALTY = 3;

        public static int KING_NEAR_HALF_OPEN_FILE_PENALTY = 24;
        public static int KING_NEAR_OPEN_FILE_ADDITIONAL_PENALTY = 14;

        public static int KING_ATTACK_KNIGHT_UNITS = 6;
        public static int KING_ATTACK_BISHOP_UNITS = 3;
        public static int KING_ATTACK_ROOK_UNITS = 6;
        public static int KING_ATTACK_QUEEN_UNITS = 4;
        public static int KING_ATTACK_XRAY_SQUARES = 2;

        public static int FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY = 0;
        public static int KING_ATTACK_MAX_SQUARES_PER_PIECE = 3;

        public static int KING_DEFENCE_KNIGHT_UNITS = 3;
        public static int KING_DEFENCE_BISHOP_UNITS = 2;
        public static int KING_DEFENCE_ROOK_UNITS = 1;
        public static int KING_DEFENCE_QUEEN_UNITS = 0;

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

        public static int OPENING_MOBILITY_PER_KNIGHT_MOVE_BONUS = 7;
        public static int OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS = 8;
        public static int OPENING_MOBILITY_PER_ROOK_MOVE_BONUS = 5;
        public static int OPENING_MOBILITY_PER_QUEEN_MOVE_BONUS = 1;
        public static int ENDGAME_MOBILITY_PER_KNIGHT_MOVE_BONUS = 9;
        public static int ENDGAME_MOBILITY_PER_BISHOP_MOVE_BONUS = 9;
        public static int ENDGAME_MOBILITY_PER_ROOK_MOVE_BONUS = 6;
        public static int ENDGAME_MOBILITY_PER_QUEEN_MOVE_BONUS = 8;
        public static int OPENING_MINOR_PIECE_INFLUENCES_CENTER = 0;

        public static int ROOK_REDUNDANCY_PENALTY = 83;
        public static int EXCHANGE_IMBALANCE_BONUS = 40;
        public static int KNIGHT_REDUNDANCY_PENALTY = 10;
        public static int SEMI_OPEN_FILE_TWO_ROOKS_BONUS = 9;
        public static int SEMI_OPEN_FILE_ONE_ROOK_BONUS = 1;

        public static int TRAPPED_ROOK_PENALTY = 95;
        public static int TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = 16;
        public static int TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = 0;

        public static int BLOCKADING_PASSED_PAWN_BONUS = 47;
        public static int KNIGHT_OUTPOST_MINOR_BONUS = 0;
        public static int KNIGHT_OUTPOST_MAJOR_BONUS = 0;

        public static int PAWN_CHAIN_BONUS = 18;
        //Per square on a side's third or fourth rank that none of its pawns can ever guard again
        public static int HOLE_PENALTY = 8;
        public static int KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = 11;
        public static int TEMPO_BONUS = 6;
        public static int CONNECTED_PASSED_PAWN_BONUS = 20;

        //Endgame scale factors, out of 64 (see EndgameScaleFactor)
        public static int ENDGAME_SCALE_FEW_PAWNS_BASE = 30;
        public static int ENDGAME_SCALE_PER_PAWN = 11;
        public static int OCB_SCALE_BASE = 1;
        public static int OCB_SCALE_PER_PASSED_PAWN = 10;
        public static int OCB_WITH_PIECES_SCALE_BASE = 40;
        public static int OCB_WITH_PIECES_SCALE_PER_PIECE = 3;

        //Mobility onto squares an enemy pawn attacks
        public static int OPENING_MOBILITY_PER_KNIGHT_UNSAFE_MOVE_BONUS = 0;
        public static int OPENING_MOBILITY_PER_BISHOP_UNSAFE_MOVE_BONUS = 0;
        public static int OPENING_MOBILITY_PER_ROOK_UNSAFE_MOVE_BONUS = 0;
        public static int OPENING_MOBILITY_PER_QUEEN_UNSAFE_MOVE_BONUS = 0;
        public static int ENDGAME_MOBILITY_PER_KNIGHT_UNSAFE_MOVE_BONUS = 0;
        public static int ENDGAME_MOBILITY_PER_BISHOP_UNSAFE_MOVE_BONUS = 7;
        public static int ENDGAME_MOBILITY_PER_ROOK_UNSAFE_MOVE_BONUS = 4;
        public static int ENDGAME_MOBILITY_PER_QUEEN_UNSAFE_MOVE_BONUS = 1;

        //King shelter for a king on the d or e file
        public static int CENTRAL_KING_FILE_SHELTER_PAWN_BONUS = 5;
        public static int CENTRAL_KING_FILE_NO_SHELTER_PAWN_PENALTY = 0;
        public static int CENTRAL_SIDE_FILE_SHELTER_PAWN_BONUS = 9;
        public static int CENTRAL_SIDE_FILE_NO_SHELTER_PAWN_PENALTY = 0;

        //Pawn chain bonus for a defended pawn on its fourth rank or further (PAWN_CHAIN_BONUS is for the third)
        public static int PAWN_CHAIN_ADVANCED_BONUS = 11;

        //Terms in ScorerExtraTerms.cs. A weight of 0 switches its term off
        public static int KNIGHT_VALUE_PER_PAWN_ABOVE_FIVE = 5;
        public static int ROOK_VALUE_LOST_PER_PAWN_ABOVE_FIVE = 12;
        public static int HOLE_NEAR_KING_PENALTY = 4;
        public static int HOLE_PENALTY_PER_ENEMY_MINOR = 0;
        public static int KNIGHT_OUTPOST_ON_HOLE_BONUS = 40;
        public static int BISHOP_OUTPOST_ON_HOLE_BONUS = 34;
        public static int STORM_PAWN_ON_THIRD_RANK_PENALTY = 49;
        public static int STORM_PAWN_ON_FOURTH_RANK_PENALTY = 23;
        public static int STORM_PAWN_ON_FIFTH_RANK_PENALTY = 14;
        public static int KING_STUCK_IN_CENTRE_PENALTY = 48;
        public static int MINOR_BEHIND_PAWN_BONUS = 12;
        public static int TRAPPED_BISHOP_PENALTY = 110;
        public static int UNDEVELOPED_MINOR_PENALTY = 3;
        public static int EARLY_QUEEN_PENALTY = 7;
        public static int SPACE_BONUS = 4;
        public static int FLANK_PAWN_ADVANCED_PENALTY = 0;
        public static int PAWN_PHALANX_BONUS = 9;
        public static int PAWN_PHALANX_ENDGAME_BONUS = 1;
        public static int UNOPPOSED_ISOLATED_PAWN_PENALTY = 10;
        public static int CANDIDATE_PASSED_PAWN_BONUS = 21;
        public static int PAWN_ISLAND_PENALTY = 0;
        public static int THREATENED_BY_PAWN_PENALTY = 38;
        public static int HANGING_PIECE_PENALTY = 21;
        public static int WEAK_KING_SQUARE_PENALTY = 10;
        public static int SAFE_KNIGHT_CHECK_PENALTY = 62;
        public static int SAFE_BISHOP_CHECK_PENALTY = 21;
        public static int SAFE_ROOK_CHECK_PENALTY = 54;
        public static int SAFE_QUEEN_CHECK_PENALTY = 48;

        #endregion

        #region Tables built from the evaluation parameters

        public static int[] MATERIAL;
        public static int[] SEE_MATERIAL;
        public static int[] KING_ATTACK_UNITS;
        public static int[] KING_DEFENCE_UNITS;
        public static int[] OPENING_MOBILITY_BONUS;
        public static int[] ENDGAME_MOBILITY_BONUS;
        public static int[] OPENING_MOBILITY_UNSAFE_BONUS;
        public static int[] ENDGAME_MOBILITY_UNSAFE_BONUS;
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
            OPENING_MOBILITY_UNSAFE_BONUS = new int[6] { 0, OPENING_MOBILITY_PER_KNIGHT_UNSAFE_MOVE_BONUS,
                OPENING_MOBILITY_PER_BISHOP_UNSAFE_MOVE_BONUS, OPENING_MOBILITY_PER_ROOK_UNSAFE_MOVE_BONUS,
                OPENING_MOBILITY_PER_QUEEN_UNSAFE_MOVE_BONUS, 0 };
            ENDGAME_MOBILITY_UNSAFE_BONUS = new int[6] { 0, ENDGAME_MOBILITY_PER_KNIGHT_UNSAFE_MOVE_BONUS,
                ENDGAME_MOBILITY_PER_BISHOP_UNSAFE_MOVE_BONUS, ENDGAME_MOBILITY_PER_ROOK_UNSAFE_MOVE_BONUS,
                ENDGAME_MOBILITY_PER_QUEEN_UNSAFE_MOVE_BONUS, 0 };

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
