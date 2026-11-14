using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
    {
        //Futility and reverse futility pruning at null-window nodes 2 to 6 plies from the leaves: true if the
        //static eval plus a margin is below alpha, or minus a margin is above beta. Futility is skipped right
        //after a check, as a side mid-attack (often down material after a sacrifice) is what static eval misjudges
        private bool TryFutilityCut(Move legalMove,
                                    byte depth,
                                    int beta,
                                    bool isInCheck,
                                    bool previousMoveWasCheck,
                                    int staticEval,
                                    NodeTypes nodeType,
                                    ref int alpha
                                   )
        {
            if (beta - alpha == 1 && !legalMove.IsCapture && !isInCheck && _fullDepth >= 10 &&
                nodeType != NodeTypes.PV &&
                alpha != 5000 && beta != 5000 && alpha != -5000 && beta != -5000)
            {
                if (depth == 6 && !previousMoveWasCheck && staticEval + DEPTH6_FUTILITY_MARGIN < alpha)
                {
                    alpha = staticEval + DEPTH6_FUTILITY_MARGIN;
                    _stats.FutilityD4 += 1;
                    return true;
                }
                if (depth == 6 && staticEval - DEPTH6_REVERSE_FUTILITY_MARGIN >= beta)
                {
                    alpha = beta;
                    _stats.ReverseFutilityD4 += 1;
                    return true;
                }
            }

            if (beta - alpha == 1 && !legalMove.IsCapture && !isInCheck && _fullDepth >= 9 &&
                nodeType != NodeTypes.PV &&
                alpha != 5000 && beta != 5000 && alpha != -5000 && beta != -5000)
            {
                if (depth == 5 && !previousMoveWasCheck && staticEval + DEPTH5_FUTILITY_MARGIN < alpha)
                {
                    alpha = staticEval + DEPTH5_FUTILITY_MARGIN;
                    _stats.FutilityD4 += 1;
                    return true;
                }
                if (depth == 5 && staticEval - DEPTH5_REVERSE_FUTILITY_MARGIN >= beta)
                {
                    alpha = beta;
                    _stats.ReverseFutilityD4 += 1;
                    return true;
                }
            }

            if (beta - alpha == 1 && !legalMove.IsCapture && !isInCheck && _fullDepth >= 8 &&
                nodeType != NodeTypes.PV &&
                alpha != 5000 && beta != 5000 && alpha != -5000 && beta != -5000)
            {
                if (depth <= 4)
                {
                    if (depth == 4 && !previousMoveWasCheck && staticEval + DEPTH4_FUTILITY_MARGIN < alpha)
                    {
                        alpha = staticEval + DEPTH4_FUTILITY_MARGIN;
                        _stats.FutilityD4 += 1;
                        return true;
                    }
                    else if (depth == 3 && !previousMoveWasCheck && staticEval + DEPTH3_FUTILITY_MARGIN < alpha)
                    {
                        alpha = staticEval + DEPTH3_FUTILITY_MARGIN;
                        _stats.FutilityD3 += 1;
                        return true;
                    }
                    else if (depth == 2 && !previousMoveWasCheck && staticEval + DEPTH2_FUTILITY_MARGIN < alpha)
                    {
                        alpha = staticEval + DEPTH2_FUTILITY_MARGIN;
                        _stats.FutilityD2 += 1;
                        return true;
                    }
                    if (depth == 4 && staticEval - DEPTH4_REVERSE_FUTILITY_MARGIN >= beta)
                    {
                        alpha = beta;
                        _stats.ReverseFutilityD4 += 1;
                        return true;
                    }
                    else if (depth == 3 && staticEval - DEPTH3_REVERSE_FUTILITY_MARGIN >= beta)
                    {
                        alpha = beta;
                        _stats.ReverseFutilityD3 += 1;
                        return true;
                    }
                    else if (depth == 2 && staticEval - DEPTH2_REVERSE_FUTILITY_MARGIN >= beta)
                    {
                        alpha = beta;
                        _stats.ReverseFutilityD2 += 1;
                        return true;
                    }
                }
            }

            return false;
        }


        //Null move pruning: the side to move passes and the other side's legal replies are searched in turn,
        //2 to 5 plies shallower. Returns true, with alpha set to the static eval, as soon as one reply still leaves
        //the side that passed at beta or above. A checking reply ends the attempt
        private bool TryNullMove(Move legalMove,
                                 int rootMoveKey,
                                 byte depth,
                                 int beta,
                                 bool isInCheck,
                                 int staticEval,
                                 NodeTypes nodeType,
                                 ref Move[] localPV,
                                 ref int alpha
                                )
        {
            if (beta - alpha == 1 &&
                !_endGame && 
                staticEval > beta && 
                beta != -5000 && 
                depth < _fullDepth && 
                !isInCheck && 
                depth > 2 &&
                (nodeType == NodeTypes.Cut || depth <= 4) && 
                legalMove.PromotionPiece == 0
                )
            {
                _stats.NullMoveAttempts += 1;
                _theBoard.MakeMove(_phantomMove, _theBoard.OnMove, true);
                int savedRepetitionFloor = _repetitionFloor;
                _repetitionFloor = _ply + 1;
                int moveStackMark = _moveStackTop;
                Span<Move> AllMovesAfterOppNull = TakeSortedMoves(false, false, false);
                for (int nn = 0; nn < AllMovesAfterOppNull.Length; nn++)
                {
                    if (_theBoard.MoveIsLegal(AllMovesAfterOppNull[nn], _theBoard.OnMove))
                    {
                        int score;
                        if (depth >= 10)
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, beta - 1, beta,
                                (byte)(depth - 5), ref localPV);
                        }
                        else if (depth >= 8)
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, beta - 1, beta,
                                (byte)(depth - 4), ref localPV);
                        }
                        else if (depth >= 6)
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, beta - 1, beta,
                                (byte)(depth - 3), ref localPV);
                        }
                        else
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, beta - 1, beta,
                                (byte)(depth - 2), ref localPV);
                        }

                        if (score >= beta)
                        {
                            _stats.NullMoveCutOffs += 1;
                            alpha = staticEval;
                            _moveStackTop = moveStackMark;
                            _repetitionFloor = savedRepetitionFloor;
                            _theBoard.UnmakeLastMove(); //Unmake the Null Move
                            return true;
                        }
                        else if (GivesCheck(AllMovesAfterOppNull[nn]))
                        {
                            //A checking reply refutes the null move outright: passing has allowed a forcing threat
                            break;
                        }
                    }
                }

                _moveStackTop = moveStackMark;
                _repetitionFloor = savedRepetitionFloor;
                _theBoard.UnmakeLastMove(); //Unmake the Null move
            }

            return false;
        }


        //At expected Cut nodes deep enough in the tree, searches the first third of the moves 4 plies
        //shallower. Returns true, with alpha set to beta, once three of them reach beta
        private bool TryProbCut(NodeTypes nodeType,
                                byte depth,
                                int beta,
                                bool isInCheck,
                                int rootMoveKey,
                                ref NodeState node
                               )
        {
            if (!isInCheck && nodeType == NodeTypes.Cut && depth > 4 && depth < _fullDepth && _fullDepth > 8)
            {
                _stats.ProbCutAttempts += 1;

                int moveStackMark = _moveStackTop;
                Span<Move> AllProbCutOppMoves = TakeSortedMoves(false, false, false);
                int BetaExceeded = 0;
                for (int nn = 0; nn < AllProbCutOppMoves.Length / 3; nn++)
                {
                    if (_theBoard.MoveIsLegal(AllProbCutOppMoves[nn], _theBoard.OnMove))
                    {
                        bool nullWindowSearchFailed = false; bool nullWindowSearch;

                    ProbCuteResearchAfterNull:

                        int score;

                        if (node.AlphaRaised && !nullWindowSearchFailed)
                        {
                            nullWindowSearch = true;
                            score = 0 - EvaluateMove(AllProbCutOppMoves[nn], rootMoveKey, -node.Alpha - 1, -node.Alpha,
                                (byte)(depth - 4), ref node.LocalPV);
                        }
                        else
                        {
                            nullWindowSearch = false;
                            score = 0 - EvaluateMove(AllProbCutOppMoves[nn], rootMoveKey, -beta, -node.Alpha,
                                (byte)(depth - 4), ref node.LocalPV);
                        }

                        if (nullWindowSearch && score > node.Alpha)
                        {
                            nullWindowSearchFailed = true;
                            goto ProbCuteResearchAfterNull;
                        }

                        if (score >= beta)
                        {
                            BetaExceeded += 1;
                            if (BetaExceeded >= 3)
                            {
                                _stats.ProbCutCutOffs += 1;
                                node.Alpha = beta;
                                _moveStackTop = moveStackMark;
                                return true;
                            }
                        }
                    }
                }
                _moveStackTop = moveStackMark;
            }

            return false;
        }


        //A quiet move is skipped when its node is 2 to 5 plies from the leaves (LMP_QUIET_MOVES_D2..D5, 0 = off), at
        //least that many quiet moves have been reached, the node is a null-window non-PV node, the side to move is not
        //in check or already being mated, and the move is not an endgame pawn move and does not give check. GivesCheck
        //is last as it is the expensive test
        private bool IsLateMovePrunable(Move move, int depth, int quietPlayed, int alpha, int beta, NodeTypes nodeType,
            bool isInCheck)
        {
            int threshold = LateMovePruningThreshold(depth);
            if (threshold == 0 || quietPlayed < threshold)
            {
                return false;
            }
            if (beta - alpha != 1 || nodeType == NodeTypes.PV || isInCheck || alpha <= -4000)
            {
                return false;
            }
            if (_endGame && _theBoard.Piece[move.From] == PAWN)
            {
                return false;
            }
            return !GivesCheck(move);
        }


        //The number of quiet moves after which LMP starts skipping them at this depth, or 0 for none
        private static int LateMovePruningThreshold(int depth)
        {
            switch (depth)
            {
                case 2:
                    return LMP_QUIET_MOVES_D2;
                case 3:
                    return LMP_QUIET_MOVES_D3;
                case 4:
                    return LMP_QUIET_MOVES_D4;
                case 5:
                    return LMP_QUIET_MOVES_D5;
                default:
                    return 0;
            }
        }


        //True if the move puts the opponent in check
        private bool GivesCheck(Move move)
        {
            return _theBoard.MoveGivesCheck(move, _theBoard.OnMove);
        }


        //True if, after this move, a pass by the opponent would allow us a mate in one. Used to keep
        //mating attacks safe from root pruning, as their payoff is often beyond the shallow iterations
        private bool ThreatensMateInOne(Move move)
        {
            bool threatensMate = false;
            _theBoard.MakeMove(move, _theBoard.OnMove, false);
            _theBoard.MakeMove(_phantomMove, _theBoard.OnMove, true);

            Move[] ourMoves = _theBoard.GenerateAllMoves(_theBoard.OnMove);
            for (int nn = 0; nn < ourMoves.Length; nn++)
            {
                if (!_theBoard.MoveIsLegal(ourMoves[nn], _theBoard.OnMove))
                {
                    continue;
                }
                _theBoard.MakeMove(ourMoves[nn], _theBoard.OnMove, false);
                if (_theBoard.IsInCheck(_theBoard.OnMove) && !HasLegalMove())
                {
                    threatensMate = true;
                }
                _theBoard.UnmakeLastMove();
                if (threatensMate)
                {
                    break;
                }
            }

            _theBoard.UnmakeLastMove();
            _theBoard.UnmakeLastMove();
            return threatensMate;
        }


        //True if the side to move has at least one legal move
        private bool HasLegalMove()
        {
            Move[] moves = _theBoard.GenerateAllMoves(_theBoard.OnMove);
            for (int nn = 0; nn < moves.Length; nn++)
            {
                if (_theBoard.MoveIsLegal(moves[nn], _theBoard.OnMove))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
