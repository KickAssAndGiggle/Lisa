using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
    {

        private int Quiesce(Move legalMove,
                            int alpha,
                            int beta,
                            byte QuiesceDepth,
                            int RootMoveKey
                           )
        {

            if (_cancel)
            {
                return -5000;
            }

            _stats.NodesLookedAt += 1;
            _stats.NodesQuiesced += 1;

            int materialCaptured = 0;
            if (legalMove.IsCapture)
            {
                if (_theBoard.Piece[legalMove.To] != -1)
                {
                    materialCaptured = MATERIAL[_theBoard.Piece[legalMove.To]];
                }
                else
                {
                    materialCaptured = 100;
                }
            }

            bool isIrreversible = MoveIsIrreversible(legalMove);
            _theBoard.MakeMove(legalMove, _theBoard.OnMove, false);

            //Only the first move into quiescence can be quiet, so only it can repeat a position
            if (QuiesceDepth == 0 && !isIrreversible)
            {
                _pathZobrist[_ply + 1] = _theBoard.CurrentZobrist;
                if (IsRepetition(_ply + 1))
                {
                    _theBoard.UnmakeLastMove();
                    return DrawScoreInWindow(alpha, beta);
                }
            }

            if (_transTable.LookupPos(_theBoard.CurrentZobrist, out TTMove transTableMove))
            {
                NodeTypes nodeType;
                if (transTableMove.PositionFlag >= 200)
                {
                    nodeType = NodeTypes.Cut;
                }
                else if (transTableMove.PositionFlag >= 100)
                {
                    nodeType = NodeTypes.All;
                }
                else
                {
                    nodeType = NodeTypes.PV;
                }
                if (nodeType == NodeTypes.PV)
                {
                    _theBoard.UnmakeLastMove();
                    alpha = transTableMove.Score;
                    return alpha;
                }
                else if (nodeType == NodeTypes.Cut)
                {
                    if (transTableMove.Score >= beta)
                    {
                        _theBoard.UnmakeLastMove();
                        alpha = transTableMove.Score;
                        return alpha;
                    }
                }
                else if (nodeType == NodeTypes.All)
                {
                    if (transTableMove.Score < alpha)
                    {
                        _theBoard.UnmakeLastMove();
                        alpha = transTableMove.Score;
                        return alpha;
                    }
                }
            }

            int StandPat = 0 - Scorer.ScorePosition(ref _theBoard, ref _transTable, alpha, beta, true,
                materialCaptured);
            bool isInCheck = QuiesceDepth == 0 ? _theBoard.IsInCheck(_theBoard.OnMove) : false;

            if (StandPat < alpha && !isInCheck)
            {
                if (StandPat + MATERIAL[QUEEN] < alpha)
                {
                    _theBoard.UnmakeLastMove();
                    return StandPat + MATERIAL[QUEEN];
                }
                else
                {
                    if (_theBoard.WhiteQueenSquare == 255 && _theBoard.BlackQueenSquare == 255)
                    {
                        if (StandPat + MATERIAL[ROOK] < alpha)
                        {
                            _theBoard.UnmakeLastMove();
                            return StandPat + MATERIAL[ROOK];
                        }
                    }
                }
            }

            if (StandPat >= beta && !isInCheck && !(QuiesceDepth == 0 && legalMove.IsCapture))
            {
                if (QuiesceDepth == 0)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, StandPat, 1, _phantomMove,
                        TT_POS_TYPE_BETA_CUT);
                }
                _theBoard.UnmakeLastMove();
                return StandPat;
            }

            if (alpha < StandPat && !isInCheck)
            {
                alpha = StandPat;
            }

            Move[] allOppMoves;
            if (isInCheck && QuiesceDepth == 0)
            {
                allOppMoves = Sorter.GetSortedMoves(ref _theBoard, !isInCheck, false, true);
            }
            else
            {
                allOppMoves = _theBoard.GenerateCaptureMovesWithScore(_theBoard.OnMove);
                Array.Sort(allOppMoves);
            }

            bool alphaRaised = false;
            bool betaCutoff = false;
            for (int nn = 0; nn < allOppMoves.Length; nn++)
            {
                if (!isInCheck && allOppMoves[nn].IsCapture)
                {
                    if (legalMove.IsCapture)
                    {
                        if (_theBoard.Piece[allOppMoves[nn].To] != -1 &&
                            StandPat + MATERIAL[_theBoard.Piece[allOppMoves[nn].To]] < alpha - 25)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (_theBoard.Piece[allOppMoves[nn].To] != -1 &&
                            StandPat + MATERIAL[_theBoard.Piece[allOppMoves[nn].To]] < alpha)
                        {
                            continue;
                        }
                    }
                }
                bool NoSeeNeeded = false;
                if (allOppMoves[nn].IsCapture)
                {
                    if (_theBoard.Piece[allOppMoves[nn].From] == KING || _theBoard.Piece[allOppMoves[nn].From] == PAWN)
                    {
                        NoSeeNeeded = true;
                    }
                    else if (_theBoard.Piece[allOppMoves[nn].From] == KNIGHT ||
                        _theBoard.Piece[allOppMoves[nn].From] == BISHOP)
                    {
                        if (_theBoard.Piece[allOppMoves[nn].To] != PAWN)
                        {
                            NoSeeNeeded = true;
                        }
                    }
                    else if (_theBoard.Piece[allOppMoves[nn].From] == ROOK)
                    {
                        if (_theBoard.Piece[allOppMoves[nn].To] == ROOK || _theBoard.Piece[allOppMoves[nn].To] == QUEEN)
                        {
                            NoSeeNeeded = true;
                        }
                    }
                    else if (_theBoard.Piece[allOppMoves[nn].From] == QUEEN)
                    {
                        if (_theBoard.Piece[allOppMoves[nn].To] == QUEEN)
                        {
                            NoSeeNeeded = true;
                        }
                    }
                }
                if (!allOppMoves[nn].IsCapture || NoSeeNeeded ||
                    _theBoard.See(allOppMoves[nn].To, allOppMoves[nn].From) >= 0)
                {
                    if (_theBoard.MoveIsLegal(allOppMoves[nn], _theBoard.OnMove))
                    {
                        int score = 0 - Quiesce(allOppMoves[nn], 0 - beta, 0 - alpha, (byte)(QuiesceDepth + 1),
                            RootMoveKey);
                        if (score >= beta)
                        {
                            alpha = score;
                            betaCutoff = true;
                            break;
                        }
                        if (score > alpha)
                        {
                            alpha = score;
                            alphaRaised = true;
                        }

                    }
                }
            }

            if (QuiesceDepth == 0)
            {
                if (betaCutoff)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, 1, _phantomMove, TT_POS_TYPE_BETA_CUT);
                }
                else if (alphaRaised)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, 1, _phantomMove, TT_POS_TYPE_EXACT);
                }
                else
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, 1, _phantomMove,
                        TT_POS_TYPE_ALPHA_NOT_BEATEN);
                }
            }

            _theBoard.UnmakeLastMove();
            return alpha;

        }


        private int DepthOneEval(Move legalMove,
                                 int alpha,
                                 int beta,
                                 int rootMoveKey
                                )
        {

            alpha = Quiesce(legalMove, alpha, beta, 0, rootMoveKey);
            return alpha;

        }

    }
}
