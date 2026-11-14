using System.Numerics;
using static Lisa.Globals;
namespace Lisa
{
    public static partial class Scorer
    {

        //Indexed by square, one bit per square (bit 0 is a8): the squares next to it, and the squares a knight's
        //move away from it
        private static readonly ulong[] _kingNeighbourMasks = BuildStepMasks(false);
        private static readonly ulong[] _knightMoveMasks = BuildStepMasks(true);

        //Indexed by square: the square itself and every square on its two diagonals
        private static readonly ulong[] _diagonalMasks = BuildDiagonalMasks();

        //d4, e4, d5 and e5
        private const ulong CENTRE_SQUARES = (1UL << 27) | (1UL << 28) | (1UL << 35) | (1UL << 36);


        public static int ScorePosition(ref Board theBoard, ref TranspositionTable tt, int alpha, int beta,
            bool allowLazyEval, int lastMoveCapMaterial)
        {

            bool InTT = tt.LookupScore(theBoard.CurrentZobrist, out TTScore lookup);
            if (InTT)
            {
                //The cached scores are stored without tempo, so add it here exactly as the full evaluation does
                if (theBoard.OnMove == BLACK)
                {
                    return (lookup.WhiteScore - (lookup.BlackScore + TEMPO_BONUS));
                }
                else
                {
                    return (lookup.BlackScore - (lookup.WhiteScore + TEMPO_BONUS));
                }
            }

            int phase = (theBoard.GamePhase * 256 + 12) / 24;

            int whiteOpeningScore = theBoard.WhiteEarlyPSTScore + theBoard.WhiteMaterial;
            int whiteEndgameScore = theBoard.WhiteLatePSTScore + theBoard.WhiteMaterial;
            int blackOpeningScore = theBoard.BlackEarlyPSTScore + theBoard.BlackMaterial;
            int blackEndgameScore = theBoard.BlackLatePSTScore + theBoard.BlackMaterial;

            if (allowLazyEval)
            {

                int whiteLazyScore = ((whiteOpeningScore * (256 - phase)) + (whiteEndgameScore * phase)) / 256;
                int blackLazyScore = ((blackOpeningScore * (256 - phase)) + (blackEndgameScore * phase)) / 256;
                int lazy = theBoard.OnMove == BLACK ? whiteLazyScore - blackLazyScore : blackLazyScore - whiteLazyScore;

                if (theBoard.WhiteQueenSquare == 255 && theBoard.BlackQueenSquare == 255)
                {
                    if (lastMoveCapMaterial == 0)
                    {
                        if (lazy + LAZY_EVAL_QUEENS_OFF_MARGIN < alpha || lazy - LAZY_EVAL_QUEENS_OFF_MARGIN > beta)
                        {
                            return lazy;
                        }
                    }
                    else
                    {
                        if (lazy + (LAZY_EVAL_QUEENS_OFF_MARGIN + lastMoveCapMaterial) < alpha ||
                            lazy - (LAZY_EVAL_QUEENS_OFF_MARGIN + lastMoveCapMaterial) > beta)
                        {
                            return lazy;
                        }
                    }
                }
                else
                {
                    if (lastMoveCapMaterial == 0)
                    {
                        if (lazy + LAZY_EVAL_QUEENS_ON_MARGIN < alpha || lazy - LAZY_EVAL_QUEENS_ON_MARGIN > beta)
                        {
                            return lazy;
                        }
                    }
                    else
                    {
                        if (lazy + (LAZY_EVAL_QUEENS_ON_MARGIN + lastMoveCapMaterial) < alpha ||
                            lazy - (LAZY_EVAL_QUEENS_ON_MARGIN + lastMoveCapMaterial) > beta)
                        {
                            return lazy;
                        }
                    }
                }

            }

            //Generated once and shared by mobility, king attack and king defence
            ReadOnlySpan<PieceAttacks> whitePieceAttacks = theBoard.GeneratePieceAttacksWithoutKing(WHITE,
                out PieceAttackMasks whitePieces);
            ReadOnlySpan<PieceAttacks> blackPieceAttacks = theBoard.GeneratePieceAttacksWithoutKing(BLACK,
                out PieceAttackMasks blackPieces);

            //Everything that depends on the pawns alone comes from the pawn hash, and is worked out and stored on a
            //miss
            if (!tt.LookupPPScore(theBoard.PawnOnlyZobrist, out TTPawnAnalysis pawns))
            {
                AnalysePawns(ref theBoard, out pawns);
                tt.AddPawnStructureToTransTable(theBoard.PawnOnlyZobrist, ref pawns);
            }
            ulong whitePassedPawns = pawns.WhitePassedPawns;
            ulong blackPassedPawns = pawns.BlackPassedPawns;

            //Every square each side attacks with anything, shared by the passed pawn and extra terms
            ulong attackedByWhite = AttackedSquares(in whitePieces, pawns.WhitePawnAttacks, theBoard.WhiteKingSquare);
            ulong attackedByBlack = AttackedSquares(in blackPieces, pawns.BlackPawnAttacks, theBoard.BlackKingSquare);

            //Early Game
            RewardBeingCastled(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardKingSafety(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            PenaliseKingAttack(ref theBoard, whitePieceAttacks, blackPieceAttacks, ref whiteOpeningScore,
                ref blackOpeningScore);
            RewardActiveBishop(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            //EncouragePawnsOnOppositeSideToKingToAdvance(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardPawnBishopSynergy(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardMobility(ref theBoard, whitePieceAttacks, blackPieceAttacks, in whitePieces, in blackPieces,
                pawns.WhitePawnAttacks, pawns.BlackPawnAttacks, ref whiteOpeningScore, ref blackOpeningScore,
                ref whiteEndgameScore, ref blackEndgameScore);
            RewardKnightOutpost(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            PenaliseTrappedRook(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);

            //Late Game          
            RewardRooksOnOpenFiles(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            RewardRooksOnTheSeventh(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            RewardBlockadingDangerousPawns(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            PenaliseFixedPawnsOnEnemyBishopColour(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);

            whiteOpeningScore += pawns.WhiteBackwardsPawnScore;
            whiteOpeningScore += pawns.WhitePawnChainScore;
            whiteEndgameScore += pawns.WhitePassedPawnScore;
            whiteOpeningScore += pawns.WhitePassedPawnScore / 2;
            whiteEndgameScore += pawns.WhiteDoubledPawnScore;
            whiteEndgameScore += pawns.WhiteIsolatedPawnScore;

            blackOpeningScore += pawns.BlackBackwardPawnScore;
            blackOpeningScore += pawns.BlackPawnChainScore;
            blackEndgameScore += pawns.BlackPassedPawnScore;
            blackOpeningScore += pawns.BlackPassedPawnScore / 2;
            blackEndgameScore += pawns.BlackDoubledPawnScore;
            blackEndgameScore += pawns.BlackIsolatedPawnScore;

            PenaliseHoles(pawns.WhiteHoles, pawns.BlackHoles, out int whiteHolePenalty, out int blackHolePenalty);
            whiteOpeningScore -= whiteHolePenalty;
            blackOpeningScore -= blackHolePenalty;

            //Blockades depend on pieces, which the pawn hash doesn't key on, so this is worked out on every evaluation
            ReduceBlockadedPassedPawns(ref theBoard, whitePassedPawns, blackPassedPawns,
                out int whiteBlockadeReduction, out int blackBlockadeReduction);
            whiteEndgameScore -= whiteBlockadeReduction;
            whiteOpeningScore -= whiteBlockadeReduction / 2;
            blackEndgameScore -= blackBlockadeReduction;
            blackOpeningScore -= blackBlockadeReduction / 2;

            //Likewise depends on kings and pieces, so it is worked out on every evaluation
            ScorePassedPawnPromotionChances(ref theBoard, attackedByWhite, attackedByBlack, whitePassedPawns,
                blackPassedPawns,
                out int whitePromotionChances, out int blackPromotionChances);
            whiteEndgameScore += whitePromotionChances;
            blackEndgameScore += blackPromotionChances;

            AddExtraTerms(ref theBoard, in whitePieces, in blackPieces, attackedByWhite, attackedByBlack, in pawns,
                phase, null, ref whiteOpeningScore, ref whiteEndgameScore, ref blackOpeningScore,
                ref blackEndgameScore);

            //In endgames that are hard to win, the stronger side keeps only part of its endgame advantage
            int endgameDifference = whiteEndgameScore - blackEndgameScore;
            int endgameScale = EndgameScaleFactor(ref theBoard, endgameDifference > 0, whitePassedPawns,
                blackPassedPawns);
            whiteEndgameScore -= endgameDifference - endgameDifference * endgameScale / 64;

            int whiteScore = ((whiteOpeningScore * (256 - phase)) + (whiteEndgameScore * phase)) / 256;
            int blackScore = ((blackOpeningScore * (256 - phase)) + (blackEndgameScore * phase)) / 256;

            if (theBoard.WhiteHasDarkSquaredBishop && theBoard.WhiteHasLightSquaredBishop)
            {
                whiteScore += BISHOP_PAIR_BONUS_VALUE;
            }
            if (theBoard.BlackHasDarkSquaredBishop && theBoard.BlackHasLightSquaredBishop)
            {
                blackScore += BISHOP_PAIR_BONUS_VALUE;
            }

            //The exchange (one more rook, one fewer minor) is worth more than piece values plus positional terms
            //suggest, unless the side with the minor has two or more extra pawns for it
            int exchangeBalance = ExchangeImbalance(ref theBoard);
            if (exchangeBalance > 0)
            {
                whiteScore += EXCHANGE_IMBALANCE_BONUS;
            }
            else if (exchangeBalance < 0)
            {
                blackScore += EXCHANGE_IMBALANCE_BONUS;
            }

            if (theBoard.WhiteRookOneSquare != 255 && theBoard.WhiteRookTwoSquare != 255)
            {
                whiteScore -= ROOK_REDUNDANCY_PENALTY;
            }
            if (theBoard.BlackRookOneSquare != 255 && theBoard.BlackRookTwoSquare != 255)
            {
                blackScore -= ROOK_REDUNDANCY_PENALTY;
            }

            if (theBoard.WhiteKnightOneSquare != 255 && theBoard.WhiteKnightTwoSquare != 255)
            {
                whiteScore -= KNIGHT_REDUNDANCY_PENALTY;
            }
            if (theBoard.BlackKnightOneSquare != 255 && theBoard.BlackKnightTwoSquare != 255)
            {
                blackScore -= KNIGHT_REDUNDANCY_PENALTY;
            }

            //Each file with none of a side's pawns is worth a bonus to that side while it has a rook, and a
            //different one while it has both
            if (theBoard.WhiteRookOneSquare != 255 || theBoard.WhiteRookTwoSquare != 255)
            {
                whiteScore += pawns.WhitePawnlessFiles *
                    (theBoard.WhiteRookOneSquare != 255 && theBoard.WhiteRookTwoSquare != 255
                        ? SEMI_OPEN_FILE_TWO_ROOKS_BONUS : SEMI_OPEN_FILE_ONE_ROOK_BONUS);
            }

            if (theBoard.BlackRookOneSquare != 255 || theBoard.BlackRookTwoSquare != 255)
            {
                blackScore += pawns.BlackPawnlessFiles *
                    (theBoard.BlackRookOneSquare != 255 && theBoard.BlackRookTwoSquare != 255
                        ? SEMI_OPEN_FILE_TWO_ROOKS_BONUS : SEMI_OPEN_FILE_ONE_ROOK_BONUS);
            }

            tt.AddScoreToTransTable(theBoard.CurrentZobrist, whiteScore, blackScore);

            if (theBoard.OnMove == BLACK)
            {
                blackScore += TEMPO_BONUS;
                return (whiteScore - blackScore);
            }
            else
            {
                whiteScore += TEMPO_BONUS;
                return (blackScore - whiteScore);
            }

        }


        //Works out every field of the pawn hash entry except Filled and Zobrist, which the table sets when it stores
        //the entry. The fields for the extra terms are counts and masks, so weights are applied when they are used
        private static void AnalysePawns(ref Board theBoard, out TTPawnAnalysis pawns)
        {
            pawns = default;

            PenaliseBackwardsPawns(ref theBoard, out pawns.WhiteBackwardsPawnScore, out pawns.BlackBackwardPawnScore);
            RewardPawnChains(ref theBoard, out pawns.WhitePawnChainScore, out pawns.BlackPawnChainScore);
            RewardPassedPawns(ref theBoard, out pawns.WhitePassedPawnScore, out pawns.BlackPassedPawnScore);
            PenaliseDoubledAndTripledPawns(ref theBoard, out pawns.WhiteDoubledPawnScore,
                out pawns.BlackDoubledPawnScore);
            PenaliseIsolatedPawns(ref theBoard, out pawns.WhiteIsolatedPawnScore, out pawns.BlackIsolatedPawnScore);
            FindPassedPawns(ref theBoard, out pawns.WhitePassedPawns, out pawns.BlackPassedPawns);

            pawns.WhiteHoles = HoleMask(ref theBoard, WHITE);
            pawns.BlackHoles = HoleMask(ref theBoard, BLACK);
            pawns.WhitePawnAttacks = PawnAttackMask(theBoard.WhitePawnSquares, WHITE);
            pawns.BlackPawnAttacks = PawnAttackMask(theBoard.BlackPawnSquares, BLACK);

            pawns.WhiteStormFiles = PawnStormFiles(ref theBoard, WHITE);
            pawns.BlackStormFiles = PawnStormFiles(ref theBoard, BLACK);
            pawns.WhitePhalanxPawns = (byte)PawnsInPhalanx(ref theBoard, WHITE);
            pawns.BlackPhalanxPawns = (byte)PawnsInPhalanx(ref theBoard, BLACK);
            pawns.WhiteSpace = (byte)SpaceCount(ref theBoard, WHITE, pawns.BlackPawnAttacks);
            pawns.BlackSpace = (byte)SpaceCount(ref theBoard, BLACK, pawns.WhitePawnAttacks);
            pawns.WhiteUnopposedIsolatedPawns = (byte)UnopposedIsolatedPawns(ref theBoard, WHITE);
            pawns.BlackUnopposedIsolatedPawns = (byte)UnopposedIsolatedPawns(ref theBoard, BLACK);
            pawns.WhiteCandidatePassedPawns = (byte)CandidatePassedPawns(ref theBoard, WHITE);
            pawns.BlackCandidatePassedPawns = (byte)CandidatePassedPawns(ref theBoard, BLACK);
            pawns.WhitePawnIslands = (byte)PawnIslands(theBoard.WhiteFilePawns);
            pawns.BlackPawnIslands = (byte)PawnIslands(theBoard.BlackFilePawns);
            pawns.WhiteAdvancedFlankPawns = (byte)AdvancedFlankPawns(ref theBoard, WHITE);
            pawns.BlackAdvancedFlankPawns = (byte)AdvancedFlankPawns(ref theBoard, BLACK);
            pawns.WhitePawnlessFiles = (byte)PawnlessFiles(theBoard.WhiteFilePawns);
            pawns.BlackPawnlessFiles = (byte)PawnlessFiles(theBoard.BlackFilePawns);
        }


        public static void PenaliseTrappedRook(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.BlackKingSquare <= 3)
            {
                if (theBoard.Piece[0] == ROOK && theBoard.Color[0] == BLACK)
                {
                    if ((theBoard.Piece[8] == PAWN && theBoard.Color[8] == BLACK) ||
                        (theBoard.Piece[16] == PAWN && theBoard.Color[16] == BLACK))
                    {
                        blackScore -= TRAPPED_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[1] == 0)
                        {
                            blackScore += TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION;
                        }
                        if (theBoard.BlackFilePawns[2] == 0)
                        {
                            blackScore += TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION;
                        }
                    }
                }
            }

            if (theBoard.BlackKingSquare == 5 || theBoard.BlackKingSquare == 6)
            {
                if (theBoard.Piece[7] == ROOK && theBoard.Color[7] == BLACK)
                {
                    if ((theBoard.Piece[15] == PAWN && theBoard.Color[15] == BLACK) ||
                        (theBoard.Piece[23] == PAWN && theBoard.Color[23] == BLACK))
                    {
                        blackScore -= TRAPPED_ROOK_PENALTY;
                    }
                }
            }

            if (theBoard.WhiteKingSquare >= 57 && theBoard.WhiteKingSquare <= 59)
            {
                if (theBoard.Piece[56] == ROOK && theBoard.Color[56] == WHITE)
                {
                    if ((theBoard.Piece[48] == PAWN && theBoard.Color[48] == WHITE) ||
                        (theBoard.Piece[40] == PAWN && theBoard.Color[40] == WHITE))
                    {
                        whiteScore -= TRAPPED_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[1] == 0)
                        {
                            whiteScore += TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION;
                        }
                        if (theBoard.WhiteFilePawns[2] == 0)
                        {
                            whiteScore += TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION;
                        }
                    }
                }
            }

            if (theBoard.WhiteKingSquare == 61 || theBoard.WhiteKingSquare == 62)
            {
                if (theBoard.Piece[63] == ROOK && theBoard.Color[63] == WHITE)
                {
                    if ((theBoard.Piece[55] == PAWN && theBoard.Color[55] == WHITE) ||
                        (theBoard.Piece[47] == PAWN && theBoard.Color[47] == WHITE))
                    {
                        whiteScore -= TRAPPED_ROOK_PENALTY;
                    }
                }
            }

        }


        public static void RewardBlockadingDangerousPawns(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            for (int NN = 32; NN <= 39; NN++)
            {
                if (theBoard.Piece[NN] == PAWN && theBoard.Color[NN] == BLACK)
                {
                    if ((theBoard.Piece[NN + 8] == BISHOP || theBoard.Piece[NN + 8] == KNIGHT) &&
                        theBoard.Color[NN + 8] == WHITE)
                    {
                        whiteScore += BLOCKADING_PASSED_PAWN_BONUS;
                    }
                }
            }

            for (int NN = 24; NN <= 31; NN++)
            {
                if (theBoard.Piece[NN] == PAWN && theBoard.Color[NN] == WHITE)
                {
                    if ((theBoard.Piece[NN - 8] == BISHOP || theBoard.Piece[NN - 8] == KNIGHT) &&
                        theBoard.Color[NN - 8] == BLACK)
                    {
                        blackScore += BLOCKADING_PASSED_PAWN_BONUS;
                    }
                }
            }

        }


        public static void RewardKnightOutpost(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.Piece[26] == KNIGHT && theBoard.Color[26] == WHITE)
            {
                if (theBoard.Piece[33] == PAWN && theBoard.Color[33] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (theBoard.Piece[35] == PAWN && theBoard.Color[35] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }

            if (theBoard.Piece[27] == KNIGHT && theBoard.Color[27] == WHITE)
            {
                if (theBoard.Piece[34] == PAWN && theBoard.Color[34] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (theBoard.Piece[36] == PAWN && theBoard.Color[36] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (theBoard.Piece[28] == KNIGHT && theBoard.Color[28] == WHITE)
            {
                if (theBoard.Piece[35] == PAWN && theBoard.Color[35] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (theBoard.Piece[37] == PAWN && theBoard.Color[37] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (theBoard.Piece[29] == KNIGHT && theBoard.Color[29] == WHITE)
            {
                if (theBoard.Piece[36] == PAWN && theBoard.Color[36] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (theBoard.Piece[38] == PAWN && theBoard.Color[38] == WHITE)
                {
                    whiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }

            if (theBoard.Piece[34] == KNIGHT && theBoard.Color[34] == BLACK)
            {
                if (theBoard.Piece[27] == PAWN && theBoard.Color[27] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (theBoard.Piece[25] == PAWN && theBoard.Color[25] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }

            if (theBoard.Piece[35] == KNIGHT && theBoard.Color[35] == BLACK)
            {
                if (theBoard.Piece[28] == PAWN && theBoard.Color[28] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (theBoard.Piece[26] == PAWN && theBoard.Color[26] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (theBoard.Piece[36] == KNIGHT && theBoard.Color[36] == BLACK)
            {
                if (theBoard.Piece[29] == PAWN && theBoard.Color[29] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (theBoard.Piece[27] == PAWN && theBoard.Color[27] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (theBoard.Piece[37] == KNIGHT && theBoard.Color[37] == BLACK)
            {
                if (theBoard.Piece[30] == PAWN && theBoard.Color[30] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (theBoard.Piece[28] == PAWN && theBoard.Color[28] == BLACK)
                {
                    blackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }


        }


        public static void RewardPawnChains(ref Board theBoard, out int WhiteChainScore, out int BlackChainScore)
        {

            WhiteChainScore = 0; BlackChainScore = 0;
            for (int PP = 0; PP <= 7; PP++)
            {
                if (theBoard.WhitePawnSquares[PP] != -1)
                {
                    if (theBoard.WhitePawnSquares[PP] < 48)
                    {
                        //Pawns on the third rank (squares 40 to 47) and further up have separate weights
                        int bonus = theBoard.WhitePawnSquares[PP] >= 40 ? PAWN_CHAIN_BONUS : PAWN_CHAIN_ADVANCED_BONUS;
                        if (theBoard.WhitePawnSquares[PP] % 8 != 0)
                        {
                            if (theBoard.Color[theBoard.WhitePawnSquares[PP] + 7] == WHITE &&
                                theBoard.Piece[theBoard.WhitePawnSquares[PP] + 7] == PAWN)
                            {
                                WhiteChainScore += bonus;
                            }
                        }
                        if (theBoard.WhitePawnSquares[PP] % 8 != 7)
                        {
                            if (theBoard.Color[theBoard.WhitePawnSquares[PP] + 9] == WHITE &&
                                theBoard.Piece[theBoard.WhitePawnSquares[PP] + 9] == PAWN)
                            {
                                WhiteChainScore += bonus;
                            }
                        }
                    }
                }
                if (theBoard.BlackPawnSquares[PP] != -1)
                {
                    if (theBoard.BlackPawnSquares[PP] > 15)
                    {
                        //Black's third rank is squares 16 to 23
                        int bonus = theBoard.BlackPawnSquares[PP] <= 23 ? PAWN_CHAIN_BONUS : PAWN_CHAIN_ADVANCED_BONUS;
                        if (theBoard.BlackPawnSquares[PP] % 8 != 0)
                        {
                            if (theBoard.Color[theBoard.BlackPawnSquares[PP] - 9] == BLACK &&
                                theBoard.Piece[theBoard.BlackPawnSquares[PP] - 9] == PAWN)
                            {
                                BlackChainScore += bonus;
                            }
                        }
                        if (theBoard.BlackPawnSquares[PP] % 8 != 7)
                        {
                            if (theBoard.Color[theBoard.BlackPawnSquares[PP] - 7] == BLACK &&
                                theBoard.Piece[theBoard.BlackPawnSquares[PP] - 7] == PAWN)
                            {
                                BlackChainScore += bonus;
                            }
                        }
                    }
                }
            }

        }


        //A square on a side's third or fourth rank is a hole when that side has no pawn behind it on either
        //neighbouring file, as pawns only move forward and none can ever guard it again. Each costs HOLE_PENALTY.
        //Takes each side's HoleMask
        public static void PenaliseHoles(ulong whiteHoles, ulong blackHoles, out int whiteHolePenalty,
            out int blackHolePenalty)
        {
            whiteHolePenalty = BitOperations.PopCount(whiteHoles) * HOLE_PENALTY;
            blackHolePenalty = BitOperations.PopCount(blackHoles) * HOLE_PENALTY;
        }


        //A side's holes, one bit per square (bit 0 is a8). A square is guardable while a pawn on a neighbouring file
        //is on a lower rank than it, so the lower of the two neighbouring rearmost pawns decides whether the third
        //and fourth rank squares on a file are holes
        private static ulong HoleMask(ref Board theBoard, int side)
        {
            //The side's rearmost pawn on each file, as a rank counted from that side, 9 when the file has none
            Span<int> rearmostPawnRank = stackalloc int[8];
            rearmostPawnRank.Fill(9);
            int[] pawns = side == WHITE ? theBoard.WhitePawnSquares : theBoard.BlackPawnSquares;
            for (int pawnIndex = 0; pawnIndex <= 7; pawnIndex++)
            {
                int square = pawns[pawnIndex];
                if (square != -1)
                {
                    int rank = side == WHITE ? 8 - square / 8 : square / 8 + 1;
                    if (rank < rearmostPawnRank[square % 8])
                    {
                        rearmostPawnRank[square % 8] = rank;
                    }
                }
            }

            ulong holes = 0;
            for (int file = 0; file <= 7; file++)
            {
                int leftRank = file > 0 ? rearmostPawnRank[file - 1] : 9;
                int rightRank = file < 7 ? rearmostPawnRank[file + 1] : 9;
                int lowestGuardRank = Math.Min(leftRank, rightRank);
                if (lowestGuardRank >= 3)
                {
                    holes |= 1UL << (side == WHITE ? 5 * 8 + file : 2 * 8 + file);
                }
                if (lowestGuardRank >= 4)
                {
                    holes |= 1UL << (side == WHITE ? 4 * 8 + file : 3 * 8 + file);
                }
            }
            return holes;
        }


        //The squares a side's pawns attack, one bit per square (bit 0 is a8), whatever stands on them. White pawns
        //attack towards lower squares, Black pawns towards higher ones
        private static ulong PawnAttackMask(int[] pawnSquares, int side)
        {
            ulong attacks = 0;
            for (int pawnIndex = 0; pawnIndex <= 7; pawnIndex++)
            {
                int square = pawnSquares[pawnIndex];
                if (square == -1)
                {
                    continue;
                }
                int ahead = side == WHITE ? square - 8 : square + 8;
                if (ahead < 0 || ahead > 63)
                {
                    continue;
                }
                if (square % 8 > 0)
                {
                    attacks |= 1UL << (ahead - 1);
                }
                if (square % 8 < 7)
                {
                    attacks |= 1UL << (ahead + 1);
                }
            }
            return attacks;
        }


        //Adds one side's mobility. targets is the squares its pieces can move to and enemyPawnAttacks the enemy's
        //PawnAttackMask
        private static void AddMobility(ReadOnlySpan<PieceAttacks> pieceAttacks, ulong targets,
            ulong enemyPawnAttacks, ref int openingScore, ref int endgameScore)
        {
            int[] openingBonus = OPENING_MOBILITY_BONUS;
            int[] endgameBonus = ENDGAME_MOBILITY_BONUS;
            int[] openingUnsafeBonus = OPENING_MOBILITY_UNSAFE_BONUS;
            int[] endgameUnsafeBonus = ENDGAME_MOBILITY_UNSAFE_BONUS;

            int opening = 0;
            int endgame = 0;
            for (int nn = 0; nn < pieceAttacks.Length; nn++)
            {
                ulong moves = pieceAttacks[nn].Attacks & targets;
                int piece = pieceAttacks[nn].Piece;
                int unsafeMoves = BitOperations.PopCount(moves & enemyPawnAttacks);
                int safeMoves = BitOperations.PopCount(moves & ~enemyPawnAttacks);
                opening += safeMoves * openingBonus[piece] + unsafeMoves * openingUnsafeBonus[piece];
                endgame += safeMoves * endgameBonus[piece] + unsafeMoves * endgameUnsafeBonus[piece];
            }
            openingScore += opening;
            endgameScore += endgame;
        }


        public static void RewardMobility(ref Board theBoard, ReadOnlySpan<PieceAttacks> whitePieceAttacks,
            ReadOnlySpan<PieceAttacks> blackPieceAttacks, in PieceAttackMasks whitePieces,
            in PieceAttackMasks blackPieces, ulong whitePawnAttacks, ulong blackPawnAttacks,
            ref int whiteOpeningScore, ref int blackOpeningScore,
            ref int whiteEndgameScore, ref int blackEndgameScore)
        {

            //A piece's moves are the squares it bears on, less those holding its own side's pieces and the enemy
            //king. Squares an enemy pawn attacks have their own (unsafe) weights.
            //Each piece type has its own per-move weight in each half: a queen collects far more moves than a knight,
            //and they mean less.
            //The knight centre and rim terms below count in the opening half only
            AddMobility(whitePieceAttacks, ~(whitePieces.Occupied | blackPieces.Kings), blackPawnAttacks,
                ref whiteOpeningScore, ref whiteEndgameScore);
            AddMobility(blackPieceAttacks, ~(blackPieces.Occupied | whitePieces.Kings), whitePawnAttacks,
                ref blackOpeningScore, ref blackEndgameScore);

            whiteOpeningScore += KnightCentreAndRim(theBoard.WhiteKnightOneSquare) +
                KnightCentreAndRim(theBoard.WhiteKnightTwoSquare);
            blackOpeningScore += KnightCentreAndRim(theBoard.BlackKnightOneSquare) +
                KnightCentreAndRim(theBoard.BlackKnightTwoSquare);

        }


        //A bonus for each of the four centre squares a knight on this square bears on, less a penalty if it stands on
        //the a or h file. knightSquare is 255 when the side has no such knight
        private static int KnightCentreAndRim(int knightSquare)
        {
            if (knightSquare == 255)
            {
                return 0;
            }
            int score = BitOperations.PopCount(_knightMoveMasks[knightSquare] & CENTRE_SQUARES) *
                OPENING_MINOR_PIECE_INFLUENCES_CENTER;
            if (knightSquare % 8 == 0 || knightSquare % 8 == 7)
            {
                score -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
            }
            return score;
        }


        private static int PawnlessFiles(int[] filePawns)
        {
            int count = 0;
            for (int file = 0; file <= 7; file++)
            {
                if (filePawns[file] == 0)
                {
                    count += 1;
                }
            }
            return count;
        }


        public static void PenaliseBackwardsPawns(ref Board theBoard, out int whiteBWPScore, out int blackBWPScore)
        {

            whiteBWPScore = 0; blackBWPScore = 0;

            //white
            if (theBoard.Color[48] == WHITE && theBoard.Piece[48] == PAWN)
            {
                //a pawn in place
                if (theBoard.Color[49] != WHITE || theBoard.Piece[49] != PAWN)
                {
                    if (theBoard.Color[41] != WHITE || theBoard.Piece[41] != PAWN)
                    {
                        //white a pawn backward
                        whiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            if (theBoard.Color[49] == WHITE && theBoard.Piece[49] == PAWN)
            {
                //b pawn in place
                if (theBoard.Color[50] != WHITE || theBoard.Piece[50] != PAWN)
                {
                    if (theBoard.Color[42] != WHITE || theBoard.Piece[42] != PAWN)
                    {
                        if (theBoard.Color[48] != WHITE || theBoard.Piece[48] != PAWN)
                        {
                            if (theBoard.Color[40] != WHITE || theBoard.Piece[40] != PAWN)
                            {
                                //white b pawn backward
                                whiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[50] == WHITE && theBoard.Piece[50] == PAWN)
            {
                //c pawn in place
                if (theBoard.Color[51] != WHITE || theBoard.Piece[51] != PAWN)
                {
                    if (theBoard.Color[43] != WHITE || theBoard.Piece[43] != PAWN)
                    {
                        if (theBoard.Color[49] != WHITE || theBoard.Piece[49] != PAWN)
                        {
                            if (theBoard.Color[41] != WHITE || theBoard.Piece[41] != PAWN)
                            {
                                //white c pawn backward
                                whiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[51] == WHITE && theBoard.Piece[51] == PAWN)
            {
                //d pawn in place
                if (theBoard.Color[52] != WHITE || theBoard.Piece[52] != PAWN)
                {
                    if (theBoard.Color[44] != WHITE || theBoard.Piece[44] != PAWN)
                    {
                        if (theBoard.Color[50] != WHITE || theBoard.Piece[50] != PAWN)
                        {
                            if (theBoard.Color[42] != WHITE || theBoard.Piece[42] != PAWN)
                            {
                                //white d pawn backward
                                whiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[52] == WHITE && theBoard.Piece[52] == PAWN)
            {
                //e pawn in place
                if (theBoard.Color[53] != WHITE || theBoard.Piece[53] != PAWN)
                {
                    if (theBoard.Color[45] != WHITE || theBoard.Piece[45] != PAWN)
                    {
                        if (theBoard.Color[51] != WHITE || theBoard.Piece[51] != PAWN)
                        {
                            if (theBoard.Color[43] != WHITE || theBoard.Piece[43] != PAWN)
                            {
                                //white e pawn backward
                                whiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[53] == WHITE && theBoard.Piece[53] == PAWN)
            {
                //f pawn in place
                if (theBoard.Color[54] != WHITE || theBoard.Piece[54] != PAWN)
                {
                    if (theBoard.Color[46] != WHITE || theBoard.Piece[46] != PAWN)
                    {
                        if (theBoard.Color[52] != WHITE || theBoard.Piece[52] != PAWN)
                        {
                            if (theBoard.Color[44] != WHITE || theBoard.Piece[44] != PAWN)
                            {
                                //white f pawn backward
                                whiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[54] == WHITE && theBoard.Piece[54] == PAWN)
            {
                //g pawn in place
                if (theBoard.Color[55] != WHITE || theBoard.Piece[55] != PAWN)
                {
                    if (theBoard.Color[47] != WHITE || theBoard.Piece[47] != PAWN)
                    {
                        if (theBoard.Color[53] != WHITE || theBoard.Piece[53] != PAWN)
                        {
                            if (theBoard.Color[45] != WHITE || theBoard.Piece[45] != PAWN)
                            {
                                //white g pawn backward
                                whiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[55] == WHITE && theBoard.Piece[55] == PAWN)
            {
                //h pawn in place
                if (theBoard.Color[54] != WHITE || theBoard.Piece[54] != PAWN)
                {
                    if (theBoard.Color[46] != WHITE || theBoard.Piece[46] != PAWN)
                    {
                        //white h pawn backward
                        whiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            //black
            if (theBoard.Color[8] == BLACK && theBoard.Piece[8] == PAWN)
            {
                if (theBoard.Color[9] != BLACK || theBoard.Piece[9] != PAWN)
                {
                    if (theBoard.Color[17] != BLACK || theBoard.Piece[17] != PAWN)
                    {
                        //black a pawn backward
                        blackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            if (theBoard.Color[9] == BLACK && theBoard.Piece[9] == PAWN)
            {
                //b pawn in place
                if (theBoard.Color[10] != BLACK || theBoard.Piece[10] != PAWN)
                {
                    if (theBoard.Color[18] != BLACK || theBoard.Piece[18] != PAWN)
                    {
                        if (theBoard.Color[8] != BLACK || theBoard.Piece[8] != PAWN)
                        {
                            if (theBoard.Color[16] != BLACK || theBoard.Piece[16] != PAWN)
                            {
                                //black b pawn backward
                                blackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[10] == BLACK && theBoard.Piece[10] == PAWN)
            {
                //c pawn in place
                if (theBoard.Color[11] != BLACK || theBoard.Piece[11] != PAWN)
                {
                    if (theBoard.Color[19] != BLACK || theBoard.Piece[19] != PAWN)
                    {
                        if (theBoard.Color[9] != BLACK || theBoard.Piece[9] != PAWN)
                        {
                            if (theBoard.Color[17] != BLACK || theBoard.Piece[17] != PAWN)
                            {
                                //black c pawn backward
                                blackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[11] == BLACK && theBoard.Piece[11] == PAWN)
            {
                //d pawn in place
                if (theBoard.Color[12] != BLACK || theBoard.Piece[12] != PAWN)
                {
                    if (theBoard.Color[20] != BLACK || theBoard.Piece[20] != PAWN)
                    {
                        if (theBoard.Color[10] != BLACK || theBoard.Piece[10] != PAWN)
                        {
                            if (theBoard.Color[18] != BLACK || theBoard.Piece[18] != PAWN)
                            {
                                //black d pawn backward
                                blackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[12] == BLACK && theBoard.Piece[12] == PAWN)
            {
                //e pawn in place
                if (theBoard.Color[13] != BLACK || theBoard.Piece[13] != PAWN)
                {
                    if (theBoard.Color[21] != BLACK || theBoard.Piece[21] != PAWN)
                    {
                        if (theBoard.Color[11] != BLACK || theBoard.Piece[11] != PAWN)
                        {
                            if (theBoard.Color[19] != BLACK || theBoard.Piece[19] != PAWN)
                            {
                                //black e pawn backward
                                blackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[13] == BLACK && theBoard.Piece[13] == PAWN)
            {
                //f pawn in place
                if (theBoard.Color[14] != BLACK || theBoard.Piece[14] != PAWN)
                {
                    if (theBoard.Color[22] != BLACK || theBoard.Piece[22] != PAWN)
                    {
                        if (theBoard.Color[12] != BLACK || theBoard.Piece[12] != PAWN)
                        {
                            if (theBoard.Color[20] != BLACK || theBoard.Piece[20] != PAWN)
                            {
                                //black f pawn backward
                                blackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[14] == BLACK && theBoard.Piece[14] == PAWN)
            {
                //g pawn in place
                if (theBoard.Color[15] != BLACK || theBoard.Piece[15] != PAWN)
                {
                    if (theBoard.Color[23] != BLACK || theBoard.Piece[23] != PAWN)
                    {
                        if (theBoard.Color[13] != BLACK || theBoard.Piece[13] != PAWN)
                        {
                            if (theBoard.Color[21] != BLACK || theBoard.Piece[21] != PAWN)
                            {
                                //black g pawn backward
                                blackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (theBoard.Color[15] == BLACK && theBoard.Piece[15] == PAWN)
            {
                //h pawn in place
                if (theBoard.Color[14] != BLACK || theBoard.Piece[14] != PAWN)
                {
                    if (theBoard.Color[22] != BLACK || theBoard.Piece[22] != PAWN)
                    {
                        //black h pawn backward
                        blackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }


        }

        public static void RewardRooksOnTheSeventh(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteRookOneSquare >= 8 && theBoard.WhiteRookOneSquare <= 15)
            {
                whiteScore += ROOK_ON_SEVENTH_BONUS;
            }

            if (theBoard.WhiteRookTwoSquare >= 8 && theBoard.WhiteRookTwoSquare <= 15)
            {
                whiteScore += ROOK_ON_SEVENTH_BONUS;
            }

            if (theBoard.BlackRookOneSquare >= 48 && theBoard.BlackRookOneSquare <= 55)
            {
                blackScore += ROOK_ON_SEVENTH_BONUS;
            }

            if (theBoard.BlackRookTwoSquare >= 48 && theBoard.BlackRookTwoSquare <= 55)
            {
                blackScore += ROOK_ON_SEVENTH_BONUS;
            }

        }


        private static void RewardPawnBishopSynergy(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.BlackHasDarkSquaredBishop && !theBoard.BlackHasLightSquaredBishop)
            {
                blackScore -= theBoard.BlackPawnsOnDarkSquares * BISHOP_PAWN_COLOR_PENALTY;
            }
            if (theBoard.BlackHasLightSquaredBishop && !theBoard.BlackHasDarkSquaredBishop)
            {
                blackScore -= theBoard.BlackPawnsOnLightSquares * BISHOP_PAWN_COLOR_PENALTY;
            }

            if (theBoard.WhiteHasDarkSquaredBishop && !theBoard.WhiteHasLightSquaredBishop)
            {
                whiteScore -= theBoard.WhitePawnsOnDarkSquares * BISHOP_PAWN_COLOR_PENALTY;
            }
            if (theBoard.WhiteHasLightSquaredBishop && !theBoard.WhiteHasDarkSquaredBishop)
            {
                whiteScore -= theBoard.WhitePawnsOnLightSquares * BISHOP_PAWN_COLOR_PENALTY;
            }


        }


        //private static void EncouragePawnsOnOppositeSideToKingToAdvance(ref Board theBoard, ref int whiteScore, ref
        //int blackScore)
        //{

        //    if (theBoard.WhiteKingSquare % 8 >= 5)
        //    {
        //        if (theBoard.Color[48] == WHITE && theBoard.Piece[48] == PAWN)
        //        {
        //            whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[49] == WHITE && theBoard.Piece[49] == PAWN)
        //        {
        //            whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[50] == WHITE && theBoard.Piece[50] == PAWN)
        //        {
        //            whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //    }
        //    else if (theBoard.WhiteKingSquare % 8 <= 2)
        //    {
        //        if (theBoard.Color[55] == WHITE && theBoard.Piece[55] == PAWN)
        //        {
        //            whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[54] == WHITE && theBoard.Piece[54] == PAWN)
        //        {
        //            whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[53] == WHITE && theBoard.Piece[53] == PAWN)
        //        {
        //            whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //    }

        //    if (theBoard.BlackKingSquare % 8 >= 5)
        //    {
        //        if (theBoard.Color[8] == BLACK && theBoard.Piece[8] == PAWN)
        //        {
        //            blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[9] == BLACK && theBoard.Piece[9] == PAWN)
        //        {
        //            blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[10] == BLACK && theBoard.Piece[10] == PAWN)
        //        {
        //            blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //    }
        //    else if (theBoard.BlackKingSquare % 8 <= 2)
        //    {
        //        if (theBoard.Color[15] == BLACK && theBoard.Piece[15] == PAWN)
        //        {
        //            blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[14] == BLACK && theBoard.Piece[14] == PAWN)
        //        {
        //            blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //        if (theBoard.Color[13] == BLACK && theBoard.Piece[13] == PAWN)
        //        {
        //            blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
        //        }
        //    }

        //}


        private static void RewardActiveBishop(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteHasDarkSquaredBishop)
            {
                whiteScore += BishopTargetBonus(theBoard.WhiteDarkBishopSquare, theBoard.BlackQueenSquare,
                    theBoard.BlackRookOneSquare, theBoard.BlackRookTwoSquare, theBoard.BlackKnightOneSquare,
                    theBoard.BlackKnightTwoSquare);
            }
            if (theBoard.WhiteHasLightSquaredBishop)
            {
                whiteScore += BishopTargetBonus(theBoard.WhiteLightBishopSquare, theBoard.BlackQueenSquare,
                    theBoard.BlackRookOneSquare, theBoard.BlackRookTwoSquare, theBoard.BlackKnightOneSquare,
                    theBoard.BlackKnightTwoSquare);
            }
            if (theBoard.BlackHasDarkSquaredBishop)
            {
                blackScore += BishopTargetBonus(theBoard.BlackDarkBishopSquare, theBoard.WhiteQueenSquare,
                    theBoard.WhiteRookOneSquare, theBoard.WhiteRookTwoSquare, theBoard.WhiteKnightOneSquare,
                    theBoard.WhiteKnightTwoSquare);
            }
            if (theBoard.BlackHasLightSquaredBishop)
            {
                blackScore += BishopTargetBonus(theBoard.BlackLightBishopSquare, theBoard.WhiteQueenSquare,
                    theBoard.WhiteRookOneSquare, theBoard.WhiteRookTwoSquare, theBoard.WhiteKnightOneSquare,
                    theBoard.WhiteKnightTwoSquare);
            }

        }


        //The bonuses for the enemy queen, rooks and knights standing on a diagonal of the bishop's square, whatever
        //lies in between. A target square is 255 when the enemy has no such piece
        private static int BishopTargetBonus(int bishopSquare, int queen, int rookOne, int rookTwo, int knightOne,
            int knightTwo)
        {
            ulong diagonals = _diagonalMasks[bishopSquare];
            return OnMask(diagonals, queen) * BISHOP_ATTACKS_QUEEN_BONUS +
                (OnMask(diagonals, rookOne) + OnMask(diagonals, rookTwo)) * BISHOP_ATTACKS_ROOK_BONUS +
                (OnMask(diagonals, knightOne) + OnMask(diagonals, knightTwo)) * BISHOP_ATTACKS_KNIGHT_BONUS;
        }


        //1 if the square is in the mask, otherwise 0. A square of 255 (no such piece) gives 0
        private static int OnMask(ulong mask, int square)
        {
            return square != 255 && ((mask >> square) & 1UL) != 0 ? 1 : 0;
        }


        public static void RewardPassedPawns(ref Board theBoard, out int whitePPScore, out int blackPPScore)
        {

            whitePPScore = 0; 
            blackPPScore = 0;

            bool[] whiteFilePassed = new bool[8];
            bool[] blackFilePassed = new bool[8];

            for (int nn = 0; nn <= 7; nn++)
            {
                if (theBoard.WhiteFilePawns[nn] >= 1 && theBoard.BlackFilePawns[nn] == 0)
                {
                    int pawnSquare = -1; 
                    bool isPassed = true;
                    for (int pp = 0; pp <= 7; pp++)
                    {
                        if (theBoard.WhitePawnSquares[pp] != -1 && theBoard.WhitePawnSquares[pp] % 8 == nn)
                        {
                            if (pawnSquare == -1)
                            {
                                pawnSquare = theBoard.WhitePawnSquares[pp];
                            }
                            else if (theBoard.WhitePawnSquares[pp] < pawnSquare)
                            {
                                pawnSquare = theBoard.WhitePawnSquares[pp];
                            }
                        }
                    }
                    for (int sq = 0; sq < theBoard.WhitePassedPawnLookUps[pawnSquare].Length; sq++)
                    {
                        if (theBoard.Piece[theBoard.WhitePassedPawnLookUps[pawnSquare][sq]] == PAWN &&
                            theBoard.Color[theBoard.WhitePassedPawnLookUps[pawnSquare][sq]] == BLACK)
                        {
                            isPassed = false;
                            break;
                        }
                    }
                    if (isPassed)
                    {
                        whiteFilePassed[nn] = true;
                        whitePPScore += PASSED_PAWN_BONUS;
                        if (pawnSquare < 16)
                        {
                            whitePPScore += PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (pawnSquare >= 16 && pawnSquare < 24)
                        {
                            whitePPScore += PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (pawnSquare >= 24 && pawnSquare < 32)
                        {
                            whitePPScore += PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS;
                        }
                        if (nn > 0)
                        {
                            if (whiteFilePassed[nn - 1])
                            {
                                whitePPScore += CONNECTED_PASSED_PAWN_BONUS;
                            }
                        }
                    }
                }
                if (theBoard.BlackFilePawns[nn] >= 1 && theBoard.WhiteFilePawns[nn] == 0)
                {
                    int pawnSquare = 255; 
                    bool isPassed = true;
                    for (int pp = 0; pp <= 7; pp++)
                    {
                        if (theBoard.BlackPawnSquares[pp] != -1 && theBoard.BlackPawnSquares[pp] % 8 == nn)
                        {
                            if (pawnSquare == 255)
                            {
                                pawnSquare = theBoard.BlackPawnSquares[pp];
                            }
                            else if (theBoard.BlackPawnSquares[pp] > pawnSquare)
                            {
                                pawnSquare = theBoard.BlackPawnSquares[pp];
                            }
                        }
                    }
                    for (int sq = 0; sq < theBoard.BlackPassedPawnLookUps[pawnSquare].Length; sq++)
                    {
                        if (theBoard.Piece[theBoard.BlackPassedPawnLookUps[pawnSquare][sq]] == PAWN &&
                            theBoard.Color[theBoard.BlackPassedPawnLookUps[pawnSquare][sq]] == WHITE)
                        {
                            isPassed = false;
                            break;
                        }
                    }
                    if (isPassed)
                    {
                        blackFilePassed[nn] = true;
                        blackPPScore += PASSED_PAWN_BONUS;
                        if (pawnSquare >= 48)
                        {
                            blackPPScore += PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (pawnSquare >= 40 && pawnSquare < 48)
                        {
                            blackPPScore += PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (pawnSquare >= 32 && pawnSquare < 40)
                        {
                            blackPPScore += PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS;
                        }
                        if (nn > 0)
                        {
                            if (blackFilePassed[nn - 1])
                            {
                                blackPPScore += CONNECTED_PASSED_PAWN_BONUS;
                            }
                        }
                    }
                }
            }

        }


        private static void RewardRooksOnOpenFiles(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteRookOneSquare != 255 && theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
            {
                //White rook one on semi open file
                whiteScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (theBoard.BlackFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                {
                    //White rook one on fully open file
                    whiteScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

            if (theBoard.WhiteRookTwoSquare != 255 && theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
            {
                //White rook two on open file
                whiteScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (theBoard.BlackFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                {
                    //White rook two on fully open file
                    whiteScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

            if (theBoard.BlackRookOneSquare != 255 && theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
            {
                //Black rook one on open file
                blackScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (theBoard.WhiteFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                {
                    //Black rook one on fully open file
                    blackScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

            if (theBoard.BlackRookTwoSquare != 255 && theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
            {
                //Black rook two on open file
                blackScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (theBoard.WhiteFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                {
                    //Black rook two on fully open file
                    blackScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

        }


        private static void PenaliseIsolatedPawns(ref Board theBoard, out int whiteIsoPawnScore,
            out int blackIsoPawnScore)
        {

            whiteIsoPawnScore = 0; blackIsoPawnScore = 0;

            if (theBoard.WhiteFilePawns[0] > 0 && theBoard.WhiteFilePawns[1] == 0)
            {
                //white a pawn(s) isolated
                whiteIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY * theBoard.WhiteFilePawns[0];
            }
            if (theBoard.WhiteFilePawns[7] > 0 && theBoard.WhiteFilePawns[6] == 0)
            {
                //white h pawn(s) isolated
                whiteIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY * theBoard.WhiteFilePawns[7];
            }
            if (theBoard.BlackFilePawns[0] > 0 && theBoard.BlackFilePawns[1] == 0)
            {
                //black a pawn(s) isolated
                blackIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY * theBoard.BlackFilePawns[0];
            }
            if (theBoard.BlackFilePawns[7] > 0 && theBoard.BlackFilePawns[6] == 0)
            {
                //black h pawn(s) isolated
                blackIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY * theBoard.BlackFilePawns[7];
            }

            for (int NN = 1; NN <= 6; NN++)
            {
                if (theBoard.WhiteFilePawns[NN] > 0 && theBoard.WhiteFilePawns[NN - 1] == 0 &&
                    theBoard.WhiteFilePawns[NN + 1] == 0)
                {
                    //more central white pawn(s) isolated
                    whiteIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY * theBoard.WhiteFilePawns[NN];
                }
                if (theBoard.BlackFilePawns[NN] > 0 && theBoard.BlackFilePawns[NN - 1] == 0 &&
                    theBoard.BlackFilePawns[NN + 1] == 0)
                {
                    //more central black pawn(s) isolated
                    blackIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY * theBoard.BlackFilePawns[NN];
                }
            }

        }


        private static void PenaliseDoubledAndTripledPawns(ref Board theBoard, out int whiteDblPawnScore,
            out int blackDblPawnScore)
        {

            whiteDblPawnScore = 0; 
            blackDblPawnScore = 0;

            for (int NN = 0; NN <= 7; NN++)
            {
                if (theBoard.WhiteFilePawns[NN] == 2)
                {
                    whiteDblPawnScore -= DOUBLED_PAWN_PENALTY;
                    if (theBoard.BlackFilePawns[NN] > 0)
                    {
                        whiteDblPawnScore -= DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
                else if (theBoard.WhiteFilePawns[NN] > 2)
                {
                    whiteDblPawnScore -= TRIPLED_PAWN_PENALTY;
                    if (theBoard.BlackFilePawns[NN] > 0)
                    {
                        whiteDblPawnScore -= TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
                if (theBoard.BlackFilePawns[NN] == 2)
                {
                    blackDblPawnScore -= DOUBLED_PAWN_PENALTY;
                    if (theBoard.WhiteFilePawns[NN] > 0)
                    {
                        blackDblPawnScore -= DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
                else if (theBoard.BlackFilePawns[NN] > 2)
                {
                    blackDblPawnScore -= TRIPLED_PAWN_PENALTY;
                    if (theBoard.WhiteFilePawns[NN] > 0)
                    {
                        blackDblPawnScore -= TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
            }

        }


        private static void RewardKingSafety(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            //###TODO: Knights?

            if (theBoard.WhiteKingSquare < 48)
            {
                if (theBoard.BlackQueenSquare != 255)
                {
                    whiteScore -= KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY;
                }
                else
                {
                    whiteScore -= KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY;
                }
            }
            else
            {
                if (theBoard.WhiteKingSquare < 56 && !IsWingFile(theBoard.WhiteKingSquare % 8))
                {
                    whiteScore -= KING_STEPPED_UP_EARLY_PENALTY;
                }
                whiteScore += KingShelterScore(ref theBoard, theBoard.WhiteKingSquare % 8, WHITE);
            }

            if (theBoard.BlackKingSquare > 15)
            {
                if (theBoard.WhiteQueenSquare != 255)
                {
                    blackScore -= KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY;
                }
                else
                {
                    blackScore -= KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY;
                }
            }
            else
            {
                if (theBoard.BlackKingSquare > 7 && !IsWingFile(theBoard.BlackKingSquare % 8))
                {
                    blackScore -= KING_STEPPED_UP_EARLY_PENALTY;
                }
                blackScore += KingShelterScore(ref theBoard, theBoard.BlackKingSquare % 8, BLACK);
            }

        }


        //Penalises a king whose zone is under attack, while the attacking side still has its queen
        private static void PenaliseKingAttack(ref Board theBoard, ReadOnlySpan<PieceAttacks> whitePieceAttacks,
            ReadOnlySpan<PieceAttacks> blackPieceAttacks, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteQueenSquare != 255)
            {
                blackScore -= KING_ATTACK_PENALTY[NetKingAttackUnits(ref theBoard, whitePieceAttacks, blackPieceAttacks,
                    theBoard.BlackKingSquare, WHITE)];
            }

            if (theBoard.BlackQueenSquare != 255)
            {
                whiteScore -= KING_ATTACK_PENALTY[NetKingAttackUnits(ref theBoard, blackPieceAttacks, whitePieceAttacks,
                    theBoard.WhiteKingSquare, BLACK)];
            }

        }


        //Attack units less the defenders' units, never below 0. Defence only reduces an attack that passed the
        //two-attacker gate: a well-defended king earns no bonus by itself.
        private static int NetKingAttackUnits(ref Board theBoard, ReadOnlySpan<PieceAttacks> attackers,
            ReadOnlySpan<PieceAttacks> defenders, int kingSquare, int attackingSide)
        {
            int units = KingAttackUnits(ref theBoard, attackers, kingSquare, attackingSide);
            if (units == 0)
            {
                return 0;
            }
            ulong kingZone = attackingSide == WHITE ? theBoard.BlackKingZone[kingSquare]
                : theBoard.WhiteKingZone[kingSquare];
            units -= KingDefenceUnits(defenders, kingZone);
            return units > 0 ? units : 0;
        }


        //Defence units from every square of its own king zone the defending side's pieces bear on, including squares
        //its own pieces stand on, weighted by piece type (knights most, sliders less)
        private static int KingDefenceUnits(ReadOnlySpan<PieceAttacks> defenders, ulong kingZone)
        {
            int units = 0;
            for (int nn = 0; nn < defenders.Length; nn++)
            {
                units += BitOperations.PopCount(defenders[nn].Attacks & kingZone) *
                    KING_DEFENCE_UNITS[defenders[nn].Piece];
            }
            return units;
        }


        //Attack units from every attacked square in the king zone, weighted by the attacking piece's type, plus x-ray
        //pressure from sliders that don't reach the zone yet. One attacker alone scores nothing.
        private static int KingAttackUnits(ref Board theBoard, ReadOnlySpan<PieceAttacks> attackingPieces,
            int kingSquare, int attackingSide)
        {
            ulong kingZone = attackingSide == WHITE ? theBoard.BlackKingZone[kingSquare]
                : theBoard.WhiteKingZone[kingSquare];
            //One piece counts for at most KING_ATTACK_MAX_SQUARES_PER_PIECE zone squares, so a queen alone can't
            //carry an attack
            int mostSquaresPerPiece = Math.Max(KING_ATTACK_MAX_SQUARES_PER_PIECE, 0);
            int units = 0;
            int attackers = 0;
            ulong directAttackers = 0;

            for (int nn = 0; nn < attackingPieces.Length; nn++)
            {
                int zoneSquares = BitOperations.PopCount(attackingPieces[nn].Attacks & kingZone);
                if (zoneSquares > 0)
                {
                    attackers++;
                    directAttackers |= 1UL << attackingPieces[nn].Square;
                    units += KING_ATTACK_UNITS[attackingPieces[nn].Piece] *
                        Math.Min(zoneSquares, mostSquaresPerPiece);
                }
            }

            if (attackingSide == WHITE)
            {
                int[] ownFilePawns = theBoard.WhiteFilePawns;
                if (theBoard.WhiteHasLightSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.WhiteLightBishopSquare, kingSquare, attackingSide,
                        ownFilePawns, directAttackers, ref units, ref attackers);
                }
                if (theBoard.WhiteHasDarkSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.WhiteDarkBishopSquare, kingSquare, attackingSide,
                        ownFilePawns, directAttackers, ref units, ref attackers);
                }
                AddXRayAttacker(ref theBoard, theBoard.WhiteRookOneSquare, kingSquare, attackingSide, ownFilePawns,
                    directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.WhiteRookTwoSquare, kingSquare, attackingSide, ownFilePawns,
                    directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.WhiteQueenSquare, kingSquare, attackingSide, ownFilePawns,
                    directAttackers, ref units, ref attackers);
            }
            else
            {
                int[] ownFilePawns = theBoard.BlackFilePawns;
                if (theBoard.BlackHasLightSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.BlackLightBishopSquare, kingSquare, attackingSide,
                        ownFilePawns, directAttackers, ref units, ref attackers);
                }
                if (theBoard.BlackHasDarkSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.BlackDarkBishopSquare, kingSquare, attackingSide,
                        ownFilePawns, directAttackers, ref units, ref attackers);
                }
                AddXRayAttacker(ref theBoard, theBoard.BlackRookOneSquare, kingSquare, attackingSide, ownFilePawns,
                    directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.BlackRookTwoSquare, kingSquare, attackingSide, ownFilePawns,
                    directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.BlackQueenSquare, kingSquare, attackingSide, ownFilePawns,
                    directAttackers, ref units, ref attackers);
            }

            if (attackers < 2)
            {
                return 0;
            }
            return units < KING_ATTACK_PENALTY.Length ? units : KING_ATTACK_PENALTY.Length - 1;
        }


        //Adds a slider aimed at the king without reaching the zone: a bishop or queen on the king's diagonal, or a rook
        //or queen on the king's file or one beside it with none of its own pawns on that file. It counts as an attacker
        //hitting KING_ATTACK_XRAY_SQUARES zone squares. Pieces already attacking the zone directly are skipped.
        private static void AddXRayAttacker(ref Board theBoard, int pieceSquare, int kingSquare, int attackingSide,
            int[] ownFilePawns, ulong directAttackers, ref int units, ref int attackers)
        {
            if (pieceSquare == 255 || ((directAttackers >> pieceSquare) & 1UL) != 0)
            {
                return;
            }

            int piece = theBoard.Piece[pieceSquare];
            bool aimed = false;

            if ((piece == BISHOP || piece == QUEEN) && theBoard.SameDiagonal[pieceSquare][kingSquare])
            {
                aimed = XRayLineIsOpen(ref theBoard, pieceSquare, kingSquare, attackingSide);
            }

            if (!aimed && (piece == ROOK || piece == QUEEN))
            {
                int file = pieceSquare % 8;
                int fileDiff = file - kingSquare % 8;
                if (fileDiff >= -1 && fileDiff <= 1 && ownFilePawns[file] == 0)
                {
                    int targetSquare = (kingSquare / 8) * 8 + file;
                    aimed = targetSquare != pieceSquare && XRayLineIsOpen(ref theBoard, pieceSquare, targetSquare,
                        attackingSide);
                }
            }

            if (aimed)
            {
                units += KING_ATTACK_UNITS[piece] * KING_ATTACK_XRAY_SQUARES;
                attackers++;
            }
        }


        //Walks the squares strictly between the two. Other pieces don't block an x-ray because they can move;
        //the attacker's own king and fixed pawns do.
        private static bool XRayLineIsOpen(ref Board theBoard, int fromSquare, int toSquare, int attackingSide)
        {
            int step = Math.Sign(toSquare / 8 - fromSquare / 8) * 8 + Math.Sign(toSquare % 8 - fromSquare % 8);
            for (int sq = fromSquare + step; sq != toSquare; sq += step)
            {
                if (theBoard.Color[sq] == attackingSide && theBoard.Piece[sq] == KING)
                {
                    return false;
                }
                if (theBoard.Color[sq] != EMPTY && theBoard.Piece[sq] == PAWN && IsFixedPawn(ref theBoard, sq))
                {
                    return false;
                }
            }
            return true;
        }


        //A pawn is fixed when a pawn stands directly in front of it and no pawn capture is possible either way. An
        //enemy pawn on either square diagonally in front could capture it or be captured by it, so checking those
        //covers both.
        private static bool IsFixedPawn(ref Board theBoard, int pawnSquare)
        {
            int side = theBoard.Color[pawnSquare];
            int enemy = side == WHITE ? BLACK : WHITE;
            int forward = side == WHITE ? -8 : 8;
            int aheadSquare = pawnSquare + forward;
            int file = pawnSquare % 8;

            if (aheadSquare < 0 || aheadSquare > 63 || theBoard.Color[aheadSquare] == EMPTY ||
                theBoard.Piece[aheadSquare] != PAWN)
            {
                return false;
            }
            if (file > 0 && theBoard.Color[aheadSquare - 1] == enemy && theBoard.Piece[aheadSquare - 1] == PAWN)
            {
                return false;
            }
            if (file < 7 && theBoard.Color[aheadSquare + 1] == enemy && theBoard.Piece[aheadSquare + 1] == PAWN)
            {
                return false;
            }
            return true;
        }


        //A fixed pawn (blocked by a pawn, no capture available) on the colour of an enemy bishop is a permanent target
        //when its owner has no bishop of that colour to defend it
        private static void PenaliseFixedPawnsOnEnemyBishopColour(ref Board theBoard, ref int whiteScore,
            ref int blackScore)
        {
            bool whiteLightAtRisk = theBoard.BlackHasLightSquaredBishop && !theBoard.WhiteHasLightSquaredBishop;
            bool whiteDarkAtRisk = theBoard.BlackHasDarkSquaredBishop && !theBoard.WhiteHasDarkSquaredBishop;
            bool blackLightAtRisk = theBoard.WhiteHasLightSquaredBishop && !theBoard.BlackHasLightSquaredBishop;
            bool blackDarkAtRisk = theBoard.WhiteHasDarkSquaredBishop && !theBoard.BlackHasDarkSquaredBishop;

            if (whiteLightAtRisk || whiteDarkAtRisk)
            {
                whiteScore -= FixedPawnsAtRisk(ref theBoard, theBoard.WhitePawnSquares, whiteLightAtRisk,
                    whiteDarkAtRisk) * FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY;
            }
            if (blackLightAtRisk || blackDarkAtRisk)
            {
                blackScore -= FixedPawnsAtRisk(ref theBoard, theBoard.BlackPawnSquares, blackLightAtRisk,
                    blackDarkAtRisk) * FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY;
            }
        }


        //A side's fixed pawns on light squares (if lightAtRisk) and on dark squares (if darkAtRisk). A colour is at
        //risk when the enemy has a bishop on it and the side has none
        private static int FixedPawnsAtRisk(ref Board theBoard, int[] pawnSquares, bool lightAtRisk, bool darkAtRisk)
        {
            int count = 0;
            for (int pp = 0; pp <= 7; pp++)
            {
                int pawn = pawnSquares[pp];
                if (pawn == -1)
                {
                    continue;
                }
                bool atRisk = theBoard.SquareColor[pawn] == LIGHTSQUARE ? lightAtRisk : darkAtRisk;
                if (atRisk && IsFixedPawn(ref theBoard, pawn))
                {
                    count += 1;
                }
            }
            return count;
        }


        //How much of the passed-pawn bonus blockaded passers lose: a passer (the front pawn on its file) with an enemy
        //piece on the square in front keeps PASSED_PAWN_BLOCKADED_KEEP_PERCENT of its bonus. The connected bonus is
        //untouched. The passers are the pawn hash's passed pawn masks
        private static void ReduceBlockadedPassedPawns(ref Board theBoard, ulong whitePassedPawns,
            ulong blackPassedPawns, out int whiteReduction, out int blackReduction)
        {
            whiteReduction = 0;
            blackReduction = 0;

            ulong remaining = whitePassedPawns;
            while (remaining != 0)
            {
                int whitePawn = BitOperations.TrailingZeroCount(remaining);
                remaining &= remaining - 1;
                int ahead = whitePawn - 8;
                if (theBoard.Color[ahead] == BLACK && theBoard.Piece[ahead] != PAWN)
                {
                    whiteReduction += PassedPawnBonus(whitePawn,
                        WHITE) * (100 - PASSED_PAWN_BLOCKADED_KEEP_PERCENT) / 100;
                }
            }

            remaining = blackPassedPawns;
            while (remaining != 0)
            {
                int blackPawn = BitOperations.TrailingZeroCount(remaining);
                remaining &= remaining - 1;
                int ahead = blackPawn + 8;
                if (theBoard.Color[ahead] == WHITE && theBoard.Piece[ahead] != PAWN)
                {
                    blackReduction += PassedPawnBonus(blackPawn,
                        BLACK) * (100 - PASSED_PAWN_BLOCKADED_KEEP_PERCENT) / 100;
                }
            }
        }


        //Endgame terms for each passed pawn that depend on kings and pieces rather than pawns alone: how close each
        //king is to the square in front of it, whether its road to promotion is empty and unattacked, and whether a
        //rook stands behind it on its file.
        //The passers come from the pawn hash (see FindPassedPawns), the attacked squares from AttackedSquares
        private static void ScorePassedPawnPromotionChances(ref Board theBoard, ulong attackedByWhite,
            ulong attackedByBlack, ulong whitePassedPawns, ulong blackPassedPawns, out int whiteScore,
            out int blackScore)
        {
            whiteScore = 0;
            blackScore = 0;

            ulong remaining = whitePassedPawns;
            while (remaining != 0)
            {
                int pawnSquare = BitOperations.TrailingZeroCount(remaining);
                remaining &= remaining - 1;
                whiteScore += PromotionChancesForPasser(ref theBoard, pawnSquare, WHITE, attackedByBlack);
            }

            remaining = blackPassedPawns;
            while (remaining != 0)
            {
                int pawnSquare = BitOperations.TrailingZeroCount(remaining);
                remaining &= remaining - 1;
                blackScore += PromotionChancesForPasser(ref theBoard, pawnSquare, BLACK, attackedByWhite);
            }
        }


        //One bit per square holding a front passed pawn (no own pawn further up its file, no enemy pawn ahead on its
        //own or an adjacent file). This depends on pawns alone, so ScorePosition works it out on a pawn hash miss and
        //stores it there
        private static void FindPassedPawns(ref Board theBoard, out ulong whitePassedPawns, out ulong blackPassedPawns)
        {
            whitePassedPawns = 0;
            blackPassedPawns = 0;

            for (int pp = 0; pp <= 7; pp++)
            {
                int whitePawn = theBoard.WhitePawnSquares[pp];
                if (whitePawn >= 8 && IsFrontPassedPawn(ref theBoard, whitePawn, WHITE))
                {
                    whitePassedPawns |= 1UL << whitePawn;
                }

                int blackPawn = theBoard.BlackPawnSquares[pp];
                if (blackPawn != -1 && blackPawn <= 55 && IsFrontPassedPawn(ref theBoard, blackPawn, BLACK))
                {
                    blackPassedPawns |= 1UL << blackPawn;
                }
            }
        }


        //King distance and free path scale with the pawn's rank: nothing on its 2nd rank, 1 on its 3rd, up to 5 on its
        //7th.
        //The rook terms are flat
        private static int PromotionChancesForPasser(ref Board theBoard, int pawnSquare, int side,
            ulong attackedByEnemy)
        {
            int step = side == WHITE ? -8 : 8;
            int relativeRank = side == WHITE ? 8 - pawnSquare / 8 : pawnSquare / 8 + 1;
            int rankFactor = relativeRank - 2;
            int front = pawnSquare + step;

            int ownKing = side == WHITE ? theBoard.WhiteKingSquare : theBoard.BlackKingSquare;
            int enemyKing = side == WHITE ? theBoard.BlackKingSquare : theBoard.WhiteKingSquare;
            int score = (PASSED_PAWN_ENEMY_KING_DISTANCE_BONUS * KingDistance(enemyKing, front)
                - PASSED_PAWN_OWN_KING_DISTANCE_PENALTY * KingDistance(ownKing, front)) * rankFactor;

            bool pathIsFree = true;
            for (int sq = front; sq >= 0 && sq <= 63; sq += step)
            {
                if (theBoard.Color[sq] != EMPTY || ((attackedByEnemy >> sq) & 1UL) != 0)
                {
                    pathIsFree = false;
                    break;
                }
            }
            if (pathIsFree)
            {
                score += PASSED_PAWN_FREE_PATH_BONUS * rankFactor;
            }
            else if (theBoard.Color[front] == EMPTY && ((attackedByEnemy >> front) & 1UL) == 0)
            {
                score += PASSED_PAWN_SAFE_STEP_BONUS * rankFactor;
            }

            for (int sq = pawnSquare - step; sq >= 0 && sq <= 63; sq -= step)
            {
                if (theBoard.Color[sq] != EMPTY)
                {
                    if (theBoard.Piece[sq] == ROOK)
                    {
                        score += theBoard.Color[sq] == side ? PASSED_PAWN_ROOK_BEHIND_BONUS
                            : -PASSED_PAWN_ENEMY_ROOK_BEHIND_PENALTY;
                    }
                    break;
                }
            }

            return score;
        }


        //Squares attacked by one side, one bit per square (bit 0 is a8): its pieces (a line stops at the first piece
        //it meets), its pawns (its PawnAttackMask) and its king
        private static ulong AttackedSquares(in PieceAttackMasks pieces, ulong pawnAttacks, int kingSquare)
        {
            ulong attacked = pieces.Knight | pieces.Bishop | pieces.Rook | pieces.Queen | pawnAttacks;
            return kingSquare <= 63 ? attacked | _kingNeighbourMasks[kingSquare] : attacked;
        }


        private static ulong[] BuildDiagonalMasks()
        {
            ulong[] masks = new ulong[64];
            for (int square = 0; square <= 63; square++)
            {
                for (int other = 0; other <= 63; other++)
                {
                    if (Math.Abs(square / 8 - other / 8) == Math.Abs(square % 8 - other % 8))
                    {
                        masks[square] |= 1UL << other;
                    }
                }
            }
            return masks;
        }


        //For each square, the squares one king step away from it, or one knight's move away
        private static ulong[] BuildStepMasks(bool knightMoves)
        {
            ulong[] masks = new ulong[64];
            for (int square = 0; square <= 63; square++)
            {
                for (int rowStep = -2; rowStep <= 2; rowStep++)
                {
                    for (int fileStep = -2; fileStep <= 2; fileStep++)
                    {
                        int row = square / 8 + rowStep;
                        int file = square % 8 + fileStep;
                        int furthest = Math.Max(Math.Abs(rowStep), Math.Abs(fileStep));
                        int nearest = Math.Min(Math.Abs(rowStep), Math.Abs(fileStep));
                        bool isStep = knightMoves ? (furthest == 2 && nearest == 1) : furthest == 1;
                        if (isStep && row >= 0 && row <= 7 && file >= 0 && file <= 7)
                        {
                            masks[square] |= 1UL << (row * 8 + file);
                        }
                    }
                }
            }
            return masks;
        }


        private static int KingDistance(int squareA, int squareB)
        {
            return Math.Max(Math.Abs(squareA % 8 - squareB % 8), Math.Abs(squareA / 8 - squareB / 8));
        }


        //The same test RewardPassedPawns uses: no own pawn further up the file, and no enemy pawn in front on this or
        //an adjacent file
        private static bool IsFrontPassedPawn(ref Board theBoard, int pawnSquare, int side)
        {
            int step = side == WHITE ? -8 : 8;
            for (int sq = pawnSquare + step; sq >= 0 && sq <= 63; sq += step)
            {
                if (theBoard.Color[sq] == side && theBoard.Piece[sq] == PAWN)
                {
                    return false;
                }
            }

            int enemy = side == WHITE ? BLACK : WHITE;
            int[] lookups = side == WHITE ? theBoard.WhitePassedPawnLookUps[pawnSquare]
                : theBoard.BlackPassedPawnLookUps[pawnSquare];
            for (int nn = 0; nn < lookups.Length; nn++)
            {
                if (theBoard.Color[lookups[nn]] == enemy && theBoard.Piece[lookups[nn]] == PAWN)
                {
                    return false;
                }
            }
            return true;
        }


        //The bonus RewardPassedPawns gives a passer on this square, excluding the connected bonus
        private static int PassedPawnBonus(int pawnSquare, int side)
        {
            int relativeSquare = side == WHITE ? pawnSquare : 63 - pawnSquare;
            if (relativeSquare < 16)
            {
                return PASSED_PAWN_BONUS + PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS;
            }
            if (relativeSquare < 24)
            {
                return PASSED_PAWN_BONUS + PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS;
            }
            if (relativeSquare < 32)
            {
                return PASSED_PAWN_BONUS + PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS;
            }
            return PASSED_PAWN_BONUS;
        }


        //Shelter for a king on its first or second rank, over its own file and the files either side (files off
        //the board are skipped). A pawn on its second or third rank shelters, so the same pawns score the same
        //whether the king is on g1 or g2. A file with none of our pawns is extra bad while the enemy has a rook
        //or queen to use it, and worse again if it is fully open.
        private static int KingShelterScore(ref Board theBoard, int kingFile, int side)
        {
            int score = 0;
            int nearRow = side == WHITE ? 6 : 1;
            int farRow = side == WHITE ? 5 : 2;
            int[] ownFilePawns = side == WHITE ? theBoard.WhiteFilePawns : theBoard.BlackFilePawns;
            int[] enemyFilePawns = side == WHITE ? theBoard.BlackFilePawns : theBoard.WhiteFilePawns;
            bool enemyHasHeavyPiece = side == WHITE
                ? (theBoard.BlackQueenSquare != 255 || theBoard.BlackRookOneSquare != 255 ||
                    theBoard.BlackRookTwoSquare != 255)
                : (theBoard.WhiteQueenSquare != 255 || theBoard.WhiteRookOneSquare != 255 ||
                    theBoard.WhiteRookTwoSquare != 255);
            //A king on the d or e file has its own shelter weights, as it usually has not castled yet
            bool kingInCentre = kingFile == 3 || kingFile == 4;

            for (int file = kingFile - 1; file <= kingFile + 1; file++)
            {
                if (file < 0 || file > 7)
                {
                    continue;
                }

                int nearSquare = nearRow * 8 + file;
                int farSquare = farRow * 8 + file;
                bool hasShelterPawn = (theBoard.Color[nearSquare] == side && theBoard.Piece[nearSquare] == PAWN)
                    || (theBoard.Color[farSquare] == side && theBoard.Piece[farSquare] == PAWN);

                if (file == kingFile)
                {
                    if (kingInCentre)
                    {
                        score += hasShelterPawn ? CENTRAL_KING_FILE_SHELTER_PAWN_BONUS
                            : -CENTRAL_KING_FILE_NO_SHELTER_PAWN_PENALTY;
                    }
                    else
                    {
                        score += hasShelterPawn ? KING_FILE_SHELTER_PAWN_BONUS : -KING_FILE_NO_SHELTER_PAWN_PENALTY;
                    }
                }
                else
                {
                    if (kingInCentre)
                    {
                        score += hasShelterPawn ? CENTRAL_SIDE_FILE_SHELTER_PAWN_BONUS
                            : -CENTRAL_SIDE_FILE_NO_SHELTER_PAWN_PENALTY;
                    }
                    else
                    {
                        score += hasShelterPawn ? SIDE_FILE_SHELTER_PAWN_BONUS : -SIDE_FILE_NO_SHELTER_PAWN_PENALTY;
                    }
                }

                if (ownFilePawns[file] == 0 && enemyHasHeavyPiece)
                {
                    score -= KING_NEAR_HALF_OPEN_FILE_PENALTY;
                    if (enemyFilePawns[file] == 0)
                    {
                        score -= KING_NEAR_OPEN_FILE_ADDITIONAL_PENALTY;
                    }
                }
            }

            return score;
        }


        //The a, b, g and h files: a king there on its second rank is tucked away on the wing, not walking up the centre
        private static bool IsWingFile(int file)
        {
            return file <= 1 || file >= 6;
        }


        private static void RewardBeingCastled(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            //A side's only king, if it still has one, stands on its king square, so that is the only square tested
            int whiteKing = theBoard.WhiteKingSquare;
            if (whiteKing <= 63 && theBoard.Color[whiteKing] == WHITE && theBoard.Piece[whiteKing] == KING)
            {
                if ((whiteKing == 62 && theBoard.Color[63] == EMPTY) || whiteKing == 63 || whiteKing == 54 ||
                    whiteKing == 55)
                {
                    whiteScore += KSIDE_CASTLE_BONUS;
                }
                if ((whiteKing == 58 && theBoard.Color[56] == EMPTY && theBoard.Color[57] == EMPTY) ||
                    (whiteKing == 57 && theBoard.Color[56] == EMPTY) || whiteKing == 56 || whiteKing == 49 ||
                    whiteKing == 48)
                {
                    whiteScore += QSIDE_CASTLE_BONUS;
                }
            }

            int blackKing = theBoard.BlackKingSquare;
            if (blackKing <= 63 && theBoard.Color[blackKing] == BLACK && theBoard.Piece[blackKing] == KING)
            {
                if ((blackKing == 6 && theBoard.Color[7] == EMPTY) || blackKing == 7 || blackKing == 14 ||
                    blackKing == 15)
                {
                    blackScore += KSIDE_CASTLE_BONUS;
                }
                if ((blackKing == 2 && theBoard.Color[1] == EMPTY && theBoard.Color[0] == EMPTY) ||
                    (blackKing == 1 && theBoard.Color[0] == EMPTY) || blackKing == 0 || blackKing == 9 ||
                    blackKing == 8)
                {
                    blackScore += QSIDE_CASTLE_BONUS;
                }
            }

            if (theBoard.Color[56] == WHITE && theBoard.Piece[56] == ROOK)
            {
                if (theBoard.WhiteKingSquare == 60)
                {
                    whiteScore += QSIDE_CASTLE_RIGHTS;
                }
            }

            if (theBoard.Color[63] == WHITE && theBoard.Piece[63] == ROOK)
            {
                if (theBoard.WhiteKingSquare == 60)
                {
                    whiteScore += KSIDE_CASTLE_RIGHTS;
                }
            }

            if (theBoard.Color[0] == BLACK && theBoard.Piece[0] == ROOK)
            {
                if (theBoard.BlackKingSquare == 4)
                {
                    blackScore += QSIDE_CASTLE_RIGHTS;
                }
            }

            if (theBoard.Color[7] == BLACK && theBoard.Piece[7] == ROOK)
            {
                if (theBoard.BlackKingSquare == 4)
                {
                    blackScore += KSIDE_CASTLE_RIGHTS;
                }
            }

        }

        //Out of 64, how much of its endgame advantage the stronger side keeps. It is below 64 only in endgames that
        //are hard to win whatever the material says: no pawns and at most a minor piece ahead, opposite coloured
        //bishops, or the stronger side down to its last few pawns. The starting numbers follow classical Stockfish
        private static int EndgameScaleFactor(ref Board theBoard, bool whiteIsStronger, ulong whitePassedPawns,
            ulong blackPassedPawns)
        {
            int whitePawns = theBoard.WhitePawnsOnLightSquares + theBoard.WhitePawnsOnDarkSquares;
            int blackPawns = theBoard.BlackPawnsOnLightSquares + theBoard.BlackPawnsOnDarkSquares;
            int whitePieceMaterial = theBoard.WhiteMaterial - whitePawns * MATERIAL[PAWN];
            int blackPieceMaterial = theBoard.BlackMaterial - blackPawns * MATERIAL[PAWN];

            int strongPawns = whiteIsStronger ? whitePawns : blackPawns;
            int strongPieceMaterial = whiteIsStronger ? whitePieceMaterial : blackPieceMaterial;
            int weakPieceMaterial = whiteIsStronger ? blackPieceMaterial : whitePieceMaterial;

            //Without pawns, being no more than a minor piece ahead does not win
            if (strongPawns == 0 && strongPieceMaterial - weakPieceMaterial <= KNIGHT_VALUE)
            {
                if (strongPieceMaterial < ROOK_VALUE)
                {
                    return 0;
                }
                return weakPieceMaterial <= KNIGHT_VALUE ? 4 : 14;
            }

            //Each side has exactly one bishop, and they are on different colours
            bool oppositeColouredBishops =
                theBoard.WhiteHasDarkSquaredBishop != theBoard.WhiteHasLightSquaredBishop &&
                theBoard.BlackHasDarkSquaredBishop != theBoard.BlackHasLightSquaredBishop &&
                theBoard.WhiteHasDarkSquaredBishop != theBoard.BlackHasDarkSquaredBishop;
            if (oppositeColouredBishops)
            {
                if (whitePieceMaterial == BISHOP_VALUE && blackPieceMaterial == BISHOP_VALUE)
                {
                    ulong strongPassedPawns = whiteIsStronger ? whitePassedPawns : blackPassedPawns;
                    return Math.Min(64, OCB_SCALE_BASE +
                        OCB_SCALE_PER_PASSED_PAWN * System.Numerics.BitOperations.PopCount(strongPassedPawns));
                }
                return Math.Min(64, OCB_WITH_PIECES_SCALE_BASE +
                    OCB_WITH_PIECES_SCALE_PER_PIECE * PieceCountWithPawnsAndKing(ref theBoard, whiteIsStronger));
            }

            return Math.Min(64, ENDGAME_SCALE_FEW_PAWNS_BASE + ENDGAME_SCALE_PER_PAWN * strongPawns);
        }


        private static int PieceCountWithPawnsAndKing(ref Board theBoard, bool white)
        {
            if (white)
            {
                return 1 + theBoard.WhitePawnsOnLightSquares + theBoard.WhitePawnsOnDarkSquares +
                    (theBoard.WhiteKnightOneSquare != 255 ? 1 : 0) + (theBoard.WhiteKnightTwoSquare != 255 ? 1 : 0) +
                    (theBoard.WhiteHasDarkSquaredBishop ? 1 : 0) + (theBoard.WhiteHasLightSquaredBishop ? 1 : 0) +
                    (theBoard.WhiteRookOneSquare != 255 ? 1 : 0) + (theBoard.WhiteRookTwoSquare != 255 ? 1 : 0) +
                    (theBoard.WhiteQueenSquare != 255 ? 1 : 0);
            }
            return 1 + theBoard.BlackPawnsOnLightSquares + theBoard.BlackPawnsOnDarkSquares +
                (theBoard.BlackKnightOneSquare != 255 ? 1 : 0) + (theBoard.BlackKnightTwoSquare != 255 ? 1 : 0) +
                (theBoard.BlackHasDarkSquaredBishop ? 1 : 0) + (theBoard.BlackHasLightSquaredBishop ? 1 : 0) +
                (theBoard.BlackRookOneSquare != 255 ? 1 : 0) + (theBoard.BlackRookTwoSquare != 255 ? 1 : 0) +
                (theBoard.BlackQueenSquare != 255 ? 1 : 0);
        }


        //Returns 1 if White is the exchange up (one more rook and one fewer minor piece), -1 if Black is, otherwise 0
        private static int ExchangeImbalance(ref Board theBoard)
        {
            int whiteRooks = (theBoard.WhiteRookOneSquare != 255 ? 1 : 0) +
                (theBoard.WhiteRookTwoSquare != 255 ? 1 : 0);
            int blackRooks = (theBoard.BlackRookOneSquare != 255 ? 1 : 0) +
                (theBoard.BlackRookTwoSquare != 255 ? 1 : 0);
            int whiteKnights = (theBoard.WhiteKnightOneSquare != 255 ? 1 : 0) +
                (theBoard.WhiteKnightTwoSquare != 255 ? 1 : 0);
            int blackKnights = (theBoard.BlackKnightOneSquare != 255 ? 1 : 0) +
                (theBoard.BlackKnightTwoSquare != 255 ? 1 : 0);
            int whiteBishops = (theBoard.WhiteHasDarkSquaredBishop ? 1 : 0) +
                (theBoard.WhiteHasLightSquaredBishop ? 1 : 0);
            int blackBishops = (theBoard.BlackHasDarkSquaredBishop ? 1 : 0) +
                (theBoard.BlackHasLightSquaredBishop ? 1 : 0);
            int whiteMinors = whiteKnights + whiteBishops;
            int blackMinors = blackKnights + blackBishops;
            int whitePawns = theBoard.WhitePawnsOnLightSquares + theBoard.WhitePawnsOnDarkSquares;
            int blackPawns = theBoard.BlackPawnsOnLightSquares + theBoard.BlackPawnsOnDarkSquares;

            if (whiteRooks - blackRooks == 1 && whiteMinors - blackMinors == -1 && blackPawns - whitePawns < 2)
            {
                return 1;
            }
            if (blackRooks - whiteRooks == 1 && blackMinors - whiteMinors == -1 && whitePawns - blackPawns < 2)
            {
                return -1;
            }
            return 0;
        }


    }
}
