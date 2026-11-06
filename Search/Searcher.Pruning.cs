using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
    {

        //previousMoveWasCheck: the opponent has just answered our check. Futility is skipped there, as a side
        //mid-attack (often material down after a sacrifice) is exactly what static eval misjudges
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
                Move[] AllMovesAfterOppNull = Sorter.GetSortedMoves(ref _theBoard, false, false);
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

                _repetitionFloor = savedRepetitionFloor;
                _theBoard.UnmakeLastMove(); //Unmake the Null move
            }

            return false;

        }


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

                Move[] AllProbCutOppMoves = Sorter.GetSortedMoves(ref _theBoard, false, false);
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
                                return true;
                            }
                        }

                    }
                }
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


        private bool GivesCheck(Move move)
        {
            _theBoard.MakeMove(move, _theBoard.OnMove, false);
            bool givesCheck = _theBoard.IsInCheck(_theBoard.OnMove);
            _theBoard.UnmakeLastMove();
            return givesCheck;
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
