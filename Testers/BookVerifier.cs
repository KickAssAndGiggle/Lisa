using System.Diagnostics;
using System.Globalization;
using System.Text;
using static Lisa.Globals;
namespace Lisa
{
    //Checks every move in an opening book with Lisa's own search, so she only ever leaves the book in a position she
    //is happy with. Each move is played and the position after it searched to BookVerifyDepth; the move is kept only
    //if the score for the side that played it is at least BookMinScoreWhite or BookMinScoreBlack. A position left with
    //no moves drops out of the book. Writes the cleaned book, a log, and every rejected move with its score.
    public sealed class BookVerifier
    {
        //Each worker thread runs its own single-threaded search, so many book moves are searched at once. A
        //fixed-depth search of an opening position needs far less hash than a game does
        private const int WORKER_TT_HASH_MB = 16;
        private const int WORKER_PAWN_HASH_MB = 4;
        private const int WORKER_SCORE_HASH_MB = 8;
        private const int WORKER_STACK_BYTES = 16 * 1024 * 1024;

        private const int MATE_SCORE = 5000;
        private const int ILLEGAL_MOVE_SCORE = int.MinValue;
        private const int PROGRESS_EVERY = 1000;

        private sealed class BookLine
        {
            public string Fen = "";
            public bool WhiteToMove;
            public string[] Moves = Array.Empty<string>();
            public string[] Weights = Array.Empty<string>();
            public int[] Scores = Array.Empty<int>();
        }

        private readonly string _inputFile;
        private readonly string _outputFile;
        private readonly string _logFile;
        private readonly string _rejectsFile;
        private readonly byte _depth;
        private readonly int _minScoreWhite;
        private readonly int _minScoreBlack;
        private readonly int _threads;

        private readonly List<string> _headerLines = new();
        private readonly List<BookLine> _lines = new();

        //Every book move is one work item: the line it is in and its place in that line
        private int[] _itemLine = Array.Empty<int>();
        private int[] _itemMove = Array.Empty<int>();
        private int _nextItem;
        private int _itemsDone;

        private readonly object _logLock = new();
        private readonly Stopwatch _timer = new();

        //Stores the file paths, search depth, the score each side's book moves must reach, and the number of
        //worker threads (0 for one per processor)
        public BookVerifier(string inputFile, string outputFile, byte depth, int minScoreWhite, int minScoreBlack,
            int threads)
        {
            _inputFile = inputFile;
            _outputFile = outputFile;
            _logFile = outputFile + ".log.txt";
            _rejectsFile = outputFile + ".rejects.txt";
            _depth = depth;
            _minScoreWhite = minScoreWhite;
            _minScoreBlack = minScoreBlack;
            _threads = threads > 0 ? threads : Environment.ProcessorCount;
        }


        //Scores every move in the book with a search, using several worker threads, then writes the moves that
        //score well enough to the output file and the rest to the rejects file. Progress goes to the log file
        public void Verify()
        {
            File.WriteAllText(_logFile, "");

            if (!File.Exists(_inputFile))
            {
                Log("Book file not found: " + _inputFile);
                return;
            }

            _timer.Start();
            ReadBook();
            Log("Read " + _lines.Count + " positions, " + _itemLine.Length + " moves from " + _inputFile);
            Log("Depth " + _depth + ", White needs " + _minScoreWhite + ", Black needs " + _minScoreBlack + ", " +
                _threads + " threads");

            //The search plays a book move instantly when its position is in the book, so the book is emptied first,
            //and every worker searches on one thread of its own
            OPENING_BOOK = new OpeningBook("");
            SEARCH_THREADS = 1;
            TT_HASH_SIZE_MB = WORKER_TT_HASH_MB;
            PAWN_STRUCTURE_HASH_SIZE_MB = WORKER_PAWN_HASH_MB;
            POSITION_SCORE_HASH_SIZE_MB = WORKER_SCORE_HASH_MB;

            List<Thread> workers = new();
            for (int tt = 0; tt < _threads; tt++)
            {
                Thread worker = new(() => VerifyMoves(), WORKER_STACK_BYTES);
                workers.Add(worker);
                worker.Start();
            }
            foreach (Thread worker in workers)
            {
                worker.Join();
            }

            WriteResults();
            Log("Finished.");
        }


        //Reads the book file into positions and their moves and weights, keeping its header lines, and makes
        //the list of moves the workers take from
        private void ReadBook()
        {
            List<int> itemLine = new();
            List<int> itemMove = new();

            foreach (string line in File.ReadLines(_inputFile))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                if (trimmed.StartsWith("#"))
                {
                    _headerLines.Add(trimmed);
                    continue;
                }

                int separator = trimmed.IndexOf(';');
                if (separator < 0)
                {
                    continue;
                }

                List<string> moves = new();
                List<string> weights = new();
                foreach (string moveText in trimmed.Substring(separator + 1).Split(','))
                {
                    string[] parts = moveText.Trim().Split(' ');
                    if (parts.Length == 2 && parts[0].Length >= 4)
                    {
                        moves.Add(parts[0]);
                        weights.Add(parts[1]);
                    }
                }
                if (moves.Count == 0)
                {
                    continue;
                }

                string fen = trimmed.Substring(0, separator);
                string[] fenFields = fen.Split(' ');
                BookLine bookLine = new()
                {
                    Fen = fen,
                    WhiteToMove = fenFields.Length > 1 && fenFields[1] == "w",
                    Moves = moves.ToArray(),
                    Weights = weights.ToArray(),
                    Scores = new int[moves.Count]
                };

                for (int mm = 0; mm < moves.Count; mm++)
                {
                    itemLine.Add(_lines.Count);
                    itemMove.Add(mm);
                }
                _lines.Add(bookLine);
            }

