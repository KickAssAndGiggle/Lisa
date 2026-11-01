using static Lisa.Globals;
namespace Lisa
{
    public static partial class Scorer
    {

        public static int ScorePosition(ref Board theBoard, ref TranspositionTable tt, int alpha, int beta, bool allowLazyEval, int lastMoveCapMaterial)
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
                        if (lazy + (LAZY_EVAL_QUEENS_OFF_MARGIN + lastMoveCapMaterial) < alpha || lazy - (LAZY_EVAL_QUEENS_OFF_MARGIN + lastMoveCapMaterial) > beta)
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
                        if (lazy + (LAZY_EVAL_QUEENS_ON_MARGIN + lastMoveCapMaterial) < alpha || lazy - (LAZY_EVAL_QUEENS_ON_MARGIN + lastMoveCapMaterial) > beta)
                        {
                            return lazy;
                        }
                    }
                }

            }

            //Generated once and shared by mobility, king attack and king defence
            Move[] whitePieceAttacks = theBoard.GeneratePieceAttacksWithoutKing(WHITE);
            Move[] blackPieceAttacks = theBoard.GeneratePieceAttacksWithoutKing(BLACK);

            //Early Game
            RewardBeingCastled(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardKingSafety(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            PenaliseKingAttack(ref theBoard, whitePieceAttacks, blackPieceAttacks, ref whiteOpeningScore, ref blackOpeningScore);
            RewardActiveBishop(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            //EncouragePawnsOnOppositeSideToKingToAdvance(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardPawnBishopSynergy(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardOpeningMobility(ref theBoard, whitePieceAttacks, blackPieceAttacks, ref whiteOpeningScore, ref blackOpeningScore);
            RewardKnightOutpost(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            PenaliseTrappedRook(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);

            //Late Game          
            RewardRooksOnOpenFiles(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            RewardRooksOnTheSeventh(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            RewardBlockadingDangerousPawns(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            PenaliseFixedPawnsOnEnemyBishopColour(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);

            bool pawnsInTT = tt.LookupPPScore(theBoard.PawnOnlyZobrist, out TTPawnAnalysis pawnLookup);
            if (pawnsInTT)
            {

                whiteOpeningScore += pawnLookup.WhiteBackwardsPawnScore;
                blackOpeningScore += pawnLookup.BlackBackwardPawnScore;
                whiteOpeningScore += pawnLookup.WhitePawnChainScore;
                blackOpeningScore += pawnLookup.BlackPawnChainScore;

                whiteEndgameScore += pawnLookup.WhitePassedPawnScore;
                blackEndgameScore += pawnLookup.BlackPassedPawnScore;
                whiteOpeningScore += pawnLookup.WhitePassedPawnScore / 2;
                blackOpeningScore += pawnLookup.BlackPassedPawnScore / 2;
                whiteEndgameScore += pawnLookup.WhiteDoubledPawnScore;
                blackEndgameScore += pawnLookup.BlackDoubledPawnScore;
                whiteEndgameScore += pawnLookup.WhiteIsolatedPawnScore;
                blackEndgameScore += pawnLookup.BlackIsolatedPawnScore;

            }
            else
            {

                PenaliseBackwardsPawns(ref theBoard, out int whiteBWPScore, out int blackBWPScore);
                RewardPawnChains(ref theBoard, out int whiteChainScore, out int blackChainScore);

                RewardPassedPawns(ref theBoard, out int whitePPScore, out int blackPPScore);
                PenaliseDoubledAndTripledPawns(ref theBoard, out int whiteDblPawnScore, out int blackDblPawnScore);
                PenaliseIsolatedPawns(ref theBoard, out int whiteIsoPawnScore, out int blackIsoPawnScore);

                tt.AddPawnStructureToTransTable(theBoard.PawnOnlyZobrist, whitePPScore, blackPPScore, whiteBWPScore,
                    blackBWPScore, whiteChainScore, blackChainScore, whiteDblPawnScore, blackDblPawnScore, 
                    whiteIsoPawnScore, blackIsoPawnScore);

                whiteOpeningScore += whiteBWPScore;
                whiteOpeningScore += whiteChainScore;
                whiteEndgameScore += whitePPScore;
                whiteOpeningScore += whitePPScore / 2;
                whiteEndgameScore += whiteDblPawnScore;
                whiteEndgameScore += whiteIsoPawnScore;

                blackOpeningScore += blackBWPScore;
                blackOpeningScore += blackChainScore;
                blackEndgameScore += blackPPScore;
                blackOpeningScore += blackPPScore / 2;
                blackEndgameScore += blackDblPawnScore;
                blackEndgameScore += blackIsoPawnScore;

            }

            //Blockades depend on pieces, which the pawn hash doesn't key on, so this is worked out on every evaluation
            ReduceBlockadedPassedPawns(ref theBoard, out int whiteBlockadeReduction, out int blackBlockadeReduction);
            whiteEndgameScore -= whiteBlockadeReduction;
            whiteOpeningScore -= whiteBlockadeReduction / 2;
            blackEndgameScore -= blackBlockadeReduction;
            blackOpeningScore -= blackBlockadeReduction / 2;

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

            //The exchange (one more rook, one fewer minor) is worth more than piece values plus positional terms suggest,
            //unless the side with the minor has two or more extra pawns for it
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

            if (theBoard.WhiteRookOneSquare != 255 || theBoard.WhiteRookTwoSquare != 255)
            {
                for (int NN = 0; NN <= 7; NN++)
                {
                    if (theBoard.WhiteFilePawns[NN] == 0)
                    {
                        if (theBoard.WhiteRookOneSquare != 255 && theBoard.WhiteRookTwoSquare != 255)
                        {
                            whiteScore += SEMI_OPEN_FILE_TWO_ROOKS_BONUS;
                        }
                        else
                        {
                            whiteScore += SEMI_OPEN_FILE_ONE_ROOK_BONUS;
                        }
                    }
                }
            }

            if (theBoard.BlackRookOneSquare != 255 || theBoard.BlackRookTwoSquare != 255)
            {
                for (int NN = 0; NN <= 7; NN++)
                {
                    if (theBoard.BlackFilePawns[NN] == 0)
                    {
                        if (theBoard.BlackRookOneSquare != 255 && theBoard.BlackRookTwoSquare != 255)
                        {
                            blackScore += SEMI_OPEN_FILE_TWO_ROOKS_BONUS;
                        }
                        else
                        {
                            blackScore += SEMI_OPEN_FILE_ONE_ROOK_BONUS;
                        }
                    }
                }
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


        public static void PenaliseTrappedRook(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.BlackKingSquare <= 3)
            {
                if (theBoard.Piece[0] == ROOK && theBoard.Color[0] == BLACK)
                {
                    if ((theBoard.Piece[8] == PAWN && theBoard.Color[8] == BLACK) || (theBoard.Piece[16] == PAWN && theBoard.Color[16] == BLACK))
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
                    if ((theBoard.Piece[15] == PAWN && theBoard.Color[15] == BLACK) || (theBoard.Piece[23] == PAWN && theBoard.Color[23] == BLACK))
                    {
                        blackScore -= TRAPPED_ROOK_PENALTY;
                    }
                }
            }

            if (theBoard.WhiteKingSquare >= 57 && theBoard.WhiteKingSquare <= 59)
            {
                if (theBoard.Piece[56] == ROOK && theBoard.Color[56] == WHITE)
                {
                    if ((theBoard.Piece[48] == PAWN && theBoard.Color[48] == WHITE) || (theBoard.Piece[40] == PAWN && theBoard.Color[40] == WHITE))
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
                    if ((theBoard.Piece[55] == PAWN && theBoard.Color[55] == WHITE) || (theBoard.Piece[47] == PAWN && theBoard.Color[47] == WHITE))
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
                    if ((theBoard.Piece[NN + 8] == BISHOP || theBoard.Piece[NN + 8] == KNIGHT) && theBoard.Color[NN + 8] == WHITE)
                    {
                        whiteScore += BLOCKADING_PASSED_PAWN_BONUS;
                    }
                }
            }

            for (int NN = 24; NN <= 31; NN++)
            {
                if (theBoard.Piece[NN] == PAWN && theBoard.Color[NN] == WHITE)
                {
                    if ((theBoard.Piece[NN - 8] == BISHOP || theBoard.Piece[NN - 8] == KNIGHT) && theBoard.Color[NN - 8] == BLACK)
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
                        if (theBoard.WhitePawnSquares[PP] % 8 != 0)
                        {
                            if (theBoard.Color[theBoard.WhitePawnSquares[PP] + 7] == WHITE && theBoard.Piece[theBoard.WhitePawnSquares[PP] + 7] == PAWN)
                            {
                                WhiteChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                        if (theBoard.WhitePawnSquares[PP] % 8 != 7)
                        {
                            if (theBoard.Color[theBoard.WhitePawnSquares[PP] + 9] == WHITE && theBoard.Piece[theBoard.WhitePawnSquares[PP] + 9] == PAWN)
                            {
                                WhiteChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                    }
                }
                if (theBoard.BlackPawnSquares[PP] != -1)
                {
                    if (theBoard.BlackPawnSquares[PP] > 15)
                    {
                        if (theBoard.BlackPawnSquares[PP] % 8 != 0)
                        {
                            if (theBoard.Color[theBoard.BlackPawnSquares[PP] - 9] == BLACK && theBoard.Piece[theBoard.BlackPawnSquares[PP] - 9] == PAWN)
                            {
                                BlackChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                        if (theBoard.BlackPawnSquares[PP] % 8 != 7)
                        {
                            if (theBoard.Color[theBoard.BlackPawnSquares[PP] - 7] == BLACK && theBoard.Piece[theBoard.BlackPawnSquares[PP] - 7] == PAWN)
                            {
                                BlackChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                    }
                }
            }

        }


        private static bool IsMobilityMove(ref Board theBoard, int toSquare, int side)
        {
            return theBoard.Color[toSquare] == EMPTY || (theBoard.Color[toSquare] != side && theBoard.Piece[toSquare] != KING);
        }


        public static void RewardOpeningMobility(ref Board theBoard, Move[] WhiteMoves, Move[] BlackMoves, ref int whiteScore, ref int blackScore)
        {

            //The attack lists include squares holding our own pieces and the enemy king, which aren't moves, so skip those.
            //Bishops score less per move than other pieces: being long-range, they collect more moves per piece
            for (int nn = 0; nn < WhiteMoves.Length; nn++)
            {
                if (!IsMobilityMove(ref theBoard, WhiteMoves[nn].To, WHITE))
                {
                    continue;
                }
                if (theBoard.Piece[WhiteMoves[nn].From] == BISHOP)
                {
                    whiteScore += OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS;
                }
                else
                {
                    whiteScore += OPENING_MOBILITY_PER_MOVE_BONUS;
                }
            }
            for (int nn = 0; nn < BlackMoves.Length; nn++)
            {
                if (!IsMobilityMove(ref theBoard, BlackMoves[nn].To, BLACK))
                {
                    continue;
                }
                if (theBoard.Piece[BlackMoves[nn].From] == BISHOP)
                {
                    blackScore += OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS;
                }
                else
                {
                    blackScore += OPENING_MOBILITY_PER_MOVE_BONUS;
                }
            }

            if (theBoard.WhiteKnightOneSquare != 255)
            {
                if (theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][35])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][36])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][27])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][28])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (theBoard.WhiteKnightOneSquare % 8 == 0)
                {
                    whiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (theBoard.WhiteKnightOneSquare % 8 == 7)
                {
                    whiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }


            }

            if (theBoard.WhiteKnightTwoSquare != 255)
            {
                if (theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][35])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][36])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][27])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][28])
                {
                    whiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (theBoard.WhiteKnightTwoSquare % 8 == 0)
                {
                    whiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (theBoard.WhiteKnightTwoSquare % 8 == 7)
                {
                    whiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }

            }

            if (theBoard.BlackKnightOneSquare != 255)
            {
                if (theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][35])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][36])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][27])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][28])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (theBoard.BlackKnightOneSquare % 8 == 0)
                {
                    blackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (theBoard.BlackKnightOneSquare % 8 == 7)
                {
                    blackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }

            }

            if (theBoard.BlackKnightTwoSquare != 255)
            {
                if (theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][35])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][36])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][27])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][28])
                {
                    blackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (theBoard.BlackKnightTwoSquare % 8 == 0)
                {
                    blackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (theBoard.BlackKnightTwoSquare % 8 == 7)
                {
                    blackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }

            }

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


        //private static void EncouragePawnsOnOppositeSideToKingToAdvance(ref Board theBoard, ref int whiteScore, ref int blackScore)
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
                if (theBoard.BlackQueenSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteDarkBishopSquare][theBoard.BlackQueenSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (theBoard.BlackRookOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteDarkBishopSquare][theBoard.BlackRookOneSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.BlackRookTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteDarkBishopSquare][theBoard.BlackRookTwoSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.BlackKnightOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteDarkBishopSquare][theBoard.BlackKnightOneSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (theBoard.BlackKnightTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteDarkBishopSquare][theBoard.BlackKnightTwoSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }

            if (theBoard.WhiteHasLightSquaredBishop)
            {
                if (theBoard.BlackQueenSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteLightBishopSquare][theBoard.BlackQueenSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (theBoard.BlackRookOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteLightBishopSquare][theBoard.BlackRookOneSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.BlackRookTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteLightBishopSquare][theBoard.BlackRookTwoSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.BlackKnightOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteLightBishopSquare][theBoard.BlackKnightOneSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (theBoard.BlackKnightTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.WhiteLightBishopSquare][theBoard.BlackKnightTwoSquare])
                    {
                        whiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }



            if (theBoard.BlackHasDarkSquaredBishop)
            {
                if (theBoard.WhiteQueenSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackDarkBishopSquare][theBoard.WhiteQueenSquare])
                    {
                        blackScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (theBoard.WhiteRookOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackDarkBishopSquare][theBoard.WhiteRookOneSquare])
                    {
                        blackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.WhiteRookTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackDarkBishopSquare][theBoard.WhiteRookTwoSquare])
                    {
                        blackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.WhiteKnightOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackDarkBishopSquare][theBoard.WhiteKnightOneSquare])
                    {
                        blackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (theBoard.WhiteKnightTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackDarkBishopSquare][theBoard.WhiteKnightTwoSquare])
                    {
                        blackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }

            if (theBoard.BlackHasLightSquaredBishop)
            {
                if (theBoard.WhiteQueenSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackLightBishopSquare][theBoard.WhiteQueenSquare])
                    {
                        blackScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (theBoard.WhiteRookOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackLightBishopSquare][theBoard.WhiteRookOneSquare])
                    {
                        blackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.WhiteRookTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackLightBishopSquare][theBoard.WhiteRookTwoSquare])
                    {
                        blackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (theBoard.WhiteKnightOneSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackLightBishopSquare][theBoard.WhiteKnightOneSquare])
                    {
                        blackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (theBoard.WhiteKnightTwoSquare != 255)
                {
                    if (theBoard.SameDiagonal[theBoard.BlackLightBishopSquare][theBoard.WhiteKnightTwoSquare])
                    {
                        blackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }



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
                        if (theBoard.Piece[theBoard.WhitePassedPawnLookUps[pawnSquare][sq]] == PAWN && theBoard.Color[theBoard.WhitePassedPawnLookUps[pawnSquare][sq]] == BLACK)
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
                        if (theBoard.Piece[theBoard.BlackPassedPawnLookUps[pawnSquare][sq]] == PAWN && theBoard.Color[theBoard.BlackPassedPawnLookUps[pawnSquare][sq]] == WHITE)
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


        private static void PenaliseIsolatedPawns(ref Board theBoard, out int whiteIsoPawnScore, out int blackIsoPawnScore)
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
                if (theBoard.WhiteFilePawns[NN] > 0 && theBoard.WhiteFilePawns[NN - 1] == 0 && theBoard.WhiteFilePawns[NN + 1] == 0)
                {
                    //more central white pawn(s) isolated
                    whiteIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY * theBoard.WhiteFilePawns[NN];
                }
                if (theBoard.BlackFilePawns[NN] > 0 && theBoard.BlackFilePawns[NN - 1] == 0 && theBoard.BlackFilePawns[NN + 1] == 0)
                {
                    //more central black pawn(s) isolated
                    blackIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY * theBoard.BlackFilePawns[NN];
                }
            }

        }


        private static void PenaliseDoubledAndTripledPawns(ref Board theBoard, out int whiteDblPawnScore, out int blackDblPawnScore)
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
        private static void PenaliseKingAttack(ref Board theBoard, Move[] whitePieceAttacks, Move[] blackPieceAttacks, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteQueenSquare != 255)
            {
                blackScore -= KING_ATTACK_PENALTY[NetKingAttackUnits(ref theBoard, whitePieceAttacks, blackPieceAttacks, theBoard.BlackKingSquare, WHITE)];
            }

            if (theBoard.BlackQueenSquare != 255)
            {
                whiteScore -= KING_ATTACK_PENALTY[NetKingAttackUnits(ref theBoard, blackPieceAttacks, whitePieceAttacks, theBoard.WhiteKingSquare, BLACK)];
            }

        }


        //Attack units less the defenders' units, never below 0. Defence only reduces an attack that passed the
        //two-attacker gate: a well-defended king earns no bonus by itself.
        private static int NetKingAttackUnits(ref Board theBoard, Move[] attackerMoves, Move[] defenderMoves, int kingSquare, int attackingSide)
        {
            int units = KingAttackUnits(ref theBoard, attackerMoves, kingSquare, attackingSide);
            if (units == 0)
            {
                return 0;
            }
            bool[] kingZone = attackingSide == WHITE ? theBoard.BlackKingZone[kingSquare] : theBoard.WhiteKingZone[kingSquare];
            units -= KingDefenceUnits(ref theBoard, defenderMoves, kingZone);
            return units > 0 ? units : 0;
        }


        //Defence units from every square of its own king zone the defending side's pieces bear on, including squares its
        //own pieces stand on, weighted by piece type (knights most, sliders less)
        private static int KingDefenceUnits(ref Board theBoard, Move[] defenderMoves, bool[] kingZone)
        {
            int units = 0;
            for (int nn = 0; nn < defenderMoves.Length; nn++)
            {
                if (kingZone[defenderMoves[nn].To])
                {
                    units += KING_DEFENCE_UNITS[theBoard.Piece[defenderMoves[nn].From]];
                }
            }
            return units;
        }


        //Attack units from every attacked square in the king zone, weighted by the attacking piece's type, plus x-ray
        //pressure from sliders that don't reach the zone yet. Attacks come grouped by From square, so a new From is a new
        //attacker. One attacker alone scores nothing.
        private static int KingAttackUnits(ref Board theBoard, Move[] attackerMoves, int kingSquare, int attackingSide)
        {
            bool[] kingZone = attackingSide == WHITE ? theBoard.BlackKingZone[kingSquare] : theBoard.WhiteKingZone[kingSquare];
            int units = 0;
            int attackers = 0;
            int lastAttackerSquare = -1;
            int squaresForThisAttacker = 0;
            ulong directAttackers = 0;

            for (int nn = 0; nn < attackerMoves.Length; nn++)
            {
                if (kingZone[attackerMoves[nn].To])
                {
                    if (attackerMoves[nn].From != lastAttackerSquare)
                    {
                        attackers++;
                        lastAttackerSquare = attackerMoves[nn].From;
                        directAttackers |= 1UL << lastAttackerSquare;
                        squaresForThisAttacker = 0;
                    }
                    //One piece counts for at most KING_ATTACK_MAX_SQUARES_PER_PIECE zone squares, so a queen alone can't carry an attack
                    if (squaresForThisAttacker < KING_ATTACK_MAX_SQUARES_PER_PIECE)
                    {
                        units += KING_ATTACK_UNITS[theBoard.Piece[attackerMoves[nn].From]];
                        squaresForThisAttacker++;
                    }
                }
            }

            if (attackingSide == WHITE)
            {
                int[] ownFilePawns = theBoard.WhiteFilePawns;
                if (theBoard.WhiteHasLightSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.WhiteLightBishopSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                }
                if (theBoard.WhiteHasDarkSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.WhiteDarkBishopSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                }
                AddXRayAttacker(ref theBoard, theBoard.WhiteRookOneSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.WhiteRookTwoSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.WhiteQueenSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
            }
            else
            {
                int[] ownFilePawns = theBoard.BlackFilePawns;
                if (theBoard.BlackHasLightSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.BlackLightBishopSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                }
                if (theBoard.BlackHasDarkSquaredBishop)
                {
                    AddXRayAttacker(ref theBoard, theBoard.BlackDarkBishopSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                }
                AddXRayAttacker(ref theBoard, theBoard.BlackRookOneSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.BlackRookTwoSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
                AddXRayAttacker(ref theBoard, theBoard.BlackQueenSquare, kingSquare, attackingSide, ownFilePawns, directAttackers, ref units, ref attackers);
            }

            if (attackers < 2)
            {
                return 0;
            }
            return units < KING_ATTACK_PENALTY.Length ? units : KING_ATTACK_PENALTY.Length - 1;
        }


        //Adds a slider aimed at the king without reaching the zone: a bishop or queen on the king's diagonal, or a rook or
        //queen on the king's file or one beside it with none of its own pawns on that file. It counts as an attacker
        //hitting KING_ATTACK_XRAY_SQUARES zone squares. Pieces already attacking the zone directly are skipped.
        private static void AddXRayAttacker(ref Board theBoard, int pieceSquare, int kingSquare, int attackingSide, int[] ownFilePawns, ulong directAttackers, ref int units, ref int attackers)
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
                    aimed = targetSquare != pieceSquare && XRayLineIsOpen(ref theBoard, pieceSquare, targetSquare, attackingSide);
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


        //A pawn is fixed when a pawn stands directly in front of it and no pawn capture is possible either way. An enemy
        //pawn on either square diagonally in front could capture it or be captured by it, so checking those covers both.
        private static bool IsFixedPawn(ref Board theBoard, int pawnSquare)
        {
            int side = theBoard.Color[pawnSquare];
            int enemy = side == WHITE ? BLACK : WHITE;
            int forward = side == WHITE ? -8 : 8;
            int aheadSquare = pawnSquare + forward;
            int file = pawnSquare % 8;

            if (aheadSquare < 0 || aheadSquare > 63 || theBoard.Color[aheadSquare] == EMPTY || theBoard.Piece[aheadSquare] != PAWN)
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
        private static void PenaliseFixedPawnsOnEnemyBishopColour(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            for (int pp = 0; pp <= 7; pp++)
            {
                int whitePawn = theBoard.WhitePawnSquares[pp];
                if (whitePawn != -1 && IsTargetForEnemyBishop(ref theBoard, whitePawn, theBoard.BlackHasLightSquaredBishop, theBoard.BlackHasDarkSquaredBishop,
                    theBoard.WhiteHasLightSquaredBishop, theBoard.WhiteHasDarkSquaredBishop))
                {
                    whiteScore -= FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY;
                }

                int blackPawn = theBoard.BlackPawnSquares[pp];
                if (blackPawn != -1 && IsTargetForEnemyBishop(ref theBoard, blackPawn, theBoard.WhiteHasLightSquaredBishop, theBoard.WhiteHasDarkSquaredBishop,
                    theBoard.BlackHasLightSquaredBishop, theBoard.BlackHasDarkSquaredBishop))
                {
                    blackScore -= FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY;
                }
            }

        }


        private static bool IsTargetForEnemyBishop(ref Board theBoard, int pawnSquare, bool enemyHasLight, bool enemyHasDark, bool ownHasLight, bool ownHasDark)
        {
            bool onLight = theBoard.SquareColor[pawnSquare] == LIGHTSQUARE;
            bool enemyBishopOnColour = onLight ? enemyHasLight : enemyHasDark;
            bool ownBishopOnColour = onLight ? ownHasLight : ownHasDark;
            return enemyBishopOnColour && !ownBishopOnColour && IsFixedPawn(ref theBoard, pawnSquare);
        }


        //How much of the passed-pawn bonus blockaded passers lose: a passer (the front pawn on its file) with an enemy piece
        //on the square in front keeps PASSED_PAWN_BLOCKADED_KEEP_PERCENT of its bonus. The connected bonus is untouched.
        private static void ReduceBlockadedPassedPawns(ref Board theBoard, out int whiteReduction, out int blackReduction)
        {
            whiteReduction = 0;
            blackReduction = 0;

            for (int pp = 0; pp <= 7; pp++)
            {
                int whitePawn = theBoard.WhitePawnSquares[pp];
                if (whitePawn >= 8)
                {
                    int ahead = whitePawn - 8;
                    if (theBoard.Color[ahead] == BLACK && theBoard.Piece[ahead] != PAWN && IsFrontPassedPawn(ref theBoard, whitePawn, WHITE))
                    {
                        whiteReduction += PassedPawnBonus(whitePawn, WHITE) * (100 - PASSED_PAWN_BLOCKADED_KEEP_PERCENT) / 100;
                    }
                }

                int blackPawn = theBoard.BlackPawnSquares[pp];
                if (blackPawn != -1 && blackPawn <= 55)
                {
                    int ahead = blackPawn + 8;
                    if (theBoard.Color[ahead] == WHITE && theBoard.Piece[ahead] != PAWN && IsFrontPassedPawn(ref theBoard, blackPawn, BLACK))
                    {
                        blackReduction += PassedPawnBonus(blackPawn, BLACK) * (100 - PASSED_PAWN_BLOCKADED_KEEP_PERCENT) / 100;
                    }
                }
            }
        }


        //The same test RewardPassedPawns uses: no own pawn further up the file, and no enemy pawn in front on this or an adjacent file
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
            int[] lookups = side == WHITE ? theBoard.WhitePassedPawnLookUps[pawnSquare] : theBoard.BlackPassedPawnLookUps[pawnSquare];
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
                ? (theBoard.BlackQueenSquare != 255 || theBoard.BlackRookOneSquare != 255 || theBoard.BlackRookTwoSquare != 255)
                : (theBoard.WhiteQueenSquare != 255 || theBoard.WhiteRookOneSquare != 255 || theBoard.WhiteRookTwoSquare != 255);

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
                    score += hasShelterPawn ? KING_FILE_SHELTER_PAWN_BONUS : -KING_FILE_NO_SHELTER_PAWN_PENALTY;
                }
                else
                {
                    score += hasShelterPawn ? SIDE_FILE_SHELTER_PAWN_BONUS : -SIDE_FILE_NO_SHELTER_PAWN_PENALTY;
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

            if (theBoard.Color[62] == WHITE && theBoard.Piece[62] == KING && theBoard.Color[63] == EMPTY)
            {
                whiteScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[63] == WHITE && theBoard.Piece[63] == KING)
            {
                whiteScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[54] == WHITE && theBoard.Piece[54] == KING)
            {
                whiteScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[55] == WHITE && theBoard.Piece[55] == KING)
            {
                whiteScore += KSIDE_CASTLE_BONUS;
            }

            if (theBoard.Color[58] == WHITE && theBoard.Piece[58] == KING && theBoard.Color[56] == EMPTY && theBoard.Color[57] == EMPTY)
            {
                whiteScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[57] == WHITE && theBoard.Piece[57] == KING && theBoard.Color[56] == EMPTY)
            {
                whiteScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[56] == WHITE && theBoard.Piece[56] == KING)
            {
                whiteScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[49] == WHITE && theBoard.Piece[49] == KING)
            {
                whiteScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[48] == WHITE && theBoard.Piece[48] == KING)
            {
                whiteScore += QSIDE_CASTLE_BONUS;
            }

            if (theBoard.Color[6] == BLACK && theBoard.Piece[6] == KING && theBoard.Color[7] == EMPTY)
            {
                blackScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[7] == BLACK && theBoard.Piece[7] == KING)
            {
                blackScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[14] == BLACK && theBoard.Piece[14] == KING)
            {
                blackScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[15] == BLACK && theBoard.Piece[15] == KING)
            {
                blackScore += KSIDE_CASTLE_BONUS;
            }

            if (theBoard.Color[2] == BLACK && theBoard.Piece[2] == KING && theBoard.Color[1] == EMPTY && theBoard.Color[0] == EMPTY)
            {
                blackScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[1] == BLACK && theBoard.Piece[1] == KING && theBoard.Color[0] == EMPTY)
            {
                blackScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[0] == BLACK && theBoard.Piece[0] == KING)
            {
                blackScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[9] == BLACK && theBoard.Piece[9] == KING)
            {
                blackScore += QSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[8] == BLACK && theBoard.Piece[8] == KING)
            {
                blackScore += QSIDE_CASTLE_BONUS;
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

        //Returns 1 if White is the exchange up (one more rook and one fewer minor piece), -1 if Black is, otherwise 0
        private static int ExchangeImbalance(ref Board theBoard)
        {
            int whiteRooks = (theBoard.WhiteRookOneSquare != 255 ? 1 : 0) + (theBoard.WhiteRookTwoSquare != 255 ? 1 : 0);
            int blackRooks = (theBoard.BlackRookOneSquare != 255 ? 1 : 0) + (theBoard.BlackRookTwoSquare != 255 ? 1 : 0);
            int whiteKnights = (theBoard.WhiteKnightOneSquare != 255 ? 1 : 0) + (theBoard.WhiteKnightTwoSquare != 255 ? 1 : 0);
            int blackKnights = (theBoard.BlackKnightOneSquare != 255 ? 1 : 0) + (theBoard.BlackKnightTwoSquare != 255 ? 1 : 0);
            int whiteBishops = (theBoard.WhiteHasDarkSquaredBishop ? 1 : 0) + (theBoard.WhiteHasLightSquaredBishop ? 1 : 0);
            int blackBishops = (theBoard.BlackHasDarkSquaredBishop ? 1 : 0) + (theBoard.BlackHasLightSquaredBishop ? 1 : 0);
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
