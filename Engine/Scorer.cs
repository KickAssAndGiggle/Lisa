using static Lisa.Globals;
namespace Lisa
{
    public static class Scorer
    {

        public static int ScorePosition(ref Board theBoard, ref TranspositionTable tt, int alpha, int beta, bool allowLazyEval, int lastMoveCapMaterial)
        {

            bool InTT = tt.LookupScore(theBoard.CurrentZobrist, out TTScore lookup);
            if (InTT)
            {
                if (theBoard.OnMove == BLACK)
                {
                    return (lookup.WhiteScore - lookup.BlackScore);
                }
                else
                {
                    return (lookup.BlackScore - lookup.WhiteScore);
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

            //Early Game
            RewardBeingCastled(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardKingSafety(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            PenaliseBishopPawnWeaknesses(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardActiveBishop(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            EncouragePawnsOnOppositeSideToKingToAdvance(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardPawnBishopSynergy(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardOpeningMobility(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            RewardKnightOutpost(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);
            PenaliseTrappedRook(ref theBoard, ref whiteOpeningScore, ref blackOpeningScore);

            //Late Game          
            RewardRooksOnOpenFiles(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            PenaliseTrappedBishop(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            RewardRooksOnTheSeventh(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);
            RewardBlockadingDangerousPawns(ref theBoard, ref whiteEndgameScore, ref blackEndgameScore);

            bool pawnsInTT = tt.LookupPPScore(theBoard.PawnOnlyZobrist, out TTPawnAnalysis pawnLookup);
            if (pawnsInTT)
            {

                whiteOpeningScore += pawnLookup.WhiteBackwardsPawnScore;
                blackOpeningScore += pawnLookup.BlackBackwardPawnScore;
                whiteOpeningScore += pawnLookup.WhitePawnChainScore;
                blackOpeningScore += pawnLookup.BlackPawnChainScore;

                whiteEndgameScore += pawnLookup.WhitePassedPawnScore;
                blackEndgameScore += pawnLookup.BlackPassedPawnScore;
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
                whiteEndgameScore += whiteDblPawnScore;
                whiteEndgameScore += whiteIsoPawnScore;

                blackOpeningScore += blackBWPScore;
                blackOpeningScore += blackChainScore;
                blackEndgameScore += blackPPScore;
                blackEndgameScore += blackDblPawnScore;
                blackEndgameScore += blackIsoPawnScore;

            }

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


        public static void RewardOpeningMobility(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            Move[] WhiteMoves = theBoard.GeneratePieceMovesWithoutKing(WHITE);
            Move[] BlackMoves = theBoard.GeneratePieceMovesWithoutKing(BLACK);

            whiteScore += (WhiteMoves.Length * OPENING_MOBILITY_PER_MOVE_BONUS);
            blackScore += (BlackMoves.Length * OPENING_MOBILITY_PER_MOVE_BONUS);

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

            if (theBoard.WhitePressureMap[27] > theBoard.BlackPressureMap[27])
            {
                whiteScore += CENTRAL_PRESSURE_BONUS;
            }
            else if (theBoard.BlackPressureMap[27] > theBoard.WhitePressureMap[27])
            {
                blackScore += CENTRAL_PRESSURE_BONUS;
            }

            if (theBoard.WhitePressureMap[28] > theBoard.BlackPressureMap[28])
            {
                whiteScore += CENTRAL_PRESSURE_BONUS;
            }
            else if (theBoard.BlackPressureMap[28] > theBoard.WhitePressureMap[28])
            {
                blackScore += CENTRAL_PRESSURE_BONUS;
            }

            if (theBoard.WhitePressureMap[35] > theBoard.BlackPressureMap[35])
            {
                whiteScore += CENTRAL_PRESSURE_BONUS;
            }
            else if (theBoard.BlackPressureMap[35] > theBoard.WhitePressureMap[35])
            {
                blackScore += CENTRAL_PRESSURE_BONUS;
            }

            if (theBoard.WhitePressureMap[36] > theBoard.BlackPressureMap[36])
            {
                whiteScore += CENTRAL_PRESSURE_BONUS;
            }
            else if (theBoard.BlackPressureMap[36] > theBoard.WhitePressureMap[36])
            {
                blackScore += CENTRAL_PRESSURE_BONUS;
            }


        }


        public static void PenaliseBackwardsPawns(ref Board theBoard, out int whiteBWPScore, out int blackBWPScore)
        {

            whiteBWPScore = 0; blackBWPScore = 0;

            //white
            if (theBoard.Color[48] == WHITE && theBoard.Piece[48] == PAWN)
            {
                //a pawn in place
                if (theBoard.Color[49] == EMPTY || theBoard.Piece[49] != PAWN)
                {
                    if (theBoard.Color[41] == EMPTY || theBoard.Piece[41] != PAWN)
                    {
                        //white a pawn backward
                        whiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            if (theBoard.Color[49] == WHITE && theBoard.Piece[49] == PAWN)
            {
                //b pawn in place
                if (theBoard.Color[50] == EMPTY || theBoard.Piece[50] != PAWN)
                {
                    if (theBoard.Color[42] == EMPTY || theBoard.Piece[42] != PAWN)
                    {
                        if (theBoard.Color[48] == EMPTY || theBoard.Piece[48] != PAWN)
                        {
                            if (theBoard.Color[40] == EMPTY || theBoard.Piece[40] != PAWN)
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
                if (theBoard.Color[51] == EMPTY || theBoard.Piece[51] != PAWN)
                {
                    if (theBoard.Color[43] == EMPTY || theBoard.Piece[43] != PAWN)
                    {
                        if (theBoard.Color[49] == EMPTY || theBoard.Piece[49] != PAWN)
                        {
                            if (theBoard.Color[41] == EMPTY || theBoard.Piece[41] != PAWN)
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
                if (theBoard.Color[52] == EMPTY || theBoard.Piece[52] != PAWN)
                {
                    if (theBoard.Color[44] == EMPTY || theBoard.Piece[44] != PAWN)
                    {
                        if (theBoard.Color[50] == EMPTY || theBoard.Piece[50] != PAWN)
                        {
                            if (theBoard.Color[42] == EMPTY || theBoard.Piece[42] != PAWN)
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
                if (theBoard.Color[53] == EMPTY || theBoard.Piece[53] != PAWN)
                {
                    if (theBoard.Color[45] == EMPTY || theBoard.Piece[45] != PAWN)
                    {
                        if (theBoard.Color[51] == EMPTY || theBoard.Piece[51] != PAWN)
                        {
                            if (theBoard.Color[43] == EMPTY || theBoard.Piece[43] != PAWN)
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
                if (theBoard.Color[54] == EMPTY || theBoard.Piece[54] != PAWN)
                {
                    if (theBoard.Color[46] == EMPTY || theBoard.Piece[46] != PAWN)
                    {
                        if (theBoard.Color[52] == EMPTY || theBoard.Piece[52] != PAWN)
                        {
                            if (theBoard.Color[44] == EMPTY || theBoard.Piece[44] != PAWN)
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
                if (theBoard.Color[55] == EMPTY || theBoard.Piece[55] != PAWN)
                {
                    if (theBoard.Color[47] == EMPTY || theBoard.Piece[47] != PAWN)
                    {
                        if (theBoard.Color[53] == EMPTY || theBoard.Piece[53] != PAWN)
                        {
                            if (theBoard.Color[45] == EMPTY || theBoard.Piece[45] != PAWN)
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
                if (theBoard.Color[54] == EMPTY || theBoard.Piece[54] != PAWN)
                {
                    if (theBoard.Color[46] == EMPTY || theBoard.Piece[46] != PAWN)
                    {
                        //white h pawn backward
                        whiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            //black
            if (theBoard.Color[8] == BLACK && theBoard.Piece[8] == PAWN)
            {
                if (theBoard.Color[9] == EMPTY || theBoard.Piece[9] != PAWN)
                {
                    if (theBoard.Color[17] == EMPTY || theBoard.Piece[17] != PAWN)
                    {
                        //black a pawn backward
                        blackBWPScore += FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            if (theBoard.Color[9] == BLACK && theBoard.Piece[9] == PAWN)
            {
                //b pawn in place
                if (theBoard.Color[10] == EMPTY || theBoard.Piece[10] != PAWN)
                {
                    if (theBoard.Color[18] == EMPTY || theBoard.Piece[18] != PAWN)
                    {
                        if (theBoard.Color[8] == EMPTY || theBoard.Piece[8] != PAWN)
                        {
                            if (theBoard.Color[16] == EMPTY || theBoard.Piece[16] != PAWN)
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
                if (theBoard.Color[11] == EMPTY || theBoard.Piece[11] != PAWN)
                {
                    if (theBoard.Color[19] == EMPTY || theBoard.Piece[19] != PAWN)
                    {
                        if (theBoard.Color[9] == EMPTY || theBoard.Piece[9] != PAWN)
                        {
                            if (theBoard.Color[17] == EMPTY || theBoard.Piece[17] != PAWN)
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
                if (theBoard.Color[12] == EMPTY || theBoard.Piece[12] != PAWN)
                {
                    if (theBoard.Color[20] == EMPTY || theBoard.Piece[20] != PAWN)
                    {
                        if (theBoard.Color[10] == EMPTY || theBoard.Piece[10] != PAWN)
                        {
                            if (theBoard.Color[18] == EMPTY || theBoard.Piece[18] != PAWN)
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
                if (theBoard.Color[13] == EMPTY || theBoard.Piece[13] != PAWN)
                {
                    if (theBoard.Color[21] == EMPTY || theBoard.Piece[21] != PAWN)
                    {
                        if (theBoard.Color[11] == EMPTY || theBoard.Piece[11] != PAWN)
                        {
                            if (theBoard.Color[19] == EMPTY || theBoard.Piece[19] != PAWN)
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
                if (theBoard.Color[14] == EMPTY || theBoard.Piece[14] != PAWN)
                {
                    if (theBoard.Color[22] == EMPTY || theBoard.Piece[22] != PAWN)
                    {
                        if (theBoard.Color[12] == EMPTY || theBoard.Piece[12] != PAWN)
                        {
                            if (theBoard.Color[20] == EMPTY || theBoard.Piece[20] != PAWN)
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
                if (theBoard.Color[15] == EMPTY || theBoard.Piece[15] != PAWN)
                {
                    if (theBoard.Color[23] == EMPTY || theBoard.Piece[23] != PAWN)
                    {
                        if (theBoard.Color[13] == EMPTY || theBoard.Piece[13] != PAWN)
                        {
                            if (theBoard.Color[21] == EMPTY || theBoard.Piece[21] != PAWN)
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
                if (theBoard.Color[14] == EMPTY || theBoard.Piece[14] != PAWN)
                {
                    if (theBoard.Color[22] == EMPTY || theBoard.Piece[22] != PAWN)
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


        private static void EncouragePawnsOnOppositeSideToKingToAdvance(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteKingSquare % 8 >= 5)
            {
                if (theBoard.Color[48] == WHITE && theBoard.Piece[48] == PAWN)
                {
                    whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[49] == WHITE && theBoard.Piece[49] == PAWN)
                {
                    whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[50] == WHITE && theBoard.Piece[50] == PAWN)
                {
                    whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }
            else if (theBoard.WhiteKingSquare % 8 <= 2)
            {
                if (theBoard.Color[55] == WHITE && theBoard.Piece[55] == PAWN)
                {
                    whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[54] == WHITE && theBoard.Piece[54] == PAWN)
                {
                    whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[53] == WHITE && theBoard.Piece[53] == PAWN)
                {
                    whiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }

            if (theBoard.BlackKingSquare % 8 >= 5)
            {
                if (theBoard.Color[8] == WHITE && theBoard.Piece[8] == PAWN)
                {
                    blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[9] == WHITE && theBoard.Piece[9] == PAWN)
                {
                    blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[10] == WHITE && theBoard.Piece[10] == PAWN)
                {
                    blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }
            else if (theBoard.BlackKingSquare % 8 <= 2)
            {
                if (theBoard.Color[15] == WHITE && theBoard.Piece[15] == PAWN)
                {
                    blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[14] == WHITE && theBoard.Piece[14] == PAWN)
                {
                    blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (theBoard.Color[13] == WHITE && theBoard.Piece[13] == PAWN)
                {
                    blackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }

        }


        private static void RewardActiveBishop(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.WhiteHasDarkSquaredBishop)
            {
                if (theBoard.SameDiagonal[theBoard.WhiteDarkBishopSquare][theBoard.BlackKingSquare])
                {
                    whiteScore += BISHOP_ATTACKS_KING_BONUS;
                }
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
                if (theBoard.SameDiagonal[theBoard.WhiteLightBishopSquare][theBoard.BlackKingSquare])
                {
                    whiteScore += BISHOP_ATTACKS_KING_BONUS;
                }
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
                if (theBoard.SameDiagonal[theBoard.BlackDarkBishopSquare][theBoard.WhiteKingSquare])
                {
                    blackScore += BISHOP_ATTACKS_KING_BONUS;
                }
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
                if (theBoard.SameDiagonal[theBoard.BlackLightBishopSquare][theBoard.WhiteKingSquare])
                {
                    blackScore += BISHOP_ATTACKS_KING_BONUS;
                }
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
                        if (theBoard.Piece[theBoard.WhitePassedPawnLookUps[pawnSquare][sq]] == PAWN)
                        {
                            isPassed = false;
                            break;
                        }
                    }
                    if (isPassed)
                    {
                        whiteFilePassed[nn] = true;
                        whitePPScore += PASSED_PAWN_BONUS;
                        if (pawnSquare < 15)
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
                        if (theBoard.Piece[theBoard.BlackPassedPawnLookUps[pawnSquare][sq]] == PAWN)
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


        private static void PenaliseBishopPawnWeaknesses(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {


            if (theBoard.Color[9] == EMPTY)
            {
                if (!theBoard.BlackHasLightSquaredBishop)
                {
                    //Black has not got his b pawn in place and does not have a light squared bishop
                    blackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (theBoard.WhiteHasLightSquaredBishop)
                    {
                        //Worse because white DOES have his light squared bishop
                        blackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (theBoard.Piece[2] == KING || theBoard.Piece[1] == KING || theBoard.Piece[0] == KING)
                    {
                        //Worse because his king is on that side
                        blackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((theBoard.Piece[0] == BISHOP && theBoard.Color[0] == BLACK) ||
                        (theBoard.Piece[2] == BISHOP && theBoard.Color[2] == BLACK) ||
                        (theBoard.Piece[16] == BISHOP && theBoard.Color[16] == BLACK))
                    {
                        //Black has his bishop well placed for this advance
                        blackScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        blackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (theBoard.WhiteHasLightSquaredBishop)
                        {
                            //Worse because white DOES have his light squared bishop
                            blackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (theBoard.Piece[2] == KING || theBoard.Piece[1] == KING || theBoard.Piece[0] == KING)
                        {
                            //Worse because his king is on that side
                            blackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }

            if (theBoard.Color[14] == EMPTY)
            {
                if (!theBoard.BlackHasDarkSquaredBishop)
                {
                    //Black has not got his g pawn in place and does not have a light squared bishop
                    blackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (theBoard.WhiteHasDarkSquaredBishop)
                    {
                        //Worse because white DOES have his light squared bishop
                        blackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (theBoard.Piece[7] == KING || theBoard.Piece[6] == KING || theBoard.Piece[5] == KING)
                    {
                        //Worse because his king is on that side
                        blackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((theBoard.Piece[7] == BISHOP && theBoard.Color[7] == BLACK) ||
                        (theBoard.Piece[5] == BISHOP && theBoard.Color[5] == BLACK) ||
                        (theBoard.Piece[23] == BISHOP && theBoard.Color[23] == BLACK))
                    {
                        //Black has his bishop well placed for this advance
                        blackScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        blackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (theBoard.WhiteHasDarkSquaredBishop)
                        {
                            //Worse because white DOES have his light squared bishop
                            blackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (theBoard.Piece[7] == KING || theBoard.Piece[6] == KING || theBoard.Piece[5] == KING)
                        {
                            //Worse because his king is on that side
                            blackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }





            if (theBoard.Color[49] == EMPTY)
            {
                if (!theBoard.WhiteHasDarkSquaredBishop)
                {
                    //White has not got his b pawn in place and does not have a dark squared bishop
                    whiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (theBoard.BlackHasDarkSquaredBishop)
                    {
                        //Worse because white DOES have his dark squared bishop
                        whiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (theBoard.Piece[56] == KING || theBoard.Piece[57] == KING || theBoard.Piece[58] == KING)
                    {
                        //Worse because his king is on that side
                        whiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((theBoard.Piece[56] == BISHOP && theBoard.Color[56] == BLACK) ||
                        (theBoard.Piece[40] == BISHOP && theBoard.Color[40] == BLACK) ||
                        (theBoard.Piece[58] == BISHOP && theBoard.Color[58] == BLACK))
                    {
                        //White has his bishop well placed for this advance
                        whiteScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        whiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (theBoard.BlackHasDarkSquaredBishop)
                        {
                            //Worse because black DOES have his dark squared bishop
                            whiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (theBoard.Piece[56] == KING || theBoard.Piece[57] == KING || theBoard.Piece[58] == KING)
                        {
                            //Worse because his king is on that side
                            whiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }

            if (theBoard.Color[54] == EMPTY)
            {
                if (!theBoard.WhiteHasLightSquaredBishop)
                {
                    //White has not got his g pawn in place and does not have a light squared bishop
                    whiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (theBoard.BlackHasLightSquaredBishop)
                    {
                        //Worse because white DOES have his light squared bishop
                        whiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (theBoard.Piece[61] == KING || theBoard.Piece[62] == KING || theBoard.Piece[63] == KING)
                    {
                        //Worse because his king is on that side
                        whiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((theBoard.Piece[63] == BISHOP && theBoard.Color[63] == BLACK) ||
                        (theBoard.Piece[61] == BISHOP && theBoard.Color[61] == BLACK) ||
                        (theBoard.Piece[47] == BISHOP && theBoard.Color[47] == BLACK))
                    {
                        //Black has his bishop well placed for this advance
                        whiteScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        whiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (theBoard.BlackHasLightSquaredBishop)
                        {
                            //Worse because white DOES have his light squared bishop
                            whiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (theBoard.Piece[61] == KING || theBoard.Piece[62] == KING || theBoard.Piece[63] == KING)
                        {
                            //Worse because his king is on that side
                            whiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }

        }


        private static void PenaliseTrappedBishop(ref Board theBoard, ref int whiteScore, ref int blackScore)
        {

            if (theBoard.Piece[8] == BISHOP && theBoard.Color[8] == WHITE)
            {
                if (theBoard.Piece[10] == PAWN && theBoard.Piece[10] == BLACK)
                {
                    if ((theBoard.Piece[9] == PAWN && theBoard.Color[9] == BLACK))
                    {

                        //White bishop on a7 can be trapped by b6 next move
                        whiteScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (theBoard.Piece[17] == PAWN && theBoard.Color[17] == BLACK)
                    {

                        //White bishop on a7 already trapped by b6
                        whiteScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

            if (theBoard.Piece[15] == BISHOP && theBoard.Color[15] == WHITE)
            {
                if (theBoard.Piece[13] == PAWN && theBoard.Piece[13] == BLACK)
                {
                    if ((theBoard.Piece[14] == PAWN && theBoard.Color[14] == BLACK))
                    {

                        //White bishop on h7 can be trapped by g6 next move
                        whiteScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (theBoard.Piece[22] == PAWN && theBoard.Color[22] == BLACK)
                    {

                        //White bishop on h7 already trapped by g6
                        whiteScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

            if (theBoard.Piece[48] == BISHOP && theBoard.Color[48] == BLACK)
            {
                if (theBoard.Piece[50] == PAWN && theBoard.Piece[50] == WHITE)
                {
                    if ((theBoard.Piece[49] == PAWN && theBoard.Color[49] == WHITE))
                    {

                        //Black bishop on a2 can be trapped by b3 next move
                        blackScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (theBoard.Piece[41] == PAWN && theBoard.Color[41] == WHITE)
                    {

                        //Black bishop on a2 already trapped by b3
                        blackScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

            if (theBoard.Piece[55] == BISHOP && theBoard.Color[55] == BLACK)
            {
                if (theBoard.Piece[53] == PAWN && theBoard.Piece[53] == WHITE)
                {
                    if ((theBoard.Piece[52] == PAWN && theBoard.Color[52] == WHITE))
                    {

                        //Black bishop on h2 can be trapped by g3 next move
                        blackScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (theBoard.Piece[46] == PAWN && theBoard.Color[46] == WHITE)
                    {

                        //Black bishop on h2 already trapped by g3
                        blackScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

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

            if (theBoard.WhiteFilePawns[0] == 1 && theBoard.WhiteFilePawns[1] == 0)
            {
                //white a pawn isolated
                whiteIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }
            if (theBoard.WhiteFilePawns[7] == 1 && theBoard.WhiteFilePawns[6] == 0)
            {
                //white h pawn isolated
                whiteIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }
            if (theBoard.BlackFilePawns[0] == 1 && theBoard.BlackFilePawns[1] == 0)
            {
                //black a pawn isolated
                blackIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }
            if (theBoard.BlackFilePawns[7] == 1 && theBoard.BlackFilePawns[6] == 0)
            {
                //black h pawn isolated
                blackIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }

            for (int NN = 1; NN <= 6; NN++)
            {
                if (theBoard.WhiteFilePawns[NN] == 1 && theBoard.WhiteFilePawns[NN - 1] == 0 && theBoard.WhiteFilePawns[NN + 1] == 0)
                {
                    //a more central white pawn is isolated
                    whiteIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY;
                }
                if (theBoard.BlackFilePawns[NN] == 1 && theBoard.BlackFilePawns[NN - 1] == 0 && theBoard.BlackFilePawns[NN + 1] == 0)
                {
                    //a more central black pawn is isolated
                    blackIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY;
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
            else if (theBoard.WhiteKingSquare < 56 && theBoard.WhiteKingSquare >= 48)
            {
                whiteScore -= KING_STEPPED_UP_EARLY_PENALTY;
                if (theBoard.Color[theBoard.WhiteKingSquare - 8] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 8] == PAWN)
                {
                    whiteScore += KING_STEPPED_UP_PAWN_SHIELD_MITIGATION;
                }
                else if (theBoard.Color[theBoard.WhiteKingSquare - 16] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 16] == PAWN)
                {
                    whiteScore += KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION;
                }
                if (theBoard.WhiteKingSquare != 48 && theBoard.Color[theBoard.WhiteKingSquare - 9] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 9] == PAWN)
                {
                    whiteScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (theBoard.WhiteKingSquare != 48 && theBoard.Color[theBoard.WhiteKingSquare - 1] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 1] == PAWN)
                {
                    whiteScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
                if (theBoard.WhiteKingSquare != 55 && theBoard.Color[theBoard.WhiteKingSquare - 7] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 7] == PAWN)
                {
                    whiteScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (theBoard.WhiteKingSquare != 55 && theBoard.Color[theBoard.WhiteKingSquare + 1] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare + 1] == PAWN)
                {
                    whiteScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
            }
            else
            {
                if (theBoard.Color[theBoard.WhiteKingSquare - 8] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 8] == PAWN)
                {
                    whiteScore += KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS;
                    if (theBoard.BlackHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 8][theBoard.BlackDarkBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 8][theBoard.BlackLightBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackRookOneSquare != 255 && theBoard.WhiteKingSquare % 8 == theBoard.BlackRookOneSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackRookTwoSquare != 255 && theBoard.WhiteKingSquare % 8 == theBoard.BlackRookTwoSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 8][theBoard.BlackQueenSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.WhiteKingSquare % 8 == theBoard.BlackQueenSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackQueenSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][theBoard.WhiteKingSquare - 8])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.BlackKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][theBoard.WhiteKingSquare - 8])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY;
                    if (theBoard.BlackHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 8][theBoard.BlackDarkBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 8][theBoard.BlackLightBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackRookOneSquare != 255 && theBoard.WhiteKingSquare % 8 == theBoard.BlackRookOneSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackRookTwoSquare != 255 && theBoard.WhiteKingSquare % 8 == theBoard.BlackRookTwoSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 8][theBoard.BlackQueenSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.WhiteKingSquare % 8 == theBoard.BlackQueenSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackQueenSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][theBoard.WhiteKingSquare - 8])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.BlackKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][theBoard.WhiteKingSquare - 8])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (theBoard.WhiteKingSquare != 56 && theBoard.Color[theBoard.WhiteKingSquare - 9] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 9] == PAWN)
                {
                    whiteScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (theBoard.BlackHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 9][theBoard.BlackDarkBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 9][theBoard.BlackLightBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackRookOneSquare != 255 && theBoard.WhiteKingSquare % 8 - 1 == theBoard.BlackRookOneSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackRookTwoSquare != 255 && theBoard.WhiteKingSquare % 8 - 1 == theBoard.BlackRookTwoSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 9][theBoard.BlackQueenSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.WhiteKingSquare % 8 - 1 == theBoard.BlackQueenSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackQueenSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][theBoard.WhiteKingSquare - 9])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.BlackKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][theBoard.WhiteKingSquare - 9])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (theBoard.BlackHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 9][theBoard.BlackDarkBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 9][theBoard.BlackLightBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackRookOneSquare != 255 && theBoard.WhiteKingSquare % 8 - 1 == theBoard.BlackRookOneSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackRookTwoSquare != 255 && theBoard.WhiteKingSquare % 8 - 1 == theBoard.BlackRookTwoSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 9][theBoard.BlackQueenSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.WhiteKingSquare % 8 - 1 == theBoard.BlackQueenSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackQueenSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][theBoard.WhiteKingSquare - 9])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.BlackKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][theBoard.WhiteKingSquare - 9])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (theBoard.WhiteKingSquare != 63 && theBoard.Color[theBoard.WhiteKingSquare - 7] == WHITE && theBoard.Piece[theBoard.WhiteKingSquare - 7] == PAWN)
                {
                    whiteScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (theBoard.BlackHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 7][theBoard.BlackDarkBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 7][theBoard.BlackLightBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackRookOneSquare != 255 && theBoard.WhiteKingSquare % 8 + 1 == theBoard.BlackRookOneSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackRookTwoSquare != 255 && theBoard.WhiteKingSquare % 8 + 1 == theBoard.BlackRookTwoSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 7][theBoard.BlackQueenSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.WhiteKingSquare % 8 + 1 == theBoard.BlackQueenSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackQueenSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][theBoard.WhiteKingSquare - 7])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.BlackKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][theBoard.WhiteKingSquare - 7])
                    {
                        whiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (theBoard.BlackHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 7][theBoard.BlackDarkBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 7][theBoard.BlackLightBishopSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.BlackRookOneSquare != 255 && theBoard.WhiteKingSquare % 8 + 1 == theBoard.BlackRookOneSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookOneSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackRookTwoSquare != 255 && theBoard.WhiteKingSquare % 8 + 1 == theBoard.BlackRookTwoSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.SameDiagonal[theBoard.WhiteKingSquare - 7][theBoard.BlackQueenSquare])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.BlackQueenSquare != 255 && theBoard.WhiteKingSquare % 8 + 1 == theBoard.BlackQueenSquare % 8)
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.BlackFilePawns[theBoard.BlackQueenSquare % 8] == 0)
                        {
                            whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.BlackKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightOneSquare][theBoard.WhiteKingSquare - 7])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.BlackKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.BlackKnightTwoSquare][theBoard.WhiteKingSquare - 7])
                    {
                        whiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
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
            else if (theBoard.BlackKingSquare > 7 && theBoard.BlackKingSquare <= 15)
            {
                blackScore -= KING_STEPPED_UP_EARLY_PENALTY;
                if (theBoard.Color[theBoard.BlackKingSquare + 8] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 8] == PAWN)
                {
                    blackScore += KING_STEPPED_UP_PAWN_SHIELD_MITIGATION;
                }
                else if (theBoard.Color[theBoard.BlackKingSquare + 16] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 16] == PAWN)
                {
                    blackScore += KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION;
                }
                if (theBoard.BlackKingSquare != 15 && theBoard.Color[theBoard.BlackKingSquare + 9] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 9] == PAWN)
                {
                    blackScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (theBoard.BlackKingSquare != 15 && theBoard.Color[theBoard.BlackKingSquare + 1] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 1] == PAWN)
                {
                    blackScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
                if (theBoard.BlackKingSquare != 8 && theBoard.Color[theBoard.BlackKingSquare + 7] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 7] == PAWN)
                {
                    blackScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (theBoard.BlackKingSquare != 8 && theBoard.Color[theBoard.BlackKingSquare - 1] == BLACK && theBoard.Piece[theBoard.BlackKingSquare - 1] == PAWN)
                {
                    blackScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
            }
            else
            {
                if (theBoard.Color[theBoard.BlackKingSquare + 8] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 8] == PAWN)
                {
                    blackScore += KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS;
                    if (theBoard.WhiteHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 8][theBoard.WhiteDarkBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 8][theBoard.WhiteLightBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteRookOneSquare != 255 && theBoard.BlackKingSquare % 8 == theBoard.WhiteRookOneSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteRookTwoSquare != 255 && theBoard.BlackKingSquare % 8 == theBoard.WhiteRookTwoSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.SameDiagonal[theBoard.BlackKingSquare + 8][theBoard.WhiteQueenSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.BlackKingSquare % 8 == theBoard.WhiteQueenSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteQueenSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][theBoard.BlackKingSquare + 8])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.WhiteKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][theBoard.BlackKingSquare + 8])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY;
                    if (theBoard.WhiteHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 8][theBoard.WhiteDarkBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 8][theBoard.WhiteLightBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteRookOneSquare != 255 && theBoard.BlackKingSquare % 8 == theBoard.WhiteRookOneSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteRookTwoSquare != 255 && theBoard.BlackKingSquare % 8 == theBoard.WhiteRookTwoSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.SameDiagonal[theBoard.BlackKingSquare + 8][theBoard.WhiteQueenSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.BlackKingSquare % 8 == theBoard.WhiteQueenSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteQueenSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][theBoard.BlackKingSquare + 8])
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.WhiteKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][theBoard.BlackKingSquare + 8])
                    {
                        blackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (theBoard.BlackKingSquare != 7 && theBoard.Color[theBoard.BlackKingSquare + 9] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 9] == PAWN)
                {
                    blackScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (theBoard.WhiteHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 9][theBoard.WhiteDarkBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 9][theBoard.WhiteLightBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteRookOneSquare != 255 && theBoard.BlackKingSquare % 8 + 1 == theBoard.WhiteRookOneSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteRookTwoSquare != 255 && theBoard.BlackKingSquare % 8 + 1 == theBoard.WhiteRookTwoSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.SameDiagonal[theBoard.BlackKingSquare + 9][theBoard.WhiteQueenSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.BlackKingSquare % 8 + 1 == theBoard.WhiteQueenSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteQueenSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][theBoard.BlackKingSquare + 9])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.WhiteKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][theBoard.BlackKingSquare + 9])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (theBoard.WhiteHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 9][theBoard.WhiteDarkBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 9][theBoard.WhiteLightBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteRookOneSquare != 255 && theBoard.BlackKingSquare % 8 + 1 == theBoard.WhiteRookOneSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteRookTwoSquare != 255 && theBoard.BlackKingSquare % 8 + 1 == theBoard.WhiteRookTwoSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.SameDiagonal[theBoard.BlackKingSquare + 9][theBoard.WhiteQueenSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.BlackKingSquare % 8 + 1 == theBoard.WhiteQueenSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteQueenSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][theBoard.BlackKingSquare + 9])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.WhiteKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][theBoard.BlackKingSquare + 9])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (theBoard.BlackKingSquare != 0 && theBoard.Color[theBoard.BlackKingSquare + 7] == BLACK && theBoard.Piece[theBoard.BlackKingSquare + 7] == PAWN)
                {
                    blackScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (theBoard.WhiteHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 7][theBoard.WhiteDarkBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 7][theBoard.WhiteLightBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteRookOneSquare != 255 && theBoard.BlackKingSquare % 8 - 1 == theBoard.WhiteRookOneSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteRookTwoSquare != 255 && theBoard.BlackKingSquare % 8 - 1 == theBoard.WhiteRookTwoSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.SameDiagonal[theBoard.BlackKingSquare + 7][theBoard.WhiteQueenSquare])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.BlackKingSquare % 8 - 1 == theBoard.WhiteQueenSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteQueenSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][theBoard.BlackKingSquare + 7])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.WhiteKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][theBoard.BlackKingSquare + 7])
                    {
                        blackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (theBoard.WhiteHasDarkSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 7][theBoard.WhiteDarkBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteHasLightSquaredBishop && theBoard.SameDiagonal[theBoard.BlackKingSquare + 7][theBoard.WhiteLightBishopSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (theBoard.WhiteRookOneSquare != 255 && theBoard.BlackKingSquare % 8 - 1 == theBoard.WhiteRookOneSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteRookTwoSquare != 255 && theBoard.BlackKingSquare % 8 - 1 == theBoard.WhiteRookTwoSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.SameDiagonal[theBoard.BlackKingSquare + 7][theBoard.WhiteQueenSquare])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (theBoard.WhiteQueenSquare != 255 && theBoard.BlackKingSquare % 8 - 1 == theBoard.WhiteQueenSquare % 8)
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (theBoard.WhiteFilePawns[theBoard.WhiteQueenSquare % 8] == 0)
                        {
                            blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (theBoard.WhiteKnightOneSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightOneSquare][theBoard.BlackKingSquare + 7])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (theBoard.WhiteKnightTwoSquare != 255 && theBoard.KnightDestinations[theBoard.WhiteKnightTwoSquare][theBoard.BlackKingSquare + 7])
                    {
                        blackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
            }

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

            if (theBoard.Color[6] == BLACK && theBoard.Piece[6] == KING && theBoard.Color[7] == EMPTY)
            {
                blackScore += KSIDE_CASTLE_BONUS;
            }
            else if (theBoard.Color[7] == BLACK && theBoard.Piece[7] == KING)
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

    }
}
