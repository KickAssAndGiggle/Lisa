using System.Diagnostics;
using System.Globalization;
using System.Text;
using static Lisa.Globals;
namespace Lisa
{
    //Builds Lisa's opening book from a PGN file. Each position in the first BookMaxPlies plies is keyed by Lisa's own GenerateFen,
    //and a move is kept when it was played there in at least BookMinGames games and scored at least BookMinScorePercent for the
    //side that played it. Its weight is the square root of the points it scored: popular, successful moves are still picked most
    //often, but rarer sound moves get a real share rather than vanishing next to the main lines.
    //Two passes over the PGN keep memory down: the first counts how often each position occurs, the second records moves only
    //for positions seen often enough to hold a book move. In the starting position only BookFirstMoves are written.
    //Book line format:  <fen>;<move> <weight>,<move> <weight>
    public sealed class BookMaker
    {

        private const string START_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private sealed class MoveStats
        {
            public int Games;
            public double Points;
        }

        private sealed class BookEntry
        {
            public string Fen = "";
            public Dictionary<string, MoveStats> Moves = new();
        }

        private readonly string _pgnFile;
        private readonly string _bookFile;
        private readonly int _maxPlies;
        private readonly int _minGames;
        private readonly int _minScorePercent;
        private readonly HashSet<string> _allowedFirstMoves = new();
        private readonly string _firstMovesSetting;

        private readonly Dictionary<long, int> _positionCounts = new();
        private readonly Dictionary<long, BookEntry> _entries = new();
        private Board _board = new();

        private long _gamesRead;
        private long _gamesUsed;
        private long _gamesFailedToParse;

        public BookMaker(string pgnFile, string bookFile, int maxPlies, int minGames, int minScorePercent, string firstMoves)
        {
            _pgnFile = pgnFile;
            _bookFile = bookFile;
            _maxPlies = maxPlies;
            _minGames = minGames;
            _minScorePercent = minScorePercent;
            _firstMovesSetting = firstMoves;
        }


        public void MakeBook()
        {

            if (!File.Exists(_pgnFile))
            {
                File.WriteAllText(_bookFile, "# PGN file not found: " + _pgnFile + "\r\n");
                return;
            }

            Stopwatch timer = Stopwatch.StartNew();

            if (!ReadAllowedFirstMoves())
            {
                File.WriteAllText(_bookFile, "# BookFirstMoves could not be read as moves from the starting position: " + _firstMovesSetting + "\r\n");
                return;
            }

            ReadGames(false);
            long positionsCounted = _positionCounts.Count;

            _gamesRead = 0;
            _gamesUsed = 0;
            _gamesFailedToParse = 0;
            ReadGames(true);

            WriteBook(timer, positionsCounted);

        }


        //The settings give first moves in SAN (e4, Nf3, ...); the book stores them as Lisa's move strings (e2e4, g1f3, ...)
        private bool ReadAllowedFirstMoves()
        {
            foreach (string san in _firstMovesSetting.Split(','))
            {
                string trimmed = san.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                _board.InitialiseFromFEN(START_FEN);
                if (!TexelFileMaker.TryParseSAN(trimmed, ref _board, out Move move))
                {
                    return false;
                }
                _allowedFirstMoves.Add(ConvertMoveToString(move));
            }
            return _allowedFirstMoves.Count > 0;
        }


        private void ReadGames(bool recordMoves)
        {

            using StreamReader reader = new(_pgnFile, Encoding.UTF8, true, 1 << 20);

            List<string> headers = new();
            StringBuilder moveText = new();
            bool inMoveText = false;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("["))
                {
                    if (inMoveText)
                    {
                        ProcessGame(headers, moveText.ToString(), recordMoves);
                        headers.Clear();
                        moveText.Clear();
                        inMoveText = false;
                    }
                    headers.Add(trimmed);
                }
                else if (trimmed.Length > 0 && !trimmed.StartsWith("%"))
                {
                    inMoveText = true;
                    moveText.Append(TexelFileMaker.WithoutLineComment(trimmed)).Append(' ');
                }
            }

