using System.Diagnostics;
using System.Text;
using static Lisa.Globals;
namespace Lisa
{
    //Reads a PGN file game by game, replays each game on a Board, picks a random sample of quiet
    //positions from it and writes them as EPD lines labelled with the game result from White's
    //point of view:  <fen> c9 "1-0";
    public sealed class TexelFileMaker
    {

        private const int PROGRESS_EVERY_GAMES = 10000;
        private const int MAX_REPORTED_FAILURES = 20;
        private const int SAMPLING_SEED = 20261101;

        private readonly string _pgnFile;
        private readonly string _epdFile;
        private readonly int _positionsPerGame;
        private readonly int _skipPlies;
        private readonly int _maxGames;
        private readonly int _gameStride;

        private readonly Random _sampler = new(SAMPLING_SEED);
        private readonly HashSet<long> _writtenZobrists = new();
        private readonly List<string> _failures = new();

        private long _gamesRead;
        private long _gamesUsed;
        private long _gamesSkippedNotStandard;
        private long _gamesSkippedNoResult;
        private long _gamesFailedToParse;
        private long _positionsWritten;
        private long _duplicatesDropped;
        private long _whiteWins;
        private long _draws;
        private long _blackWins;

        private struct Candidate
        {
            public string Fen;
            public long Zobrist;
        }

        public TexelFileMaker(string pgnFile, string epdFile, int positionsPerGame, int skipPlies, int maxGames, int gameStride)
        {
            _pgnFile = pgnFile;
            _epdFile = epdFile;
            _positionsPerGame = positionsPerGame;
            _skipPlies = skipPlies;
            _maxGames = maxGames;
            _gameStride = Math.Max(1, gameStride);
        }


        public void MakeFile()
        {

            if (!File.Exists(_pgnFile))
            {
                Console.WriteLine("PGN file not found: " + _pgnFile);
                return;
            }

            Stopwatch timer = Stopwatch.StartNew();

            using StreamReader reader = new(_pgnFile, Encoding.UTF8, true, 1 << 20);
            using StreamWriter writer = new(_epdFile, false, new UTF8Encoding(false), 1 << 20);

            List<string> headers = new();
            StringBuilder moveText = new();
            bool inMoveText = false;
            bool enoughGames = false;

            string? line;
            while (!enoughGames && (line = reader.ReadLine()) != null)
            {

                string trimmed = line.Trim();

                if (trimmed.StartsWith("["))
                {
                    if (inMoveText)
                    {
                        enoughGames = !ProcessGame(headers, moveText.ToString(), writer, timer);
                        headers.Clear();
                        moveText.Clear();
                        inMoveText = false;
                    }
                    headers.Add(trimmed);
                }
                else if (trimmed.Length > 0 && !trimmed.StartsWith("%"))
                {
                    inMoveText = true;
                    moveText.Append(WithoutLineComment(trimmed)).Append(' ');
                }
            }

            if (inMoveText && !enoughGames)
            {
                ProcessGame(headers, moveText.ToString(), writer, timer);
            }

            writer.Flush();
            timer.Stop();

            WriteReport(timer);

        }


        //Returns false once enough games have been used
        private bool ProcessGame(List<string> headers, string moveText, StreamWriter writer, Stopwatch timer)
        {

            _gamesRead += 1;

            if ((_gamesRead - 1) % _gameStride != 0)
            {
                return true;
            }

            string result = "";
            foreach (string header in headers)
            {
                string tag = HeaderTag(header);
                if (tag == "FEN" || tag == "SetUp" || tag == "Variant")
                {
                    _gamesSkippedNotStandard += 1;
                    return true;
                }
                if (tag == "Result")
                {
                    result = HeaderValue(header);
                }
            }

            if (result != "1-0" && result != "0-1" && result != "1/2-1/2")
            {
                _gamesSkippedNoResult += 1;
                return true;
            }

            List<string> sanMoves = ExtractSANMoves(moveText);
            List<Candidate> candidates = new();

            if (!ReplayGame(sanMoves, candidates))
            {
                _gamesFailedToParse += 1;
                return true;
            }

            _gamesUsed += 1;
            WriteSample(candidates, result, writer);

            if (_gamesUsed % PROGRESS_EVERY_GAMES == 0)
            {
                Console.WriteLine("Games read " + _gamesRead + ", used " + _gamesUsed + ", positions " + _positionsWritten + ", " + (timer.ElapsedMilliseconds / 1000) + "s");
            }

            return _gamesUsed < _maxGames;

        }


