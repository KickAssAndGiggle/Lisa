using static Lisa.Globals;
namespace Lisa
{
    //Move ordering state: quiet history, cut counts, killers and refutations. Each searching thread owns one, so the
    //threads never write into each other's ordering
    public sealed class Sorter
    {

        private const int MAX_DEPTH = 30;

        //Quiet move history, by side to move, from square and to square. Every update pulls the value towards
        //+/-QUIET_HISTORY_MAX in proportion to how far it still has to go, so values stay bounded and recent results
        //can overturn old ones
        private const int QUIET_HISTORY_MAX = 16384;
        private const int QUIET_HISTORY_BONUS_CAP = 1200;

        //Each root move gets its own 64 x 64 row of cut counts. A position has at most 218 legal moves
        private const int MAX_ROOT_MOVES = 256;

        //Public fields rather than properties: this is faster than the "correct" practice, and I've tested it enough
        //times to be certain about it by now

        public readonly int[] QuietHistory = new int[2 * 64 * 64];
        public readonly int[] CutCount = new int[6363];

        public readonly Move[] KillerOnes = new Move[MAX_DEPTH];
        public readonly Move[] KillerTwos = new Move[MAX_DEPTH];
        public readonly int[] FirstKillerPieces = new int[MAX_DEPTH];
        public readonly int[] SecondKillerPieces = new int[MAX_DEPTH];

        public readonly Move[] Refutations = new Move[MAX_DEPTH];
        public readonly int[] RefutationPieces = new int[MAX_DEPTH];

        //Killer scores for each from square, to square and ply from the root
        private readonly int[,,] _killerScores = new int[64, 64, MAX_DEPTH];

        //Cut counts under each root move: row _rootMoveRow[rootMoveKey], column from * 64 + to
        private readonly int[] _cutCountUnderRootMove = new int[MAX_ROOT_MOVES * 64 * 64];
        private readonly int[] _rootMoveRow = new int[6363];


        public Move[] GetSortedMoves(ref Board theBoard, bool capturesOnly, bool nonCapturesOnly,
            bool lowDepth = false)
        {

            Move[] list;
            if (capturesOnly)
            {
                if (lowDepth)
                {
                    list = theBoard.GenerateCaptureMovesWithScore(theBoard.OnMove);
                    return list;
                }
                else
                {
                    list = theBoard.GenerateCaptureMoves(theBoard.OnMove);
                }
            }
            else if (nonCapturesOnly)
            {
                list = theBoard.GenerateNonCaptureMoves(theBoard.OnMove);
            }
            else
            {
                list = theBoard.GenerateAllMoves(theBoard.OnMove);
            }

            for (int nn = 0; nn < list.Length; nn++)
            {
                if (list[nn].IsCapture)
                {
                    if (theBoard.Piece[list[nn].To] != -1)
                    {
                        //No en-pasant
                        list[nn].Score = 4000 + theBoard.See(list[nn].To, list[nn].From);
                        if (list[nn].Score == 4000)
                        {
                            list[nn].Score += theBoard.Piece[list[nn].To];
                        }
                    }
                    else
                    {
                        //en-pasant: as a one-off chance, score it highly
                        list[nn].Score = 4750;
                    }
                }
                else
                {
                    list[nn].Score = QuietHistory[QuietHistoryIndex(theBoard.OnMove, list[nn])];
                }
            }

            Array.Sort(list);
            return list;

        }


        //Called with the node's side to move and remaining depth: the quiet move that caused a cutoff gets a bonus, and
        //every quiet move searched before it in that node gets the same amount taken off
        public void RewardQuietMove(int side, Move move, int depth)
        {
            UpdateQuietHistory(QuietHistoryIndex(side, move), Math.Min(depth * depth, QUIET_HISTORY_BONUS_CAP));
        }


        public void PenaliseQuietMove(int side, Move move, int depth)
        {
            UpdateQuietHistory(QuietHistoryIndex(side, move), -Math.Min(depth * depth, QUIET_HISTORY_BONUS_CAP));
        }


        private void UpdateQuietHistory(int index, int bonus)
        {
            QuietHistory[index] += bonus - QuietHistory[index] * Math.Abs(bonus) / QUIET_HISTORY_MAX;
        }


