using static Lisa.Globals;
namespace Lisa
{
    public static class Scorer
    {

        public static int ScorePosition(ref Board TheBoard, ref TranspositionTable TT, int Alpha, int Beta, bool allowLazyEval, int lastMoveCapMaterial)
        {

            bool InTT = TT.LookupScore(TheBoard.CurrentZobrist, out TTScore Lookup);
            if (InTT)
            {
                if (TheBoard.OnMove == BLACK)
                {
                    return (Lookup.WhiteScore - Lookup.BlackScore);
                }
                else
                {
                    return (Lookup.BlackScore - Lookup.WhiteScore);
                }
            }

            int Phase = (TheBoard.GamePhase * 256 + 12) / 24;

            int WhiteOpeningScore = TheBoard.WhiteEarlyPSTScore + TheBoard.WhiteMaterial;
            int WhiteEndgameScore = TheBoard.WhiteLatePSTScore + TheBoard.WhiteMaterial;
            int BlackOpeningScore = TheBoard.BlackEarlyPSTScore + TheBoard.BlackMaterial;
            int BlackEndgameScore = TheBoard.BlackLatePSTScore + TheBoard.BlackMaterial;

            if (allowLazyEval)
            {

                int WhiteLazyScore = ((WhiteOpeningScore * (256 - Phase)) + (WhiteEndgameScore * Phase)) / 256;
                int BlackLazyScore = ((BlackOpeningScore * (256 - Phase)) + (BlackEndgameScore * Phase)) / 256;
                int lazy = TheBoard.OnMove == BLACK ? WhiteLazyScore - BlackLazyScore : BlackLazyScore - WhiteLazyScore;

                if (TheBoard.WhiteQueenSquare == 255 && TheBoard.BlackQueenSquare == 255)
                {
                    if (lastMoveCapMaterial == 0)
                    {
                        if (lazy + LAZY_EVAL_QUEENS_OFF_MARGIN < Alpha || lazy - LAZY_EVAL_QUEENS_OFF_MARGIN > Beta)
                        {
                            return lazy;
                        }
                    }
                    else
                    {
                        if (lazy + (LAZY_EVAL_QUEENS_OFF_MARGIN + lastMoveCapMaterial) < Alpha || lazy - (LAZY_EVAL_QUEENS_OFF_MARGIN + lastMoveCapMaterial) > Beta)
                        {
                            return lazy;
                        }
                    }
                }
                else
                {
                    if (lastMoveCapMaterial == 0)
                    {
                        if (lazy + LAZY_EVAL_QUEENS_ON_MARGIN < Alpha || lazy - LAZY_EVAL_QUEENS_ON_MARGIN > Beta)
                        {
                            return lazy;
                        }
                    }
                    else
                    {
                        if (lazy + (LAZY_EVAL_QUEENS_ON_MARGIN + lastMoveCapMaterial) < Alpha || lazy - (LAZY_EVAL_QUEENS_ON_MARGIN + lastMoveCapMaterial) > Beta)
                        {
                            return lazy;
                        }
                    }
                }

            }

            //Early Game
            RewardBeingCastled(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            RewardKingSafety(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            PenaliseBishopPawnWeaknesses(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            RewardActiveBishop(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            EncouragePawnsOnOppositeSideToKingToAdvance(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            RewardPawnBishopSynergy(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            RewardOpeningMobility(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            RewardKnightOutpost(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            PenaliseTrappedRook(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);
            //PenaliseObviousPins(ref TheBoard, ref WhiteOpeningScore, ref BlackOpeningScore);

            //Late Game          
            RewardRooksOnOpenFiles(ref TheBoard, TheBoard.WhiteFilePawns, TheBoard.BlackFilePawns, ref WhiteEndgameScore, ref BlackEndgameScore);
            PenaliseTrappedBishop(ref TheBoard, ref WhiteEndgameScore, ref BlackEndgameScore);
            RewardRooksOnTheSeventh(ref TheBoard, ref WhiteEndgameScore, ref BlackEndgameScore);
            RewardBlockadingDangerousPawns(ref TheBoard, ref WhiteEndgameScore, ref BlackEndgameScore);

            bool PawnsInTT = TT.LookupPPScore(TheBoard.PawnOnlyZobrist, out TTPawnAnalysis PawnLookup);
            if (PawnsInTT)
            {

                WhiteOpeningScore += PawnLookup.WhiteBackwardsPawnScore;
                BlackOpeningScore += PawnLookup.BlackBackwardPawnScore;
                WhiteOpeningScore += PawnLookup.WhitePawnChainScore;
                BlackOpeningScore += PawnLookup.BlackPawnChainScore;

                WhiteEndgameScore += PawnLookup.WhitePassedPawnScore;
                BlackEndgameScore += PawnLookup.BlackPassedPawnScore;
                WhiteEndgameScore += PawnLookup.WhiteDoubledPawnScore;
                BlackEndgameScore += PawnLookup.BlackDoubledPawnScore;
                WhiteEndgameScore += PawnLookup.WhiteIsolatedPawnScore;
                BlackEndgameScore += PawnLookup.BlackIsolatedPawnScore;

            }
            else
            {

                PenaliseBackwardsPawns(ref TheBoard, out int WhiteBWPScore, out int BlackBWPScore);
                RewardPawnChains(ref TheBoard, out int WhiteChainScore, out int BlackChainScore);

                RewardPassedPawns(ref TheBoard, out int WhitePPScore, out int BlackPPScore);
                PenaliseDoubledAndTripledPawns(ref TheBoard, out int WhiteDblPawnScore, out int BlackDblPawnScore);
                PenaliseIsolatedPawns(ref TheBoard, out int WhiteIsoPawnScore, out int BlackIsoPawnScore);

                TT.AddPawnStructureToTransTable(TheBoard.PawnOnlyZobrist, WhitePPScore, BlackPPScore, WhiteBWPScore,
                    BlackBWPScore, WhiteChainScore, BlackChainScore, WhiteDblPawnScore, BlackDblPawnScore, WhiteIsoPawnScore, BlackIsoPawnScore);

                WhiteOpeningScore += WhiteBWPScore;
                WhiteOpeningScore += WhiteChainScore;
                WhiteEndgameScore += WhitePPScore;
                WhiteEndgameScore += WhiteDblPawnScore;
                WhiteEndgameScore += WhiteIsoPawnScore;

                BlackOpeningScore += BlackBWPScore;
                BlackOpeningScore += BlackChainScore;
                BlackEndgameScore += BlackPPScore;
                BlackEndgameScore += BlackDblPawnScore;
                BlackEndgameScore += BlackIsoPawnScore;

            }

            int WhiteScore = ((WhiteOpeningScore * (256 - Phase)) + (WhiteEndgameScore * Phase)) / 256;
            int BlackScore = ((BlackOpeningScore * (256 - Phase)) + (BlackEndgameScore * Phase)) / 256;

            if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.WhiteHasLightSquaredBishop)
            {
                WhiteScore += BISHOP_PAIR_BONUS_VALUE;
            }
            if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.BlackHasLightSquaredBishop)
            {
                BlackScore += BISHOP_PAIR_BONUS_VALUE;
            }

            if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.WhiteRookTwoSquare != 255)
            {
                WhiteScore -= ROOK_REDUNDANCY_PENALTY;
            }
            if (TheBoard.BlackRookOneSquare != 255 && TheBoard.BlackRookTwoSquare != 255)
            {
                BlackScore -= ROOK_REDUNDANCY_PENALTY;
            }

            if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.WhiteKnightTwoSquare != 255)
            {
                WhiteScore -= KNIGHT_REDUNDANCY_PENALTY;
            }
            if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.BlackKnightTwoSquare != 255)
            {
                BlackScore -= KNIGHT_REDUNDANCY_PENALTY;
            }

            if (TheBoard.WhiteRookOneSquare != 255 || TheBoard.WhiteRookTwoSquare != 255)
            {
                for (int NN = 0; NN <= 7; NN++)
                {
                    if (TheBoard.WhiteFilePawns[NN] == 0)
                    {
                        if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.WhiteRookTwoSquare != 255)
                        {
                            WhiteScore += SEMI_OPEN_FILE_TWO_ROOKS_BONUS;
                        }
                        else
                        {
                            WhiteScore += SEMI_OPEN_FILE_ONE_ROOK_BONUS;
                        }
                    }
                }
            }

            if (TheBoard.BlackRookOneSquare != 255 || TheBoard.BlackRookTwoSquare != 255)
            {
                for (int NN = 0; NN <= 7; NN++)
                {
                    if (TheBoard.BlackFilePawns[NN] == 0)
                    {
                        if (TheBoard.BlackRookOneSquare != 255 && TheBoard.BlackRookTwoSquare != 255)
                        {
                            BlackScore += SEMI_OPEN_FILE_TWO_ROOKS_BONUS;
                        }
                        else
                        {
                            BlackScore += SEMI_OPEN_FILE_ONE_ROOK_BONUS;
                        }
                    }
                }
            }

            TT.AddScoreToTransTable(TheBoard.CurrentZobrist, WhiteScore, BlackScore);

            if (TheBoard.OnMove == BLACK)
            {
                BlackScore += TEMPO_BONUS;
                return (WhiteScore - BlackScore);
            }
            else
            {
                WhiteScore += TEMPO_BONUS;
                return (BlackScore - WhiteScore);
            }

        }


        public static void PenaliseTrappedRook(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.BlackKingSquare <= 3)
            {
                if (TheBoard.Piece[0] == ROOK && TheBoard.Color[0] == BLACK)
                {
                    if ((TheBoard.Piece[8] == PAWN && TheBoard.Color[8] == BLACK) || (TheBoard.Piece[16] == PAWN && TheBoard.Color[16] == BLACK))
                    {
                        BlackScore -= TRAPPED_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[1] == 0)
                        {
                            BlackScore += TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION;
                        }
                        if (TheBoard.BlackFilePawns[2] == 0)
                        {
                            BlackScore += TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION;
                        }
                    }
                }
            }

            if (TheBoard.BlackKingSquare == 5 || TheBoard.BlackKingSquare == 6)
            {
                if (TheBoard.Piece[7] == ROOK && TheBoard.Color[7] == BLACK)
                {
                    if ((TheBoard.Piece[15] == PAWN && TheBoard.Color[15] == BLACK) || (TheBoard.Piece[23] == PAWN && TheBoard.Color[23] == BLACK))
                    {
                        BlackScore -= TRAPPED_ROOK_PENALTY;
                    }
                }
            }

            if (TheBoard.WhiteKingSquare >= 57 && TheBoard.WhiteKingSquare <= 59)
            {
                if (TheBoard.Piece[56] == ROOK && TheBoard.Color[56] == WHITE)
                {
                    if ((TheBoard.Piece[48] == PAWN && TheBoard.Color[48] == WHITE) || (TheBoard.Piece[40] == PAWN && TheBoard.Color[40] == WHITE))
                    {
                        WhiteScore -= TRAPPED_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[1] == 0)
                        {
                            WhiteScore += TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION;
                        }
                        if (TheBoard.WhiteFilePawns[2] == 0)
                        {
                            WhiteScore += TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION;
                        }
                    }
                }
            }