        //A position is a candidate when the side to move is not in check, the move that reached it was
        //not a capture, and the move played from it is neither a capture nor a promotion
        private bool ReplayGame(List<string> sanMoves, List<Candidate> candidates)
        {

            Board gameBoard = new();
            gameBoard.InitialiseFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            gameBoard.ResetUndoMoves();

            bool previousMoveWasCapture = false;

            for (int ply = 0; ply < sanMoves.Count; ply++)
            {

                if (!TryParseSAN(sanMoves[ply], ref gameBoard, out Move played))
                {
                    if (_failures.Count < MAX_REPORTED_FAILURES)
                    {
                        _failures.Add("Game " + _gamesRead + ", ply " + (ply + 1) + ": '" + sanMoves[ply] + "' in " + gameBoard.GenerateFen());
                    }
                    return false;
                }

                bool quiet = ply >= _skipPlies
                    && !previousMoveWasCapture
                    && !played.IsCapture
                    && played.PromotionPiece == 0
                    && !gameBoard.IsInCheck(gameBoard.OnMove);

                if (quiet)
                {
                    candidates.Add(new Candidate { Fen = gameBoard.GenerateFen(), Zobrist = gameBoard.CurrentZobrist });
                }

                gameBoard.MakeMove(played, gameBoard.OnMove, false);
                gameBoard.ResetUndoMoves();
                previousMoveWasCapture = played.IsCapture;
            }

            return true;

        }


        //Partial Fisher-Yates shuffle: takes candidates in random order until enough unseen positions are found
        private void WriteSample(List<Candidate> candidates, string result, StreamWriter writer)
        {

            int taken = 0;
            for (int i = 0; i < candidates.Count && taken < _positionsPerGame; i++)
            {

                int pick = _sampler.Next(i, candidates.Count);
                Candidate chosen = candidates[pick];
                candidates[pick] = candidates[i];
                candidates[i] = chosen;

                if (!_writtenZobrists.Add(chosen.Zobrist))
                {
                    _duplicatesDropped += 1;
                    continue;
                }

                writer.Write(chosen.Fen);
                writer.Write(" c9 \"");
                writer.Write(result);
                writer.Write("\";\r\n");

                taken += 1;
                _positionsWritten += 1;

                if (result == "1-0")
                {
                    _whiteWins += 1;
                }
                else if (result == "0-1")
                {
                    _blackWins += 1;
                }
                else
                {
                    _draws += 1;
                }
            }

        }


        //A ; comment runs to the end of its line, unless the ; sits inside a {...} comment on that line
        private static string WithoutLineComment(string line)
        {
            int semicolon = line.IndexOf(';');
            if (semicolon < 0)
            {
                return line;
            }
            int brace = line.IndexOf('{');
            if (brace >= 0 && brace < semicolon)
            {
                return line;
            }
            return line.Substring(0, semicolon);
        }


        //Strips comments {...}, variations (...), NAGs $n, move numbers and the result token
        private static List<string> ExtractSANMoves(string moveText)
        {

            List<string> moves = new();
            StringBuilder token = new();
            int variationDepth = 0;
            bool inBraceComment = false;

            for (int i = 0; i <= moveText.Length; i++)
            {

                char c = i < moveText.Length ? moveText[i] : ' ';

                if (inBraceComment)
                {
                    if (c == '}')
                    {
                        inBraceComment = false;
                    }
                    continue;
                }
                if (c == '{' || c == '(' || c == ')' || char.IsWhiteSpace(c))
                {
                    if (token.Length > 0 && variationDepth == 0)
                    {
                        AddMoveToken(token.ToString(), moves);
                    }
                    token.Clear();

                    if (c == '{')
                    {
                        inBraceComment = true;
                    }
                    else if (c == '(')
                    {
                        variationDepth += 1;
                    }
                    else if (c == ')')
                    {
                        variationDepth = Math.Max(0, variationDepth - 1);
                    }
                    continue;
                }

                token.Append(c);
            }

            return moves;

        }