            _itemLine = itemLine.ToArray();
            _itemMove = itemMove.ToArray();
        }


        //Run by each worker thread: takes the next unscored book move until none are left
        private void VerifyMoves()
        {
            Searcher searcher = new();
            Board board = new();

            while (true)
            {
                int item = Interlocked.Increment(ref _nextItem) - 1;
                if (item >= _itemLine.Length)
                {
                    break;
                }

                BookLine line = _lines[_itemLine[item]];
                int moveIndex = _itemMove[item];
                line.Scores[moveIndex] = ScoreForMover(searcher, board, line.Fen, line.Moves[moveIndex]);

                int done = Interlocked.Increment(ref _itemsDone);
                if (done % PROGRESS_EVERY == 0)
                {
                    Log(done + " of " + _itemLine.Length + " moves searched");
                }
            }
        }


        //The score of a book move for the side that played it, or ILLEGAL_MOVE_SCORE if it can't be played there
        private int ScoreForMover(Searcher searcher, Board board, string fen, string moveText)
        {
            board.InitialiseFromFEN(fen);

            List<Move> legal = LegalMoves(board);
            foreach (Move move in legal)
            {
                if (ConvertMoveToString(move) == moveText)
                {
                    board.MakeMove(move, board.OnMove, false);
                    board.ResetUndoMoves();
                    return -SideToMoveScore(searcher, board);
                }
            }

            return ILLEGAL_MOVE_SCORE;
        }


        //The search plays a lone legal move without scoring it, so a forced move is made here and the position after
        //it scored instead
        private int SideToMoveScore(Searcher searcher, Board board)
        {
            List<Move> legal = LegalMoves(board);

            if (legal.Count == 0)
            {
                return board.IsInCheck(board.OnMove) ? -MATE_SCORE : 0;
            }

            if (legal.Count == 1)
            {
                board.MakeMove(legal[0], board.OnMove, false);
                board.ResetUndoMoves();
                return -SideToMoveScore(searcher, board);
            }

            searcher.Search(ref board, _depth);
            return searcher.BestScore;
        }


        //The legal moves for the side to move on the board
        private static List<Move> LegalMoves(Board board)
        {
            List<Move> legal = new();
            foreach (Move move in board.GenerateAllMoves(board.OnMove))
            {
                if (board.MoveIsLegal(move, board.OnMove))
                {
                    legal.Add(move);
                }
            }
            return legal;
        }


        //Writes the verified book (only moves scoring at least the minimum for their side, dropping positions
        //left with none) and the rejects file, with the score each rejected move got
        private void WriteResults()
        {
            List<string> bookLines = new();
            List<string> rejectLines = new()
            {
                "# Book moves rejected: <fen>;<move> <weight>  score <score for the side that played it>, needed " +
                    "<minimum>"
            };
            int movesKept = 0;
            int movesRejected = 0;
            int positionsDropped = 0;

            foreach (BookLine line in _lines)
            {
                int minScore = line.WhiteToMove ? _minScoreWhite : _minScoreBlack;
                List<string> kept = new();

                for (int mm = 0; mm < line.Moves.Length; mm++)
                {
                    string moveText = line.Moves[mm] + " " + line.Weights[mm];
                    int score = line.Scores[mm];
                    if (score >= minScore)
                    {
                        kept.Add(moveText);
                        movesKept += 1;
                    }
                    else
                    {
                        string scoreText = score == ILLEGAL_MOVE_SCORE ? "illegal"
                            : score.ToString(CultureInfo.InvariantCulture);
                        rejectLines.Add(line.Fen + ";" + moveText + "  score " + scoreText + ", needed " + minScore);
                        movesRejected += 1;
                    }
                }

                if (kept.Count > 0)
                {
                    bookLines.Add(line.Fen + ";" + string.Join(",", kept));
                }
                else
                {
                    positionsDropped += 1;
                }
            }

            List<string> output = new(_headerLines)
            {
                "# Verified from " + _inputFile + " at depth " + _depth + ": a move is kept when the side that " +
                    "played it scores at least " + _minScoreWhite + " (White) or " + _minScoreBlack + " (Black)",
                "# Moves kept " + movesKept + ", rejected " + movesRejected + ", positions dropped " +
                    positionsDropped + ", time " + (_timer.ElapsedMilliseconds / 1000) + "s"
            };
            output.AddRange(bookLines);

            File.WriteAllLines(_outputFile, output);
            File.WriteAllLines(_rejectsFile, rejectLines);

            Log("Moves kept " + movesKept + ", rejected " + movesRejected + ", positions dropped " + positionsDropped);
            Log("Cleaned book written to " + _outputFile + ", rejected moves to " + _rejectsFile);
        }


        //Adds a line to the log file, stamped with the time since verifying began. Safe to call from any thread
        private void Log(string message)
        {
            TimeSpan elapsed = _timer.Elapsed;
            string line = elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) + "  " + message;
            lock (_logLock)
            {
                File.AppendAllText(_logFile, line + "\r\n", Encoding.UTF8);
            }
        }
    }
}
