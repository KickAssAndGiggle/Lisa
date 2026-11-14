using static Lisa.Globals;
namespace Lisa
{
    public class UCIInterface
    {
        private Board _gameBoard;
        private Searcher _search;

        private const byte MAX_SEARCH_DEPTH = 20;
        //Given to both clocks for "go depth N", so only the depth ends the search
        private const int FIXED_DEPTH_CLOCK_MILLISECONDS = 1000000000;

        private int _timeInMils = 0;
        private int _oppTimeInMils = 0;
        private byte _maxDepth = MAX_SEARCH_DEPTH;
        private List<long> _seenZobrist = new();

        //Creates the searcher and an empty game board
        public UCIInterface()
        {
            _search = new Searcher();
            _gameBoard = new Board();
        }

        //Runs the UCI loop: reads commands from standard input one line at a time and handles uci, isready,
        //setoption, ucinewgame, position, go (starting the search on its own thread), stop and quit
        public void InitiateUCI()
        {
            bool exit = false;

            _search.BestMoveSelected += Search_BestMoveSelected;
            _search.InfoUpdated += Search_InfoUpdated;
            _search.TablebaseHit += Search_TablebaseHit;

            do
            {
                string command = "";

                do
                {
                    int streamVal = Console.In.Read();
                    if (streamVal != -1)
                    {
                        if (streamVal < 32)
                        {
                            break;
                        }
                        command += Convert.ToString(Convert.ToChar(streamVal));
                    }
                } while (true);

                if (command.StartsWith("uci"))
                {
                    UCIReportID();
                }
                else if (command.Contains("quit"))
                {
                    exit = true;
                }
                else if (command.Contains("isready"))
                {
                    if (_search.IsSearching)
                    {
                        CancelSearch();
                    }
                    InitialiseScorer();
                    Console.Out.Write("readyok" + Convert.ToChar(10));
                }
                else if (command.StartsWith("setoption"))
                {
                    //Lisa has no UCI options: setoption is accepted and ignored, so it can't fall through to the
                    //branches below
                }
                else if (command.Contains("new"))
                {
                    if (_search.IsSearching)
                    {
                        CancelSearch();
                    }

                    SetupGame("startpos");
                }
                else if (command.Contains("position"))
                {
                    if (_search.IsSearching)
                    {
                        CancelSearch();
                    }

                    string[] splits = command.Split(Convert.ToChar(" "));
                    string posString = splits[1];

                    if (posString.ToLower() == "fen")
                    {
                        posString = "";
                        int moveStartIndex = 0;
                        for (int nn = 2; nn < splits.Length; nn++)
                        {
                            if (splits[nn].ToLower() != "moves")
                            {
                                posString += splits[nn] + " ";
                            }
                            else
                            {
                                posString = posString.TrimEnd();
                                moveStartIndex = nn + 1;
                                break;
                            }
                        }

                        if (moveStartIndex > 0)
                        {
                            string[] moveARR = new string[splits.Length - moveStartIndex];
                            Array.Copy(splits, moveStartIndex, moveARR, 0, splits.Length - moveStartIndex);
                            SetupGame(posString, moveARR);
                        }
                        else
                        {
                            SetupGame(posString, null);
                        }
                    }
                    else if (posString.ToLower() == "startpos")
                    {
                        if (splits.Length > 2)
                        {
                            string[] moveARR = new string[splits.Length - 3];
                            Array.Copy(splits, 3, moveARR, 0, splits.Length - 3);
                            SetupGame(posString, moveARR);
                        }
                        else
                        {
                            SetupGame(posString, null);
                        }
                    }
                }
                else if (command.StartsWith("go"))
                {
                    string[] splits = command.Split(Convert.ToChar(" "));
                    _maxDepth = MAX_SEARCH_DEPTH;
                    bool fixedDepth = false;

                    for (int nn = 0; nn < splits.Length; nn++)
                    {
                        string commandPart = splits[nn];
                        if (commandPart.ToLower() == "wtime" && _gameBoard.OnMove == WHITE)
                        {
                            string msString = splits[nn + 1];
                            _timeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "wtime" && _gameBoard.OnMove == BLACK)
                        {
                            string msString = splits[nn + 1];
                            _oppTimeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "btime" && _gameBoard.OnMove == BLACK)
                        {
                            string msString = splits[nn + 1];
                            _timeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "btime" && _gameBoard.OnMove == WHITE)
                        {
                            string msString = splits[nn + 1];
                            _oppTimeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "depth")
                        {
                            int depth = Convert.ToInt32(splits[nn + 1]);
                            _maxDepth = (byte)Math.Clamp(depth, 1, MAX_SEARCH_DEPTH);
                            fixedDepth = true;
                        }
                    }

                    if (fixedDepth)
                    {
                        _timeInMils = FIXED_DEPTH_CLOCK_MILLISECONDS;
                        _oppTimeInMils = FIXED_DEPTH_CLOCK_MILLISECONDS;
                    }

                    Thread searchThread = new(new ThreadStart(SearchPosition));
                    searchThread.Priority = ThreadPriority.Highest;
                    searchThread.Start();
                }
                else if (command.StartsWith("stop"))
                {
                    CancelSearch();
                    UCIMoveMade(_search.BestMove);
                }
            } while (!exit);
        }


