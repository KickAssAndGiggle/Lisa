using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
    {

        //The state of the node EvaluateMove is searching, shared by ref with the move stages (TryPrimaryVariation,
        //TryKiller and the rest). A plain struct on the stack rather than a ref struct: same storage, and passing ref
        //node together with ref node.SomeField is always allowed
        private struct NodeState
        {
            public int Alpha;
            public int Played;
            public bool AlphaRaised;
            public bool AlphaRaisedByNonGeneratedMove;
            public bool AlphaRaisedByKillerOne;
            public bool AlphaRaisedByKillerTwo;
            public bool AlphaRaisedByRefutation;
            public bool BetaCutoff;
            public int BestIndex;
            public bool CapsOnlyCut;
            public bool CapsOnlyAlphaRaised;
            public bool KillerOneSeparated;
            public bool KillerTwoSeparated;
            public bool RefutationSeparated;
            public Move[] LocalPV;
            public Move[] PV;
        }


        private int EvaluateMove(Move legalMove,
                                 int rootMoveKey,
                                 int alphaOnEntry,
                                 int beta,
                                 byte depth,
                                 ref Move[] pv
                                )
        {

            int ourDepth = _fullDepth - depth;

            NodeState node = new()
            {
                Alpha = alphaOnEntry,
                BestIndex = -1,
                LocalPV = new Move[depth],
                PV = pv
            };

            //Clear this node's slots in the caller's PV, so an early return can't leave a sibling's line behind
            Array.Clear(node.PV, 0, depth);

            if (depth == 1)
            {
                return DepthOneEval(legalMove, node.Alpha, beta, rootMoveKey);
            }

            _stats.NodesLookedAt += 1; _stats.NodesLookedAtWithoutQuiesce += 1;
            bool isIrreversible = MoveIsIrreversible(legalMove);
            _theBoard.MakeMove(legalMove, _theBoard.OnMove, false);
            bool isInCheck = _theBoard.IsInCheck(_theBoard.OnMove);
            _ply += 1;
            _inCheckAtPly[_ply] = isInCheck;

            int savedRepetitionFloor = _repetitionFloor;
            _pathZobrist[_ply] = _theBoard.CurrentZobrist;
            if (isIrreversible)
            {
                _repetitionFloor = _ply;
            }
            else if (IsRepetition(_ply))
            {
                node.Alpha = DrawScoreInWindow(node.Alpha, beta);
                goto ReturnEarly;
            }
            bool hasTTMove = false;

            bool ttCut = TryTranspositionTable(depth, _theBoard.CurrentZobrist, beta, ref node.Alpha, out Move ttMove,
                out NodeTypes nodeType);

            if (ttCut)
            {
                goto ReturnEarly;
            }
            else
            {
                if (ttMove.From != 0 || ttMove.To != 0)
                {
                    hasTTMove = true;
                }
            }

            int staticEval = 0 - Scorer.ScorePosition(ref _theBoard, ref _transTable, node.Alpha, beta, false, 0);
            bool futilityCut = TryFutilityCut(legalMove, depth, beta, isInCheck, _inCheckAtPly[_ply - 1], staticEval,
                nodeType, ref node.Alpha);
            if (futilityCut)
            {
                goto ReturnEarly;
            }

            bool nullCut = TryNullMove(legalMove, rootMoveKey, depth, beta, isInCheck, staticEval, nodeType,
                ref node.LocalPV, ref node.Alpha);
            if (nullCut)
            {
                goto ReturnEarly;
            }

            Move[]? allOppMoves = null;
            Move[]? allOpCaps = null;

            if (hasTTMove)
            {
                bool pvCut = TryPrimaryVariation(hasTTMove, ttMove, depth, beta, rootMoveKey, ref node);
                if (pvCut)
                {
                    Sorter.SetRefutationMove(_fullDepth, depth, ttMove, _theBoard.Piece[ttMove.From]);
                    int MoveKey = ttMove.From * 100 + ttMove.To;
                    Sorter.CountCut(rootMoveKey, MoveKey);
                    if (!ttMove.IsCapture)
                    {
                        Sorter.RewardQuietMove(_theBoard.OnMove, ttMove, depth);
                    }
                    goto ReturnEarly;
                }
            }

            bool refutationCut = TryRefutationMove(ttMove, nodeType, depth, beta, rootMoveKey, ref node);

            if (refutationCut)
            {
                int MoveKey = Sorter.Refutations[ourDepth].From * 100 + Sorter.Refutations[ourDepth].To;
                Sorter.CountCut(rootMoveKey, MoveKey);
                if (!Sorter.Refutations[ourDepth].IsCapture)
                {
                    Sorter.RewardQuietMove(_theBoard.OnMove, Sorter.Refutations[ourDepth], depth);
                }
                goto ReturnEarly;
            }

            bool iidUsedForCaps = false;
            if (!hasTTMove && ((nodeType != NodeTypes.All && nodeType != NodeTypes.Unknown) ||
                depth >= _fullDepth - 1) && depth >= 6)
            {

                iidUsedForCaps = true;
                
                int iidAlpha = node.Alpha;
                int iidBeta = beta;
                allOpCaps = Sorter.GetSortedMoves(ref _theBoard, true, false, true);

                byte iidReduceDepth = (byte)(depth - 4);
                for (int nn = 0; nn < allOpCaps.Length; nn++)
                {
                    if (!_theBoard.MoveIsLegal(allOpCaps[nn], _theBoard.OnMove))
                    {
                        allOpCaps[nn].Score = -5000;
                        continue;
                    }
                    allOpCaps[nn].Score = -EvaluateMove(allOpCaps[nn], rootMoveKey, -iidBeta, -iidAlpha, iidReduceDepth,
                        ref node.LocalPV);
                    if (allOpCaps[nn].Score > iidAlpha)
                    {
                        iidAlpha = allOpCaps[nn].Score;
                    }
                }
                Array.Sort(allOpCaps);
            }

            if (allOpCaps == null)
            {
                allOpCaps = Sorter.GetSortedMoves(ref _theBoard, true, false,
                    (depth <= _fullDepth - 6 && nodeType != NodeTypes.PV));
            }

            bool winningCapsCut = TryWinningCaptures(allOpCaps, ttMove, nodeType, depth, beta, isInCheck, rootMoveKey,
                iidUsedForCaps, staticEval, ref node);

            if (winningCapsCut)
            {
                goto StoreInTransTable;
            }


            //Killers are not tried at expected All-nodes: nothing cuts there, and the quiet loop searches them reduced
            if (nodeType != NodeTypes.All)
            {
                if (!node.RefutationSeparated ||
                    (Sorter.KillerOnes[ourDepth].From != Sorter.Refutations[ourDepth].From ||
                    Sorter.KillerOnes[ourDepth].To != Sorter.Refutations[ourDepth].To))
                {
                    bool killerOneCut = TryKiller(Sorter.KillerOnes, Sorter.FirstKillerPieces, ref _stats.KillerOne, 10,
                        false, ttMove, nodeType, depth, beta, rootMoveKey,
                        ref node, ref node.KillerOneSeparated, ref node.AlphaRaisedByKillerOne);

                    if (killerOneCut)
                    {
                        goto ReturnEarly;
                    }
                }
                else
                {
                    _stats.KillerOne.SameAsRefutation += 1;
                }

                if (!node.RefutationSeparated ||
                    (Sorter.KillerTwos[ourDepth].From != Sorter.Refutations[ourDepth].From ||
                    Sorter.KillerTwos[ourDepth].To != Sorter.Refutations[ourDepth].To))
                {
                    bool killerTwoCut = TryKiller(Sorter.KillerTwos, Sorter.SecondKillerPieces, ref _stats.KillerTwo, 8,
                        true, ttMove, nodeType, depth, beta, rootMoveKey,
                        ref node, ref node.KillerTwoSeparated, ref node.AlphaRaisedByKillerTwo);

                    if (killerTwoCut)
                    {
                        goto ReturnEarly;
                    }
                }
                else
                {
                    _stats.KillerTwo.SameAsRefutation += 1;
                }
            }
            else
            {
                _stats.KillerStageSkippedAtAll += 1;
            }

            bool probCutCut = TryProbCut(nodeType, depth, beta, isInCheck, rootMoveKey, ref node);
            if (probCutCut)
            {
                goto ReturnEarly;
            }

            bool losingCapsCut = TryLosingCaptures(allOpCaps, ttMove, nodeType, depth, beta, rootMoveKey,
                iidUsedForCaps, staticEval, ref node);
            if (losingCapsCut)
            {
                goto StoreInTransTable;
            }

            if (!hasTTMove && ((nodeType != NodeTypes.All && nodeType != NodeTypes.Unknown) ||
                depth >= _fullDepth - 1) && depth >= 6)
            {
                int iidAlpha = node.Alpha;
                int iidBeta = beta;

                allOppMoves = Sorter.GetSortedMoves(ref _theBoard, false, true, true);

                byte iidReduceDepth = (byte)(depth - 4);
                for (int nn = 0; nn < allOppMoves.Length; nn++)
                {
                    if (!_theBoard.MoveIsLegal(allOppMoves[nn], _theBoard.OnMove))
                    {
                        allOppMoves[nn].Score = -5000;
                        continue;
                    }
                    allOppMoves[nn].Score = -EvaluateMove(allOppMoves[nn], rootMoveKey, -iidBeta, -iidAlpha,
                        iidReduceDepth, ref node.LocalPV);
                    if (allOppMoves[nn].Score > iidAlpha)
                    {
                        iidAlpha = allOppMoves[nn].Score;
                    }
                    int moveKey = allOppMoves[nn].From * 100 + allOppMoves[nn].To;
                    allOppMoves[nn].Score += Sorter.CutCount[moveKey];
                    allOppMoves[nn].Score += Sorter.CutCountSingleMove[rootMoveKey, moveKey];
                }
                Array.Sort(allOppMoves);
            }

            if (allOppMoves == null)
            {
                allOppMoves = Sorter.GetSortedMoves(ref _theBoard, false, true, (depth >= 5));
            }

            int quietPlayed = 0;
            //Quiet moves searched in this node without causing a cutoff: if a later quiet move cuts, each of these
            //loses history
            Span<Move> quietsSearched = stackalloc Move[allOppMoves.Length];
            int quietsSearchedCount = 0;
            for (int nn = 0; nn < allOppMoves.Length; nn++)
            {
                if ((ttMove.From == allOppMoves[nn].From && ttMove.To == allOppMoves[nn].To &&
                    ttMove.PromotionPiece == allOppMoves[nn].PromotionPiece) ||
                    (node.KillerOneSeparated && allOppMoves[nn].From == Sorter.KillerOnes[ourDepth].From &&
                        allOppMoves[nn].To == Sorter.KillerOnes[ourDepth].To) ||
                    (node.KillerTwoSeparated && allOppMoves[nn].From == Sorter.KillerTwos[ourDepth].From &&
                        allOppMoves[nn].To == Sorter.KillerTwos[ourDepth].To) ||
                    (node.RefutationSeparated && allOppMoves[nn].From == Sorter.Refutations[ourDepth].From &&
                        allOppMoves[nn].To == Sorter.Refutations[ourDepth].To))
                {
                    continue;
                }
                if (allOppMoves[nn].Score > -4997 && _theBoard.MoveIsLegal(allOppMoves[nn], _theBoard.OnMove))
                {
                    node.Played += 1; quietPlayed += 1;

                    int SearchDepth = (int)depth;

                    //Late move pruning: near the leaves, quiet moves after the first few are not searched at all
                    if (IsLateMovePrunable(allOppMoves[nn], SearchDepth, quietPlayed, node.Alpha, beta, nodeType,
                        isInCheck))
                    {
                        _stats.LMPPruned[SearchDepth - 2] += 1;
                        continue;
                    }

                    byte ByteDepth;
                    bool WasReduced = false;
                    byte originalDepth = (byte)(SearchDepth - 1);
                    //Checks are never reduced: GivesCheck is last so it only runs for moves that would otherwise be
                    //reduced
                    if (isInCheck || quietPlayed == 1 || (_endGame && _theBoard.Piece[allOppMoves[nn].From] == PAWN) ||
                        GivesCheck(allOppMoves[nn]))
                    {
                        ByteDepth = (byte)(SearchDepth - 1);
                    }
                    else
                    {
                        if (SearchDepth - 3 < 1)
                        {
                            ByteDepth = 1;
                        }
                        else
                        {
                            if (SearchDepth - 6 >= 1 && quietPlayed > 10)
                            {
                                ByteDepth = (byte)(SearchDepth - 6);
                            }
                            else if (SearchDepth - 5 >= 1 && quietPlayed > 5)
                            {
                                ByteDepth = (byte)(SearchDepth - 5);
                            }
                            else if (SearchDepth - 4 >= 1)
                            {
                                ByteDepth = (byte)(SearchDepth - 4);
                            }
                            else
                            {
                                ByteDepth = (byte)(SearchDepth - 3);
                            }
                        }
                        //At depth 2 the "reduced" depth is the normal depth: not flagging it avoids an identical
                        //re-search
                        WasReduced = ByteDepth < SearchDepth - 1;
                    }

                    int lmrReduction = WasReduced ? (SearchDepth - 1) - ByteDepth : 0;
                    if (WasReduced)
                    {
                        _stats.LMRReducedBy[lmrReduction] += 1;
                    }
                    else
                    {
                        _stats.LMRNotReduced += 1;
                    }
                    bool lmrReSearched = false;
                    int nodesBeforeLMRReSearch = 0;

                    bool nullWindowSearchFailed = false;

                ReducedBeatAlpha:

                    int score;
                    bool nullWindowSearch = false;

                    if (depth >= _fullDepth - 5 || quietPlayed == 1 || nullWindowSearchFailed)
                    {
                        score = 0 - EvaluateMove(allOppMoves[nn], rootMoveKey, -beta, -node.Alpha, ByteDepth,
                            ref node.LocalPV);
                    }
                    else
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(allOppMoves[nn], rootMoveKey, -node.Alpha - 1, -node.Alpha, ByteDepth,
                            ref node.LocalPV);
                    }

                    if (nullWindowSearch && score > node.Alpha && score < beta)
                    {
                        if (WasReduced && !lmrReSearched)
                        {
                            lmrReSearched = true;
                            nodesBeforeLMRReSearch = _stats.NodesLookedAt;
                        }
                        nullWindowSearchFailed = true;
                        WasReduced = false;
                        ByteDepth = originalDepth;
                        goto ReducedBeatAlpha;
                    }
                    if (WasReduced && score > node.Alpha)
                    {
                        if (!lmrReSearched)
                        {
                            lmrReSearched = true;
                            nodesBeforeLMRReSearch = _stats.NodesLookedAt;
                        }
                        WasReduced = false;
                        ByteDepth = originalDepth;
                        _stats.LMRReSearches += 1;
                        goto ReducedBeatAlpha;
                    }

                    if (lmrReSearched)
                    {
                        _stats.LMRReSearchNodes += _stats.NodesLookedAt - nodesBeforeLMRReSearch;
                        if (score >= beta)
                        {
                            _stats.LMRReSearchCut += 1;
                            _stats.LMRReSearchCutBy[lmrReduction] += 1;
                        }
                        else if (score > node.Alpha)
                        {
                            _stats.LMRReSearchRaised += 1;
                            _stats.LMRReSearchRaisedBy[lmrReduction] += 1;
                        }
                        else
                        {
                            _stats.LMRReSearchFailed += 1;
                            _stats.LMRReSearchFailedBy[lmrReduction] += 1;
                        }
                    }

                    if (score >= beta)
                    {
                        Sorter.RewardQuietMove(_theBoard.OnMove, allOppMoves[nn], SearchDepth);
                        for (int qq = 0; qq < quietsSearchedCount; qq++)
                        {
                            Sorter.PenaliseQuietMove(_theBoard.OnMove, quietsSearched[qq], SearchDepth);
                        }
                        if (!isInCheck && quietPlayed <= 4)
                        {
                            Sorter.IncreaseKillerScore(_fullDepth, depth, allOppMoves[nn],
                                _theBoard.Piece[allOppMoves[nn].From], 5 - quietPlayed);
                        }
                        node.Alpha = score;
                        node.BetaCutoff = true;
                        node.BestIndex = nn;
                        Sorter.SetRefutationMove(_fullDepth, depth, allOppMoves[nn],
                            _theBoard.Piece[allOppMoves[nn].From]);

                        if (quietPlayed == 1)
                        {
                            _stats.NodesCutOffWithFirstSortedMove += 1;
                        }
                        else if (quietPlayed == 2)
                        {
                            _stats.NodesCutoffWithSecondSortedMove += 1;
                        }
                        else if (quietPlayed == 3)
                        {
                            _stats.NodesCutOffWithThirdSortedMove += 1;
                        }
                        else
                        {
                            _stats.NodesCutOffWithLaterSortedMove += 1;
                        }
                        break;
                    }
                    else
                    {
                        quietsSearched[quietsSearchedCount] = allOppMoves[nn];
                        quietsSearchedCount += 1;
                    }
                    if (score > node.Alpha)
                    {
                        node.PV[depth - 1] = allOppMoves[nn];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            node.PV[ND] = node.LocalPV[ND];
                        }
                        node.Alpha = score;
                        node.AlphaRaised = true;
                        node.BestIndex = nn;
                        node.CapsOnlyAlphaRaised = false;
                        node.AlphaRaisedByNonGeneratedMove = false;
                    }

                }
            }


        StoreInTransTable:

            if (node.BetaCutoff)
            {
                if (!node.CapsOnlyCut && allOppMoves != null)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth,
                        allOppMoves[node.BestIndex], TT_POS_TYPE_BETA_CUT);
                    int MoveKey = allOppMoves[node.BestIndex].From * 100 + allOppMoves[node.BestIndex].To;
                    Sorter.CountCut(rootMoveKey, MoveKey);
                }
                else
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth, allOpCaps[node.BestIndex],
                        TT_POS_TYPE_BETA_CUT);
                }
            }
            else if (node.AlphaRaised)
            {
                if (!node.CapsOnlyAlphaRaised && !node.AlphaRaisedByNonGeneratedMove && allOppMoves != null)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth,
                        allOppMoves[node.BestIndex], TT_POS_TYPE_EXACT);
                }
                else if (!node.AlphaRaisedByNonGeneratedMove)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth, allOpCaps[node.BestIndex],
                        TT_POS_TYPE_EXACT);
                }
                else
                {
                    if (node.AlphaRaisedByKillerTwo)
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth,
                            Sorter.KillerTwos[ourDepth], TT_POS_TYPE_EXACT);
                    }
                    else if (node.AlphaRaisedByKillerOne)
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth,
                            Sorter.KillerOnes[ourDepth], TT_POS_TYPE_EXACT);
                    }
                    else if (node.AlphaRaisedByRefutation)
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth,
                            Sorter.Refutations[ourDepth], TT_POS_TYPE_EXACT);
                    }
                    else
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth, ttMove,
                            TT_POS_TYPE_EXACT);
                    }
                }
            }
            else
            {

                _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth, _phantomMove,
                    TT_POS_TYPE_ALPHA_NOT_BEATEN);

            }

        ReturnEarly:

            _repetitionFloor = savedRepetitionFloor;
            _ply -= 1;
            _theBoard.UnmakeLastMove();
            return node.Alpha;

        }


        private bool TryTranspositionTable(byte depth,
                                           long BoardZobrist,
                                           int beta,
                                           ref int alpha,
                                           out Move ttMove,
                                           out NodeTypes nodeType
                                          )
        {

            ttMove = default;
            nodeType = NodeTypes.Unknown;
            if (_transTable.LookupPos(_theBoard.CurrentZobrist, out TTMove transTableMove))
            {
                byte TTMoveDepth;
                if (transTableMove.PositionFlag >= 200)
                {
                    nodeType = NodeTypes.Cut;
                    TTMoveDepth = (byte)(transTableMove.PositionFlag - 200);
                    _stats.CutNodesFoundInTT += 1;
                }
                else if (transTableMove.PositionFlag >= 100)
                {
                    nodeType = NodeTypes.All;
                    TTMoveDepth = (byte)(transTableMove.PositionFlag - 100);
                    _stats.AllNodesFoundInTT += 1;
                }
                else
                {
                    nodeType = NodeTypes.PV;
                    TTMoveDepth = transTableMove.PositionFlag;
                    _stats.PVNodesFoundInTT += 1;
                }

                if (nodeType == NodeTypes.PV)
                {
                    if (TTMoveDepth >= depth)
                    {
                        //No exact cutoff in open-window searches: the node must be searched so its line is recorded
                        if (beta - alpha == 1 && transTableMove.Score >= alpha && transTableMove.Score <= beta)
                        {
                            alpha = transTableMove.Score;
                            _stats.PVTTCutoffs += 1;
                            return true;
                        }
                        else
                        {
                            if (transTableMove.BestResponseFrom != 0 || transTableMove.BestResponseTo != 0)
                            {
                                ttMove = new Move()
                                {
                                    From = transTableMove.BestResponseFrom,
                                    To = transTableMove.BestResponseTo,
                                    IsCapture = transTableMove.MoveAttributeFlag >= 100,
                                    PromotionPiece = transTableMove.MoveAttributeFlag >= 100
                                        ? (byte)(transTableMove.MoveAttributeFlag - 100)
                                        : transTableMove.MoveAttributeFlag
                                };
                            }
                        }
                    }
                    else
                    {
                        if (transTableMove.BestResponseFrom != 0 || transTableMove.BestResponseTo != 0)
                        {
                            ttMove = new Move()
                            {
                                From = transTableMove.BestResponseFrom,
                                To = transTableMove.BestResponseTo,
                                IsCapture = transTableMove.MoveAttributeFlag >= 100,
                                PromotionPiece = transTableMove.MoveAttributeFlag >= 100
                                    ? (byte)(transTableMove.MoveAttributeFlag - 100) : transTableMove.MoveAttributeFlag
                            };
                        }
                    }
                }
                else if (nodeType == NodeTypes.Cut) //Lower bound, beta cutuoff
                {
                    if (TTMoveDepth >= depth && transTableMove.Score >= beta)
                    {
                        alpha = transTableMove.Score;
                        _stats.BetaTTCutoffs += 1;
                        return true;
                    }
                    else
                    {
                        if (transTableMove.BestResponseFrom != 0 || transTableMove.BestResponseTo != 0)
                        {
                            ttMove = new Move()
                            {
                                From = transTableMove.BestResponseFrom,
                                To = transTableMove.BestResponseTo,
                                IsCapture = transTableMove.MoveAttributeFlag >= 100,
                                PromotionPiece = transTableMove.MoveAttributeFlag >= 100
                                    ? (byte)(transTableMove.MoveAttributeFlag - 100) : transTableMove.MoveAttributeFlag
                            };
                        }
                    }
                }
                else if (nodeType == NodeTypes.All) // Upper bound, alpha was never raised
                {
                    if (TTMoveDepth >= depth && transTableMove.Score < alpha)
                    {
                        alpha = transTableMove.Score;
                        _stats.AlphaTTCutoffs += 1;
                        return true;
                    }
                }
            }

            return false;

        }


        private bool TryPrimaryVariation(bool hasTTMove,
                                         Move ttMove,
                                         byte depth,
                                         int beta,
                                         int rootMoveKey,
                                         ref NodeState node
                                        )
        {

            if (hasTTMove)
            {
                if (_theBoard.MoveIsLegal(ttMove, _theBoard.OnMove))
                {

                    node.Played += 1;
                    int Score = 0 - EvaluateMove(ttMove, rootMoveKey, -beta, -node.Alpha, (byte)(depth - 1),
                        ref node.LocalPV);

                    if (Score >= beta)
                    {
                        _stats.CutOffWithPVMoveOnly += 1;
                        node.Alpha = Score;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth, ttMove,
                            TT_POS_TYPE_BETA_CUT);
                        Sorter.IncreaseKillerScore(_fullDepth, depth, ttMove, _theBoard.Piece[ttMove.From], 12);
                        return true;
                    }
                    else if (Score > node.Alpha)
                    {
                        node.Alpha = Score;
                        node.AlphaRaised = true;
                        node.AlphaRaisedByNonGeneratedMove = true;
                        node.PV[depth - 1] = ttMove;
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            node.PV[ND] = node.LocalPV[ND];
                        }
                    }

                }
            }

            return false;

        }


        private bool TryRefutationMove(Move ttMove,
                                       NodeTypes nodeType,
                                       byte depth,
                                       int beta,
                                       int rootMoveKey,
                                       ref NodeState node
                                      )
        {

            int ourDepth = _fullDepth - depth;

            if ((Sorter.Refutations[ourDepth].From != 0 || Sorter.Refutations[ourDepth].To != 0)
                && (Sorter.Refutations[ourDepth].From != ttMove.From ||
                Sorter.Refutations[ourDepth].To != ttMove.To))
            {
                if (_theBoard.Piece[Sorter.Refutations[ourDepth].From] == Sorter.RefutationPieces[ourDepth] &&
                    _theBoard.MoveIsLegal(Sorter.Refutations[ourDepth], _theBoard.OnMove, true))
                {

                    node.Played += 1;
                    node.RefutationSeparated = true;
                    _stats.RefutationTried += 1;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                RefutationMoveResearchAfterNull:

                    int score;

                    if (nodeType == NodeTypes.PV && node.AlphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(Sorter.Refutations[ourDepth], rootMoveKey, -node.Alpha - 1,
                            -node.Alpha, (byte)(depth - 1), ref node.LocalPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(Sorter.Refutations[ourDepth], rootMoveKey, -beta, -node.Alpha,
                            (byte)(depth - 1), ref node.LocalPV);
                    }

                    if (nullWindowSearch && score > node.Alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto RefutationMoveResearchAfterNull;
                    }

                    if (score >= beta)
                    {
                        node.Alpha = score;
                        _stats.CutOffUsingRefutation += 1;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth,
                            Sorter.Refutations[ourDepth], TT_POS_TYPE_BETA_CUT);
                        return true;
                    }
                    else if (score > node.Alpha)
                    {
                        node.Alpha = score;
                        node.AlphaRaisedByNonGeneratedMove = true;
                        node.AlphaRaised = true;
                        node.AlphaRaisedByRefutation = true;
                        node.PV[depth - 1] = Sorter.Refutations[ourDepth];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            node.PV[ND] = node.LocalPV[ND];
                        }
                    }
                }
            }


            return false;

        }


        private bool TryWinningCaptures(Move[] allOppCaps,
                                        Move ttMove,
                                        NodeTypes nodeType,
                                        byte depth,
                                        int beta,
                                        bool isInCheck,
                                        int rootMoveKey,
                                        bool sortedUsingIID,
                                        int staticScore,
                                        ref NodeState node
                                       )
        {

            int ourDepth = _fullDepth - depth;

            for (int nn = 0; nn < allOppCaps.Length; nn++)
            {
                if ((isInCheck || (allOppCaps[nn].Score >= 4000 ||
                    (sortedUsingIID && allOppCaps[nn].Score >= staticScore))) &&
                    _theBoard.MoveIsLegal(allOppCaps[nn], _theBoard.OnMove) &&
                    (allOppCaps[nn].From != ttMove.From || allOppCaps[nn].To != ttMove.To ||
                        allOppCaps[nn].PromotionPiece != ttMove.PromotionPiece))
                {
                    if (node.RefutationSeparated && allOppCaps[nn].From == Sorter.Refutations[ourDepth].From &&
                        allOppCaps[nn].To == Sorter.Refutations[ourDepth].To)
                    {
                        continue;
                    }

                    node.Played += 1;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                PositiveCaptureResearchAfterNull:

                    int score;
                    if (nodeType == NodeTypes.PV && node.AlphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(allOppCaps[nn], rootMoveKey, -node.Alpha - 1, -node.Alpha,
                            (byte)(depth - 1), ref node.LocalPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(allOppCaps[nn], rootMoveKey, -beta, -node.Alpha, (byte)(depth - 1),
                            ref node.LocalPV);
                    }

                    if (nullWindowSearch && score > node.Alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto PositiveCaptureResearchAfterNull;
                    }

                    if (score >= beta)
                    {
                        _stats.CutOffOnlyUsingWinningCaps += 1;
                        node.Alpha = score;
                        node.BestIndex = nn;
                        node.BetaCutoff = true;
                        node.CapsOnlyCut = true;
                        Sorter.SetRefutationMove(_fullDepth, depth, allOppCaps[nn],
                            _theBoard.Piece[allOppCaps[nn].From]);
                        return true;
                    }
                    if (score > node.Alpha)
                    {
                        node.PV[depth - 1] = allOppCaps[nn];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            node.PV[ND] = node.LocalPV[ND];
                        }
                        node.Alpha = score;
                        node.BestIndex = nn;
                        node.AlphaRaised = true;
                        node.CapsOnlyAlphaRaised = true;
                    }

                }
            }

            return false;

        }


        //Tries the killer in one slot. Killer two passes refutationPieceFromToSquare as true, recording the piece on
        //the killer's to square rather than its from square; it is a known bug, kept so that fixing it can be measured
        //on its own
        private bool TryKiller(Move[] killers,
                               int[] killerPieces,
                               ref KillerStats killerStats,
                               int killerBonus,
                               bool refutationPieceFromToSquare,
                               Move ttMove,
                               NodeTypes nodeType,
                               byte depth,
                               int beta,
                               int rootMoveKey,
                               ref NodeState node,
                               ref bool killerSeparated,
                               ref bool alphaRaisedByKiller
                              )
        {

            int ourDepth = _fullDepth - depth;

            if ((killers[ourDepth].From != 0 || killers[ourDepth].To != 0)
                    && killers[ourDepth].PromotionPiece == 0 && (killers[ourDepth].From != ttMove.From ||
                    killers[ourDepth].To != ttMove.To))
            {

                if (_theBoard.Piece[killers[ourDepth].From] == killerPieces[ourDepth] &&
                    _theBoard.MoveIsLegal(killers[ourDepth], _theBoard.OnMove, true))
                {

                    node.Played += 1;
                    killerSeparated = true;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                    killerStats.Tried += 1;
                    if (killers[ourDepth].IsCapture)
                    {
                        killerStats.CaptureTried += 1;
                    }
                    if (nodeType == NodeTypes.All)
                    {
                        killerStats.TriedAtAll += 1;
                    }
                    else if (nodeType == NodeTypes.Unknown)
                    {
                        killerStats.TriedAtUnknown += 1;
                    }
                    int nodesBeforeKiller = _stats.NodesLookedAt;

                KillerResearchAfterNull:

                    int score;

                    if (nodeType == NodeTypes.PV && node.AlphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(killers[ourDepth], rootMoveKey, -node.Alpha - 1, -node.Alpha,
                            (byte)(depth - 1), ref node.LocalPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(killers[ourDepth], rootMoveKey, -beta, -node.Alpha, (byte)(depth - 1),
                            ref node.LocalPV);
                    }

                    if (nullWindowSearch && score > node.Alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto KillerResearchAfterNull;
                    }

                    int killerNodes = _stats.NodesLookedAt - nodesBeforeKiller;
                    if (score < beta)
                    {
                        killerStats.NodesFailed += killerNodes;
                    }
                    if (score >= beta)
                    {
                        killerStats.NodesCut += killerNodes;
                        if (killers[ourDepth].IsCapture)
                        {
                            killerStats.CaptureCut += 1;
                        }
                        if (nodeType == NodeTypes.All)
                        {
                            killerStats.CutAtAll += 1;
                        }
                        else if (nodeType == NodeTypes.Unknown)
                        {
                            killerStats.CutAtUnknown += 1;
                        }
                        node.Alpha = score;
                        killerStats.CutOffs += 1;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, node.Alpha, depth, killers[ourDepth],
                            TT_POS_TYPE_BETA_CUT);
                        Sorter.IncreaseKillerScore(_fullDepth, depth, killers[ourDepth],
                            _theBoard.Piece[killers[ourDepth].From], killerBonus);
                        if (!killers[ourDepth].IsCapture)
                        {
                            Sorter.RewardQuietMove(_theBoard.OnMove, killers[ourDepth], depth);
                        }
                        Sorter.SetRefutationMove(_fullDepth, depth, killers[ourDepth],
                            _theBoard.Piece[refutationPieceFromToSquare ? killers[ourDepth].To
                                : killers[ourDepth].From]);
                        return true;
                    }
                    else if (score > node.Alpha)
                    {
                        killerStats.AlphaRaised += 1;
                        node.Alpha = score;
                        node.AlphaRaisedByNonGeneratedMove = true;
                        node.AlphaRaised = true;
                        alphaRaisedByKiller = true;
                        node.PV[depth - 1] = killers[ourDepth];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            node.PV[ND] = node.LocalPV[ND];
                        }
                    }
                }
                else
                {
                    killerStats.Invalid += 1;
                }
            }
            else
            {
                killerStats.EmptyOrTT += 1;
            }

            return false;

        }


        private bool TryLosingCaptures(Move[] allOpCaps,
                                       Move ttMove,
                                       NodeTypes nodeType,
                                       byte depth,
                                       int beta,
                                       int rootMoveKey,
                                       bool sortedUsingIID,
                                       int staticScore,
                                       ref NodeState node
                                      )
        {

            int ourDepth = _fullDepth - depth;

            for (int LC = 0; LC < allOpCaps.Length; LC++)
            {
                if ((allOpCaps[LC].Score < 4000 || (sortedUsingIID && allOpCaps[LC].Score < staticScore)) &&
                    _theBoard.MoveIsLegal(allOpCaps[LC], _theBoard.OnMove) &&
                    (allOpCaps[LC].From != ttMove.From || allOpCaps[LC].To != ttMove.To ||
                    allOpCaps[LC].PromotionPiece != ttMove.PromotionPiece))
                {

                    if (node.RefutationSeparated && allOpCaps[LC].From == Sorter.Refutations[ourDepth].From &&
                        allOpCaps[LC].To == Sorter.Refutations[ourDepth].To)
                    {
                        continue;
                    }

                    node.Played += 1;
                    bool LCNullSearchFailed = false;
                    bool LCNullWindowSearch;

                NegativeCaptureResearchAfterNull:

                    int LCScore;

                    if (nodeType == NodeTypes.PV && node.AlphaRaised && !LCNullSearchFailed)
                    {
                        LCNullWindowSearch = true;
                        LCScore = 0 - EvaluateMove(allOpCaps[LC], rootMoveKey, -node.Alpha - 1, -node.Alpha,
                            (byte)(depth - 1), ref node.LocalPV);
                    }
                    else
                    {
                        LCNullWindowSearch = false;
                        LCScore = 0 - EvaluateMove(allOpCaps[LC], rootMoveKey, -beta, -node.Alpha, (byte)(depth - 1),
                            ref node.LocalPV);
                    }

                    if (LCNullWindowSearch && LCScore > node.Alpha)
                    {
                        LCNullSearchFailed = true;
                        goto NegativeCaptureResearchAfterNull;
                    }

                    if (LCScore >= beta)
                    {
                        _stats.CutOffOnlyUsingLosingCaps += 1;
                        node.Alpha = LCScore;
                        node.BetaCutoff = true;
                        node.BestIndex = LC;
                        node.CapsOnlyCut = true;
                        Sorter.SetRefutationMove(_fullDepth, depth, allOpCaps[LC], _theBoard.Piece[allOpCaps[LC].From]);
                        return true;
                    }
                    if (LCScore > node.Alpha)
                    {
                        node.PV[depth - 1] = allOpCaps[LC];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            node.PV[ND] = node.LocalPV[ND];
                        }
                        node.Alpha = LCScore;
                        node.AlphaRaised = true;
                        node.BestIndex = LC;
                        node.CapsOnlyAlphaRaised = true;
                    }

                }
            }

            return false;
        }

    }
}