            if (inMoveText)
            {
                ProcessGame(headers, moveText.ToString(), recordMoves);
            }

        }


        //Replays the game's first plies. Counting pass: tallies each position. Recording pass: for positions seen at least
        //BookMinGames times, tallies the move played and the points it earned for the side that played it
        private void ProcessGame(List<string> headers, string moveText, bool recordMoves)
        {

            _gamesRead += 1;

            string result = "";
            foreach (string header in headers)
            {
                string tag = TexelFileMaker.HeaderTag(header);
                if (tag == "FEN" || tag == "SetUp" || tag == "Variant")
                {
                    return;
                }
                if (tag == "Result")
                {
                    result = TexelFileMaker.HeaderValue(header);
                }
            }

            double whitePoints;
            if (result == "1-0")
            {
                whitePoints = 1.0;
            }
            else if (result == "0-1")
            {
                whitePoints = 0.0;
            }
            else if (result == "1/2-1/2")
            {
                whitePoints = 0.5;
            }
            else
            {
                return;
            }

            List<string> sanMoves = TexelFileMaker.ExtractSANMoves(moveText);
            _board.InitialiseFromFEN(START_FEN);
            _board.ResetUndoMoves();
            _gamesUsed += 1;

            int plies = Math.Min(_maxPlies, sanMoves.Count);
            for (int ply = 0; ply < plies; ply++)
            {

                long key = _board.CurrentZobrist;
                int mover = _board.OnMove;

                if (!TexelFileMaker.TryParseSAN(sanMoves[ply], ref _board, out Move played))
                {
                    _gamesFailedToParse += 1;
                    return;
                }

                if (!recordMoves)
                {
                    _positionCounts.TryGetValue(key, out int count);
                    _positionCounts[key] = count + 1;
                }
                else if (_positionCounts.TryGetValue(key, out int seen) && seen >= _minGames)
                {
                    if (!_entries.TryGetValue(key, out BookEntry? entry))
                    {
                        entry = new BookEntry { Fen = _board.GenerateFen() };
                        _entries[key] = entry;
                    }

                    string moveString = ConvertMoveToString(played);
                    if (!entry.Moves.TryGetValue(moveString, out MoveStats? stats))
                    {
                        stats = new MoveStats();
                        entry.Moves[moveString] = stats;
                    }
                    stats.Games += 1;
                    stats.Points += mover == WHITE ? whitePoints : 1.0 - whitePoints;
                }

                _board.MakeMove(played, _board.OnMove, false);
                _board.ResetUndoMoves();
            }

        }


        private void WriteBook(Stopwatch timer, long positionsCounted)
        {

            string startFen = StartPositionFen();
            List<string> bookLines = new();
            long movesWritten = 0;

            foreach (BookEntry entry in _entries.Values)
            {

                List<KeyValuePair<string, MoveStats>> kept = new();
                foreach (KeyValuePair<string, MoveStats> move in entry.Moves)
                {
                    bool allowed = entry.Fen != startFen || _allowedFirstMoves.Contains(move.Key);
                    bool enoughGames = move.Value.Games >= _minGames;
                    bool scoresWell = move.Value.Points * 100.0 >= move.Value.Games * (double)_minScorePercent;
                    if (allowed && enoughGames && scoresWell)
                    {
                        kept.Add(move);
                    }
                }

                if (kept.Count == 0)
                {
                    continue;
                }

                kept.Sort((a, b) => b.Value.Points.CompareTo(a.Value.Points));

                List<string> moveTexts = new();
                foreach (KeyValuePair<string, MoveStats> move in kept)
                {
                    int weight = Math.Max(1, (int)Math.Round(Math.Sqrt(move.Value.Points)));
                    moveTexts.Add(move.Key + " " + weight.ToString(CultureInfo.InvariantCulture));
                }

                bookLines.Add(entry.Fen + ";" + string.Join(",", moveTexts));
                movesWritten += kept.Count;
            }

            bookLines.Sort(string.CompareOrdinal);

            List<string> lines = new()
            {
                "# Lisa opening book, built from " + _pgnFile,
                "# Max plies " + _maxPlies + ", min games " + _minGames + ", min score " + _minScorePercent + "%, first moves " + _firstMovesSetting,
                "# Games read " + _gamesRead + ", used " + _gamesUsed + ", stopped early by an unreadable move " + _gamesFailedToParse,
                "# Positions counted " + positionsCounted + ", positions written " + bookLines.Count + ", moves written " + movesWritten,
                "# Time " + (timer.ElapsedMilliseconds / 1000) + "s",
                "# Format: <fen>;<move> <weight>,<move> <weight>  (weight = square root of the points scored with the move)"
            };
            lines.AddRange(bookLines);

            File.WriteAllLines(_bookFile, lines);

        }


        private string StartPositionFen()
        {
            _board.InitialiseFromFEN(START_FEN);
            return _board.GenerateFen();
        }

    }
}
