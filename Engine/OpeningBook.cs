using static Lisa.Globals;
namespace Lisa
{
    //Positions are keyed by Board.GenerateFen, the same call BookMaker uses to build the book, so both always agree.
    //Line format:  <fen>;<move> <weight>,<move> <weight>   Lines starting # are comments.
    //A move is picked at random in proportion to its weight
    public sealed class OpeningBook
    {

        private readonly Dictionary<string, BookMove[]> _positions = new();
        private readonly Random _random = new();

        public OpeningBook(string bookFile)
        {

            try
            {
                foreach (string line in File.ReadLines(bookFile))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    int separator = trimmed.IndexOf(';');
                    if (separator < 0)
                    {
                        continue;
                    }

                    List<BookMove> moves = new();
                    foreach (string moveText in trimmed.Substring(separator + 1).Split(','))
                    {
                        string[] parts = moveText.Trim().Split(' ');
                        if (parts.Length != 2 || parts[0].Length < 4)
                        {
                            continue;
                        }
                        moves.Add(new BookMove
                        {
                            From = SquareFromName(parts[0].Substring(0, 2)),
                            To = SquareFromName(parts[0].Substring(2, 2)),
                            Weight = Convert.ToInt32(parts[1])
                        });
                    }

                    if (moves.Count > 0)
                    {
                        _positions[trimmed.Substring(0, separator)] = moves.ToArray();
                    }
                }
            }
            catch { }

        }


        public bool LookInBook(string fen, out byte fromSquare, out byte toSquare)
        {

            fromSquare = 255;
            toSquare = 255;

            if (!_positions.TryGetValue(fen, out BookMove[]? moves))
            {
                return false;
            }

            int totalWeight = 0;
            foreach (BookMove move in moves)
            {
                totalWeight += move.Weight;
            }

            int pick = _random.Next(totalWeight);
            foreach (BookMove move in moves)
            {
                pick -= move.Weight;
                if (pick < 0)
                {
                    fromSquare = move.From;
                    toSquare = move.To;
                    return true;
                }
            }

            return false;

        }


        //a8 = 0, h1 = 63
        private static byte SquareFromName(string name)
        {
            return (byte)((8 - (name[1] - '0')) * 8 + (name[0] - 'a'));
        }

    }
}