        private static int QuietHistoryIndex(int side, Move move)
        {
            return (side == WHITE ? 0 : 4096) + move.From * 64 + move.To;
        }


        //Gives each root move its own row of cut counts. Called once per search, with the root's legal moves
        public void SetRootMoves(Move[] rootMoves)
        {
            for (int nn = 0; nn < rootMoves.Length; nn++)
            {
                _rootMoveRow[rootMoves[nn].From * 100 + rootMoves[nn].To] = nn;
            }
        }


        //Counts a cutoff by a move, overall and under the current root move. Quiet IID adds these counts to its
        //scores. Move keys are from * 100 + to
        public void CountCut(int rootMoveKey, int moveKey)
        {
            CutCount[moveKey] += 1;
            _cutCountUnderRootMove[CutCountUnderRootMoveIndex(rootMoveKey, moveKey)] += 1;
        }


        public int CutCountUnderRootMove(int rootMoveKey, int moveKey)
        {
            return _cutCountUnderRootMove[CutCountUnderRootMoveIndex(rootMoveKey, moveKey)];
        }


        private int CutCountUnderRootMoveIndex(int rootMoveKey, int moveKey)
        {
            return _rootMoveRow[rootMoveKey] * 4096 + (moveKey / 100) * 64 + moveKey % 100;
        }


        public void IncreaseKillerScore(int fullDepth, int depth, Move kMove, int piece, int score)
        {
            _killerScores[kMove.From, kMove.To, fullDepth - depth] += score;
            ReplaceKillerIfAppropriate(kMove, fullDepth - depth, piece);
        }


        private void ReplaceKillerIfAppropriate(Move kMove, int killerDepth, int killerPiece)
        {

            if ((kMove.From == KillerOnes[killerDepth].From && kMove.To == KillerOnes[killerDepth].To) ||
                (kMove.From == KillerTwos[killerDepth].From && kMove.To == KillerTwos[killerDepth].To))
            {
                return;
            }

            if (KillerOnes[killerDepth].From == 0 && KillerOnes[killerDepth].To == 0)
            {
                KillerOnes[killerDepth] = kMove;
                FirstKillerPieces[killerDepth] = killerPiece;
            }
            else
            {
                if (_killerScores[kMove.From, kMove.To, killerDepth] > _killerScores[KillerOnes[killerDepth].From,
                    KillerOnes[killerDepth].To, killerDepth])
                {
                    KillerTwos[killerDepth] = KillerOnes[killerDepth];
                    SecondKillerPieces[killerDepth] = FirstKillerPieces[killerDepth];
                    KillerOnes[killerDepth] = kMove;
                    FirstKillerPieces[killerDepth] = killerPiece;
                }
                else
                {
                    if (KillerTwos[killerDepth].From == 0 && KillerTwos[killerDepth].To == 0)
                    {
                        KillerTwos[killerDepth] = kMove;
                        SecondKillerPieces[killerDepth] = killerPiece;
                    }
                    else
                    {
                        if (_killerScores[kMove.From, kMove.To, killerDepth] >
                            _killerScores[KillerTwos[killerDepth].From, KillerTwos[killerDepth].To, killerDepth])
                        {
                            KillerTwos[killerDepth] = kMove;
                            SecondKillerPieces[killerDepth] = killerPiece;
                        }
                    }
                }
            }

        }


        public void SetRefutationMove(int fullDepth, int refutationDepth, Move rMove, int fromSquarePiece)
        {
            Refutations[fullDepth - refutationDepth] = rMove;
            RefutationPieces[fullDepth - refutationDepth] = fromSquarePiece;
        }


        public void Reset()
        {
            Array.Clear(QuietHistory);
            Array.Clear(CutCount);
            Array.Clear(_cutCountUnderRootMove);
            Array.Clear(_killerScores);
            Array.Clear(KillerOnes);
            Array.Clear(KillerTwos);
            Array.Clear(FirstKillerPieces);
            Array.Clear(SecondKillerPieces);
            Array.Clear(Refutations);
            Array.Clear(RefutationPieces);
        }

    }

}