            if (TheBoard.WhiteKingSquare == 61 || TheBoard.WhiteKingSquare == 62)
            {
                if (TheBoard.Piece[63] == ROOK && TheBoard.Color[63] == WHITE)
                {
                    if ((TheBoard.Piece[55] == PAWN && TheBoard.Color[55] == WHITE) || (TheBoard.Piece[47] == PAWN && TheBoard.Color[47] == WHITE))
                    {
                        WhiteScore -= TRAPPED_ROOK_PENALTY;
                    }
                }
            }

        }


        public static void RewardBlockadingDangerousPawns(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            for (int NN = 32; NN <= 39; NN++)
            {
                if (TheBoard.Piece[NN] == PAWN && TheBoard.Color[NN] == BLACK)
                {
                    if ((TheBoard.Piece[NN + 8] == BISHOP || TheBoard.Piece[NN + 8] == KNIGHT) && TheBoard.Color[NN + 8] == WHITE)
                    {
                        WhiteScore += BLOCKADING_PASSED_PAWN_BONUS;
                    }
                }
            }

            for (int NN = 24; NN <= 31; NN++)
            {
                if (TheBoard.Piece[NN] == PAWN && TheBoard.Color[NN] == WHITE)
                {
                    if ((TheBoard.Piece[NN - 8] == BISHOP || TheBoard.Piece[NN - 8] == KNIGHT) && TheBoard.Color[NN - 8] == BLACK)
                    {
                        BlackScore += BLOCKADING_PASSED_PAWN_BONUS;
                    }
                }
            }

        }


