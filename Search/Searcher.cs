using static Lisa.Globals;
namespace Lisa
{
    public sealed class Searcher
    {

        #region Events and delegates

        public delegate void BestMoveSelectedEventHandler();
        public event BestMoveSelectedEventHandler? BestMoveSelected;

        public delegate void InfoUpdatedEventHandler();
        public event InfoUpdatedEventHandler? InfoUpdated;

        public delegate void TablebaseHitEventHandler();
        public event TablebaseHitEventHandler? TablebaseHit;

        #endregion



        private Board _theBoard;
        private TranspositionTable _transTable;

        private byte _fullDepth;

        private System.Timers.Timer _secondTimer;

        #region Stats trackers

        private int _infoSecondsUsed;
        private int _infoNodesLookedAt;
        private int _infoNodesPerSecond;
        private byte _infoCurrentSearchDepth;
        private byte _infoMaxSearchDepth;
        private int _infoPVNodesFoundInTT;
        private int _infoCutNodesFoundInTT;
        private int _infoAllNodesFoundInTT;
        private int _infoNullMoveCutOffs;
        private int _infoNullMoveAttempts;
        private int _infoProbCutCutOffs;
        private int _infoProbCutAttempts;
        private int _infoCutOffWithPVMoveOnly;
        private int _infoNodesLookedAtWithoutQuiesce;
        private int _infoNodesCutOffWithFirstSortedMove;
        private int _infoNodesCutoffWithSecondSortedMove;
        private int _infoNodesCutOffWithThirdSortedMove;
        private int _infoNodesCutOffWithLaterSortedMove;
        private int _infoCutOffUsingKillerOne;
        private int _infoCutOffUsingKillerTwo;
        private int _infoCutOffUsingRefutation;
        private int _infoCutOffOnlyUsingWinningCaps;
        private int _infoCutOffOnlyUsingLosingCaps;
        private int _infoNodesQuiesced;
        private int _infoFutilityD4;
        private int _infoFutilityD3;
        private int _infoFutilityD2;
        private int _infoReverseFutilityD4;
        private int _infoReverseFutilityD3;
        private int _infoReverseFutilityD2;
        private int _infoPVTTCutoffs;
        private int _infoBetaTTCutoffs;
        private int _infoAlphaTTCutoffs;

        public int InfoSecondsUsed => _infoSecondsUsed;
        public int InfoNodesLookedAt => _infoNodesLookedAt;
        public int InfoNodesPerSecond => _infoNodesPerSecond;
        public byte InfoCurrentSearchDepth => _infoCurrentSearchDepth;
        public byte InfoMaxSearchDepth => _infoMaxSearchDepth;
        public int InfoPVNodesFoundInTT => _infoPVNodesFoundInTT;
        public int InfoCutNodesFoundInTT => _infoCutNodesFoundInTT;
        public int InfoAllNodesFoundInTT => _infoAllNodesFoundInTT;
        public int InfoNullMoveCutOffs => _infoNullMoveCutOffs;
        public int InfoNullMoveAttempts => _infoNullMoveAttempts;
        public int InfoCutOffWithPVMoveOnly => _infoCutOffWithPVMoveOnly;
        public int InfoNodesLookedAtWithoutQuiesce => _infoNodesLookedAtWithoutQuiesce;
        public int InfoNodesCutOffWithFirstSortedMove => _infoNodesCutOffWithFirstSortedMove;
        public int InfoCutOffUsingKillerOne => _infoCutOffUsingKillerOne;
        public int InfoCutOffUsingKillerTwo => _infoCutOffUsingKillerTwo;
        public int InfoCutOffUsingRefutation => _infoCutOffUsingRefutation;
        public int InfoCutOffOnlyUsingWinningCaps => _infoCutOffOnlyUsingWinningCaps;
        public int InfoNodesCutoffWithSecondSortedMove => _infoNodesCutoffWithSecondSortedMove;
        public int InfoNodesCutOffWithThirdSortedMove => _infoNodesCutOffWithThirdSortedMove;
        public int InfoCutOffOnlyUsingLosingCaps => _infoCutOffOnlyUsingLosingCaps;
        public int InfoNodesCutOffWithLaterSortedMove => _infoNodesCutOffWithLaterSortedMove;
        public int InfoNodesQuiesced => _infoNodesQuiesced;
        public int InfoFutilityD4 => _infoFutilityD4;
        public int InfoFutilityD3 => _infoFutilityD3;
        public int InfoFutilityD2 => _infoFutilityD2;
        public int InfoReverseFutilityD4 => _infoReverseFutilityD4;
        public int InfoReverseFutilityD3 => _infoReverseFutilityD3;
        public int InfoReverseFutilityD2 => _infoReverseFutilityD2;
        public int InfoPVTTCutoffs => _infoPVTTCutoffs;
        public int InfoBetaTTCutoffs => _infoBetaTTCutoffs;
        public int InfoAlphaTTCutoffs => _infoAlphaTTCutoffs;
        public int InfoProbCutCutOffs => _infoProbCutCutOffs;
        public int InfoProbCutAttempts => _infoProbCutAttempts;

        #endregion


        #region Interaction with caller properties

        private Move _bestMove;
        private int _bestScore;
        private Move[]? _bestPV;

        private bool _cancel = false;
        private bool _hasCancelled = false;
        private bool _isSearching = false;

        public string _uciTablebaseHit = "";

        public bool HasCancelled => _hasCancelled;
        public bool IsSearching => _isSearching;
        public Move BestMove => _bestMove;
        public int BestScore => _bestScore;
        public Move[]? BestPV => _bestPV;
        public string UCITablebaseHit => _uciTablebaseHit;

        #endregion

        private bool _endGame = false;
        private bool _opening = false;

        private int _startingWhiteMaterial;
        private int _startingBlackMaterial;

        private Move _phantomMove = new Move();

        public Searcher()
        {
            _transTable = new TranspositionTable();
            _theBoard = new Board();
            _secondTimer = new System.Timers.Timer();
            _secondTimer.Elapsed += SecondTimer_Elapsed;
        }


        public void Cancel()
        {
            _cancel = true;
        }


        /// <summary>
        /// Search for the best move on a particular board state
        /// </summary>
        /// <param name="gameBoard">
        /// A reference to the board in the current state to search for
        /// </param>
        /// <param name="maxDepth">
        /// The maximum depth we should search to. This may be sent by a GUI, or we can set it ourselves. Note that we will always exit early if we are
        /// running out of time
        /// </param>
        /// <param name="ourTimeMilliseconds">
        /// How much time do WE have left, for time management
        /// </param>
        /// <param name="oppTimeMillisconds">
        /// How much time does our opponent have left, used for deciding whether to accept a draw or not (if we are losing by a little, but we have 5 mins 
        /// and opponent has 2 seconds, we can probably win by flagging them)
        /// </param>
        /// <param name="prevZobrists">
        /// The Zobrist hashes of all positions we have already had in the game, to avoid (or maybe seek) 3-fold repetition draws
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
            TimeManager timeController = new TimeManager(ourTimeMilliseconds, _theBoard);

