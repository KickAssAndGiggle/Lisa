using static Lisa.Globals;
namespace Lisa
{
    //Breaks a position's static eval into its terms for the EPD and MultiFen output files. Only the testers call this.
    //It calls the same term functions as ScorePosition, into the same halves, then checks its total against
    //ScorePosition itself, so if the two ever drift apart the output says so.
    public static partial class Scorer
    {

        public static List<string> EvalBreakdownLines(string fen)
        {
            Board theBoard = new();
            theBoard.InitialiseFromFEN(fen);

            int phase = (theBoard.GamePhase * 256 + 12) / 24;
            int whiteOpening = 0;
            int whiteEndgame = 0;
            int blackOpening = 0;
            int blackEndgame = 0;

            List<string> lines = new();
            lines.Add("Static eval breakdown:");
            lines.Add("Phase " + phase.ToString() + " (GamePhase " + theBoard.GamePhase.ToString() +
                "): opening terms x" + (256 - phase).ToString() + "/256, endgame terms x" + phase.ToString() + "/256");
            lines.Add(string.Format("  {0,-48}{1,-10}{2,8}{3,8}{4,8}{5,10}", "Term", "Half", "White", "Black", "W-B",
                "Weighted"));

            AddTaperedTerm(lines, "Material", "both", theBoard.WhiteMaterial, theBoard.BlackMaterial, phase, true, true,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            AddTaperedTerm(lines, "PST (early)", "opening", theBoard.WhiteEarlyPSTScore, theBoard.BlackEarlyPSTScore,
                phase, true, false, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            AddTaperedTerm(lines, "PST (late)", "endgame", theBoard.WhiteLatePSTScore, theBoard.BlackLatePSTScore,
                phase, false, true, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            Move[] whitePieceAttacks = theBoard.GeneratePieceAttacksWithoutKing(WHITE);
            Move[] blackPieceAttacks = theBoard.GeneratePieceAttacksWithoutKing(BLACK);
            int white;
            int black;

            white = 0; black = 0;
            RewardBeingCastled(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardBeingCastled", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            RewardKingSafety(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardKingSafety", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            PenaliseKingAttack(ref theBoard, whitePieceAttacks, blackPieceAttacks, ref white, ref black);
            AddTaperedTerm(lines, "PenaliseKingAttack", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);
            lines.Add("      White king: attack " + KingAttackUnits(ref theBoard, blackPieceAttacks,
                theBoard.WhiteKingSquare, BLACK).ToString()
                + ", defence " + KingDefenceUnits(ref theBoard, whitePieceAttacks,
                    theBoard.WhiteKingZone[theBoard.WhiteKingSquare]).ToString()
                + ", net " + NetKingAttackUnits(ref theBoard, blackPieceAttacks, whitePieceAttacks,
                    theBoard.WhiteKingSquare, BLACK).ToString()
                + "   Black king: attack " + KingAttackUnits(ref theBoard, whitePieceAttacks, theBoard.BlackKingSquare,
                    WHITE).ToString()
                + ", defence " + KingDefenceUnits(ref theBoard, blackPieceAttacks,
                    theBoard.BlackKingZone[theBoard.BlackKingSquare]).ToString()
                + ", net " + NetKingAttackUnits(ref theBoard, whitePieceAttacks, blackPieceAttacks,
                    theBoard.BlackKingSquare, WHITE).ToString());
            lines.Add("      (attack is 0 with fewer than two attackers; " +
                "the penalty also needs the attacker's queen on the board)");

            white = 0; black = 0;
            RewardActiveBishop(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardActiveBishop", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);

            //white = 0; black = 0;
            //EncouragePawnsOnOppositeSideToKingToAdvance(ref theBoard, ref white, ref black);
            //AddTaperedTerm(lines, "EncouragePawnsOnOppositeSideToKingToAdvance", "opening", white, black, phase, true,
            //false, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            RewardPawnBishopSynergy(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardPawnBishopSynergy", "opening", white, black, phase, true, false,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            int whiteMobilityEndgame = 0;
            int blackMobilityEndgame = 0;
            RewardMobility(ref theBoard, whitePieceAttacks, blackPieceAttacks, ref white, ref black,
                ref whiteMobilityEndgame, ref blackMobilityEndgame);
            AddTaperedTerm(lines, "RewardMobility", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);
            AddTaperedTerm(lines, "RewardMobility", "endgame", whiteMobilityEndgame, blackMobilityEndgame, phase, false,
                true, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            RewardKnightOutpost(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardKnightOutpost", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            PenaliseTrappedRook(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "PenaliseTrappedRook", "opening", white, black, phase, true, false, ref whiteOpening,
                ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            RewardRooksOnOpenFiles(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardRooksOnOpenFiles", "endgame", white, black, phase, false, true,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            RewardRooksOnTheSeventh(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardRooksOnTheSeventh", "endgame", white, black, phase, false, true,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            RewardBlockadingDangerousPawns(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "RewardBlockadingDangerousPawns", "endgame", white, black, phase, false, true,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            white = 0; black = 0;
            PenaliseFixedPawnsOnEnemyBishopColour(ref theBoard, ref white, ref black);
            AddTaperedTerm(lines, "PenaliseFixedPawnsOnEnemyBishopColour", "endgame", white, black, phase, false, true,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            PenaliseBackwardsPawns(ref theBoard, out int whiteBWPScore, out int blackBWPScore);
            AddTaperedTerm(lines, "PenaliseBackwardsPawns", "opening", whiteBWPScore, blackBWPScore, phase, true, false,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            RewardPawnChains(ref theBoard, out int whiteChainScore, out int blackChainScore);
            AddTaperedTerm(lines, "RewardPawnChains", "opening", whiteChainScore, blackChainScore, phase, true, false,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            RewardPassedPawns(ref theBoard, out int whitePPScore, out int blackPPScore);
            AddTaperedTerm(lines, "RewardPassedPawns", "endgame", whitePPScore, blackPPScore, phase, false, true,
                ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            AddTaperedTerm(lines, "RewardPassedPawns (half again)", "opening", whitePPScore / 2, blackPPScore / 2,
                phase, true, false, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            ReduceBlockadedPassedPawns(ref theBoard, out int whiteBlockadeReduction, out int blackBlockadeReduction);
            AddTaperedTerm(lines, "ReduceBlockadedPassedPawns", "endgame", -whiteBlockadeReduction,
                -blackBlockadeReduction, phase, false, true, ref whiteOpening, ref whiteEndgame, ref blackOpening,
                ref blackEndgame);
            AddTaperedTerm(lines, "ReduceBlockadedPassedPawns (half again)", "opening", -(whiteBlockadeReduction / 2),
                -(blackBlockadeReduction / 2), phase, true, false, ref whiteOpening, ref whiteEndgame, ref blackOpening,
                ref blackEndgame);

            FindPassedPawns(ref theBoard, out ulong whitePassedPawns, out ulong blackPassedPawns);
            ScorePassedPawnPromotionChances(ref theBoard, whitePieceAttacks, blackPieceAttacks, whitePassedPawns,
                blackPassedPawns,
                out int whitePromotionChances, out int blackPromotionChances);
            AddTaperedTerm(lines, "ScorePassedPawnPromotionChances", "endgame", whitePromotionChances,
                blackPromotionChances, phase, false, true, ref whiteOpening, ref whiteEndgame, ref blackOpening,
                ref blackEndgame);

            PenaliseDoubledAndTripledPawns(ref theBoard, out int whiteDblPawnScore, out int blackDblPawnScore);
            AddTaperedTerm(lines, "PenaliseDoubledAndTripledPawns", "endgame", whiteDblPawnScore, blackDblPawnScore,
                phase, false, true, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            PenaliseIsolatedPawns(ref theBoard, out int whiteIsoPawnScore, out int blackIsoPawnScore);
            AddTaperedTerm(lines, "PenaliseIsolatedPawns", "endgame", whiteIsoPawnScore, blackIsoPawnScore, phase,
                false, true, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);

            int whiteScore = ((whiteOpening * (256 - phase)) + (whiteEndgame * phase)) / 256;
            int blackScore = ((blackOpening * (256 - phase)) + (blackEndgame * phase)) / 256;
            lines.Add(string.Format("  {0,-48}{1,-10}{2,8}{3,8}{4,8}", "Tapered total", "", whiteScore, blackScore,
                whiteScore - blackScore));

            //The terms ScorePosition adds after the taper, at full value
            white = 0; black = 0;
            if (theBoard.WhiteHasDarkSquaredBishop && theBoard.WhiteHasLightSquaredBishop)
            {
                white += BISHOP_PAIR_BONUS_VALUE;
            }
            if (theBoard.BlackHasDarkSquaredBishop && theBoard.BlackHasLightSquaredBishop)
            {
                black += BISHOP_PAIR_BONUS_VALUE;
            }
            AddUntaperedTerm(lines, "Bishop pair", white, black, ref whiteScore, ref blackScore);

            white = 0; black = 0;
            int exchangeBalance = ExchangeImbalance(ref theBoard);
            if (exchangeBalance > 0)
            {
                white += EXCHANGE_IMBALANCE_BONUS;
            }
            else if (exchangeBalance < 0)
            {
                black += EXCHANGE_IMBALANCE_BONUS;
            }
            AddUntaperedTerm(lines, "Exchange imbalance", white, black, ref whiteScore, ref blackScore);

            white = 0; black = 0;
            if (theBoard.WhiteRookOneSquare != 255 && theBoard.WhiteRookTwoSquare != 255)
            {
                white -= ROOK_REDUNDANCY_PENALTY;
            }
            if (theBoard.BlackRookOneSquare != 255 && theBoard.BlackRookTwoSquare != 255)
            {
                black -= ROOK_REDUNDANCY_PENALTY;
            }
            AddUntaperedTerm(lines, "Rook redundancy", white, black, ref whiteScore, ref blackScore);

            white = 0; black = 0;
            if (theBoard.WhiteKnightOneSquare != 255 && theBoard.WhiteKnightTwoSquare != 255)
            {
                white -= KNIGHT_REDUNDANCY_PENALTY;
            }
            if (theBoard.BlackKnightOneSquare != 255 && theBoard.BlackKnightTwoSquare != 255)
            {
                black -= KNIGHT_REDUNDANCY_PENALTY;
            }
            AddUntaperedTerm(lines, "Knight redundancy", white, black, ref whiteScore, ref blackScore);

            white = 0; black = 0;
            for (int nn = 0; nn <= 7; nn++)
            {
                if (theBoard.WhiteFilePawns[nn] == 0 && (theBoard.WhiteRookOneSquare != 255 ||
                    theBoard.WhiteRookTwoSquare != 255))
                {
                    white += (theBoard.WhiteRookOneSquare != 255 && theBoard.WhiteRookTwoSquare != 255)
                        ? SEMI_OPEN_FILE_TWO_ROOKS_BONUS : SEMI_OPEN_FILE_ONE_ROOK_BONUS;
                }
                if (theBoard.BlackFilePawns[nn] == 0 && (theBoard.BlackRookOneSquare != 255 ||
                    theBoard.BlackRookTwoSquare != 255))
                {
                    black += (theBoard.BlackRookOneSquare != 255 && theBoard.BlackRookTwoSquare != 255)
                        ? SEMI_OPEN_FILE_TWO_ROOKS_BONUS : SEMI_OPEN_FILE_ONE_ROOK_BONUS;
                }
            }
            AddUntaperedTerm(lines, "Semi-open files with rooks on the board", white, black, ref whiteScore,
                ref blackScore);

            white = theBoard.OnMove == WHITE ? TEMPO_BONUS : 0;
            black = theBoard.OnMove == BLACK ? TEMPO_BONUS : 0;
            AddUntaperedTerm(lines, "Tempo (side to move)", white, black, ref whiteScore, ref blackScore);

            int whitePOV = whiteScore - blackScore;
            lines.Add(string.Format("  {0,-48}{1,-10}{2,8}{3,8}{4,8}", "Total", "", whiteScore, blackScore, whitePOV));

            TranspositionTable checkTable = new(true);
            int scorePosition = ScorePosition(ref theBoard, ref checkTable, -5000, 5000, false, 0);
            int scorePositionWhitePOV = theBoard.OnMove == BLACK ? scorePosition : -scorePosition;
            lines.Add("  White POV " + FormatSigned(whitePOV) + ", side to move POV " +
                FormatSigned(theBoard.OnMove == WHITE ? whitePOV : -whitePOV)
                + (scorePositionWhitePOV == whitePOV ? "  (matches ScorePosition)"
                    : "  (DOES NOT MATCH ScorePosition, which gives White POV " + FormatSigned(scorePositionWhitePOV) +
                    ")"));

            lines.Add(" ");
            lines.Add("PST by piece (early / late):");
            for (int sq = 0; sq <= 63; sq++)
            {
                if (theBoard.Color[sq] != EMPTY)
                {
                    int piece = theBoard.Piece[sq];
                    int side = theBoard.Color[sq];
                    string squareName = ((char)('a' + sq % 8)).ToString() + (8 - sq / 8).ToString();
                    lines.Add(string.Format("  {0,-6}{1}{2,-4}{3,6}{4,6}", side == WHITE ? "White" : "Black",
                        "PNBRQK"[piece], squareName,
                        PSTFor(piece, side, true)[sq], PSTFor(piece, side, false)[sq]));
                }
            }

            return lines;
        }


        private static void AddTaperedTerm(List<string> lines, string name, string half, int white, int black,
            int phase, bool inOpening, bool inEndgame,
            ref int whiteOpening, ref int whiteEndgame, ref int blackOpening, ref int blackEndgame)
        {
            double weight = (inOpening ? 256 - phase : 0) + (inEndgame ? phase : 0);
            double weighted = (white - black) * weight / 256.0;
            lines.Add(string.Format("  {0,-48}{1,-10}{2,8}{3,8}{4,8}{5,10:0.0}", name, half, white, black,
                white - black, weighted));
            if (inOpening)
            {
                whiteOpening += white;
                blackOpening += black;
            }
            if (inEndgame)
            {
                whiteEndgame += white;
                blackEndgame += black;
            }
        }


        private static void AddUntaperedTerm(List<string> lines, string name, int white, int black, ref int whiteScore,
            ref int blackScore)
        {
            lines.Add(string.Format("  {0,-48}{1,-10}{2,8}{3,8}{4,8}{5,10:0.0}", name, "full", white, black,
                white - black, (double)(white - black)));
            whiteScore += white;
            blackScore += black;
        }


        private static int[] PSTFor(int piece, int side, bool early)
        {
            switch (piece)
            {
                case PAWN:
                    return side == WHITE ? (early ? WhitePawnEarlyPST : WhitePawnLatePST)
                        : (early ? BlackPawnEarlyPST : BlackPawnLatePST);
                case KNIGHT:
                    return side == WHITE ? (early ? WhiteKnightEarlyPST : WhiteKnightLatePST)
                        : (early ? BlackKnightEarlyPST : BlackKnightLatePST);
                case BISHOP:
                    return side == WHITE ? (early ? WhiteBishopEarlyPST : WhiteBishopLatePST)
                        : (early ? BlackBishopEarlyPST : BlackBishopLatePST);
                case ROOK:
                    return side == WHITE ? (early ? WhiteRookEarlyPST : WhiteRookLatePST)
                        : (early ? BlackRookEarlyPST : BlackRookLatePST);
                case QUEEN:
                    return side == WHITE ? (early ? WhiteQueenEarlyPST : WhiteQueenLatePST)
                        : (early ? BlackQueenEarlyPST : BlackQueenLatePST);
                default:
                    return side == WHITE ? (early ? WhiteKingEarlyPST : WhiteKingLatePST)
                        : (early ? BlackKingEarlyPST : BlackKingLatePST);
            }
        }


        private static string FormatSigned(int value)
        {
            return value.ToString("+0;-0;0");
        }

    }
}