        //Sends the move the endgame tablebase chose as the best move
        private void Search_TablebaseHit()
        {
            UCISendToGUI("bestmove " + _search.UCITablebaseHit);
        }

        //Stops the running search and waits until it has finished
        private void CancelSearch()
        {
            _search.Cancel();
            do
            {
                Thread.Sleep(1);
            } while (!_search.HasCancelled);
            Thread.Sleep(100);
        }


        //Sends the GUI an info line for the search so far: depth, time, nodes, nodes per second, score and
        //principal variation
        private void Search_InfoUpdated()
        {
            try
            {
                Move[]? pv = _search.BestPV;

                if (pv != null)
                {
                    string infoString = "info seldepth " + _search.Stats.CurrentSearchDepth.ToString() + " depth " +
                        _search.Stats.CurrentSearchDepth.ToString() +
                        " time " + (_search.Stats.SecondsUsed * 1000).ToString();
                    infoString += " nodes " + _search.NodesLookedAtAllThreads().ToString() + " nps " +
                        _search.Stats.NodesPerSecond.ToString();

                    string pvString = "";
                    for (int nn = pv.Length - 1; nn >= 0; nn--)
                    {
                        if (!(pv[nn].From == 0 && pv[nn].To == 0))
                        {
                            pvString += ConvertMoveToString(pv[nn]) + " ";
                        }
                        else
                        {
                            break;
                        }
                    }

                    pvString = pvString.Trim();
                    infoString += " score cp " + _search.BestScore.ToString() + " pv " + pvString;
                    UCISendToGUI(infoString);
                }
            }
            catch { }
        }


        //Sends the search's best move to the GUI, unless the search was cancelled
        private void Search_BestMoveSelected()
        {
            try
            {
                if (!_search.HasCancelled)
                {
                    UCIMoveMade(_search.BestMove);
                }
            }
            catch { }
        }


        //Answers the uci command with the engine's name and author, then uciok
        private static void UCIReportID()
        {
            UCISendToGUI("id name PawnStorm");
            UCISendToGUI("id author Russell Lambert");
            UCISendToGUI("id country ENG");
            UCISendToGUI("uciok");
        }


        //Sends the given move to the GUI as the best move
        private static void UCIMoveMade(Move made)
        {
            string moveString = ConvertMoveToString(made);
            UCISendToGUI("bestmove " + moveString);
        }


        //Searches the current game position with the depth and clock times from the last go command. Runs on
        //its own thread
        private void SearchPosition()
        {
            _search.Search(ref _gameBoard, _maxDepth, _timeInMils, _oppTimeInMils, _seenZobrist);
        }