            if (TryEndgameTableBase(ref timeController))
            {
                _isSearching = false;
                _secondTimer.Stop();
                TablebaseHit?.Invoke();
                return;
            }

            _theBoard.RecalcPSTScores();

            //Check the opening book: is this position in it, if yes, we will play one of the responses in the book
            bool posInBook = Book.LookInBook(_theBoard.CurrentZobrist,
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

            //Create an array to store the best ordered moves from the previous depth of
            //iterative deepening. If we run out of time, we will fall-back to the last
            //fully completed depth's results
            Move[]? prevIterationBest = null;
            int totalUsedTickCount = 0;
            bool quittingEarly = false;
            _infoMaxSearchDepth = maxDepth;
            Dictionary<int, Move[]> pvDict = new();

            _startingWhiteMaterial = _theBoard.WhiteMaterial;
            _startingBlackMaterial = _theBoard.BlackMaterial;

            int prevIterationAlpha = -5000;
            int bestScoreAtDepth = 0;
            int windowSize = 0;
            bool nullWindowSearch;

            for (byte depth = 1; depth <= maxDepth; depth++)
            {

                _fullDepth = depth;
                _infoCurrentSearchDepth = depth;

                windowSize = 50;
                bestScoreAtDepth = -5000;

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
                        legal[nn].Score = 0 - EvaluateMove(legal[nn], MoveKey, -5000, 5000, depth, ref pv);
                    }
                    else
                    {
                        if (legal[nn].Score > -5000)
                        {

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
                                legal[nn].Score = -EvaluateMove(legal[nn], MoveKey, -aspAlpha - 1, -aspAlpha, depth, ref pv);
                            }

                            if (nullWindowSearch && (legal[nn].Score > aspAlpha))
                            {
                                nullWindowFailure = true;
                                goto ReSearchAfterFallingOutsideWindow;
                            }

                            if (legal[nn].Score <= aspAlpha && aspAlpha != -5000)
                            {
                                if (!alphaWidened)
                                {
                                    aspAlpha -= windowSize;
                                    alphaWidened = true;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                                else
                                {
                                    aspAlpha = -5000;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                            }
                            else if (legal[nn].Score >= aspBeta && aspBeta != 5000)
                            {
                                if (!betaWidened)
                                {
                                    aspBeta += windowSize;
                                    betaWidened = true;
                                    goto ReSearchAfterFallingOutsideWindow;
                                }
                                else
                                {
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
                                aspAlpha = legal[nn].Score - windowSize;
                                aspBeta = legal[nn].Score + windowSize;
                            }

                        }
                    }

                    int moveEndTickCount = Environment.TickCount;
                    totalUsedTickCount += (moveEndTickCount - moveStartTickCount);
                    _infoCurrentSearchDepth = depth;

                    if ((totalUsedTickCount > timeController.MaxTimeToUse && nn < legal.Length / 3 && depth >= 7) || (_cancel && depth > 1))
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
                    prevIterationBest = (Move[])legal.Clone();
                    Array.Sort(legal);

                    _bestMove = legal[0];
                    _bestScore = legal[0].Score;
                    _bestPV = pvDict[legal[0].From * 100 + legal[0].To];

                    if (depth >= 4)
                    {
                        if (totalUsedTickCount > timeController.MaxTimeToUse)
                        {
                            //We must exit here, we have used too much time
                            break;
                        }
                        else if (totalUsedTickCount > Convert.ToInt32(timeController.MaxTimeToUse * 0.5))
                        {
                            //We've used 50% of our max time...odds of completing another ply is minimal
                            break;
                        }
                    }

                    if (_theBoard.WhiteMaterial + _theBoard.BlackMaterial > MaxMaterial / 2)
                    {
                        if (depth == 6 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 7 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 8 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 9 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 10 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth > 10 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (depth == 6 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 7 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 8 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 9 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth == 10 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                        else if (depth > 10 && legal.Length > 1)
                        {
                            for (int NRed = 1; NRed < legal.Length; NRed++)
                            {
                                if (legal[NRed].Score < _bestScore - DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF)
                                {
                                    legal[NRed].Score = -5000;
                                }
                            }
                        }
                    }

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

            //Set the best move to the legal moves sorted to the highest score
            _bestMove = legal[0];

            //Check for 3-fold repetition possibilities
            if (repetitionZobrist != -1)
            {
                //There is the possibility of repetition in the position...check if we will trigger it with the best scored move
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
                                //We have more than 30 seconds...we do not want repetition...but we need our second best move to be comparable
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30 : legal[1].Score > -150))
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
                                //We have more than 60...so let's reject the repetition and hope (provided second best move OK)
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30 : legal[1].Score > -150))
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
                                //We have more than a minute left, so let's not take the draw (again, provided the second move is still OK)
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30 : legal[1].Score > -50))
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
                                //We have more than 20 seconds...so even with less than our opponent, we won't take the draw (if second best move was OK)
                                if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30 : legal[1].Score > 30))
                                {
                                    //Second best move is within 30 CP...accept this and don't take the draw
                                    _bestMove = legal[1];
                                }
                            }
                        }
                        else
                        {
                            //We have more time than they do, so as long as second move is still winning, no draw
                            if (legal.Length > 1 && (_bestMove.Score < 100 ? legal[1].Score > _bestMove.Score - 30 : legal[1].Score > 30))
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
            _opening = (pieceCount > 28);

            if (Mode == ProgramMode.UCI && UseTablebase && ((pieceCount <= 7 && timeController.MaxTimeToUse >= 15000) || pieceCount <= 6))
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


        private int DepthOneEval(Move legalMove,
                                 int alpha,
                                 int beta,
                                 int rootMoveKey,
                                 ref Move[] pv
                                )
        {

            int PreQAlpha = alpha;
            alpha = Quiesce(legalMove, alpha, beta, 0, rootMoveKey);
            if (alpha > PreQAlpha)
            {
                pv[0] = legalMove;
            }
            return alpha;

        }


        private void CalculateReductionsAndExtensions(Move legalMove, bool isInCheck, bool reduced, ref byte depth, ref Move[] localPV,
            ref int forcingMoves, ref bool isPawnPush)
        {

            if (isInCheck)
            {
                forcingMoves += 1;
                if (reduced)
                {
                    depth += 2;
                    localPV = new Move[depth];
                }
            }
            else
            {
                if (_endGame && _theBoard.Piece[legalMove.To] == PAWN)
                {
                    if ((_theBoard.Color[legalMove.To] == WHITE && legalMove.To < 32) || (_theBoard.Color[legalMove.To] == BLACK && legalMove.To >= 32))
                    {
                        forcingMoves += 1;
                        isPawnPush = true;
                    }
                }
                else if (_endGame && legalMove.PromotionPiece > 0)
                {
                    forcingMoves += 1;
                }
                else if (legalMove.IsCapture)
                {
                    forcingMoves += 1;
                }
                else if (_theBoard.Piece[legalMove.To] == PAWN)
                {
                    if ((_theBoard.Color[legalMove.To] == WHITE && legalMove.To < 24) || (_theBoard.Color[legalMove.To] == BLACK && legalMove.To >= 40))
                    {
                        forcingMoves += 1;
                        isPawnPush = true;
                    }
                }
            }

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
            nodeType = NodeTypes.All;
            if (_transTable.LookupPos(_theBoard.CurrentZobrist, out TTMove transTableMove))
            {
                byte TTMoveDepth;
                if (transTableMove.PositionFlag >= 200)
                {
                    nodeType = NodeTypes.Cut;
                    TTMoveDepth = (byte)(transTableMove.PositionFlag - 200);
                    _infoCutNodesFoundInTT += 1;
                }
                else if (transTableMove.PositionFlag >= 100)
                {
                    nodeType = NodeTypes.All;
                    TTMoveDepth = (byte)(transTableMove.PositionFlag - 100);
                    _infoAllNodesFoundInTT += 1;
                }
                else
                {
                    nodeType = NodeTypes.PV;
                    TTMoveDepth = transTableMove.PositionFlag;
                    _infoPVNodesFoundInTT += 1;
                }

                if (nodeType == NodeTypes.PV)
                {
                    if (TTMoveDepth >= depth)
                    {
                        if (transTableMove.Score >= alpha && transTableMove.Score <= beta)
                        {
                            alpha = transTableMove.Score;
                            _infoPVTTCutoffs += 1;
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
                                    PromotionPiece = transTableMove.MoveAttributeFlag >= 100 ? (byte)(transTableMove.MoveAttributeFlag - 100) : transTableMove.MoveAttributeFlag
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
                                PromotionPiece = transTableMove.MoveAttributeFlag >= 100 ? (byte)(transTableMove.MoveAttributeFlag - 100) : transTableMove.MoveAttributeFlag
                            };
                        }
                    }
                }
                else if (nodeType == NodeTypes.Cut) //Lower bound, beta cutuoff
                {
                    if (TTMoveDepth >= depth && transTableMove.Score >= beta)
                    {
                        alpha = transTableMove.Score;
                        _infoBetaTTCutoffs += 1;
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
                                PromotionPiece = transTableMove.MoveAttributeFlag >= 100 ? (byte)(transTableMove.MoveAttributeFlag - 100) : transTableMove.MoveAttributeFlag
                            };
                        }
                        if (transTableMove.Score > alpha && TTMoveDepth >= depth)
                        {
                            alpha = transTableMove.Score;
                        }
                    }
                }
                else if (nodeType == NodeTypes.All) // Upper bound, alpha was never raised
                {
                    if (TTMoveDepth >= depth && transTableMove.Score <= alpha)
                    {
                        alpha = transTableMove.Score;
                        _infoAlphaTTCutoffs += 1;
                        return true;
                    }
                }
            }

            return false;

        }


        private bool TryFutilityCut(Move legalMove,
                                    byte depth,
                                    int beta,
                                    bool isInCheck,
                                    int staticEval,
                                    NodeTypes nodeType,
                                    ref int alpha
                                   )
        {

            if (!legalMove.IsCapture && !isInCheck && _fullDepth >= 10 && nodeType != NodeTypes.PV &&
                alpha != 5000 && beta != 5000 && alpha != -5000 && beta != -5000)
            {
                if (depth == 6 && staticEval + DEPTH6_FUTILITY_MARGIN < alpha)
                {
                    alpha = staticEval + DEPTH6_FUTILITY_MARGIN;
                    _infoFutilityD4 += 1;
                    return true;
                }
                if (depth == 6 && staticEval - DEPTH6_REVERSE_FUTILITY_MARGIN >= beta)
                {
                    alpha = beta;
                    //alpha = staticEval - DEPTH6_REVERSE_FUTILITY_MARGIN;
                    _infoReverseFutilityD4 += 1;
                    return true;
                }
            }

            if (!legalMove.IsCapture && !isInCheck && _fullDepth >= 9 && nodeType != NodeTypes.PV &&
                alpha != 5000 && beta != 5000 && alpha != -5000 && beta != -5000)
            {
                if (depth == 5 && staticEval + DEPTH5_FUTILITY_MARGIN < alpha)
                {
                    alpha = staticEval + DEPTH5_FUTILITY_MARGIN;
                    _infoFutilityD4 += 1;
                    return true;
                }
                if (depth == 5 && staticEval - DEPTH5_REVERSE_FUTILITY_MARGIN >= beta)
                {
                    alpha = beta;
                    //alpha = staticEval - DEPTH5_REVERSE_FUTILITY_MARGIN;
                    _infoReverseFutilityD4 += 1;
                    return true;
                }
            }

            if (!legalMove.IsCapture && !isInCheck && _fullDepth >= 8 && nodeType != NodeTypes.PV &&
                alpha != 5000 && beta != 5000 && alpha != -5000 && beta != -5000)
            {
                if (depth <= 4)
                {
                    if (depth == 4 && staticEval + DEPTH4_FUTILITY_MARGIN < alpha)
                    {
                        alpha = staticEval + DEPTH4_FUTILITY_MARGIN;
                        _infoFutilityD4 += 1;
                        return true;
                    }
                    else if (depth == 3 && staticEval + DEPTH3_FUTILITY_MARGIN < alpha)
                    {
                        alpha = staticEval + DEPTH3_FUTILITY_MARGIN;
                        _infoFutilityD3 += 1;
                        return true;
                    }
                    else if (depth == 2 && staticEval + DEPTH2_FUTILITY_MARGIN < alpha)
                    {
                        alpha = staticEval + DEPTH2_FUTILITY_MARGIN;
                        _infoFutilityD2 += 1;
                        return true;
                    }
                    if (depth == 4 && staticEval - DEPTH4_REVERSE_FUTILITY_MARGIN >= beta)
                    {
                        alpha = beta;
                        _infoReverseFutilityD4 += 1;
                        return true;
                    }
                    else if (depth == 3 && staticEval - DEPTH3_REVERSE_FUTILITY_MARGIN >= beta)
                    {
                        alpha = beta;
                        _infoReverseFutilityD3 += 1;
                        return true;
                    }
                    else if (depth == 2 && staticEval - DEPTH2_REVERSE_FUTILITY_MARGIN >= beta)
                    {
                        alpha = beta;
                        _infoReverseFutilityD2 += 1;
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
                                 bool isPawnPush,
                                 ref Move[] localPV,
                                 ref int alpha
                                )
        {

            if (!_endGame && 
                !isPawnPush && 
                staticEval > beta && 
                beta != -5000 && 
                depth < _fullDepth && 
                !isInCheck && 
                depth > 2 &&
                (nodeType == NodeTypes.Cut || depth <= 4) && 
                legalMove.PromotionPiece == 0
                )
            {
                _infoNullMoveAttempts += 1;
                _theBoard.MakeMove(_phantomMove, _theBoard.OnMove, true);
                Move[] AllMovesAfterOppNull = Sorter.GetSortedMoves(ref _theBoard, rootMoveKey, false, false);
                for (int nn = 0; nn < AllMovesAfterOppNull.Length; nn++)
                {
                    if (_theBoard.MoveIsLegal(AllMovesAfterOppNull[nn], _theBoard.OnMove))
                    {
                        int score;
                        if (depth >= 10)
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, -beta, 1 - beta, (byte)(depth - 5), ref localPV);
                        }
                        else if (depth >= 8)
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, -beta, 1 - beta, (byte)(depth - 4), ref localPV);
                        }
                        else if (depth >= 6)
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, -beta, 1 - beta, (byte)(depth - 3), ref localPV);
                        }
                        else
                        {
                            score = EvaluateMove(AllMovesAfterOppNull[nn], rootMoveKey, -beta, 1 - beta, (byte)(depth - 2), ref localPV);
                        }

                        if (score >= beta)
                        {
                            _infoNullMoveCutOffs += 1;
                            alpha = beta;
                            _theBoard.UnmakeLastMove(); //Unmake the Null Move
                            return true;
                        }
                    }
                }

                _theBoard.UnmakeLastMove(); //Unmake the Null move
            }

            return false;

        }


        private bool TryPrimaryVariation(bool hasTTMove,
                                         Move ttMove,
                                         byte depth,
                                         int beta,
                                         int rootMoveKey,
                                         ref int played,
                                         ref bool alphaRaised,
                                         ref bool alphaRaisedByNonGeneratedMove,
                                         ref Move[] localPV,
                                         ref Move[] pv,
                                         ref int alpha
                                        )
        {

            if (hasTTMove && ttMove.PromotionPiece == 0)
            {
                if (_theBoard.MoveIsLegal(ttMove, _theBoard.OnMove))
                {

                    played += 1;
                    int Score = 0 - EvaluateMove(ttMove, rootMoveKey, -beta, -alpha, (byte)(depth - 1), ref localPV);

                    if (Score >= beta)
                    {
                        _infoCutOffWithPVMoveOnly += 1;
                        alpha = Score;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, ttMove, TransTablePosTypeBetaCutoff);
                        Sorter.IncreaseKillerScore(_fullDepth, depth, ttMove, _theBoard.Piece[ttMove.From], 12);
                        return true;
                    }
                    else if (Score > alpha)
                    {
                        alpha = Score;
                        alphaRaised = true;
                        alphaRaisedByNonGeneratedMove = true;
                        pv[depth - 1] = ttMove;
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
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
                                        ref int played,
                                        ref bool alphaRaised,
                                        ref bool alphaRaisedByNonGeneratedMove,
                                        ref Move[] localPV,
                                        ref Move[] pv,
                                        ref bool betaCutoff,
                                        ref int bestIndex,
                                        ref bool capsOnlyCut,
                                        ref bool capsOnlyAlphaRaised,
                                        ref int alpha,
                                        bool refutationSeparated
                                       )
        {

            for (int nn = 0; nn < allOppCaps.Length; nn++)
            {
                if ((isInCheck || allOppCaps[nn].Score >= 4000) && _theBoard.MoveIsLegal(allOppCaps[nn], _theBoard.OnMove) &&
                    (allOppCaps[nn].From != ttMove.From || allOppCaps[nn].To != ttMove.To))
                {
                    if (refutationSeparated && allOppCaps[nn].From == Sorter.Refutations[_fullDepth - depth].From &&
                        allOppCaps[nn].To == Sorter.Refutations[_fullDepth - depth].To)
                    {
                        continue;
                    }

                    played += 1;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                PositiveCaptureResearchAfterNull:

                    int score;
                    if (nodeType == NodeTypes.PV && alphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(allOppCaps[nn], rootMoveKey, -alpha - 1, -alpha, (byte)(depth - 1), ref localPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(allOppCaps[nn], rootMoveKey, -beta, -alpha, (byte)(depth - 1), ref localPV);
                    }

                    if (nullWindowSearch && score > alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto PositiveCaptureResearchAfterNull;
                    }

                    if (score >= beta)
                    {
                        _infoCutOffOnlyUsingWinningCaps += 1;
                        alpha = score;
                        bestIndex = nn;
                        betaCutoff = true;
                        capsOnlyCut = true;
                        Sorter.SetRefutationMove(_fullDepth, depth, allOppCaps[nn], _theBoard.Piece[allOppCaps[nn].From]);
                        return true;
                    }
                    if (score > alpha)
                    {
                        pv[depth - 1] = allOppCaps[nn];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
                        }
                        alpha = score;
                        bestIndex = nn;
                        alphaRaised = true;
                        capsOnlyAlphaRaised = true;
                    }

                }
            }

            return false;

        }


        private bool TryRefutationMove(Move ttMove,
                                       NodeTypes nodeType,
                                       byte depth,
                                       int beta,
                                       bool isInCheck,
                                       int rootMoveKey,
                                       ref int played,
                                       ref bool alphaRaised,
                                       ref bool alphaRaisedByNonGeneratedMove,
                                       ref Move[] localPV,
                                       ref Move[] pv,
                                       ref bool refutationSeparated,
                                       ref bool alphaRaisedByRefutation,
                                       ref int alpha
                                      )
        {

            if ((Sorter.Refutations[_fullDepth - depth].From != 0 || Sorter.Refutations[_fullDepth - depth].To != 0)
                && (Sorter.Refutations[_fullDepth - depth].From != ttMove.From ||
                Sorter.Refutations[_fullDepth - depth].To != ttMove.To))
            {
                if (_theBoard.Piece[Sorter.Refutations[_fullDepth - depth].From] == Sorter.RefutationPieces[_fullDepth - depth] && _theBoard.MoveIsLegal(Sorter.Refutations[_fullDepth - depth], _theBoard.OnMove, true))
                {

                    played += 1;
                    refutationSeparated = true;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                RefutationMoveResearchAfterNull:

                    int score;

                    if (nodeType == NodeTypes.PV && alphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(Sorter.Refutations[_fullDepth - depth], rootMoveKey, -alpha - 1, -alpha, (byte)(depth - 1), ref localPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(Sorter.Refutations[_fullDepth - depth], rootMoveKey, -beta, -alpha, (byte)(depth - 1), ref localPV);
                    }

                    if (nullWindowSearch && score > alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto RefutationMoveResearchAfterNull;
                    }

                    if (score >= beta)
                    {
                        alpha = score;
                        _infoCutOffUsingRefutation += 1;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, Sorter.Refutations[_fullDepth - depth], TransTablePosTypeBetaCutoff);
                        return true;
                    }
                    else if (score > alpha)
                    {
                        alpha = score;
                        alphaRaisedByNonGeneratedMove = true;
                        alphaRaised = true;
                        alphaRaisedByRefutation = true;
                        pv[depth - 1] = Sorter.Refutations[_fullDepth - depth];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
                        }
                    }
                }
            }


            return false;

        }


        private bool TryKillerOne(Move ttMove,
                                  NodeTypes nodeType,
                                  byte depth,
                                  int beta,
                                  bool isInCheck,
                                  int rootMoveKey,
                                  ref int played,
                                  ref bool alphaRaised,
                                  ref bool alphaRaisedByNonGeneratedMove,
                                  ref Move[] localPV,
                                  ref Move[] pv,
                                  ref bool killerOneSeparated,
                                  ref bool alphaRaisedByKillerOne,
                                  ref int alpha
                                 )
        {

            if ((Sorter.KillerOnes[_fullDepth - depth].From != 0 || Sorter.KillerOnes[_fullDepth - depth].To != 0)
                    && Sorter.KillerOnes[_fullDepth - depth].PromotionPiece == 0 && (Sorter.KillerOnes[_fullDepth - depth].From != ttMove.From ||
                    Sorter.KillerOnes[_fullDepth - depth].To != ttMove.To))
            {

                if (_theBoard.Piece[Sorter.KillerOnes[_fullDepth - depth].From] == Sorter.FirstKillerPieces[_fullDepth - depth] && _theBoard.MoveIsLegal(Sorter.KillerOnes[_fullDepth - depth], _theBoard.OnMove, true))
                {

                    played += 1;
                    killerOneSeparated = true;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                FirstKillerResearchAfterNull:

                    int score;

                    if (nodeType == NodeTypes.PV && alphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(Sorter.KillerOnes[_fullDepth - depth], rootMoveKey, -alpha - 1, -alpha, (byte)(depth - 1), ref localPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(Sorter.KillerOnes[_fullDepth - depth], rootMoveKey, -beta, -alpha, (byte)(depth - 1), ref localPV);
                    }

                    if (nullWindowSearch && score > alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto FirstKillerResearchAfterNull;
                    }

                    if (score >= beta)
                    {
                        alpha = score;
                        _infoCutOffUsingKillerOne += 1;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, Sorter.KillerOnes[_fullDepth - depth], TransTablePosTypeBetaCutoff);
                        Sorter.IncreaseKillerScore(_fullDepth, depth, Sorter.KillerOnes[_fullDepth - depth], _theBoard.Piece[Sorter.KillerOnes[_fullDepth - depth].From], 10);
                        Sorter.SetRefutationMove(_fullDepth, depth, Sorter.KillerOnes[_fullDepth - depth], _theBoard.Piece[Sorter.KillerOnes[_fullDepth - depth].From]);
                        return true;
                    }
                    else if (score > alpha)
                    {
                        alpha = score;
                        alphaRaisedByNonGeneratedMove = true;
                        alphaRaised = true;
                        alphaRaisedByKillerOne = true;
                        pv[depth - 1] = Sorter.KillerOnes[_fullDepth - depth];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
                        }
                    }
                }
            }

            return false;

        }


        private bool TryKillerTwo(Move ttMove,
                                  NodeTypes nodeType,
                                  byte depth,
                                  int beta,
                                  bool isInCheck,
                                  int rootMoveKey,
                                  ref int played,
                                  ref bool alphaRaised,
                                  ref bool alphaRaisedByNonGeneratedMove,
                                  ref Move[] localPV,
                                  ref Move[] pv,
                                  ref bool killerTwoSeparated,
                                  ref bool alphaRaisedByKillerTwo,
                                  ref int alpha
                                 )
        {

            if ((Sorter.KillerTwos[_fullDepth - depth].From != 0 || Sorter.KillerTwos[_fullDepth - depth].To != 0)
                    && Sorter.KillerTwos[_fullDepth - depth].PromotionPiece == 0 && (Sorter.KillerTwos[_fullDepth - depth].From != ttMove.From ||
                    Sorter.KillerTwos[_fullDepth - depth].To != ttMove.To))
            {

                if (_theBoard.Piece[Sorter.KillerTwos[_fullDepth - depth].From] == Sorter.SecondKillerPieces[_fullDepth - depth] && _theBoard.MoveIsLegal(Sorter.KillerTwos[_fullDepth - depth], _theBoard.OnMove, true))
                {

                    played += 1;
                    killerTwoSeparated = true;
                    bool nullWindowSearchFailed = false;
                    bool nullWindowSearch;

                SecondKillerResearchAfterNull:

                    int score;

                    if (nodeType == NodeTypes.PV && alphaRaised && !nullWindowSearchFailed)
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(Sorter.KillerTwos[_fullDepth - depth], rootMoveKey, -alpha - 1, -alpha, (byte)(depth - 1), ref localPV);
                    }
                    else
                    {
                        nullWindowSearch = false;
                        score = 0 - EvaluateMove(Sorter.KillerTwos[_fullDepth - depth], rootMoveKey, -beta, -alpha, (byte)(depth - 1), ref localPV);
                    }

                    if (nullWindowSearch && score > alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        goto SecondKillerResearchAfterNull;
                    }

                    if (score >= beta)
                    {
                        alpha = score;
                        _infoCutOffUsingKillerTwo += 1;
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, Sorter.KillerTwos[_fullDepth - depth], TransTablePosTypeBetaCutoff);
                        Sorter.IncreaseKillerScore(_fullDepth, depth, Sorter.KillerTwos[_fullDepth - depth], _theBoard.Piece[Sorter.KillerTwos[_fullDepth - depth].From], 8);
                        Sorter.SetRefutationMove(_fullDepth, depth, Sorter.KillerTwos[_fullDepth - depth], _theBoard.Piece[Sorter.KillerTwos[_fullDepth - depth].To]);
                        return true;
                    }
                    else if (score > alpha)
                    {
                        alpha = score;
                        alphaRaisedByNonGeneratedMove = true;
                        alphaRaised = true;
                        alphaRaisedByKillerTwo = true;
                        pv[depth - 1] = Sorter.KillerTwos[_fullDepth - depth];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
                        }
                    }
                }

            }

            return false;

        }


        private bool TryProbCut(NodeTypes nodeType,
                                byte depth,
                                int beta,
                                bool isInCheck,
                                int rootMoveKey,
                                ref bool alphaRaised,
                                ref Move[] localPV,
                                ref int alpha
                               )
        {

            if (!isInCheck && nodeType == NodeTypes.Cut && depth > 4 && depth < _fullDepth && _fullDepth > 8)
            {

                _infoProbCutAttempts += 1;

                Move[] AllProbCutOppMoves = Sorter.GetSortedMoves(ref _theBoard, rootMoveKey, false, false);
                int BetaExceeded = 0;
                for (int nn = 0; nn < AllProbCutOppMoves.Length / 3; nn++)
                {
                    if (_theBoard.MoveIsLegal(AllProbCutOppMoves[nn], _theBoard.OnMove))
                    {

                        bool nullWindowSearchFailed = false; bool nullWindowSearch;

                    ProbCuteResearchAfterNull:

                        int score;

                        if (alphaRaised && !nullWindowSearchFailed)
                        {
                            nullWindowSearch = true;
                            score = 0 - EvaluateMove(AllProbCutOppMoves[nn], rootMoveKey, -alpha - 1, -alpha, (byte)(depth - 4), ref localPV);
                        }
                        else
                        {
                            nullWindowSearch = false;
                            score = 0 - EvaluateMove(AllProbCutOppMoves[nn], rootMoveKey, -beta, -alpha, (byte)(depth - 4), ref localPV);
                        }

                        if (nullWindowSearch && score > alpha)
                        {
                            nullWindowSearchFailed = true;
                            goto ProbCuteResearchAfterNull;
                        }

                        if (score >= beta)
                        {
                            BetaExceeded += 1;
                            if (BetaExceeded >= 3)
                            {
                                _infoProbCutCutOffs += 1;
                                alpha = beta;
                                return true;
                            }
                        }

                    }
                }
            }

            return false;

        }


        private bool TryLosingCaptures(Move[] allOpCaps,
                                       Move ttMove,
                                       NodeTypes nodeType,
                                       byte depth,
                                       int beta,
                                       int rootMoveKey,
                                       ref int played,
                                       ref bool alphaRaised,
                                       ref Move[] localPV,
                                       ref Move[] pv,
                                       ref bool betaCutoff,
                                       ref int bestIndex,
                                       ref bool capsOnlyCut,
                                       ref bool capsOnlyAlphaRaised,
                                       ref int alpha,
                                       bool refutationSeparated
                                      )
        {

            for (int LC = 0; LC < allOpCaps.Length; LC++)
            {
                if (allOpCaps[LC].Score < 4000 && _theBoard.MoveIsLegal(allOpCaps[LC], _theBoard.OnMove) && (allOpCaps[LC].From != ttMove.From || allOpCaps[LC].To != ttMove.To))
                {

                    if (refutationSeparated && allOpCaps[LC].From == Sorter.Refutations[_fullDepth - depth].From &&
                        allOpCaps[LC].To == Sorter.Refutations[_fullDepth - depth].To)
                    {
                        continue;
                    }

                    played += 1;
                    bool LCNullSearchFailed = false;
                    bool LCNullWindowSearch;

                NegativeCaptureResearchAfterNull:

                    int LCScore;

                    if (nodeType == NodeTypes.PV && alphaRaised && !LCNullSearchFailed)
                    {
                        LCNullWindowSearch = true;
                        LCScore = 0 - EvaluateMove(allOpCaps[LC], rootMoveKey, -alpha - 1, -alpha, (byte)(depth - 1), ref localPV);
                    }
                    else
                    {
                        LCNullWindowSearch = false;
                        LCScore = 0 - EvaluateMove(allOpCaps[LC], rootMoveKey, -beta, -alpha, (byte)(depth - 1), ref localPV);
                    }

                    if (LCNullWindowSearch && LCScore > alpha)
                    {
                        LCNullSearchFailed = true;
                        goto NegativeCaptureResearchAfterNull;
                    }

                    if (LCScore >= beta)
                    {
                        _infoCutOffOnlyUsingLosingCaps += 1;
                        alpha = LCScore;
                        betaCutoff = true;
                        bestIndex = LC;
                        capsOnlyCut = true;
                        Sorter.SetRefutationMove(_fullDepth, depth, allOpCaps[LC], _theBoard.Piece[allOpCaps[LC].From]);
                        return true;
                    }
                    if (LCScore > alpha)
                    {
                        pv[depth - 1] = allOpCaps[LC];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
                        }
                        alpha = LCScore;
                        alphaRaised = true;
                        bestIndex = LC;
                        capsOnlyAlphaRaised = true;
                    }

                }
            }

            return false;
        }


        private int EvaluateMove(Move legalMove,
                                 int rootMoveKey,
                                 int alpha,
                                 int beta,
                                 byte depth,
                                 ref Move[] pv
                                )
        {

            Move[] localPV = new Move[depth];

            if (depth == 1)
            {
                return DepthOneEval(legalMove, alpha, beta, rootMoveKey, ref pv);
            }

            _infoNodesLookedAt += 1; _infoNodesLookedAtWithoutQuiesce += 1;
            _theBoard.MakeMove(legalMove, _theBoard.OnMove, false);
            bool isInCheck = _theBoard.IsInCheck(_theBoard.OnMove);
            bool isPawnPush = false;

            //CalculateReductionsAndExtensions(legalMove, isInCheck, reduced, ref depth, ref localPV, ref forcingMoves, ref isPawnPush);

            bool hasTTMove = false;

            bool ttCut = TryTranspositionTable(depth, _theBoard.CurrentZobrist, beta, ref alpha, out Move ttMove, out NodeTypes nodeType);

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

            int staticEval = 0 - Scorer.ScorePosition(ref _theBoard, ref _transTable, alpha, beta, false, 0);
            bool futilityCut = TryFutilityCut(legalMove, depth, beta, isInCheck, staticEval, nodeType, ref alpha);
            if (futilityCut)
            {
                goto ReturnEarly;
            }

            bool nullCut = TryNullMove(legalMove, rootMoveKey, depth, beta, isInCheck, staticEval, nodeType, isPawnPush, ref localPV, ref alpha);
            if (nullCut)
            {
                goto ReturnEarly;
            }

            int played = 0;
            bool betaCutoff = false;
            bool alphaRaised = false;
            bool alphaRaisedByNonGeneratedMove = false;
            bool killerOneSeparated = false;
            bool killerTwoSeparated = false;
            bool refutationSeparated = false;
            bool alphaRaisedByKillerOne = false;
            bool alphaRaisedByKillerTwo = false;
            bool alphaRaisedByRefutation = false;
            int bestIndex = -1;
            bool capsOnlyCut = false;
            bool capsOnlyAlphaRaised = false;
            Move[]? allOppMoves = null;
            Move[]? allOpCaps = null;

            if (hasTTMove)
            {
                bool pvCut = TryPrimaryVariation(hasTTMove, ttMove, depth, beta, rootMoveKey, ref played, ref alphaRaised, ref alphaRaisedByNonGeneratedMove, ref localPV, ref pv, ref alpha);
                if (pvCut)
                {
                    Sorter.SetRefutationMove(_fullDepth, depth, ttMove, _theBoard.Piece[ttMove.From]);
                    int MoveKey = ttMove.From * 100 + ttMove.To;
                    Sorter.UpdateHistoryAggressive(rootMoveKey, MoveKey, depth);
                    goto ReturnEarly;
                }
            }

            bool refutationCut = TryRefutationMove(ttMove, nodeType, depth, beta, isInCheck, rootMoveKey, ref played, ref alphaRaised, ref alphaRaisedByNonGeneratedMove, ref localPV,
                ref pv, ref refutationSeparated, ref alphaRaisedByRefutation, ref alpha);

            if (refutationCut)
            {
                int MoveKey = Sorter.Refutations[_fullDepth - depth].From * 100 + Sorter.Refutations[_fullDepth - depth].To;
                Sorter.UpdateHistoryAggressive(rootMoveKey, MoveKey, depth);
                goto ReturnEarly;
            }

            if (!hasTTMove && (nodeType != NodeTypes.All || depth >= _fullDepth - 1) && depth >= 6)
            {
                int iidAlpha = alpha;
                int iidBeta = beta;
                allOpCaps = Sorter.GetSortedMoves(ref _theBoard, rootMoveKey, true, false, true);
                allOppMoves = Sorter.GetSortedMoves(ref _theBoard, rootMoveKey, false, true, true);

                byte iidReduceDepth = (byte)(depth - 4);
                for (int nn = 0; nn < allOpCaps.Length; nn++)
                {
                    allOpCaps[nn].Score = -EvaluateMove(allOpCaps[nn], rootMoveKey, -iidBeta, -iidAlpha, iidReduceDepth, ref localPV);
                    if (allOpCaps[nn].Score > iidAlpha)
                    {
                        iidAlpha = allOpCaps[nn].Score;
                    }
                }
                for (int nn = 0; nn < allOppMoves.Length; nn++)
                {
                    allOppMoves[nn].Score = -EvaluateMove(allOppMoves[nn], rootMoveKey, -iidBeta, -iidAlpha, iidReduceDepth, ref localPV);
                    if (allOppMoves[nn].Score > iidAlpha)
                    {
                        iidAlpha = allOppMoves[nn].Score;
                    }
                }
                Array.Sort(allOpCaps);
                Array.Sort(allOppMoves);
            }

            if (allOpCaps == null)
            {
                allOpCaps = Sorter.GetSortedMoves(ref _theBoard, rootMoveKey, true, false, (depth <= _fullDepth - 6 && nodeType != NodeTypes.PV));
            }

            bool winningCapsCut = TryWinningCaptures(allOpCaps, ttMove, nodeType, depth, beta, isInCheck, rootMoveKey, ref played, ref alphaRaised, ref alphaRaisedByNonGeneratedMove, ref localPV,
                ref pv, ref betaCutoff, ref bestIndex, ref capsOnlyCut, ref capsOnlyAlphaRaised, ref alpha, refutationSeparated);

            if (winningCapsCut)
            {
                goto StoreInTransTable;
            }


            if (!refutationSeparated || (Sorter.KillerOnes[_fullDepth - depth].From != Sorter.Refutations[_fullDepth - depth].From ||
                Sorter.KillerOnes[_fullDepth - depth].To != Sorter.Refutations[_fullDepth - depth].To))
            {
                bool killerOneCut = TryKillerOne(ttMove, nodeType, depth, beta, isInCheck, rootMoveKey, ref played, ref alphaRaised, ref alphaRaisedByNonGeneratedMove, ref localPV, ref pv,
                    ref killerOneSeparated, ref alphaRaisedByKillerOne, ref alpha);

                if (killerOneCut)
                {
                    goto ReturnEarly;
                }
            }

            if (!refutationSeparated || (Sorter.KillerTwos[_fullDepth - depth].From != Sorter.Refutations[_fullDepth - depth].From ||
                Sorter.KillerTwos[_fullDepth - depth].To != Sorter.Refutations[_fullDepth - depth].To))
            {
                bool killerTwoCut = TryKillerTwo(ttMove, nodeType, depth, beta, isInCheck, rootMoveKey, ref played, ref alphaRaised,
                    ref alphaRaisedByNonGeneratedMove, ref localPV, ref pv, ref killerTwoSeparated, ref alphaRaisedByKillerTwo, ref alpha);

                if (killerTwoCut)
                {
                    goto ReturnEarly;
                }
            }

            bool probCutCut = TryProbCut(nodeType, depth, beta, isInCheck, rootMoveKey, ref alphaRaised, ref localPV, ref alpha);
            if (probCutCut)
            {
                goto ReturnEarly;
            }

            bool losingCapsCut = TryLosingCaptures(allOpCaps, ttMove, nodeType, depth, beta, rootMoveKey, ref played, ref alphaRaised, ref localPV,
                ref pv, ref betaCutoff, ref bestIndex, ref capsOnlyCut,
                ref capsOnlyAlphaRaised, ref alpha, refutationSeparated);
            if (losingCapsCut)
            {
                goto StoreInTransTable;
            }

            if (allOppMoves == null)
            {
                if (nodeType != NodeTypes.All)
                {
                    allOppMoves = Sorter.GetSortedMoves(ref _theBoard, rootMoveKey, false, true);
                }
                else
                {
                    allOppMoves = _theBoard.GenerateNonCaptureMoves(_theBoard.OnMove);
                }
            }

            int quietPlayed = 0;
            for (int nn = 0; nn < allOppMoves.Length; nn++)
            {
                if ((ttMove.From == allOppMoves[nn].From && ttMove.To == allOppMoves[nn].To) ||
                    (killerOneSeparated && allOppMoves[nn].From == Sorter.KillerOnes[_fullDepth - depth].From && allOppMoves[nn].To == Sorter.KillerOnes[_fullDepth - depth].To) ||
                    (killerTwoSeparated && allOppMoves[nn].From == Sorter.KillerTwos[_fullDepth - depth].From && allOppMoves[nn].To == Sorter.KillerTwos[_fullDepth - depth].To) ||
                    (refutationSeparated && allOppMoves[nn].From == Sorter.Refutations[_fullDepth - depth].From && allOppMoves[nn].To == Sorter.Refutations[_fullDepth - depth].To))
                {
                    continue;
                }
                if (allOppMoves[nn].Score > -4997 && _theBoard.MoveIsLegal(allOppMoves[nn], _theBoard.OnMove))
                {
                    played += 1; quietPlayed += 1;

                    int SearchDepth = (int)depth;
                    byte ByteDepth;
                    bool WasReduced = false;
                    byte originalDepth = (byte)(SearchDepth - 1);
                    if (isInCheck || quietPlayed == 1 || allOppMoves[nn].Score == 4950 || legalMove.Score == 4950 || (_endGame && _theBoard.Piece[allOppMoves[nn].From] == PAWN))
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
                            if (_fullDepth > 8 && SearchDepth - 5 >= 1 && quietPlayed > 5)
                            {
                                ByteDepth = (byte)(SearchDepth - 5);
                            }
                            else if (_fullDepth > 8 && SearchDepth - 4 >= 1)
                            {
                                ByteDepth = (byte)(SearchDepth - 4);
                            }
                            else
                            {
                                ByteDepth = (byte)(SearchDepth - 3);
                            }
                        }
                        WasReduced = true;
                    }

                    bool nullWindowSearchFailed = false;

                ReducedBeatAlpha:

                    int score;
                    bool nullWindowSearch = false;

                    if (depth >= _fullDepth - 5 || quietPlayed == 1 || nullWindowSearchFailed)
                    {
                        score = 0 - EvaluateMove(allOppMoves[nn], rootMoveKey, -beta, -alpha, ByteDepth, ref localPV);
                    }
                    else
                    {
                        nullWindowSearch = true;
                        score = 0 - EvaluateMove(allOppMoves[nn], rootMoveKey, -alpha - 1, -alpha, ByteDepth, ref localPV);
                    }

                    if (nullWindowSearch && score > alpha && score < beta)
                    {
                        nullWindowSearchFailed = true;
                        WasReduced = false;
                        ByteDepth = originalDepth;
                        goto ReducedBeatAlpha;
                    }
                    if (WasReduced && score > alpha)
                    {
                        WasReduced = false;
                        ByteDepth = originalDepth;
                        goto ReducedBeatAlpha;
                    }

                    if (score >= beta)
                    {
                        if (!isInCheck && quietPlayed <= 4)
                        {
                            Sorter.IncreaseKillerScore(_fullDepth, depth, allOppMoves[nn], _theBoard.Piece[allOppMoves[nn].From], 5 - quietPlayed);
                        }
                        alpha = score;
                        betaCutoff = true;
                        bestIndex = nn;
                        Sorter.SetRefutationMove(_fullDepth, depth, allOppMoves[nn], _theBoard.Piece[allOppMoves[nn].From]);

                        if (nn == 0)
                        {
                            _infoNodesCutOffWithFirstSortedMove += 1;
                        }
                        else if (nn == 1)
                        {
                            _infoNodesCutoffWithSecondSortedMove += 1;
                        }
                        else if (nn == 2)
                        {
                            _infoNodesCutOffWithThirdSortedMove += 1;
                        }
                        else
                        {
                            _infoNodesCutOffWithLaterSortedMove += 1;
                        }
                        break;
                    }
                    if (score > alpha)
                    {
                        pv[depth - 1] = allOppMoves[nn];
                        for (int ND = depth - 2; ND >= 0; ND--)
                        {
                            pv[ND] = localPV[ND];
                        }
                        alpha = score;
                        alphaRaised = true;
                        bestIndex = nn;
                        capsOnlyAlphaRaised = false;
                        alphaRaisedByNonGeneratedMove = false;
                    }

                }
            }


        StoreInTransTable:

            if (betaCutoff)
            {
                if (!capsOnlyCut && allOppMoves != null)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, allOppMoves[bestIndex], TransTablePosTypeBetaCutoff);
                    int MoveKey = allOppMoves[bestIndex].From * 100 + allOppMoves[bestIndex].To;
                    Sorter.UpdateHistoryStandard(rootMoveKey, MoveKey, depth);
                }
                else
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, allOpCaps[bestIndex], TransTablePosTypeBetaCutoff);
                }
            }
            else if (alphaRaised)
            {
                if (!capsOnlyAlphaRaised && !alphaRaisedByNonGeneratedMove && allOppMoves != null)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, allOppMoves[bestIndex], TransTablePosTypeExact);
                    int MoveKey = allOppMoves[bestIndex].From * 100 + allOppMoves[bestIndex].To;
                    Sorter.UpdateHistoryAggressive(rootMoveKey, MoveKey, depth);
                }
                else if (!alphaRaisedByNonGeneratedMove)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, allOpCaps[bestIndex], TransTablePosTypeExact);
                }
                else
                {
                    if (alphaRaisedByKillerTwo)
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, Sorter.KillerTwos[_fullDepth - depth], TransTablePosTypeExact);
                    }
                    else if (alphaRaisedByKillerOne)
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, Sorter.KillerOnes[_fullDepth - depth], TransTablePosTypeExact);
                    }
                    else if (alphaRaisedByRefutation)
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, Sorter.Refutations[_fullDepth - depth], TransTablePosTypeExact);
                    }
                    else
                    {
                        _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, ttMove, TransTablePosTypeExact);
                    }
                }
            }
            else
            {

                _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, depth, _phantomMove, TransTablePosTypeAlphaNotExceeded);

            }

        ReturnEarly:

            _theBoard.UnmakeLastMove();
            return alpha;

        }


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

            _infoNodesLookedAt += 1;
            _infoNodesQuiesced += 1;

            int materialCaptured = 0;
            if (legalMove.IsCapture)
            {
                if (_theBoard.Piece[legalMove.To] != -1)
                {
                    materialCaptured = Material[_theBoard.Piece[legalMove.To]];
                }
                else
                {
                    materialCaptured = 100;
                }
            }

            _theBoard.MakeMove(legalMove, _theBoard.OnMove, false);

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
                    if (transTableMove.Score >= alpha && transTableMove.Score <= beta)
                    {
                        _theBoard.UnmakeLastMove();
                        alpha = transTableMove.Score;
                        return alpha;
                    }
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
                    if (transTableMove.Score <= alpha)
                    {
                        _theBoard.UnmakeLastMove();
                        alpha = transTableMove.Score;
                        return alpha;
                    }
                }
            }

            int StandPat = 0 - Scorer.ScorePosition(ref _theBoard, ref _transTable, alpha, beta, true, materialCaptured);
            bool isInCheck = QuiesceDepth == 0 ? _theBoard.IsInCheck(_theBoard.OnMove) : false;

            if (StandPat < alpha && !isInCheck)
            {
                if (StandPat + Material[QUEEN] < alpha)
                {
                    _theBoard.UnmakeLastMove();
                    return StandPat + Material[QUEEN];
                }
                else
                {
                    if (_theBoard.WhiteQueenSquare == 255 && _theBoard.BlackQueenSquare == 255)
                    {
                        if (StandPat + Material[ROOK] < alpha)
                        {
                            _theBoard.UnmakeLastMove();
                            return StandPat + Material[ROOK];
                        }
                    }
                }
            }

            if (StandPat >= beta && !isInCheck && !(QuiesceDepth == 0 && legalMove.IsCapture))
            {
                if (QuiesceDepth == 0)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, StandPat, 1, _phantomMove, TransTablePosTypeBetaCutoff);
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
                allOppMoves = Sorter.GetSortedMoves(ref _theBoard, RootMoveKey, !isInCheck, false, true);
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
                        if (_theBoard.Piece[allOppMoves[nn].To] != -1 && StandPat + Material[_theBoard.Piece[allOppMoves[nn].To]] < alpha - 25)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (_theBoard.Piece[allOppMoves[nn].To] != -1 && StandPat + Material[_theBoard.Piece[allOppMoves[nn].To]] < alpha)
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
                    else if (_theBoard.Piece[allOppMoves[nn].From] == KNIGHT || _theBoard.Piece[allOppMoves[nn].From] == BISHOP)
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
                if (!allOppMoves[nn].IsCapture || NoSeeNeeded || _theBoard.See(allOppMoves[nn].To) >= 0)
                {
                    if (_theBoard.MoveIsLegal(allOppMoves[nn], _theBoard.OnMove))
                    {
                        int score = 0 - Quiesce(allOppMoves[nn], 0 - beta, 0 - alpha, (byte)(QuiesceDepth + 1), RootMoveKey);
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
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, 1, _phantomMove, TransTablePosTypeBetaCutoff);
                }
                else if (alphaRaised)
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, 1, _phantomMove, TransTablePosTypeExact);
                }
                else
                {
                    _transTable.AddToTranstable(_theBoard.CurrentZobrist, alpha, 1, _phantomMove, TransTablePosTypeAlphaNotExceeded);
                }
            }

            _theBoard.UnmakeLastMove();
            return alpha;

        }


        #region Minor private helper functions


        private void SecondTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_cancel && _isSearching)
            {
                _infoSecondsUsed += 1;
                _infoNodesPerSecond = _infoNodesLookedAt / _infoSecondsUsed;
                InfoUpdated?.Invoke();
            }
        }


        private void ResetStatTrackers()
        {

            _isSearching = true;
            _hasCancelled = false;
            _cancel = false;

            _bestScore = 0;
            _infoSecondsUsed = 0;
            _infoNodesPerSecond = 0;
            _infoNodesLookedAt = 0;
            _infoCurrentSearchDepth = 0;

            //Create a timer for one second so we can periodically update whichever UCI GUI/Command line is controlling us            
            _secondTimer.Interval = 1000;
            _secondTimer.Start();

        }


        #endregion

    }
}