        public static void RewardKnightOutpost(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.Piece[26] == KNIGHT && TheBoard.Color[26] == WHITE)
            {
                if (TheBoard.Piece[33] == PAWN && TheBoard.Color[33] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (TheBoard.Piece[35] == PAWN && TheBoard.Color[35] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }

            if (TheBoard.Piece[27] == KNIGHT && TheBoard.Color[27] == WHITE)
            {
                if (TheBoard.Piece[34] == PAWN && TheBoard.Color[34] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (TheBoard.Piece[36] == PAWN && TheBoard.Color[36] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (TheBoard.Piece[28] == KNIGHT && TheBoard.Color[28] == WHITE)
            {
                if (TheBoard.Piece[35] == PAWN && TheBoard.Color[35] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (TheBoard.Piece[37] == PAWN && TheBoard.Color[37] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (TheBoard.Piece[29] == KNIGHT && TheBoard.Color[29] == WHITE)
            {
                if (TheBoard.Piece[36] == PAWN && TheBoard.Color[36] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (TheBoard.Piece[38] == PAWN && TheBoard.Color[38] == WHITE)
                {
                    WhiteScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }

            if (TheBoard.Piece[34] == KNIGHT && TheBoard.Color[34] == BLACK)
            {
                if (TheBoard.Piece[27] == PAWN && TheBoard.Color[27] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (TheBoard.Piece[25] == PAWN && TheBoard.Color[25] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }

            if (TheBoard.Piece[35] == KNIGHT && TheBoard.Color[35] == BLACK)
            {
                if (TheBoard.Piece[28] == PAWN && TheBoard.Color[28] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (TheBoard.Piece[26] == PAWN && TheBoard.Color[26] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (TheBoard.Piece[36] == KNIGHT && TheBoard.Color[36] == BLACK)
            {
                if (TheBoard.Piece[29] == PAWN && TheBoard.Color[29] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
                if (TheBoard.Piece[27] == PAWN && TheBoard.Color[27] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MAJOR_BONUS;
                }
            }

            if (TheBoard.Piece[37] == KNIGHT && TheBoard.Color[37] == BLACK)
            {
                if (TheBoard.Piece[30] == PAWN && TheBoard.Color[30] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
                if (TheBoard.Piece[28] == PAWN && TheBoard.Color[28] == BLACK)
                {
                    BlackScore += KNIGHT_OUTPOST_MINOR_BONUS;
                }
            }


        }


        public static void RewardPawnChains(ref Board TheBoard, out int WhiteChainScore, out int BlackChainScore)
        {

            WhiteChainScore = 0; BlackChainScore = 0;
            for (int PP = 0; PP <= 7; PP++)
            {
                if (TheBoard.WhitePawnSquares[PP] != -1)
                {
                    if (TheBoard.WhitePawnSquares[PP] < 48)
                    {
                        if (TheBoard.WhitePawnSquares[PP] % 8 != 0)
                        {
                            if (TheBoard.Color[TheBoard.WhitePawnSquares[PP] + 7] == WHITE && TheBoard.Piece[TheBoard.WhitePawnSquares[PP] + 7] == PAWN)
                            {
                                WhiteChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                        if (TheBoard.WhitePawnSquares[PP] % 8 != 7)
                        {
                            if (TheBoard.Color[TheBoard.WhitePawnSquares[PP] + 9] == WHITE && TheBoard.Piece[TheBoard.WhitePawnSquares[PP] + 9] == PAWN)
                            {
                                WhiteChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                    }
                }
                if (TheBoard.BlackPawnSquares[PP] != -1)
                {
                    if (TheBoard.BlackPawnSquares[PP] > 15)
                    {
                        if (TheBoard.BlackPawnSquares[PP] % 8 != 0)
                        {
                            if (TheBoard.Color[TheBoard.BlackPawnSquares[PP] - 9] == BLACK && TheBoard.Piece[TheBoard.BlackPawnSquares[PP] - 9] == PAWN)
                            {
                                BlackChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                        if (TheBoard.BlackPawnSquares[PP] % 8 != 7)
                        {
                            if (TheBoard.Color[TheBoard.BlackPawnSquares[PP] - 7] == BLACK && TheBoard.Piece[TheBoard.BlackPawnSquares[PP] - 7] == PAWN)
                            {
                                BlackChainScore += PAWN_CHAIN_BONUS;
                            }
                        }
                    }
                }
            }

        }


        public static void RewardOpeningMobility(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            Move[] WhiteMoves = TheBoard.GeneratePieceMovesWithoutKing(WHITE);
            Move[] BlackMoves = TheBoard.GeneratePieceMovesWithoutKing(BLACK);

            WhiteScore += (WhiteMoves.Length * OPENING_MOBILITY_PER_MOVE_BONUS);
            BlackScore += (BlackMoves.Length * OPENING_MOBILITY_PER_MOVE_BONUS);

            if (TheBoard.WhiteKnightOneSquare != 255)
            {
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][35])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][36])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][27])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][28])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (TheBoard.WhiteKnightOneSquare % 8 == 0)
                {
                    WhiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (TheBoard.WhiteKnightOneSquare % 8 == 7)
                {
                    WhiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }


            }

            if (TheBoard.WhiteKnightTwoSquare != 255)
            {
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][35])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][36])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][27])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][28])
                {
                    WhiteScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (TheBoard.WhiteKnightTwoSquare % 8 == 0)
                {
                    WhiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (TheBoard.WhiteKnightTwoSquare % 8 == 7)
                {
                    WhiteScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }

            }

            if (TheBoard.BlackKnightOneSquare != 255)
            {
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][35])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][36])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][27])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][28])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (TheBoard.BlackKnightOneSquare % 8 == 0)
                {
                    BlackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (TheBoard.BlackKnightOneSquare % 8 == 7)
                {
                    BlackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }

            }

            if (TheBoard.BlackKnightTwoSquare != 255)
            {
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][35])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][36])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][27])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }
                if (TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][28])
                {
                    BlackScore += OPENING_MINOR_PIECE_INFLUENCES_CENTER;
                }

                if (TheBoard.BlackKnightTwoSquare % 8 == 0)
                {
                    BlackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }
                if (TheBoard.BlackKnightTwoSquare % 8 == 7)
                {
                    BlackScore -= KNIGHT_ON_THE_RIM_IS_DIM_PENALTY;
                }

            }

            if (TheBoard.WhitePressureMap[27] > TheBoard.BlackPressureMap[27])
            {
                WhiteScore += 25;
            }
            else if (TheBoard.BlackPressureMap[27] > TheBoard.WhitePressureMap[27])
            {
                BlackScore += 25;
            }

            if (TheBoard.WhitePressureMap[28] > TheBoard.BlackPressureMap[28])
            {
                WhiteScore += 25;
            }
            else if (TheBoard.BlackPressureMap[28] > TheBoard.WhitePressureMap[28])
            {
                BlackScore += 25;
            }

            if (TheBoard.WhitePressureMap[35] > TheBoard.BlackPressureMap[35])
            {
                WhiteScore += 25;
            }
            else if (TheBoard.BlackPressureMap[35] > TheBoard.WhitePressureMap[35])
            {
                BlackScore += 25;
            }

            if (TheBoard.WhitePressureMap[36] > TheBoard.BlackPressureMap[36])
            {
                WhiteScore += 25;
            }
            else if (TheBoard.BlackPressureMap[36] > TheBoard.WhitePressureMap[36])
            {
                BlackScore += 25;
            }


        }


        public static void PenaliseBackwardsPawns(ref Board TheBoard, out int WhiteBWPScore, out int BlackBWPScore)
        {

            WhiteBWPScore = 0; BlackBWPScore = 0;

            //white
            if (TheBoard.Color[48] == WHITE && TheBoard.Piece[48] == PAWN)
            {
                //a pawn in place
                if (TheBoard.Color[49] == EMPTY || TheBoard.Piece[49] != PAWN)
                {
                    if (TheBoard.Color[41] == EMPTY || TheBoard.Piece[41] != PAWN)
                    {
                        //white a pawn backward
                        WhiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            if (TheBoard.Color[49] == WHITE && TheBoard.Piece[49] == PAWN)
            {
                //b pawn in place
                if (TheBoard.Color[50] == EMPTY || TheBoard.Piece[50] != PAWN)
                {
                    if (TheBoard.Color[42] == EMPTY || TheBoard.Piece[42] != PAWN)
                    {
                        if (TheBoard.Color[48] == EMPTY || TheBoard.Piece[48] != PAWN)
                        {
                            if (TheBoard.Color[40] == EMPTY || TheBoard.Piece[40] != PAWN)
                            {
                                //white b pawn backward
                                WhiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[50] == WHITE && TheBoard.Piece[50] == PAWN)
            {
                //c pawn in place
                if (TheBoard.Color[51] == EMPTY || TheBoard.Piece[51] != PAWN)
                {
                    if (TheBoard.Color[43] == EMPTY || TheBoard.Piece[43] != PAWN)
                    {
                        if (TheBoard.Color[49] == EMPTY || TheBoard.Piece[49] != PAWN)
                        {
                            if (TheBoard.Color[41] == EMPTY || TheBoard.Piece[41] != PAWN)
                            {
                                //white c pawn backward
                                WhiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[51] == WHITE && TheBoard.Piece[51] == PAWN)
            {
                //d pawn in place
                if (TheBoard.Color[52] == EMPTY || TheBoard.Piece[52] != PAWN)
                {
                    if (TheBoard.Color[44] == EMPTY || TheBoard.Piece[44] != PAWN)
                    {
                        if (TheBoard.Color[50] == EMPTY || TheBoard.Piece[50] != PAWN)
                        {
                            if (TheBoard.Color[42] == EMPTY || TheBoard.Piece[42] != PAWN)
                            {
                                //white d pawn backward
                                WhiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[52] == WHITE && TheBoard.Piece[52] == PAWN)
            {
                //e pawn in place
                if (TheBoard.Color[53] == EMPTY || TheBoard.Piece[53] != PAWN)
                {
                    if (TheBoard.Color[45] == EMPTY || TheBoard.Piece[45] != PAWN)
                    {
                        if (TheBoard.Color[51] == EMPTY || TheBoard.Piece[51] != PAWN)
                        {
                            if (TheBoard.Color[43] == EMPTY || TheBoard.Piece[43] != PAWN)
                            {
                                //white e pawn backward
                                WhiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[53] == WHITE && TheBoard.Piece[53] == PAWN)
            {
                //f pawn in place
                if (TheBoard.Color[54] == EMPTY || TheBoard.Piece[54] != PAWN)
                {
                    if (TheBoard.Color[46] == EMPTY || TheBoard.Piece[46] != PAWN)
                    {
                        if (TheBoard.Color[52] == EMPTY || TheBoard.Piece[52] != PAWN)
                        {
                            if (TheBoard.Color[44] == EMPTY || TheBoard.Piece[44] != PAWN)
                            {
                                //white f pawn backward
                                WhiteBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[54] == WHITE && TheBoard.Piece[54] == PAWN)
            {
                //g pawn in place
                if (TheBoard.Color[55] == EMPTY || TheBoard.Piece[55] != PAWN)
                {
                    if (TheBoard.Color[47] == EMPTY || TheBoard.Piece[47] != PAWN)
                    {
                        if (TheBoard.Color[53] == EMPTY || TheBoard.Piece[53] != PAWN)
                        {
                            if (TheBoard.Color[45] == EMPTY || TheBoard.Piece[45] != PAWN)
                            {
                                //white g pawn backward
                                WhiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[55] == WHITE && TheBoard.Piece[55] == PAWN)
            {
                //h pawn in place
                if (TheBoard.Color[54] == EMPTY || TheBoard.Piece[54] != PAWN)
                {
                    if (TheBoard.Color[46] == EMPTY || TheBoard.Piece[46] != PAWN)
                    {
                        //white h pawn backward
                        WhiteBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            //black
            if (TheBoard.Color[8] == BLACK && TheBoard.Piece[8] == PAWN)
            {
                if (TheBoard.Color[9] == EMPTY || TheBoard.Piece[9] != PAWN)
                {
                    if (TheBoard.Color[17] == EMPTY || TheBoard.Piece[17] != PAWN)
                    {
                        //black a pawn backward
                        BlackBWPScore += FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }

            if (TheBoard.Color[9] == BLACK && TheBoard.Piece[9] == PAWN)
            {
                //b pawn in place
                if (TheBoard.Color[10] == EMPTY || TheBoard.Piece[10] != PAWN)
                {
                    if (TheBoard.Color[18] == EMPTY || TheBoard.Piece[18] != PAWN)
                    {
                        if (TheBoard.Color[8] == EMPTY || TheBoard.Piece[8] != PAWN)
                        {
                            if (TheBoard.Color[16] == EMPTY || TheBoard.Piece[16] != PAWN)
                            {
                                //black b pawn backward
                                BlackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[10] == BLACK && TheBoard.Piece[10] == PAWN)
            {
                //c pawn in place
                if (TheBoard.Color[11] == EMPTY || TheBoard.Piece[11] != PAWN)
                {
                    if (TheBoard.Color[19] == EMPTY || TheBoard.Piece[19] != PAWN)
                    {
                        if (TheBoard.Color[9] == EMPTY || TheBoard.Piece[9] != PAWN)
                        {
                            if (TheBoard.Color[17] == EMPTY || TheBoard.Piece[17] != PAWN)
                            {
                                //black c pawn backward
                                BlackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[11] == BLACK && TheBoard.Piece[11] == PAWN)
            {
                //d pawn in place
                if (TheBoard.Color[12] == EMPTY || TheBoard.Piece[12] != PAWN)
                {
                    if (TheBoard.Color[20] == EMPTY || TheBoard.Piece[20] != PAWN)
                    {
                        if (TheBoard.Color[10] == EMPTY || TheBoard.Piece[10] != PAWN)
                        {
                            if (TheBoard.Color[18] == EMPTY || TheBoard.Piece[18] != PAWN)
                            {
                                //black d pawn backward
                                BlackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[12] == BLACK && TheBoard.Piece[12] == PAWN)
            {
                //e pawn in place
                if (TheBoard.Color[13] == EMPTY || TheBoard.Piece[13] != PAWN)
                {
                    if (TheBoard.Color[21] == EMPTY || TheBoard.Piece[21] != PAWN)
                    {
                        if (TheBoard.Color[11] == EMPTY || TheBoard.Piece[11] != PAWN)
                        {
                            if (TheBoard.Color[19] == EMPTY || TheBoard.Piece[19] != PAWN)
                            {
                                //black e pawn backward
                                BlackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[13] == BLACK && TheBoard.Piece[13] == PAWN)
            {
                //f pawn in place
                if (TheBoard.Color[14] == EMPTY || TheBoard.Piece[14] != PAWN)
                {
                    if (TheBoard.Color[22] == EMPTY || TheBoard.Piece[22] != PAWN)
                    {
                        if (TheBoard.Color[12] == EMPTY || TheBoard.Piece[12] != PAWN)
                        {
                            if (TheBoard.Color[20] == EMPTY || TheBoard.Piece[20] != PAWN)
                            {
                                //black f pawn backward
                                BlackBWPScore -= CENTER_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[14] == BLACK && TheBoard.Piece[14] == PAWN)
            {
                //g pawn in place
                if (TheBoard.Color[15] == EMPTY || TheBoard.Piece[15] != PAWN)
                {
                    if (TheBoard.Color[23] == EMPTY || TheBoard.Piece[23] != PAWN)
                    {
                        if (TheBoard.Color[13] == EMPTY || TheBoard.Piece[13] != PAWN)
                        {
                            if (TheBoard.Color[21] == EMPTY || TheBoard.Piece[21] != PAWN)
                            {
                                //black g pawn backward
                                BlackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                            }
                        }
                    }
                }
            }

            if (TheBoard.Color[15] == BLACK && TheBoard.Piece[15] == PAWN)
            {
                //h pawn in place
                if (TheBoard.Color[14] == EMPTY || TheBoard.Piece[14] != PAWN)
                {
                    if (TheBoard.Color[22] == EMPTY || TheBoard.Piece[22] != PAWN)
                    {
                        //black h pawn backward
                        BlackBWPScore -= FLANK_BACKWARD_PAWN_PENALTY;
                    }
                }
            }


        }

        public static void RewardRooksOnTheSeventh(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.WhiteRookOneSquare >= 8 && TheBoard.WhiteRookOneSquare <= 15)
            {
                WhiteScore += ROOK_ON_SEVENTH_BONUS;
            }

            if (TheBoard.WhiteRookTwoSquare >= 8 && TheBoard.WhiteRookTwoSquare <= 15)
            {
                WhiteScore += ROOK_ON_SEVENTH_BONUS;
            }

            if (TheBoard.BlackRookOneSquare >= 48 && TheBoard.BlackRookOneSquare <= 55)
            {
                BlackScore += ROOK_ON_SEVENTH_BONUS;
            }

            if (TheBoard.BlackRookTwoSquare >= 48 && TheBoard.BlackRookTwoSquare <= 55)
            {
                BlackScore += ROOK_ON_SEVENTH_BONUS;
            }

        }


        private static void RewardPawnBishopSynergy(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.BlackHasDarkSquaredBishop && !TheBoard.BlackHasLightSquaredBishop)
            {
                BlackScore -= TheBoard.BlackPawnsOnDarkSquares * BISHOP_PAWN_COLOR_PENALTY;
            }
            if (TheBoard.BlackHasLightSquaredBishop && !TheBoard.BlackHasDarkSquaredBishop)
            {
                BlackScore -= TheBoard.BlackPawnsOnLightSquares * BISHOP_PAWN_COLOR_PENALTY;
            }

            if (TheBoard.WhiteHasDarkSquaredBishop && !TheBoard.WhiteHasLightSquaredBishop)
            {
                WhiteScore -= TheBoard.WhitePawnsOnDarkSquares * BISHOP_PAWN_COLOR_PENALTY;
            }
            if (TheBoard.WhiteHasLightSquaredBishop && !TheBoard.WhiteHasDarkSquaredBishop)
            {
                WhiteScore -= TheBoard.WhitePawnsOnLightSquares * BISHOP_PAWN_COLOR_PENALTY;
            }


        }


        private static void EncouragePawnsOnOppositeSideToKingToAdvance(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.WhiteKingSquare % 8 >= 5)
            {
                if (TheBoard.Color[48] == WHITE && TheBoard.Piece[48] == PAWN)
                {
                    WhiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[49] == WHITE && TheBoard.Piece[49] == PAWN)
                {
                    WhiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[50] == WHITE && TheBoard.Piece[50] == PAWN)
                {
                    WhiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }
            else if (TheBoard.WhiteKingSquare % 8 <= 2)
            {
                if (TheBoard.Color[55] == WHITE && TheBoard.Piece[55] == PAWN)
                {
                    WhiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[54] == WHITE && TheBoard.Piece[54] == PAWN)
                {
                    WhiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[53] == WHITE && TheBoard.Piece[53] == PAWN)
                {
                    WhiteScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }

            if (TheBoard.BlackKingSquare % 8 >= 5)
            {
                if (TheBoard.Color[8] == WHITE && TheBoard.Piece[8] == PAWN)
                {
                    BlackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[9] == WHITE && TheBoard.Piece[9] == PAWN)
                {
                    BlackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[10] == WHITE && TheBoard.Piece[10] == PAWN)
                {
                    BlackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }
            else if (TheBoard.BlackKingSquare % 8 <= 2)
            {
                if (TheBoard.Color[15] == WHITE && TheBoard.Piece[15] == PAWN)
                {
                    BlackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[14] == WHITE && TheBoard.Piece[14] == PAWN)
                {
                    BlackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
                if (TheBoard.Color[13] == WHITE && TheBoard.Piece[13] == PAWN)
                {
                    BlackScore -= PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY;
                }
            }

        }


        private static void RewardActiveBishop(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.WhiteHasDarkSquaredBishop)
            {
                if (TheBoard.SameDiagonal[TheBoard.WhiteDarkBishopSquare][TheBoard.BlackKingSquare])
                {
                    WhiteScore += BISHOP_ATTACKS_KING_BONUS;
                }
                if (TheBoard.BlackQueenSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteDarkBishopSquare][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (TheBoard.BlackRookOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteDarkBishopSquare][TheBoard.BlackRookOneSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.BlackRookTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteDarkBishopSquare][TheBoard.BlackRookTwoSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.BlackKnightOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteDarkBishopSquare][TheBoard.BlackKnightOneSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (TheBoard.BlackKnightTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteDarkBishopSquare][TheBoard.BlackKnightTwoSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }

            if (TheBoard.WhiteHasLightSquaredBishop)
            {
                if (TheBoard.SameDiagonal[TheBoard.WhiteLightBishopSquare][TheBoard.BlackKingSquare])
                {
                    WhiteScore += BISHOP_ATTACKS_KING_BONUS;
                }
                if (TheBoard.BlackQueenSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteLightBishopSquare][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (TheBoard.BlackRookOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteLightBishopSquare][TheBoard.BlackRookOneSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.BlackRookTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteLightBishopSquare][TheBoard.BlackRookTwoSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.BlackKnightOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteLightBishopSquare][TheBoard.BlackKnightOneSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (TheBoard.BlackKnightTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.WhiteLightBishopSquare][TheBoard.BlackKnightTwoSquare])
                    {
                        WhiteScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }



            if (TheBoard.BlackHasDarkSquaredBishop)
            {
                if (TheBoard.SameDiagonal[TheBoard.BlackDarkBishopSquare][TheBoard.WhiteKingSquare])
                {
                    BlackScore += BISHOP_ATTACKS_KING_BONUS;
                }
                if (TheBoard.WhiteQueenSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackDarkBishopSquare][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (TheBoard.WhiteRookOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackDarkBishopSquare][TheBoard.WhiteRookOneSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.WhiteRookTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackDarkBishopSquare][TheBoard.WhiteRookTwoSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.WhiteKnightOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackDarkBishopSquare][TheBoard.WhiteKnightOneSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (TheBoard.WhiteKnightTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackDarkBishopSquare][TheBoard.WhiteKnightTwoSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }

            if (TheBoard.BlackHasLightSquaredBishop)
            {
                if (TheBoard.SameDiagonal[TheBoard.BlackLightBishopSquare][TheBoard.WhiteKingSquare])
                {
                    BlackScore += BISHOP_ATTACKS_KING_BONUS;
                }
                if (TheBoard.WhiteQueenSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackLightBishopSquare][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_QUEEN_BONUS;
                    }
                }
                if (TheBoard.WhiteRookOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackLightBishopSquare][TheBoard.WhiteRookOneSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.WhiteRookTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackLightBishopSquare][TheBoard.WhiteRookTwoSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_ROOK_BONUS;
                    }
                }
                if (TheBoard.WhiteKnightOneSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackLightBishopSquare][TheBoard.WhiteKnightOneSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
                if (TheBoard.WhiteKnightTwoSquare != 255)
                {
                    if (TheBoard.SameDiagonal[TheBoard.BlackLightBishopSquare][TheBoard.WhiteKnightTwoSquare])
                    {
                        BlackScore += BISHOP_ATTACKS_KNIGHT_BONUS;
                    }
                }
            }



        }


        public static void RewardPassedPawns(ref Board TheBoard, out int WhitePPScore, out int BlackPPScore)
        {

            WhitePPScore = 0; BlackPPScore = 0;
            bool[] WhiteFilePassed = new bool[8];
            bool[] BlackFilePassed = new bool[8];

            for (int NN = 0; NN <= 7; NN++)
            {
                if (TheBoard.WhiteFilePawns[NN] >= 1 && TheBoard.BlackFilePawns[NN] == 0)
                {
                    int PawnSquare = -1; bool IsPassed = true;
                    for (int PP = 0; PP <= 7; PP++)
                    {
                        if (TheBoard.WhitePawnSquares[PP] != -1 && TheBoard.WhitePawnSquares[PP] % 8 == NN)
                        {
                            if (PawnSquare == -1)
                            {
                                PawnSquare = TheBoard.WhitePawnSquares[PP];
                            }
                            else if (TheBoard.WhitePawnSquares[PP] < PawnSquare)
                            {
                                PawnSquare = TheBoard.WhitePawnSquares[PP];
                            }
                        }
                    }
                    for (int SQ = 0; SQ < TheBoard.WhitePassedPawnLookUps[PawnSquare].Length; SQ++)
                    {
                        if (TheBoard.Piece[TheBoard.WhitePassedPawnLookUps[PawnSquare][SQ]] == PAWN)
                        {
                            IsPassed = false;
                            break;
                        }
                    }
                    if (IsPassed)
                    {
                        WhiteFilePassed[NN] = true;
                        WhitePPScore += PASSED_PAWN_BONUS;
                        if (PawnSquare < 15)
                        {
                            WhitePPScore += PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (PawnSquare >= 16 && PawnSquare < 24)
                        {
                            WhitePPScore += PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (PawnSquare >= 24 && PawnSquare < 32)
                        {
                            WhitePPScore += PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS;
                        }
                        if (NN > 0)
                        {
                            if (WhiteFilePassed[NN - 1])
                            {
                                WhitePPScore += CONNECTED_PASSED_PAWN_BONUS;
                            }
                        }
                    }
                }
                if (TheBoard.BlackFilePawns[NN] >= 1 && TheBoard.WhiteFilePawns[NN] == 0)
                {
                    int PawnSquare = 255; bool IsPassed = true;
                    for (int PP = 0; PP <= 7; PP++)
                    {
                        if (TheBoard.BlackPawnSquares[PP] != -1 && TheBoard.BlackPawnSquares[PP] % 8 == NN)
                        {
                            if (PawnSquare == 255)
                            {
                                PawnSquare = TheBoard.BlackPawnSquares[PP];
                            }
                            else if (TheBoard.BlackPawnSquares[PP] > PawnSquare)
                            {
                                PawnSquare = TheBoard.BlackPawnSquares[PP];
                            }
                        }
                    }
                    for (int SQ = 0; SQ < TheBoard.BlackPassedPawnLookUps[PawnSquare].Length; SQ++)
                    {
                        if (TheBoard.Piece[TheBoard.BlackPassedPawnLookUps[PawnSquare][SQ]] == PAWN)
                        {
                            IsPassed = false;
                            break;
                        }
                    }
                    if (IsPassed)
                    {
                        BlackFilePassed[NN] = true;
                        BlackPPScore += PASSED_PAWN_BONUS;
                        if (PawnSquare >= 48)
                        {
                            BlackPPScore += PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (PawnSquare >= 40 && PawnSquare < 48)
                        {
                            BlackPPScore += PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS;
                        }
                        else if (PawnSquare >= 32 && PawnSquare < 40)
                        {
                            BlackPPScore += PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS;
                        }
                        if (NN > 0)
                        {
                            if (BlackFilePassed[NN - 1])
                            {
                                BlackPPScore += CONNECTED_PASSED_PAWN_BONUS;
                            }
                        }
                    }
                }
            }

        }


        private static void PenaliseBishopPawnWeaknesses(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {


            if (TheBoard.Color[9] == EMPTY)
            {
                if (!TheBoard.BlackHasLightSquaredBishop)
                {
                    //Black has not got his b pawn in place and does not have a light squared bishop
                    BlackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (TheBoard.WhiteHasLightSquaredBishop)
                    {
                        //Worse because white DOES have his light squared bishop
                        BlackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (TheBoard.Piece[2] == KING || TheBoard.Piece[1] == KING || TheBoard.Piece[0] == KING)
                    {
                        //Worse because his king is on that side
                        BlackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((TheBoard.Piece[0] == BISHOP && TheBoard.Color[0] == BLACK) ||
                        (TheBoard.Piece[2] == BISHOP && TheBoard.Color[2] == BLACK) ||
                        (TheBoard.Piece[16] == BISHOP && TheBoard.Color[16] == BLACK))
                    {
                        //Black has his bishop well placed for this advance
                        BlackScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        BlackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (TheBoard.WhiteHasLightSquaredBishop)
                        {
                            //Worse because white DOES have his light squared bishop
                            BlackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (TheBoard.Piece[2] == KING || TheBoard.Piece[1] == KING || TheBoard.Piece[0] == KING)
                        {
                            //Worse because his king is on that side
                            BlackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }

            if (TheBoard.Color[14] == EMPTY)
            {
                if (!TheBoard.BlackHasDarkSquaredBishop)
                {
                    //Black has not got his g pawn in place and does not have a light squared bishop
                    BlackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (TheBoard.WhiteHasDarkSquaredBishop)
                    {
                        //Worse because white DOES have his light squared bishop
                        BlackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (TheBoard.Piece[7] == KING || TheBoard.Piece[6] == KING || TheBoard.Piece[5] == KING)
                    {
                        //Worse because his king is on that side
                        BlackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((TheBoard.Piece[7] == BISHOP && TheBoard.Color[7] == BLACK) ||
                        (TheBoard.Piece[5] == BISHOP && TheBoard.Color[5] == BLACK) ||
                        (TheBoard.Piece[23] == BISHOP && TheBoard.Color[23] == BLACK))
                    {
                        //Black has his bishop well placed for this advance
                        BlackScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        BlackScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (TheBoard.WhiteHasDarkSquaredBishop)
                        {
                            //Worse because white DOES have his light squared bishop
                            BlackScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (TheBoard.Piece[7] == KING || TheBoard.Piece[6] == KING || TheBoard.Piece[5] == KING)
                        {
                            //Worse because his king is on that side
                            BlackScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }





            if (TheBoard.Color[49] == EMPTY)
            {
                if (!TheBoard.WhiteHasDarkSquaredBishop)
                {
                    //White has not got his b pawn in place and does not have a dark squared bishop
                    WhiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (TheBoard.BlackHasDarkSquaredBishop)
                    {
                        //Worse because white DOES have his dark squared bishop
                        WhiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (TheBoard.Piece[56] == KING || TheBoard.Piece[57] == KING || TheBoard.Piece[58] == KING)
                    {
                        //Worse because his king is on that side
                        WhiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((TheBoard.Piece[56] == BISHOP && TheBoard.Color[56] == BLACK) ||
                        (TheBoard.Piece[40] == BISHOP && TheBoard.Color[40] == BLACK) ||
                        (TheBoard.Piece[58] == BISHOP && TheBoard.Color[58] == BLACK))
                    {
                        //White has his bishop well placed for this advance
                        WhiteScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        WhiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (TheBoard.BlackHasDarkSquaredBishop)
                        {
                            //Worse because black DOES have his dark squared bishop
                            WhiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (TheBoard.Piece[56] == KING || TheBoard.Piece[57] == KING || TheBoard.Piece[58] == KING)
                        {
                            //Worse because his king is on that side
                            WhiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }

            if (TheBoard.Color[54] == EMPTY)
            {
                if (!TheBoard.WhiteHasLightSquaredBishop)
                {
                    //White has not got his g pawn in place and does not have a light squared bishop
                    WhiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                    if (TheBoard.BlackHasLightSquaredBishop)
                    {
                        //Worse because white DOES have his light squared bishop
                        WhiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                    }
                    if (TheBoard.Piece[61] == KING || TheBoard.Piece[62] == KING || TheBoard.Piece[63] == KING)
                    {
                        //Worse because his king is on that side
                        WhiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                    }
                }
                else
                {
                    if ((TheBoard.Piece[63] == BISHOP && TheBoard.Color[63] == BLACK) ||
                        (TheBoard.Piece[61] == BISHOP && TheBoard.Color[61] == BLACK) ||
                        (TheBoard.Piece[47] == BISHOP && TheBoard.Color[47] == BLACK))
                    {
                        //Black has his bishop well placed for this advance
                        WhiteScore += FIANCETTO_IS_GOOD_BONUS;
                    }
                    else
                    {
                        //If the black bishop is not well placed for it, treat it as if as bad as not having it
                        WhiteScore -= FIANCETTO_WITHOUT_BISHOP_PENALTY;
                        if (TheBoard.BlackHasLightSquaredBishop)
                        {
                            //Worse because white DOES have his light squared bishop
                            WhiteScore -= FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY;
                        }
                        if (TheBoard.Piece[61] == KING || TheBoard.Piece[62] == KING || TheBoard.Piece[63] == KING)
                        {
                            //Worse because his king is on that side
                            WhiteScore -= FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY;
                        }
                    }
                }
            }

        }


        private static void PenaliseTrappedBishop(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.Piece[8] == BISHOP && TheBoard.Color[8] == WHITE)
            {
                if (TheBoard.Piece[10] == PAWN && TheBoard.Piece[10] == BLACK)
                {
                    if ((TheBoard.Piece[9] == PAWN && TheBoard.Color[9] == BLACK))
                    {

                        //White bishop on a7 can be trapped by b6 next move
                        WhiteScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (TheBoard.Piece[17] == PAWN && TheBoard.Color[17] == BLACK)
                    {

                        //White bishop on a7 already trapped by b6
                        WhiteScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

            if (TheBoard.Piece[15] == BISHOP && TheBoard.Color[15] == WHITE)
            {
                if (TheBoard.Piece[13] == PAWN && TheBoard.Piece[13] == BLACK)
                {
                    if ((TheBoard.Piece[14] == PAWN && TheBoard.Color[14] == BLACK))
                    {

                        //White bishop on h7 can be trapped by g6 next move
                        WhiteScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (TheBoard.Piece[22] == PAWN && TheBoard.Color[22] == BLACK)
                    {

                        //White bishop on h7 already trapped by g6
                        WhiteScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

            if (TheBoard.Piece[48] == BISHOP && TheBoard.Color[48] == BLACK)
            {
                if (TheBoard.Piece[50] == PAWN && TheBoard.Piece[50] == WHITE)
                {
                    if ((TheBoard.Piece[49] == PAWN && TheBoard.Color[49] == WHITE))
                    {

                        //Black bishop on a2 can be trapped by b3 next move
                        BlackScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (TheBoard.Piece[41] == PAWN && TheBoard.Color[41] == WHITE)
                    {

                        //Black bishop on a2 already trapped by b3
                        BlackScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

            if (TheBoard.Piece[55] == BISHOP && TheBoard.Color[55] == BLACK)
            {
                if (TheBoard.Piece[53] == PAWN && TheBoard.Piece[53] == WHITE)
                {
                    if ((TheBoard.Piece[52] == PAWN && TheBoard.Color[52] == WHITE))
                    {

                        //Black bishop on h2 can be trapped by g3 next move
                        BlackScore -= BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                    else if (TheBoard.Piece[46] == PAWN && TheBoard.Color[46] == WHITE)
                    {

                        //Black bishop on h2 already trapped by g3
                        BlackScore -= BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY;

                    }
                }
            }

        }


        private static void RewardRooksOnOpenFiles(ref Board TheBoard, int[] WhiteFilePawns, int[] BlackFilePawns, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.WhiteRookOneSquare != 255 && WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
            {
                //White rook one on semi open file
                WhiteScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (BlackFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                {
                    //White rook one on fully open file
                    WhiteScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

            if (TheBoard.WhiteRookTwoSquare != 255 && WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
            {
                //White rook two on open file
                WhiteScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (BlackFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                {
                    //White rook two on fully open file
                    WhiteScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

            if (TheBoard.BlackRookOneSquare != 255 && BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
            {
                //Black rook one on open file
                BlackScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (WhiteFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                {
                    //Black rook one on fully open file
                    BlackScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

            if (TheBoard.BlackRookTwoSquare != 255 && BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
            {
                //Black rook two on open file
                BlackScore += ROOK_ON_SEMI_OPEN_FILE_BONUS;
                if (WhiteFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                {
                    //Black rook two on fully open file
                    BlackScore += ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS;
                }
            }

        }


        private static void PenaliseIsolatedPawns(ref Board TheBoard, out int WhiteIsoPawnScore, out int BlackIsoPawnScore)
        {

            WhiteIsoPawnScore = 0; BlackIsoPawnScore = 0;

            if (TheBoard.WhiteFilePawns[0] == 1 && TheBoard.WhiteFilePawns[1] == 0)
            {
                //white a pawn isolated
                WhiteIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }
            if (TheBoard.WhiteFilePawns[7] == 1 && TheBoard.WhiteFilePawns[6] == 0)
            {
                //white h pawn isolated
                WhiteIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }
            if (TheBoard.BlackFilePawns[0] == 1 && TheBoard.BlackFilePawns[1] == 0)
            {
                //black a pawn isolated
                BlackIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }
            if (TheBoard.BlackFilePawns[7] == 1 && TheBoard.BlackFilePawns[6] == 0)
            {
                //black h pawn isolated
                BlackIsoPawnScore -= EDGE_PAWN_ISOLATED_PENALTY;
            }

            for (int NN = 1; NN <= 6; NN++)
            {
                if (TheBoard.WhiteFilePawns[NN] == 1 && TheBoard.WhiteFilePawns[NN - 1] == 0 && TheBoard.WhiteFilePawns[NN + 1] == 0)
                {
                    //a more central white pawn is isolated
                    WhiteIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY;
                }
                if (TheBoard.BlackFilePawns[NN] == 1 && TheBoard.BlackFilePawns[NN - 1] == 0 && TheBoard.BlackFilePawns[NN + 1] == 0)
                {
                    //a more central black pawn is isolated
                    BlackIsoPawnScore -= CENTER_PAWN_ISOLATED_PENALTY;
                }
            }

        }


        private static void PenaliseDoubledAndTripledPawns(ref Board TheBoard, out int WhiteDblPawnScore, out int BlackDblPawnScore)
        {

            WhiteDblPawnScore = 0; BlackDblPawnScore = 0;

            for (int NN = 0; NN <= 7; NN++)
            {
                if (TheBoard.WhiteFilePawns[NN] == 2)
                {
                    WhiteDblPawnScore -= DOUBLED_PAWN_PENALTY;
                    if (TheBoard.BlackFilePawns[NN] > 0)
                    {
                        WhiteDblPawnScore -= DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
                else if (TheBoard.WhiteFilePawns[NN] > 2)
                {
                    WhiteDblPawnScore -= TRIPLED_PAWN_PENALTY;
                    if (TheBoard.BlackFilePawns[NN] > 0)
                    {
                        WhiteDblPawnScore -= TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
                if (TheBoard.BlackFilePawns[NN] == 2)
                {
                    BlackDblPawnScore -= DOUBLED_PAWN_PENALTY;
                    if (TheBoard.WhiteFilePawns[NN] > 0)
                    {
                        BlackDblPawnScore -= DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
                else if (TheBoard.BlackFilePawns[NN] > 2)
                {
                    BlackDblPawnScore -= TRIPLED_PAWN_PENALTY;
                    if (TheBoard.WhiteFilePawns[NN] > 0)
                    {
                        BlackDblPawnScore -= TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY;
                    }
                }
            }

        }


        private static void RewardKingSafety(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {


            //###TODO: Knights?

            if (TheBoard.WhiteKingSquare < 48)
            {
                if (TheBoard.BlackQueenSquare != 255)
                {
                    WhiteScore -= KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY;
                }
                else
                {
                    WhiteScore -= KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY;
                }
            }
            else if (TheBoard.WhiteKingSquare < 56 && TheBoard.WhiteKingSquare >= 48)
            {
                WhiteScore -= KING_STEPPED_UP_EARLY_PENALTY;
                if (TheBoard.Color[TheBoard.WhiteKingSquare - 8] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 8] == PAWN)
                {
                    WhiteScore += KING_STEPPED_UP_PAWN_SHIELD_MITIGATION;
                }
                else if (TheBoard.Color[TheBoard.WhiteKingSquare - 16] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 16] == PAWN)
                {
                    WhiteScore += KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION;
                }
                if (TheBoard.WhiteKingSquare != 48 && TheBoard.Color[TheBoard.WhiteKingSquare - 9] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 9] == PAWN)
                {
                    WhiteScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (TheBoard.WhiteKingSquare != 48 && TheBoard.Color[TheBoard.WhiteKingSquare - 1] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 1] == PAWN)
                {
                    WhiteScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
                if (TheBoard.WhiteKingSquare != 55 && TheBoard.Color[TheBoard.WhiteKingSquare - 7] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 7] == PAWN)
                {
                    WhiteScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (TheBoard.WhiteKingSquare != 55 && TheBoard.Color[TheBoard.WhiteKingSquare + 1] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare + 1] == PAWN)
                {
                    WhiteScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
            }
            else
            {
                if (TheBoard.Color[TheBoard.WhiteKingSquare - 8] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 8] == PAWN)
                {
                    WhiteScore += KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS;
                    if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 8][TheBoard.BlackDarkBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 8][TheBoard.BlackLightBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackRookOneSquare != 255 && TheBoard.WhiteKingSquare % 8 == TheBoard.BlackRookOneSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackRookTwoSquare != 255 && TheBoard.WhiteKingSquare % 8 == TheBoard.BlackRookTwoSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 8][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.WhiteKingSquare % 8 == TheBoard.BlackQueenSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackQueenSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][TheBoard.WhiteKingSquare - 8])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.BlackKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][TheBoard.WhiteKingSquare - 8])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY;
                    if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 8][TheBoard.BlackDarkBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 8][TheBoard.BlackLightBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackRookOneSquare != 255 && TheBoard.WhiteKingSquare % 8 == TheBoard.BlackRookOneSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackRookTwoSquare != 255 && TheBoard.WhiteKingSquare % 8 == TheBoard.BlackRookTwoSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 8][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.WhiteKingSquare % 8 == TheBoard.BlackQueenSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackQueenSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][TheBoard.WhiteKingSquare - 8])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.BlackKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][TheBoard.WhiteKingSquare - 8])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (TheBoard.WhiteKingSquare != 56 && TheBoard.Color[TheBoard.WhiteKingSquare - 9] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 9] == PAWN)
                {
                    WhiteScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 9][TheBoard.BlackDarkBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 9][TheBoard.BlackLightBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackRookOneSquare != 255 && TheBoard.WhiteKingSquare % 8 - 1 == TheBoard.BlackRookOneSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackRookTwoSquare != 255 && TheBoard.WhiteKingSquare % 8 - 1 == TheBoard.BlackRookTwoSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 9][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.WhiteKingSquare % 8 - 1 == TheBoard.BlackQueenSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackQueenSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][TheBoard.WhiteKingSquare - 9])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.BlackKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][TheBoard.WhiteKingSquare - 9])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 9][TheBoard.BlackDarkBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 9][TheBoard.BlackLightBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackRookOneSquare != 255 && TheBoard.WhiteKingSquare % 8 - 1 == TheBoard.BlackRookOneSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackRookTwoSquare != 255 && TheBoard.WhiteKingSquare % 8 - 1 == TheBoard.BlackRookTwoSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 9][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.WhiteKingSquare % 8 - 1 == TheBoard.BlackQueenSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackQueenSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][TheBoard.WhiteKingSquare - 9])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.BlackKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][TheBoard.WhiteKingSquare - 9])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (TheBoard.WhiteKingSquare != 63 && TheBoard.Color[TheBoard.WhiteKingSquare - 7] == WHITE && TheBoard.Piece[TheBoard.WhiteKingSquare - 7] == PAWN)
                {
                    WhiteScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 7][TheBoard.BlackDarkBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 7][TheBoard.BlackLightBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackRookOneSquare != 255 && TheBoard.WhiteKingSquare % 8 + 1 == TheBoard.BlackRookOneSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackRookTwoSquare != 255 && TheBoard.WhiteKingSquare % 8 + 1 == TheBoard.BlackRookTwoSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 7][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.WhiteKingSquare % 8 + 1 == TheBoard.BlackQueenSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackQueenSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][TheBoard.WhiteKingSquare - 7])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.BlackKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][TheBoard.WhiteKingSquare - 7])
                    {
                        WhiteScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (TheBoard.BlackHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 7][TheBoard.BlackDarkBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 7][TheBoard.BlackLightBishopSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.BlackRookOneSquare != 255 && TheBoard.WhiteKingSquare % 8 + 1 == TheBoard.BlackRookOneSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookOneSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackRookTwoSquare != 255 && TheBoard.WhiteKingSquare % 8 + 1 == TheBoard.BlackRookTwoSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackRookTwoSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.WhiteKingSquare - 7][TheBoard.BlackQueenSquare])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.BlackQueenSquare != 255 && TheBoard.WhiteKingSquare % 8 + 1 == TheBoard.BlackQueenSquare % 8)
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.BlackFilePawns[TheBoard.BlackQueenSquare % 8] == 0)
                        {
                            WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.BlackKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightOneSquare][TheBoard.WhiteKingSquare - 7])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.BlackKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.BlackKnightTwoSquare][TheBoard.WhiteKingSquare - 7])
                    {
                        WhiteScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
            }

            if (TheBoard.BlackKingSquare > 15)
            {
                if (TheBoard.WhiteQueenSquare != 255)
                {
                    BlackScore -= KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY;
                }
                else
                {
                    BlackScore -= KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY;
                }
            }
            else if (TheBoard.BlackKingSquare > 7 && TheBoard.BlackKingSquare <= 15)
            {
                BlackScore -= KING_STEPPED_UP_EARLY_PENALTY;
                if (TheBoard.Color[TheBoard.BlackKingSquare + 8] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 8] == PAWN)
                {
                    BlackScore += KING_STEPPED_UP_PAWN_SHIELD_MITIGATION;
                }
                else if (TheBoard.Color[TheBoard.BlackKingSquare + 16] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 16] == PAWN)
                {
                    BlackScore += KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION;
                }
                if (TheBoard.BlackKingSquare != 15 && TheBoard.Color[TheBoard.BlackKingSquare + 9] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 9] == PAWN)
                {
                    BlackScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (TheBoard.BlackKingSquare != 15 && TheBoard.Color[TheBoard.BlackKingSquare + 1] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 1] == PAWN)
                {
                    BlackScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
                if (TheBoard.BlackKingSquare != 8 && TheBoard.Color[TheBoard.BlackKingSquare + 7] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 7] == PAWN)
                {
                    BlackScore += KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION;
                }
                if (TheBoard.BlackKingSquare != 8 && TheBoard.Color[TheBoard.BlackKingSquare - 1] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare - 1] == PAWN)
                {
                    BlackScore += KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION;
                }
            }
            else
            {
                if (TheBoard.Color[TheBoard.BlackKingSquare + 8] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 8] == PAWN)
                {
                    BlackScore += KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS;
                    if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 8][TheBoard.WhiteDarkBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 8][TheBoard.WhiteLightBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.BlackKingSquare % 8 == TheBoard.WhiteRookOneSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteRookTwoSquare != 255 && TheBoard.BlackKingSquare % 8 == TheBoard.WhiteRookTwoSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 8][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.BlackKingSquare % 8 == TheBoard.WhiteQueenSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteQueenSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][TheBoard.BlackKingSquare + 8])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.WhiteKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][TheBoard.BlackKingSquare + 8])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY;
                    if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 8][TheBoard.WhiteDarkBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 8][TheBoard.WhiteLightBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.BlackKingSquare % 8 == TheBoard.WhiteRookOneSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteRookTwoSquare != 255 && TheBoard.BlackKingSquare % 8 == TheBoard.WhiteRookTwoSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 8][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.BlackKingSquare % 8 == TheBoard.WhiteQueenSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteQueenSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][TheBoard.BlackKingSquare + 8])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.WhiteKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][TheBoard.BlackKingSquare + 8])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (TheBoard.BlackKingSquare != 7 && TheBoard.Color[TheBoard.BlackKingSquare + 9] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 9] == PAWN)
                {
                    BlackScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 9][TheBoard.WhiteDarkBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 9][TheBoard.WhiteLightBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.BlackKingSquare % 8 + 1 == TheBoard.WhiteRookOneSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteRookTwoSquare != 255 && TheBoard.BlackKingSquare % 8 + 1 == TheBoard.WhiteRookTwoSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 9][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.BlackKingSquare % 8 + 1 == TheBoard.WhiteQueenSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteQueenSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][TheBoard.BlackKingSquare + 9])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.WhiteKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][TheBoard.BlackKingSquare + 9])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 9][TheBoard.WhiteDarkBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 9][TheBoard.WhiteLightBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.BlackKingSquare % 8 + 1 == TheBoard.WhiteRookOneSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteRookTwoSquare != 255 && TheBoard.BlackKingSquare % 8 + 1 == TheBoard.WhiteRookTwoSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 9][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.BlackKingSquare % 8 + 1 == TheBoard.WhiteQueenSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteQueenSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][TheBoard.BlackKingSquare + 9])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.WhiteKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][TheBoard.BlackKingSquare + 9])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                if (TheBoard.BlackKingSquare != 0 && TheBoard.Color[TheBoard.BlackKingSquare + 7] == BLACK && TheBoard.Piece[TheBoard.BlackKingSquare + 7] == PAWN)
                {
                    BlackScore += KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS;
                    if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 7][TheBoard.WhiteDarkBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 7][TheBoard.WhiteLightBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.BlackKingSquare % 8 - 1 == TheBoard.WhiteRookOneSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteRookTwoSquare != 255 && TheBoard.BlackKingSquare % 8 - 1 == TheBoard.WhiteRookTwoSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 7][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.BlackKingSquare % 8 - 1 == TheBoard.WhiteQueenSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteQueenSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][TheBoard.BlackKingSquare + 7])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.WhiteKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][TheBoard.BlackKingSquare + 7])
                    {
                        BlackScore -= KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
                else
                {
                    BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY;
                    if (TheBoard.WhiteHasDarkSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 7][TheBoard.WhiteDarkBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteHasLightSquaredBishop && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 7][TheBoard.WhiteLightBishopSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY;
                    }
                    if (TheBoard.WhiteRookOneSquare != 255 && TheBoard.BlackKingSquare % 8 - 1 == TheBoard.WhiteRookOneSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookOneSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteRookTwoSquare != 255 && TheBoard.BlackKingSquare % 8 - 1 == TheBoard.WhiteRookTwoSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteRookTwoSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.SameDiagonal[TheBoard.BlackKingSquare + 7][TheBoard.WhiteQueenSquare])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY;
                    }
                    if (TheBoard.WhiteQueenSquare != 255 && TheBoard.BlackKingSquare % 8 - 1 == TheBoard.WhiteQueenSquare % 8)
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY;
                        if (TheBoard.WhiteFilePawns[TheBoard.WhiteQueenSquare % 8] == 0)
                        {
                            BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY;
                        }
                    }
                    if (TheBoard.WhiteKnightOneSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightOneSquare][TheBoard.BlackKingSquare + 7])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                    if (TheBoard.WhiteKnightTwoSquare != 255 && TheBoard.KnightDestinations[TheBoard.WhiteKnightTwoSquare][TheBoard.BlackKingSquare + 7])
                    {
                        BlackScore -= KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY;
                    }
                }
            }

        }


        private static void RewardBeingCastled(ref Board TheBoard, ref int WhiteScore, ref int BlackScore)
        {

            if (TheBoard.Color[62] == WHITE && TheBoard.Piece[62] == KING && TheBoard.Color[63] == EMPTY)
            {
                WhiteScore += KSIDE_CASTLE_BONUS;
            }
            else if (TheBoard.Color[63] == WHITE && TheBoard.Piece[63] == KING)
            {
                WhiteScore += KSIDE_CASTLE_BONUS;
            }

            if (TheBoard.Color[58] == WHITE && TheBoard.Piece[58] == KING && TheBoard.Color[56] == EMPTY && TheBoard.Color[57] == EMPTY)
            {
                WhiteScore += QSIDE_CASTLE_BONUS;
            }
            else if (TheBoard.Color[57] == WHITE && TheBoard.Piece[57] == KING && TheBoard.Color[56] == EMPTY)
            {
                WhiteScore += QSIDE_CASTLE_BONUS;
            }
            else if (TheBoard.Color[56] == WHITE && TheBoard.Piece[56] == KING)
            {
                WhiteScore += QSIDE_CASTLE_BONUS;
            }

            if (TheBoard.Color[6] == BLACK && TheBoard.Piece[6] == KING && TheBoard.Color[7] == EMPTY)
            {
                BlackScore += KSIDE_CASTLE_BONUS;
            }
            else if (TheBoard.Color[7] == BLACK && TheBoard.Piece[7] == KING)
            {
                BlackScore += KSIDE_CASTLE_BONUS;
            }

            if (TheBoard.Color[2] == BLACK && TheBoard.Piece[2] == KING && TheBoard.Color[1] == EMPTY && TheBoard.Color[0] == EMPTY)
            {
                BlackScore += QSIDE_CASTLE_BONUS;
            }
            else if (TheBoard.Color[1] == BLACK && TheBoard.Piece[1] == KING && TheBoard.Color[0] == EMPTY)
            {
                BlackScore += QSIDE_CASTLE_BONUS;
            }
            else if (TheBoard.Color[0] == BLACK && TheBoard.Piece[0] == KING)
            {
                BlackScore += QSIDE_CASTLE_BONUS;
            }

            if (TheBoard.Color[56] == WHITE && TheBoard.Piece[56] == ROOK)
            {
                if (TheBoard.WhiteKingSquare == 60)
                {
                    WhiteScore += QSIDE_CASTLE_RIGHTS;
                }
            }

            if (TheBoard.Color[63] == WHITE && TheBoard.Piece[63] == ROOK)
            {
                if (TheBoard.WhiteKingSquare == 60)
                {
                    WhiteScore += KSIDE_CASTLE_RIGHTS;
                }
            }

            if (TheBoard.Color[0] == BLACK && TheBoard.Piece[0] == ROOK)
            {
                if (TheBoard.BlackKingSquare == 4)
                {
                    BlackScore += QSIDE_CASTLE_RIGHTS;
                }
            }

            if (TheBoard.Color[7] == BLACK && TheBoard.Piece[7] == ROOK)
            {
                if (TheBoard.BlackKingSquare == 4)
                {
                    BlackScore += KSIDE_CASTLE_RIGHTS;
                }
            }

        }

    }
}