        //Sets the game board to the start position or a FEN, then plays the GUI's moves on it, recording each
        //position's Zobrist key so the search can spot repetitions
        private void SetupGame(string startPos, string[]? guiMoves = null)
        {
            _gameBoard = new Board();
            if (startPos == "startpos")
            {
                _gameBoard.InitialiseFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            }
            else
            {
                _gameBoard.InitialiseFromFEN(startPos);
            }

            _seenZobrist.Clear();
            _seenZobrist.Add(_gameBoard.CurrentZobrist);
            if (guiMoves != null)
            {
                for (int NN = 0; NN <= guiMoves.Length - 1; NN++)
                {
                    string startBit = guiMoves[NN].Substring(0, 2);
                    string endBit = guiMoves[NN].Substring(2);

                    int startRank = Convert.ToInt32(startBit.Substring(1));
                    int startFile = GetXByColumn(startBit.Substring(0, 1));

                    byte baseStartSquare = 0;
                    if (startRank == 7)
                    {
                        baseStartSquare = 8;
                    }
                    else if (startRank == 6)
                    {
                        baseStartSquare = 16;
                    }
                    else if (startRank == 5)
                    {
                        baseStartSquare = 24;
                    }
                    else if (startRank == 4)
                    {
                        baseStartSquare = 32;
                    }
                    else if (startRank == 3)
                    {
                        baseStartSquare = 40;
                    }
                    else if (startRank == 2)
                    {
                        baseStartSquare = 48;
                    }
                    else if (startRank == 1)
                    {
                        baseStartSquare = 56;
                    }

                    baseStartSquare += (byte)startFile;

                    int endRank = Convert.ToInt32(endBit.Substring(1, 1));
                    int endFile = GetXByColumn(endBit.Substring(0, 1));

                    byte baseEndSquare = 0;
                    if (endRank == 7)
                    {
                        baseEndSquare = 8;
                    }
                    else if (endRank == 6)
                    {
                        baseEndSquare = 16;
                    }
                    else if (endRank == 5)
                    {
                        baseEndSquare = 24;
                    }
                    else if (endRank == 4)
                    {
                        baseEndSquare = 32;
                    }
                    else if (endRank == 3)
                    {
                        baseEndSquare = 40;
                    }
                    else if (endRank == 2)
                    {
                        baseEndSquare = 48;
                    }
                    else if (endRank == 1)
                    {
                        baseEndSquare = 56;
                    }

                    baseEndSquare += (byte)endFile;

                    Move mv = new()
                    {
                        From = baseStartSquare,
                        To = baseEndSquare
                    };

                    if (guiMoves[NN].ToUpper().EndsWith("Q"))
                    {
                        mv.PromotionPiece = QUEEN;
                    }
                    else if (guiMoves[NN].ToUpper().EndsWith("N"))
                    {
                        mv.PromotionPiece = KNIGHT;
                    }
                    else if (guiMoves[NN].ToUpper().EndsWith("R"))
                    {
                        mv.PromotionPiece = ROOK;
                    }
                    else if (guiMoves[NN].ToUpper().EndsWith("B"))
                    {
                        mv.PromotionPiece = BISHOP;
                    }

                    if (_gameBoard.Color[baseEndSquare] != EMPTY)
                    {
                        mv.IsCapture = true;
                    }
                    else if (baseEndSquare == _gameBoard.EnPasantCapSquare && _gameBoard.Piece[baseStartSquare] == PAWN)
                    {
                        mv.IsCapture = true;
                    }

                    _gameBoard.MakeMove(mv, _gameBoard.OnMove, false);
                    _gameBoard.ResetUndoMoves();
                    _seenZobrist.Add(_gameBoard.CurrentZobrist);
                }
            }
        }


        //Writes a line to the GUI on standard output
        private static void UCISendToGUI(string Value)
        {
            Console.Out.Write(Value + Convert.ToChar(10));
        }
    }
}