        private static void AddMoveToken(string token, List<string> moves)
        {

            if (token.StartsWith("$") || token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
            {
                return;
            }

            //Move numbers: "12." "12..." or attached as "12.e4"
            int start = 0;
            while (start < token.Length && char.IsDigit(token[start]))
            {
                start += 1;
            }
            if (start > 0 && start < token.Length && token[start] == '.')
            {
                while (start < token.Length && token[start] == '.')
                {
                    start += 1;
                }
                token = token.Substring(start);
            }
            else if (start == token.Length)
            {
                return;
            }

            token = token.TrimEnd('+', '#', '!', '?');
            if (token.Length > 0)
            {
                moves.Add(token);
            }

        }


        //Matches a SAN token against the legal moves; succeeds only when exactly one legal move fits
        private static bool TryParseSAN(string san, ref Board theBoard, out Move found)
        {

            found = new Move();
            int toMove = theBoard.OnMove;

            if (san == "O-O" || san == "0-0" || san == "O-O-O" || san == "0-0-0")
            {
                bool kingSide = san.Length == 3;
                byte kingFrom = (byte)(toMove == WHITE ? 60 : 4);
                byte kingTo = (byte)(kingSide ? kingFrom + 2 : kingFrom - 2);
                return FindUniqueMove(ref theBoard, KING, kingFrom, kingTo, -1, -1, 0, out found);
            }

            string body = san.Replace("x", "").Replace(":", "");

            byte promotion = 0;
            int equals = body.IndexOf('=');
            if (equals >= 0)
            {
                if (equals + 1 >= body.Length)
                {
                    return false;
                }
                promotion = PromotionPieceFromLetter(body[equals + 1]);
                body = body.Substring(0, equals);
            }
            else if (body.Length >= 3 && "NBRQ".IndexOf(body[^1]) >= 0 && char.IsDigit(body[^2]))
            {
                promotion = PromotionPieceFromLetter(body[^1]);
                body = body.Substring(0, body.Length - 1);
            }

            int piece = PAWN;
            if (body.Length > 0 && "NBRQK".IndexOf(body[0]) >= 0)
            {
                piece = "PNBRQK".IndexOf(body[0]);
                body = body.Substring(1);
            }

            if (body.Length < 2)
            {
                return false;
            }

            int toSquare = SquareFromName(body.Substring(body.Length - 2));
            if (toSquare < 0)
            {
                return false;
            }

            int fromFile = -1;
            int fromRank = -1;
            foreach (char d in body.Substring(0, body.Length - 2))
            {
                if (d >= 'a' && d <= 'h')
                {
                    fromFile = d - 'a';
                }
                else if (d >= '1' && d <= '8')
                {
                    fromRank = d - '1';
                }
                else
                {
                    return false;
                }
            }

            return FindUniqueMove(ref theBoard, piece, -1, toSquare, fromFile, fromRank, promotion, out found);

        }


        private static bool FindUniqueMove(ref Board theBoard, int piece, int fromSquare, int toSquare, int fromFile, int fromRank, byte promotion, out Move found)
        {

            found = new Move();
            int matches = 0;
            int toMove = theBoard.OnMove;

            Move[] allMoves = theBoard.GenerateAllMoves(toMove);
            foreach (Move m in allMoves)
            {
                if (m.To != toSquare || theBoard.Piece[m.From] != piece || m.PromotionPiece != promotion)
                {
                    continue;
                }
                if (fromSquare >= 0 && m.From != fromSquare)
                {
                    continue;
                }
                if (fromFile >= 0 && m.From % 8 != fromFile)
                {
                    continue;
                }
                if (fromRank >= 0 && 8 - m.From / 8 - 1 != fromRank)
                {
                    continue;
                }
                if (!theBoard.MoveIsLegal(m, toMove))
                {
                    continue;
                }
                found = m;
                matches += 1;
            }

            return matches == 1;

        }


        private static byte PromotionPieceFromLetter(char letter)
        {
            return letter switch
            {
                'N' => KNIGHT,
                'B' => BISHOP,
                'R' => ROOK,
                'Q' => QUEEN,
                _ => (byte)0
            };
        }


        //a8 = 0, h1 = 63
        private static int SquareFromName(string name)
        {
            if (name[0] < 'a' || name[0] > 'h' || name[1] < '1' || name[1] > '8')
            {
                return -1;
            }
            return (8 - (name[1] - '0')) * 8 + (name[0] - 'a');
        }


        private static string HeaderTag(string header)
        {
            int space = header.IndexOf(' ');
            return space > 1 ? header.Substring(1, space - 1) : "";
        }


        private static string HeaderValue(string header)
        {
            int first = header.IndexOf('"');
            int last = header.LastIndexOf('"');
            return (first >= 0 && last > first) ? header.Substring(first + 1, last - first - 1) : "";
        }


        private void WriteReport(Stopwatch timer)
        {

            List<string> report = new()
            {
                "Texel file: " + _epdFile,
                "From PGN: " + _pgnFile,
                "Settings: positions/game " + _positionsPerGame + ", skip plies " + _skipPlies + ", max games " + _maxGames + ", stride " + _gameStride,
                "",
                "Games read: " + _gamesRead,
                "Games used: " + _gamesUsed,
                "Skipped, not standard start: " + _gamesSkippedNotStandard,
                "Skipped, no decisive or drawn result: " + _gamesSkippedNoResult,
                "Failed to parse: " + _gamesFailedToParse,
                "",
                "Positions written: " + _positionsWritten,
                "Duplicates dropped: " + _duplicatesDropped,
                "White wins / draws / Black wins: " + _whiteWins + " / " + _draws + " / " + _blackWins,
                "Time: " + (timer.ElapsedMilliseconds / 1000) + "s"
            };

            if (_failures.Count > 0)
            {
                report.Add("");
                report.Add("First parse failures:");
                report.AddRange(_failures);
            }

            foreach (string reportLine in report)
            {
                Console.WriteLine(reportLine);
            }

            File.WriteAllLines(_epdFile + ".report.txt", report);

        }

    }
}
