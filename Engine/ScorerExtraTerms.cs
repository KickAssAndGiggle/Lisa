using System.Numerics;
using System.Runtime.CompilerServices;
using static Lisa.Globals;
namespace Lisa
{
    //Terms that start either off (weight 0) or matching what they replaced, so Texel can decide what each is worth.
    //A term whose weight is 0 is skipped, so it costs no time.
    public static partial class Scorer
    {
        //Adds every extra term to the four halves. With lines, each term is also written to the breakdown, through
        //the same AddTaperedTerm the other terms use, so the breakdown total still matches ScorePosition.
        //The piece masks come from Board.GeneratePieceAttacksWithoutKing, attackedByWhite and attackedByBlack from
        //AttackedSquares, and pawns from the pawn hash or AnalysePawns
        public static void AddExtraTerms(ref Board theBoard, in PieceAttackMasks whitePieces,
            in PieceAttackMasks blackPieces, ulong attackedByWhite, ulong attackedByBlack,
            in TTPawnAnalysis pawns, int phase, List<string>? lines, ref int whiteOpening, ref int whiteEndgame,
            ref int blackOpening, ref int blackEndgame)
        {
            if (KNIGHT_VALUE_PER_PAWN_ABOVE_FIVE != 0 || ROOK_VALUE_LOST_PER_PAWN_ABOVE_FIVE != 0)
            {
                AddBothTerm(lines, "Pawn count adjustment (knights, rooks)",
                    PawnCountAdjustment(ref theBoard, WHITE), PawnCountAdjustment(ref theBoard, BLACK), phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (HOLE_NEAR_KING_PENALTY != 0)
            {
                AddOpeningTerm(lines, "Holes near the king",
                    -HolesNearKing(pawns.WhiteHoles, theBoard.WhiteKingSquare % 8) * HOLE_NEAR_KING_PENALTY,
                    -HolesNearKing(pawns.BlackHoles, theBoard.BlackKingSquare % 8) * HOLE_NEAR_KING_PENALTY, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (HOLE_PENALTY_PER_ENEMY_MINOR != 0)
            {
                int whiteHoleCount = BitOperations.PopCount(pawns.WhiteHoles);
                int blackHoleCount = BitOperations.PopCount(pawns.BlackHoles);
                AddBothTerm(lines, "Holes per enemy minor piece",
                    -whiteHoleCount * MinorPieceCount(ref theBoard, BLACK) * HOLE_PENALTY_PER_ENEMY_MINOR,
                    -blackHoleCount * MinorPieceCount(ref theBoard, WHITE) * HOLE_PENALTY_PER_ENEMY_MINOR, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (KNIGHT_OUTPOST_ON_HOLE_BONUS != 0 || BISHOP_OUTPOST_ON_HOLE_BONUS != 0)
            {
                AddOpeningTerm(lines, "Outposts on enemy holes",
                    OutpostsOnHoles(ref theBoard, WHITE, pawns.BlackHoles & pawns.WhitePawnAttacks),
                    OutpostsOnHoles(ref theBoard, BLACK, pawns.WhiteHoles & pawns.BlackPawnAttacks), phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (STORM_PAWN_ON_THIRD_RANK_PENALTY != 0 || STORM_PAWN_ON_FOURTH_RANK_PENALTY != 0 ||
                STORM_PAWN_ON_FIFTH_RANK_PENALTY != 0)
            {
                AddOpeningTerm(lines, "Pawn storms",
                    -PawnStormPenalty(pawns.WhiteStormFiles, theBoard.WhiteKingSquare % 8),
                    -PawnStormPenalty(pawns.BlackStormFiles, theBoard.BlackKingSquare % 8), phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (KING_STUCK_IN_CENTRE_PENALTY != 0)
            {
                AddOpeningTerm(lines, "King stuck in the centre",
                    KingStuckInCentre(ref theBoard, WHITE) ? -KING_STUCK_IN_CENTRE_PENALTY : 0,
                    KingStuckInCentre(ref theBoard, BLACK) ? -KING_STUCK_IN_CENTRE_PENALTY : 0, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (MINOR_BEHIND_PAWN_BONUS != 0)
            {
                AddOpeningTerm(lines, "Minor piece behind a pawn",
                    MinorsBehindPawns(ref theBoard, WHITE) * MINOR_BEHIND_PAWN_BONUS,
                    MinorsBehindPawns(ref theBoard, BLACK) * MINOR_BEHIND_PAWN_BONUS, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (TRAPPED_BISHOP_PENALTY != 0)
            {
                AddBothTerm(lines, "Trapped bishop",
                    -TrappedBishops(ref theBoard, WHITE) * TRAPPED_BISHOP_PENALTY,
                    -TrappedBishops(ref theBoard, BLACK) * TRAPPED_BISHOP_PENALTY, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (UNDEVELOPED_MINOR_PENALTY != 0 || EARLY_QUEEN_PENALTY != 0)
            {
                AddOpeningTerm(lines, "Development (minors home, early queen)",
                    -DevelopmentPenalty(ref theBoard, WHITE), -DevelopmentPenalty(ref theBoard, BLACK), phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (SPACE_BONUS != 0)
            {
                AddOpeningTerm(lines, "Space",
                    pawns.WhiteSpace * SPACE_BONUS, pawns.BlackSpace * SPACE_BONUS,
                    phase, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (FLANK_PAWN_ADVANCED_PENALTY != 0)
            {
                AddOpeningTerm(lines, "Flank pawns advanced",
                    -pawns.WhiteAdvancedFlankPawns * FLANK_PAWN_ADVANCED_PENALTY,
                    -pawns.BlackAdvancedFlankPawns * FLANK_PAWN_ADVANCED_PENALTY, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (PAWN_PHALANX_BONUS != 0 || PAWN_PHALANX_ENDGAME_BONUS != 0)
            {
                int whitePhalanxPawns = pawns.WhitePhalanxPawns;
                int blackPhalanxPawns = pawns.BlackPhalanxPawns;
                AddOpeningTerm(lines, "Pawn phalanxes", whitePhalanxPawns * PAWN_PHALANX_BONUS,
                    blackPhalanxPawns * PAWN_PHALANX_BONUS, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
                AddEndgameTerm(lines, "Pawn phalanxes", whitePhalanxPawns * PAWN_PHALANX_ENDGAME_BONUS,
                    blackPhalanxPawns * PAWN_PHALANX_ENDGAME_BONUS, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (UNOPPOSED_ISOLATED_PAWN_PENALTY != 0)
            {
                AddBothTerm(lines, "Unopposed isolated pawns",
                    -pawns.WhiteUnopposedIsolatedPawns * UNOPPOSED_ISOLATED_PAWN_PENALTY,
                    -pawns.BlackUnopposedIsolatedPawns * UNOPPOSED_ISOLATED_PAWN_PENALTY, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (CANDIDATE_PASSED_PAWN_BONUS != 0)
            {
                AddEndgameTerm(lines, "Candidate passed pawns",
                    pawns.WhiteCandidatePassedPawns * CANDIDATE_PASSED_PAWN_BONUS,
                    pawns.BlackCandidatePassedPawns * CANDIDATE_PASSED_PAWN_BONUS, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (PAWN_ISLAND_PENALTY != 0)
            {
                AddEndgameTerm(lines, "Pawn islands",
                    -pawns.WhitePawnIslands * PAWN_ISLAND_PENALTY,
                    -pawns.BlackPawnIslands * PAWN_ISLAND_PENALTY, phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (THREATENED_BY_PAWN_PENALTY != 0)
            {
                AddBothTerm(lines, "Pieces attacked by pawns",
                    -BitOperations.PopCount(whitePieces.PieceSquares & pawns.BlackPawnAttacks) *
                        THREATENED_BY_PAWN_PENALTY,
                    -BitOperations.PopCount(blackPieces.PieceSquares & pawns.WhitePawnAttacks) *
                        THREATENED_BY_PAWN_PENALTY,
                    phase,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (HANGING_PIECE_PENALTY != 0)
            {
                AddBothTerm(lines, "Hanging pieces",
                    -HangingPieces(whitePieces.PieceSquares, attackedByBlack, attackedByWhite) * HANGING_PIECE_PENALTY,
                    -HangingPieces(blackPieces.PieceSquares, attackedByWhite, attackedByBlack) * HANGING_PIECE_PENALTY,
                    phase, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (WEAK_KING_SQUARE_PENALTY != 0)
            {
                AddOpeningTerm(lines, "Weak squares next to the king",
                    -WeakKingSquares(theBoard.WhiteKingSquare, attackedByBlack,
                        whitePieces.Knight | whitePieces.Bishop | whitePieces.Rook | pawns.WhitePawnAttacks) *
                        WEAK_KING_SQUARE_PENALTY,
                    -WeakKingSquares(theBoard.BlackKingSquare, attackedByWhite,
                        blackPieces.Knight | blackPieces.Bishop | blackPieces.Rook | pawns.BlackPawnAttacks) *
                        WEAK_KING_SQUARE_PENALTY,
                    phase, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }

            if (SAFE_KNIGHT_CHECK_PENALTY != 0 || SAFE_BISHOP_CHECK_PENALTY != 0 || SAFE_ROOK_CHECK_PENALTY != 0 ||
                SAFE_QUEEN_CHECK_PENALTY != 0)
            {
                AddOpeningTerm(lines, "Safe checks against the king",
                    -SafeCheckPenalty(ref theBoard, WHITE, in blackPieces, attackedByWhite),
                    -SafeCheckPenalty(ref theBoard, BLACK, in whitePieces, attackedByBlack),
                    phase, ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
            }
        }


        //Adds a term's white and black values to the opening halves. With lines, it goes through AddTaperedTerm,
        //which also writes it to the breakdown
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddOpeningTerm(List<string>? lines, string name, int white, int black, int phase,
            ref int whiteOpening, ref int whiteEndgame, ref int blackOpening, ref int blackEndgame)
        {
            if (lines != null)
            {
                AddTaperedTerm(lines, name, "opening", white, black, phase, true, false,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
                return;
            }
            whiteOpening += white;
            blackOpening += black;
        }


        //Adds a term's white and black values to the endgame halves. With lines, it goes through AddTaperedTerm,
        //which also writes it to the breakdown
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddEndgameTerm(List<string>? lines, string name, int white, int black, int phase,
            ref int whiteOpening, ref int whiteEndgame, ref int blackOpening, ref int blackEndgame)
        {
            if (lines != null)
            {
                AddTaperedTerm(lines, name, "endgame", white, black, phase, false, true,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
                return;
            }
            whiteEndgame += white;
            blackEndgame += black;
        }


        //Adds a term's white and black values to both the opening and endgame halves. With lines, it goes
        //through AddTaperedTerm, which also writes it to the breakdown
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddBothTerm(List<string>? lines, string name, int white, int black, int phase,
            ref int whiteOpening, ref int whiteEndgame, ref int blackOpening, ref int blackEndgame)
        {
            if (lines != null)
            {
                AddTaperedTerm(lines, name, "both", white, black, phase, true, true,
                    ref whiteOpening, ref whiteEndgame, ref blackOpening, ref blackEndgame);
                return;
            }
            whiteOpening += white;
            blackOpening += black;
            whiteEndgame += white;
            blackEndgame += black;
        }


        //The other side
        private static int Opponent(int side)
        {
            return side == WHITE ? BLACK : WHITE;
        }


        //A pawn's rank counted from its own side: 2 on its starting rank, 8 on the promotion rank
        private static int RelativeRank(int square, int side)
        {
            return side == WHITE ? 8 - square / 8 : square / 8 + 1;
        }


        //The square on the given file at a rank counted from the given side
        private static int SquareAtRelativeRank(int file, int relativeRank, int side)
        {
            return side == WHITE ? (8 - relativeRank) * 8 + file : (relativeRank - 1) * 8 + file;
        }


        //The side's pawn square list (-1 for an empty slot)
        private static int[] PawnSquaresOf(ref Board theBoard, int side)
        {
            return side == WHITE ? theBoard.WhitePawnSquares : theBoard.BlackPawnSquares;
        }


        //How many knights and bishops the side has
        private static int MinorPieceCount(ref Board theBoard, int side)
        {
            if (side == WHITE)
            {
                return (theBoard.WhiteKnightOneSquare != 255 ? 1 : 0) +
                    (theBoard.WhiteKnightTwoSquare != 255 ? 1 : 0) +
                    (theBoard.WhiteHasDarkSquaredBishop ? 1 : 0) + (theBoard.WhiteHasLightSquaredBishop ? 1 : 0);
            }
            return (theBoard.BlackKnightOneSquare != 255 ? 1 : 0) +
                (theBoard.BlackKnightTwoSquare != 255 ? 1 : 0) +
                (theBoard.BlackHasDarkSquaredBishop ? 1 : 0) + (theBoard.BlackHasLightSquaredBishop ? 1 : 0);
        }


        //Kaufman: each knight gains, and each rook loses, a little for every one of its own side's pawns above five,
        //and the reverse below five
        private static int PawnCountAdjustment(ref Board theBoard, int side)
        {
            int pawnsAboveFive;
            int knights;
            int rooks;
            if (side == WHITE)
            {
                pawnsAboveFive = theBoard.WhitePawnsOnLightSquares + theBoard.WhitePawnsOnDarkSquares - 5;
                knights = (theBoard.WhiteKnightOneSquare != 255 ? 1 : 0) +
                    (theBoard.WhiteKnightTwoSquare != 255 ? 1 : 0);
                rooks = (theBoard.WhiteRookOneSquare != 255 ? 1 : 0) + (theBoard.WhiteRookTwoSquare != 255 ? 1 : 0);
            }
            else
            {
                pawnsAboveFive = theBoard.BlackPawnsOnLightSquares + theBoard.BlackPawnsOnDarkSquares - 5;
                knights = (theBoard.BlackKnightOneSquare != 255 ? 1 : 0) +
                    (theBoard.BlackKnightTwoSquare != 255 ? 1 : 0);
                rooks = (theBoard.BlackRookOneSquare != 255 ? 1 : 0) + (theBoard.BlackRookTwoSquare != 255 ? 1 : 0);
            }
            return pawnsAboveFive * (knights * KNIGHT_VALUE_PER_PAWN_ABOVE_FIVE -
                rooks * ROOK_VALUE_LOST_PER_PAWN_ABOVE_FIVE);
        }


        //A side's holes (its HoleMask) on its own king's file and the files either side
        private static int HolesNearKing(ulong holes, int kingFile)
        {
            const ulong FILE_A = 0x0101010101010101UL;
            ulong nearFiles = FILE_A << kingFile;
            if (kingFile > 0)
            {
                nearFiles |= FILE_A << (kingFile - 1);
            }
            if (kingFile < 7)
            {
                nearFiles |= FILE_A << (kingFile + 1);
            }
            return BitOperations.PopCount(holes & nearFiles);
        }


        //A knight or bishop on one of the enemy's holes, guarded by one of its own pawns. guardedEnemyHoles is the
        //enemy's HoleMask and this side's PawnAttackMask, ANDed
        private static int OutpostsOnHoles(ref Board theBoard, int side, ulong guardedEnemyHoles)
        {
            if (guardedEnemyHoles == 0)
            {
                return 0;
            }
            if (side == WHITE)
            {
                return OutpostBonus(theBoard.WhiteKnightOneSquare, guardedEnemyHoles, KNIGHT_OUTPOST_ON_HOLE_BONUS) +
                    OutpostBonus(theBoard.WhiteKnightTwoSquare, guardedEnemyHoles, KNIGHT_OUTPOST_ON_HOLE_BONUS) +
                    OutpostBonus(theBoard.WhiteLightBishopSquare, guardedEnemyHoles, BISHOP_OUTPOST_ON_HOLE_BONUS) +
                    OutpostBonus(theBoard.WhiteDarkBishopSquare, guardedEnemyHoles, BISHOP_OUTPOST_ON_HOLE_BONUS);
            }
            return OutpostBonus(theBoard.BlackKnightOneSquare, guardedEnemyHoles, KNIGHT_OUTPOST_ON_HOLE_BONUS) +
                OutpostBonus(theBoard.BlackKnightTwoSquare, guardedEnemyHoles, KNIGHT_OUTPOST_ON_HOLE_BONUS) +
                OutpostBonus(theBoard.BlackLightBishopSquare, guardedEnemyHoles, BISHOP_OUTPOST_ON_HOLE_BONUS) +
                OutpostBonus(theBoard.BlackDarkBishopSquare, guardedEnemyHoles, BISHOP_OUTPOST_ON_HOLE_BONUS);
        }


        //pieceSquare is 255 when the side has no such piece
        private static int OutpostBonus(int pieceSquare, ulong guardedEnemyHoles, int bonus)
        {
            return pieceSquare != 255 && ((guardedEnemyHoles >> pieceSquare) & 1UL) != 0 ? bonus : 0;
        }


        //A storm class for each file, two bits per file from the a file up, for the side defending against the
        //storm. The class comes from the enemy pawn on the file nearest that side's end of the board, by the rank
        //it has reached counted from that side: 1 on the third, 2 on the fourth, 3 on the fifth, otherwise 0
        private static ushort PawnStormFiles(ref Board theBoard, int side)
        {
            int[] enemyPawns = PawnSquaresOf(ref theBoard, Opponent(side));
            int stormFiles = 0;
            for (int file = 0; file <= 7; file++)
            {
                int nearestRank = 9;
                for (int pp = 0; pp <= 7; pp++)
                {
                    int pawn = enemyPawns[pp];
                    if (pawn != -1 && pawn % 8 == file)
                    {
                        //The enemy pawn's rank counted from our side of the board
                        int rankFromUs = 9 - RelativeRank(pawn, Opponent(side));
                        nearestRank = Math.Min(nearestRank, rankFromUs);
                    }
                }
                if (nearestRank >= 3 && nearestRank <= 5)
                {
                    stormFiles |= (nearestRank - 2) << (file * 2);
                }
            }
            return (ushort)stormFiles;
        }


        //The storm penalty for a king on kingFile: the classes of its file and the files either side, from that
        //side's PawnStormFiles, each with its own weight
        private static int PawnStormPenalty(int stormFiles, int kingFile)
        {
            int penalty = 0;
            for (int file = kingFile - 1; file <= kingFile + 1; file++)
            {
                if (file < 0 || file > 7)
                {
                    continue;
                }
                int stormClass = (stormFiles >> (file * 2)) & 3;
                if (stormClass == 1)
                {
                    penalty += STORM_PAWN_ON_THIRD_RANK_PENALTY;
                }
                else if (stormClass == 2)
                {
                    penalty += STORM_PAWN_ON_FOURTH_RANK_PENALTY;
                }
                else if (stormClass == 3)
                {
                    penalty += STORM_PAWN_ON_FIFTH_RANK_PENALTY;
                }
            }
            return penalty;
        }


        //King on the d or e file with the enemy queen on, and no longer able to castle: off its starting square, or
        //on it with neither corner rook at home
        private static bool KingStuckInCentre(ref Board theBoard, int side)
        {
            int king = side == WHITE ? theBoard.WhiteKingSquare : theBoard.BlackKingSquare;
            int enemyQueen = side == WHITE ? theBoard.BlackQueenSquare : theBoard.WhiteQueenSquare;
            if (enemyQueen == 255 || (king % 8 != 3 && king % 8 != 4))
            {
                return false;
            }
            int home = side == WHITE ? 60 : 4;
            if (king != home)
            {
                return true;
            }
            int kingsideCorner = side == WHITE ? 63 : 7;
            int queensideCorner = side == WHITE ? 56 : 0;
            bool rookHome = (theBoard.Color[kingsideCorner] == side && theBoard.Piece[kingsideCorner] == ROOK) ||
                (theBoard.Color[queensideCorner] == side && theBoard.Piece[queensideCorner] == ROOK);
            return !rookHome;
        }


        //A knight or bishop with one of its own pawns directly in front of it
        private static int MinorsBehindPawns(ref Board theBoard, int side)
        {
            if (side == WHITE)
            {
                return OwnPawnAhead(ref theBoard, theBoard.WhiteKnightOneSquare, WHITE, -8) +
                    OwnPawnAhead(ref theBoard, theBoard.WhiteKnightTwoSquare, WHITE, -8) +
                    OwnPawnAhead(ref theBoard, theBoard.WhiteLightBishopSquare, WHITE, -8) +
                    OwnPawnAhead(ref theBoard, theBoard.WhiteDarkBishopSquare, WHITE, -8);
            }
            return OwnPawnAhead(ref theBoard, theBoard.BlackKnightOneSquare, BLACK, 8) +
                OwnPawnAhead(ref theBoard, theBoard.BlackKnightTwoSquare, BLACK, 8) +
                OwnPawnAhead(ref theBoard, theBoard.BlackLightBishopSquare, BLACK, 8) +
                OwnPawnAhead(ref theBoard, theBoard.BlackDarkBishopSquare, BLACK, 8);
        }


        //1 if one of the side's pawns stands directly in front of pieceSquare, otherwise 0. pieceSquare is 255 when
        //the side has no such piece
        private static int OwnPawnAhead(ref Board theBoard, int pieceSquare, int side, int forward)
        {
            if (pieceSquare == 255)
            {
                return 0;
            }
            int ahead = pieceSquare + forward;
            return ahead >= 0 && ahead <= 63 && theBoard.Color[ahead] == side && theBoard.Piece[ahead] == PAWN
                ? 1 : 0;
        }


        //A bishop on a7 or h7 shut in by an enemy pawn on b6 or g6, or on a6 or h6 by a pawn on b5 or g5 (and the
        //same from Black's side)
        private static int TrappedBishops(ref Board theBoard, int side)
        {
            if (side == WHITE)
            {
                return TrappedBishop(ref theBoard, theBoard.WhiteLightBishopSquare, WHITE) +
                    TrappedBishop(ref theBoard, theBoard.WhiteDarkBishopSquare, WHITE);
            }
            return TrappedBishop(ref theBoard, theBoard.BlackLightBishopSquare, BLACK) +
                TrappedBishop(ref theBoard, theBoard.BlackDarkBishopSquare, BLACK);
        }


        //1 if the bishop on this square is trapped, otherwise 0. bishop is 255 when the side has no such bishop
        private static int TrappedBishop(ref Board theBoard, int bishop, int side)
        {
            if (bishop == 255)
            {
                return 0;
            }
            int rank = RelativeRank(bishop, side);
            int file = bishop % 8;
            if ((rank != 6 && rank != 7) || (file != 0 && file != 7))
            {
                return 0;
            }
            int trapFile = file == 0 ? 1 : 6;
            int trapSquare = SquareAtRelativeRank(trapFile, rank - 1, side);
            return theBoard.Color[trapSquare] == Opponent(side) && theBoard.Piece[trapSquare] == PAWN ? 1 : 0;
        }


        //Each knight or bishop still on its starting square, and each of those again if the queen has already left
        //its own
        private static int DevelopmentPenalty(ref Board theBoard, int side)
        {
            int undeveloped = side == WHITE
                ? MinorOnSquare(ref theBoard, 57, WHITE) + MinorOnSquare(ref theBoard, 58, WHITE) +
                    MinorOnSquare(ref theBoard, 61, WHITE) + MinorOnSquare(ref theBoard, 62, WHITE)
                : MinorOnSquare(ref theBoard, 1, BLACK) + MinorOnSquare(ref theBoard, 2, BLACK) +
                    MinorOnSquare(ref theBoard, 5, BLACK) + MinorOnSquare(ref theBoard, 6, BLACK);
            int queen = side == WHITE ? theBoard.WhiteQueenSquare : theBoard.BlackQueenSquare;
            int queenHome = side == WHITE ? 59 : 3;
            bool queenOut = queen != 255 && queen != queenHome;
            return undeveloped * UNDEVELOPED_MINOR_PENALTY + (queenOut ? undeveloped * EARLY_QUEEN_PENALTY : 0);
        }


        //1 if a knight or bishop of the side stands on the square, otherwise 0
        private static int MinorOnSquare(ref Board theBoard, int square, int side)
        {
            return theBoard.Color[square] == side &&
                (theBoard.Piece[square] == KNIGHT || theBoard.Piece[square] == BISHOP) ? 1 : 0;
        }


        //Squares on the c to f files, on a side's second to fourth ranks, that hold none of its pawns and no enemy
        //pawn attacks. A square with one of its own pawns up to three squares in front counts twice.
        //enemyPawnAttacks is the enemy's PawnAttackMask
        private static int SpaceCount(ref Board theBoard, int side, ulong enemyPawnAttacks)
        {
            int count = 0;
            for (int file = 2; file <= 5; file++)
            {
                for (int rank = 2; rank <= 4; rank++)
                {
                    int square = SquareAtRelativeRank(file, rank, side);
                    bool ownPawnHere = theBoard.Color[square] == side && theBoard.Piece[square] == PAWN;
                    if (ownPawnHere || ((enemyPawnAttacks >> square) & 1UL) != 0)
                    {
                        continue;
                    }
                    count += 1;
                    for (int ahead = rank + 1; ahead <= rank + 3 && ahead <= 7; ahead++)
                    {
                        int aheadSquare = SquareAtRelativeRank(file, ahead, side);
                        if (theBoard.Color[aheadSquare] == side && theBoard.Piece[aheadSquare] == PAWN)
                        {
                            count += 1;
                            break;
                        }
                    }
                }
            }
            return count;
        }


        //Pawns on the a, b, g and h files on their third to fifth ranks
        private static int AdvancedFlankPawns(ref Board theBoard, int side)
        {
            int[] pawns = PawnSquaresOf(ref theBoard, side);
            int count = 0;
            for (int pp = 0; pp <= 7; pp++)
            {
                int pawn = pawns[pp];
                if (pawn == -1)
                {
                    continue;
                }
                int file = pawn % 8;
                int rank = RelativeRank(pawn, side);
                if ((file <= 1 || file >= 6) && rank >= 3 && rank <= 5)
                {
                    count += 1;
                }
            }
            return count;
        }


        //Pawns with one of their own pawns beside them on the same rank, on any rank
        private static int PawnsInPhalanx(ref Board theBoard, int side)
        {
            int[] pawns = PawnSquaresOf(ref theBoard, side);
            int count = 0;
            for (int pp = 0; pp <= 7; pp++)
            {
                int pawn = pawns[pp];
                if (pawn == -1)
                {
                    continue;
                }
                bool left = pawn % 8 != 0 && theBoard.Color[pawn - 1] == side && theBoard.Piece[pawn - 1] == PAWN;
                bool right = pawn % 8 != 7 && theBoard.Color[pawn + 1] == side && theBoard.Piece[pawn + 1] == PAWN;
                if (left || right)
                {
                    count += 1;
                }
            }
            return count;
        }


        //Isolated pawns with no enemy pawn in front of them on their file, so rooks can attack them down it
        private static int UnopposedIsolatedPawns(ref Board theBoard, int side)
        {
            int[] pawns = PawnSquaresOf(ref theBoard, side);
            int[] ownFilePawns = side == WHITE ? theBoard.WhiteFilePawns : theBoard.BlackFilePawns;
            int[] enemyPawns = PawnSquaresOf(ref theBoard, Opponent(side));
            int count = 0;
            for (int pp = 0; pp <= 7; pp++)
            {
                int pawn = pawns[pp];
                if (pawn == -1)
                {
                    continue;
                }
                int file = pawn % 8;
                bool isolated = (file == 0 || ownFilePawns[file - 1] == 0) &&
                    (file == 7 || ownFilePawns[file + 1] == 0);
                if (isolated && !EnemyPawnAheadOnFile(pawn, side, enemyPawns, file))
                {
                    count += 1;
                }
            }
            return count;
        }


        //True if an enemy pawn stands on the given file further up the board than the pawn, counted from the
        //pawn's side
        private static bool EnemyPawnAheadOnFile(int pawn, int side, int[] enemyPawns, int file)
        {
            int rank = RelativeRank(pawn, side);
            for (int pp = 0; pp <= 7; pp++)
            {
                int enemy = enemyPawns[pp];
                if (enemy != -1 && enemy % 8 == file && RelativeRank(enemy, side) > rank)
                {
                    return true;
                }
            }
            return false;
        }


        //Pawns that are not passed but could become so: nothing in front on their own file, and at least as many of
        //their own pawns beside or behind them on the neighbouring files as enemy pawns in front on those files
        private static int CandidatePassedPawns(ref Board theBoard, int side)
        {
            int[] pawns = PawnSquaresOf(ref theBoard, side);
            int[] enemyPawns = PawnSquaresOf(ref theBoard, Opponent(side));
            int count = 0;
            for (int pp = 0; pp <= 7; pp++)
            {
                int pawn = pawns[pp];
                if (pawn == -1)
                {
                    continue;
                }
                int file = pawn % 8;
                int rank = RelativeRank(pawn, side);
                if (EnemyPawnAheadOnFile(pawn, side, enemyPawns, file))
                {
                    continue;
                }
                int sentries = 0;
                for (int ee = 0; ee <= 7; ee++)
                {
                    int enemy = enemyPawns[ee];
                    if (enemy != -1 && Math.Abs(enemy % 8 - file) == 1 && RelativeRank(enemy, side) > rank)
                    {
                        sentries += 1;
                    }
                }
                if (sentries == 0)
                {
                    //Already passed, which the passed pawn terms score
                    continue;
                }
                int supporters = 0;
                for (int oo = 0; oo <= 7; oo++)
                {
                    int own = pawns[oo];
                    if (own != -1 && Math.Abs(own % 8 - file) == 1 && RelativeRank(own, side) <= rank)
                    {
                        supporters += 1;
                    }
                }
                if (supporters >= sentries)
                {
                    count += 1;
                }
            }
            return count;
        }


        //Groups of neighbouring files that hold pawns
        private static int PawnIslands(int[] filePawns)
        {
            int islands = 0;
            bool inIsland = false;
            for (int file = 0; file <= 7; file++)
            {
                if (filePawns[file] > 0 && !inIsland)
                {
                    islands += 1;
                }
                inIsland = filePawns[file] > 0;
            }
            return islands;
        }


        //Knights, bishops, rooks and queens the enemy attacks and nothing of ours defends. ourPieces is the
        //PieceSquares of this side's PieceAttackMasks
        private static int HangingPieces(ulong ourPieces, ulong attackedByEnemy, ulong attackedByUs)
        {
            return BitOperations.PopCount(ourPieces & attackedByEnemy & ~attackedByUs);
        }


        //Squares next to the king the enemy attacks and none of our pawns, knights, bishops or rooks defend.
        //defendedWithoutQueen is the squares those four attack
        private static int WeakKingSquares(int king, ulong attackedByEnemy, ulong defendedWithoutQueen)
        {
            if (king > 63)
            {
                return 0;
            }
            return BitOperations.PopCount(_kingNeighbourMasks[king] & attackedByEnemy & ~defendedWithoutQueen);
        }


        //Squares from which an enemy knight, bishop, rook or queen that can reach them would give check, are not
        //held by an enemy piece, and that we do not attack. Each kind of check has its own weight, and two pieces of
        //the same kind checking from one square count once
        private static int SafeCheckPenalty(ref Board theBoard, int side, in PieceAttackMasks enemyPieces,
            ulong attackedByUs)
        {
            int enemy = Opponent(side);
            int king = side == WHITE ? theBoard.WhiteKingSquare : theBoard.BlackKingSquare;
            if (king > 63)
            {
                return 0;
            }

            ulong diagonalChecks = CheckRays(ref theBoard, king, enemy, true);
            ulong straightChecks = CheckRays(ref theBoard, king, enemy, false);
            ulong safe = ~attackedByUs;

            return SquaresNotHeldBy(ref theBoard, enemy, enemyPieces.Knight & _knightMoveMasks[king] & safe) *
                    SAFE_KNIGHT_CHECK_PENALTY +
                SquaresNotHeldBy(ref theBoard, enemy, enemyPieces.Bishop & diagonalChecks & safe) *
                    SAFE_BISHOP_CHECK_PENALTY +
                SquaresNotHeldBy(ref theBoard, enemy, enemyPieces.Rook & straightChecks & safe) *
                    SAFE_ROOK_CHECK_PENALTY +
                SquaresNotHeldBy(ref theBoard, enemy, enemyPieces.Queen & (diagonalChecks | straightChecks) & safe) *
                    SAFE_QUEEN_CHECK_PENALTY;
        }


        //How many of the squares in the mask hold no piece of the given side
        private static int SquaresNotHeldBy(ref Board theBoard, int side, ulong squares)
        {
            int count = 0;
            while (squares != 0)
            {
                if (theBoard.Color[BitOperations.TrailingZeroCount(squares)] != side)
                {
                    count += 1;
                }
                squares &= squares - 1;
            }
            return count;
        }


        //Walks out from the king along the diagonals (or the ranks and files), marking each square a checking piece
        //could stand on: the empty squares, and the first piece reached unless it belongs to the checking side
        private static ulong CheckRays(ref Board theBoard, int king, int checkingSide, bool diagonals)
        {
            if (diagonals)
            {
                return CheckRay(ref theBoard, king, checkingSide, -1, -1) |
                    CheckRay(ref theBoard, king, checkingSide, -1, 1) |
                    CheckRay(ref theBoard, king, checkingSide, 1, -1) |
                    CheckRay(ref theBoard, king, checkingSide, 1, 1);
            }
            return CheckRay(ref theBoard, king, checkingSide, -1, 0) |
                CheckRay(ref theBoard, king, checkingSide, 1, 0) |
                CheckRay(ref theBoard, king, checkingSide, 0, -1) |
                CheckRay(ref theBoard, king, checkingSide, 0, 1);
        }


        //One direction of CheckRays: walks out from the king one step at a time, marking the empty squares and
        //stopping at the first piece, which is marked too unless it belongs to the checking side
        private static ulong CheckRay(ref Board theBoard, int king, int checkingSide, int rowStep, int fileStep)
        {
            ulong checkSquares = 0;
            int row = king / 8 + rowStep;
            int file = king % 8 + fileStep;
            while (row >= 0 && row <= 7 && file >= 0 && file <= 7)
            {
                int square = row * 8 + file;
                if (theBoard.Color[square] == EMPTY)
                {
                    checkSquares |= 1UL << square;
                }
                else
                {
                    if (theBoard.Color[square] != checkingSide)
                    {
                        checkSquares |= 1UL << square;
                    }
                    break;
                }
                row += rowStep;
                file += fileStep;
            }
            return checkSquares;
        }
    }
}
