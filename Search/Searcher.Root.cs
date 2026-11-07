using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
    {

        /// <summary>
        /// Search for the best move on a particular board state
        /// </summary>
        /// <param name="gameBoard">
        /// A reference to the board in the current state to search for
        /// </param>
        /// <param name="maxDepth">
        /// The maximum depth we should search to. This may be sent by a GUI, or we can set it ourselves. Note that we
        /// will always exit early if we are running out of time
        /// </param>
        /// <param name="ourTimeMilliseconds">
        /// How much time do WE have left, for time management
        /// </param>
        /// <param name="oppTimeMillisconds">
        /// How much time does our opponent have left, used for deciding whether to accept a draw or not (if we are
        /// losing by a little, but we have 5 mins and opponent has 2 seconds, we can probably win by flagging them)
        /// </param>
        /// <param name="prevZobrists">
        /// The Zobrist hashes of all positions we have already had in the game, to avoid (or maybe seek) 3-fold
        /// repetition draws
        /// </param>
        public void Search(ref Board gameBoard,
                           byte maxDepth = 8,
                           int ourTimeMilliseconds = 20000000,
                           int oppTimeMillisconds = 20000000,
                           List<long>? prevZobrists = null
                          )
        {

            //Reset the variables we use to track stats and status for the UCI protocol,
            //start the timer, initialise some key variables
            ResetStatTrackers();

            //Store the board in an object-wide variable
            _theBoard = gameBoard;

            //Create a time controller, which will calculate how long we can afford to use on this move
            TimeManager timeController = new (ourTimeMilliseconds, _theBoard);

            if (TryEndgameTableBase(ref timeController))
            {
                _isSearching = false;
                _secondTimer.Stop();
                TablebaseHit?.Invoke();
                return;
            }

            _theBoard.RecalcPSTScores();

            //Check the opening book: is this position in it, if yes, we will play one of the responses in the book
            bool posInBook = OPENING_BOOK.LookInBook(_theBoard.GenerateFen(),
                out byte bookFromSquare, out byte bookToSquare);

            //Generate all possible moves, and check if each is legal. If the position was in the book,
            //and a legal move matches, play it immediately
            List<Move> legalList = new();
            Move[] AllMoves = _theBoard.GenerateAllMoves(_theBoard.OnMove);

            foreach (Move M in AllMoves)
            {
                if (_theBoard.MoveIsLegal(M, _theBoard.OnMove))
                {
                    //Move is legal, add it to the legal list
                    legalList.Add(M);
                    if (posInBook)
                    {
                        if (M.From == bookFromSquare && M.To == bookToSquare)
                        {
                            //Move was in book, play this and end the search
                            _bestMove = M;
                            goto EndSearch;
                        }
                    }
                }
            }

            //Exit if there are no legal moves (which should of course only happen if we have been passed a board
            //with a checkmate or stalemate position already on it)
            if (legalList.Count == 0)
            {
                goto EndSearch;
            }
            else if (legalList.Count == 1)
            {
                //Only 1 legal move...play it
                _bestMove = legalList[0];
                goto EndSearch;
            }

            List<long> seenZobrists = new();
            long repetitionZobrist = -1;
            long secondRepetitionZobrist = -1;

            if (prevZobrists != null)
            {
                foreach (long Zob in prevZobrists)
                {
                    if (!seenZobrists.Contains(Zob))
                    {
                        seenZobrists.Add(Zob);
                    }
                    else
                    {
                        if (repetitionZobrist == -1)
                        {
                            repetitionZobrist = Zob;
                        }
                        else
                        {
                            secondRepetitionZobrist = Zob;
                        }
                    }
                }
            }

            //Get the legal moves into an array and sort it. As we have done no scoring,
            //sort at this stage will simply be captures first, non-captures after
            Move[] legal = legalList.ToArray();
            Array.Sort(legal);

            _stats.MaxSearchDepth = maxDepth;
            Dictionary<int, Move[]> pvDict = new();

            _startingWhiteMaterial = _theBoard.WhiteMaterial;
            _startingBlackMaterial = _theBoard.BlackMaterial;

            _sorter.Reset();
            _sorter.SetRootMoves(legal);

            _ply = 0;
            _inCheckAtPly[0] = _theBoard.IsInCheck(_theBoard.OnMove);
            _pathZobrist[0] = _theBoard.CurrentZobrist;
            _repetitionFloor = 0;
            _gameZobrists = prevZobrists != null ? new HashSet<long>(prevZobrists) : new HashSet<long>();

            StartHelpers(legal, maxDepth);
            legal = SearchRoot(legal, 1, maxDepth, timeController.MaxTimeToUse, pvDict);

            StopHelpers();

            //Set the best move to the legal moves sorted to the highest score
            _bestMove = legal[0];
            _rootMoves = legal;
            _rootPVs = pvDict;

            //Check for 3-fold repetition possibilities
            if (repetitionZobrist != -1)
            {
                //There is the possibility of repetition in the position...check if we will trigger it with the best
                //scored move
                _theBoard.MakeMove(_bestMove, _theBoard.OnMove, false);
                long EndZobrist = _theBoard.CurrentZobrist;

                if (seenZobrists.Contains(EndZobrist))
                {
                    //This move would cause repetition: do we want it?
                    if (_bestMove.Score >= -150 && _bestMove.Score < -50)
                    {
                        //We are more than half a point down...we might well want repetition
                        //Depends on how much time our opponent has compared to us...
                        if (oppTimeMillisconds < 5000)
                        {
                            //Opponent has less than 5 seconds...
                            if (ourTimeMilliseconds >= 30000)
                            {
                                //We have more than 30 seconds...we do not want repetition...but we need our second best
                                //move to be comparable
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30
                                    : legal[1].Score > -150))
                                {
                                    //Second best move is within 30 CP...accept this and don't take the draw
                                    _bestMove = legal[1];
                                }
                            }
                        }
                        else if (oppTimeMillisconds >= 5000 && oppTimeMillisconds < 10000)
                        {
                            //Opponent has between 5 and 10 seconds
                            if (ourTimeMilliseconds > 60000)
                            {
                                //We have more than 60...so let's reject the repetition and hope (provided second best
                                //move OK)
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30
                                    : legal[1].Score > -150))
                                {
                                    //Second best move is within 30 CP...accept this and don't take the draw
                                    _bestMove = legal[1];
                                }
                            }
                        }
                    }
                    else if (_bestMove.Score >= -50 && _bestMove.Score < 50)
                    {
                        //Fairly even position...we'll see how much time we have vs our opponent
                        if (oppTimeMillisconds < ourTimeMilliseconds)
                        {
                            //Our opponent has less time (we will just always accept a draw if this is not true)
                            if (ourTimeMilliseconds > 60000)
                            {
                                //We have more than a minute left, so let's not take the draw (again, provided the
                                //second move is still OK)
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30
                                    : legal[1].Score > -50))
                                {
                                    //Second best move is within 30 CP...accept this and don't take the draw
                                    _bestMove = legal[1];
                                }
                            }
                        }
                    }
                    else if (_bestMove.Score >= 50)
                    {
                        //We are winning, only in desperate time trouble should we accept repetition
                        if (ourTimeMilliseconds < oppTimeMillisconds)
                        {
                            //Only even consider it if we have less time
                            if (ourTimeMilliseconds > 5000)
                            {
                                //We have more than 20 seconds...so even with less than our opponent, we won't take the
                                //draw (if second best move was OK)
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30
                                    : legal[1].Score > 30))
                                {
                                    //Second best move is within 30 CP...accept this and don't take the draw
                                    _bestMove = legal[1];
                                }
                            }
                        }
                        else
                        {
                            //We have more time than they do, so as long as second move is still winning, no draw
                            if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30
                                : legal[1].Score > 30))
                            {
                                //Second best move is within 30 CP...accept this and don't take the draw
                                _bestMove = legal[1];
                            }
                        }
                    }
                }
            }


            _transTable.Clear();

        EndSearch:

            _isSearching = false;
            _secondTimer.Stop();

            if (_cancel)
            {
                _hasCancelled = true;
            }

            InfoUpdated?.Invoke();
            BestMoveSelected?.Invoke();

        }


        //Iterative deepening over the root moves, run by the main search and by every helper: full windows to depth
        //4, aspiration windows from depth 5, root selectivity from depth 6. Returns the root moves sorted best first,
        //or the last completed iteration's if the time ran out. Helpers pass int.MaxValue as the time
        private Move[] SearchRoot(Move[] legal, byte startDepth, byte maxDepth, int maxTimeToUse,
            Dictionary<int, Move[]> pvDict)
        {

            //Create an array to store the best ordered moves from the previous depth of
            //iterative deepening. If we run out of time, we will fall-back to the last
            //fully completed depth's results
            Move[]? prevIterationBest = null;
            int totalUsedTickCount = 0;
            bool quittingEarly = false;

            int prevIterationAlpha = -5000;
            int bestScoreAtDepth = 0;
            int windowSize = 0;
            bool nullWindowSearch;

            _iterationLines.Clear();
            int searchStartTickCount = Environment.TickCount;

            for (byte depth = startDepth; depth <= maxDepth; depth++)
            {

                _fullDepth = depth;
                _stats.CurrentSearchDepth = depth;

                windowSize = 50;
                bestScoreAtDepth = -5000;

                int rootMovesSearched = 0;
                int nullWindowReSearches = 0;
                int failLowReSearches = 0;
                int failHighReSearches = 0;
                int belowBestSearches = 0;
                int rootMovesDropped = 0;
                string newlyDroppedMoves = "";
                int windowSlides = 0;
                int windowSlideTotal = 0;

                int aspAlpha = prevIterationAlpha - windowSize;
                int aspBeta = prevIterationAlpha + windowSize;

                for (int nn = 0; nn < legal.Length; nn++)
                {

                    int moveStartTickCount = Environment.TickCount;
                    int MoveKey = legal[nn].From * 100 + legal[nn].To;

                    Move[] pv = new Move[depth + 1];
                    pv[depth] = legal[nn];

                    if (depth <= 4)
                    {
                        rootMovesSearched += 1;
                        legal[nn].Score = 0 - EvaluateMove(legal[nn], MoveKey, -5000, 5000, depth, ref pv);
                    }
                    else
                    {
                        if (legal[nn].Score > -5000)
                        {

                            rootMovesSearched += 1;
                            bool alphaWidened = false;
                            bool betaWidened = false;
                            bool nullWindowFailure = false;

                        ReSearchAfterFallingOutsideWindow:

                            if (nn == 0 || nullWindowFailure)
                            {
                                nullWindowSearch = false;
                                legal[nn].Score = -EvaluateMove(legal[nn], MoveKey, -aspBeta, -aspAlpha, depth, ref pv);

                            }
                            else
                            {
                                nullWindowSearch = true;
                                legal[nn].Score = -EvaluateMove(legal[nn], MoveKey, -aspAlpha - 1, -aspAlpha, depth,
                                    ref pv);
                            }

                            if (nullWindowSearch && (legal[nn].Score > aspAlpha))
                            {
                                nullWindowReSearches += 1;
                                nullWindowFailure = true;
                                goto ReSearchAfterFallingOutsideWindow;
                            }

                            if (legal[nn].Score <= aspAlpha && aspAlpha != -5000)
                            {
                                if (nn > 0)
                                {
                                    //Not the best move, so no exact score is needed: only whether root selectivity will
                                    //drop it
                                    belowBestSearches += 1;
                                    legal[nn].Score = ScoreRootMoveBelowBest(legal[nn], MoveKey, depth,
                                        bestScoreAtDepth, legal[nn].Score, ref pv);
                                }
                                else if (!alphaWidened)
                                {
                                    failLowReSearches += 1;
                                    aspAlpha -= windowSize;
                                    alphaWidened = true;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                                else
                                {
                                    failLowReSearches += 1;
                                    aspAlpha = -5000;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                            }
                            else if (legal[nn].Score >= aspBeta && aspBeta != 5000)
                            {
                                if (!betaWidened)
                                {
                                    failHighReSearches += 1;
                                    aspBeta += windowSize;
                                    betaWidened = true;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                                else
                                {
                                    failHighReSearches += 1;
                                    aspBeta = 5000;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                            }

                            if (legal[nn].Score > bestScoreAtDepth)
                            {
                                bestScoreAtDepth = legal[nn].Score;
                            }

                            if (legal[nn].Score > aspAlpha)
                            {
                                if (legal[nn].Score - windowSize < aspAlpha)
                                {
                                    windowSlides += 1;
                                    windowSlideTotal += aspAlpha - (legal[nn].Score - windowSize);
                                }
                                aspAlpha = legal[nn].Score - windowSize;
                                aspBeta = legal[nn].Score + windowSize;
                            }

                        }
                    }

                    int moveEndTickCount = Environment.TickCount;
                    totalUsedTickCount += (moveEndTickCount - moveStartTickCount);
                    _stats.CurrentSearchDepth = depth;

                    if ((totalUsedTickCount > maxTimeToUse && nn < legal.Length / 3 && depth >= 7) ||
                        (_cancel && depth > 1))
                    {
                        if (prevIterationBest != null)
                        {
                            //We've gone over, and we are not almost done
                            quittingEarly = true;
                            legal = prevIterationBest;
                            break;
                        }

                    }
                    else
                    {
                        if (pvDict.ContainsKey(legal[nn].From * 100 + legal[nn].To))
                        {
                            pvDict.Remove(legal[nn].From * 100 + legal[nn].To);
                        }
                        pvDict.Add(legal[nn].From * 100 + legal[nn].To, pv);
                    }

                }

                prevIterationAlpha = bestScoreAtDepth;

                if (!quittingEarly)
                {

                    //Save a copy of of the sorted list, so if we have to give up in the next
                    //iteration due to time constraints, we have a fallback result
                    SortRootMoves(legal);
                    prevIterationBest = (Move[])legal.Clone();

                    _bestMove = legal[0];
                    _bestScore = legal[0].Score;
                    _bestPV = pvDict[legal[0].From * 100 + legal[0].To];

                    if (depth >= 4)
                    {
                        if (totalUsedTickCount > maxTimeToUse)
                        {
                            //We must exit here, we have used too much time
                            break;
                        }
                        else if (totalUsedTickCount > Convert.ToInt32(maxTimeToUse * 0.5))
                        {
                            //We've used 50% of our max time...odds of completing another ply is minimal
                            break;
                        }
                    }

                    if (depth >= 6)
                    {
                        int cutoff = RootSelectivityCutoff(depth);
                        for (int NRed = 1; NRed < legal.Length; NRed++)
                        {
                            if (legal[NRed].Score < _bestScore - cutoff && !GivesCheck(legal[NRed]) &&
                                !ThreatensMateInOne(legal[NRed]))
                            {
                                if (legal[NRed].Score > -5000)
                                {
                                    newlyDroppedMoves += " " + ConvertMoveToString(legal[NRed]) + " " +
                                        legal[NRed].Score;
                                }
                                legal[NRed].Score = -5000;
                                rootMovesDropped += 1;
                            }
                        }
                    }

                    _iterationLines.Add("Depth " + depth + ": " + ConvertMoveToString(legal[0]) + " " + _bestScore +
                        ", " + (Environment.TickCount - searchStartTickCount) + "ms, main nodes " +
                        _stats.NodesLookedAt + ", all nodes " + NodesLookedAtAllThreads() + ", root moves " +
                        rootMovesSearched +
                        ", re-searches null/low/high " + nullWindowReSearches + "/" + failLowReSearches + "/" +
                        failHighReSearches + ", below-best " + belowBestSearches + ", dropped " + rootMovesDropped +
                        ", window slid down " + windowSlides + " times by " + windowSlideTotal +
                        ", newly dropped:" + newlyDroppedMoves);

                }
                else
                {
                    break;
                }

                if (legal.Length > 1 && legal[1].Score == -5000 && depth >= 6)
                {
                    break;
                }

            }

            return legal;

        }


        private bool TryEndgameTableBase(ref TimeManager timeController)
        {

            int pieceCount = 0;
            for (int nn = 0; nn <= 63; nn++)
            {
                if (_theBoard.Color[nn] != EMPTY)
                {
                    pieceCount += 1;
                }
            }

            _endGame = (pieceCount <= 18);

            if (RUN_MODE == ProgramMode.UCI && USE_TABLEBASE &&
                ((pieceCount <= 7 && timeController.MaxTimeToUse >= 15000) || pieceCount <= 6))
            {
                EndgameTablebase EGT = new();
                string UCIMove = EGT.FindBestMoveFrom7ManTablebase(_theBoard.GenerateFen());
                if (UCIMove != "")
                {
                    _uciTablebaseHit = UCIMove;
                    return true;
                }
            }

            return false;

        }


        //A root move that failed low against the best so far. After the iteration, root selectivity drops moves scoring
        //below the best minus its cutoff, so a search on that line decides the move's fate; otherwise the fail-low
        //bound stands. The window is two wide, not one, so the pruning reserved for null windows (null move, futility,
        //LMP), which can hide sacrifices, stays off
        private int ScoreRootMoveBelowBest(Move move, int moveKey, byte depth, int bestScore, int failLowScore,
            ref Move[] pv)
        {
            int cutoff = RootSelectivityCutoff(depth);
            if (cutoff == 0)
            {
                return failLowScore;
            }
            int dropLine = bestScore - cutoff;
            if (dropLine >= failLowScore || dropLine <= -4999)
            {
                return failLowScore;
            }
            int score = -EvaluateMove(move, moveKey, -dropLine - 1, -dropLine + 1, depth, ref pv);
            return score < dropLine ? score : failLowScore;
        }


        //Root selectivity, from depth 6: after an iteration, root moves scoring more than this below the best are
        //dropped (unless they give check or threaten mate in one). 0 below depth 6
        private int RootSelectivityCutoff(byte depth)
        {
            if (depth < 6)
            {
                return 0;
            }
            bool early = _theBoard.WhiteMaterial + _theBoard.BlackMaterial > MAX_MATERIAL / 2;
            if (depth == 6)
            {
                return early ? DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF : DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF;
            }
            if (depth == 7)
            {
                return early ? DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF : DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF;
            }
            if (depth == 8)
            {
                return early ? DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF : DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF;
            }
            if (depth == 9)
            {
                return early ? DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF : DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF;
            }
            if (depth == 10)
            {
                return early ? DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF : DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF;
            }
            return early ? DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF : DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF;
        }


        //Insertion sort, highest score first. Stable, so root moves sharing a fail-low bound keep the previous
        //iteration's order
        private static void SortRootMoves(Move[] moves)
        {
            for (int ii = 1; ii < moves.Length; ii++)
            {
                Move moving = moves[ii];
                int jj = ii - 1;
                while (jj >= 0 && moves[jj].Score < moving.Score)
                {
                    moves[jj + 1] = moves[jj];
                    jj--;
                }
                moves[jj + 1] = moving;
            }
        }

    }
}
