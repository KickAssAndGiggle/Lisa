using static Lisa.Globals;
namespace Lisa
{
    public sealed class OpeningBook
    {

        BookPosition[] _bookPositions;

        public OpeningBook(string bookFile)
        {
                        
            List<BookPosition> posList = new();

            try
            {

                string[] lines = File.ReadAllLines(bookFile);
                foreach (string line in lines)
                {

                    string trimmed = line.Trim();
                    if (trimmed != "" && !trimmed.StartsWith("#"))
                    {

                        string[] splits = trimmed.Split('=');
                        BookPosition pos = new()
                        {
                            Zobrist = Convert.ToInt64(splits[0])
                        };

                        string[] moves = splits[1].Split('|');
                        List<BookMove> PosMovesList = new();

                        foreach (string move in moves)
                        {
                            string[] parts = move.Split(',');
                            BookMove fromBook = new()
                            {
                                From = Convert.ToByte(parts[0]),
                                To = Convert.ToByte(parts[1])
                            };
                            PosMovesList.Add(fromBook);
                        }

                        pos.Moves = PosMovesList.ToArray();
                        posList.Add(pos);

                    }

                }
            }
            catch { }

            _bookPositions = posList.ToArray();

        }

        public bool LookInBook(long zobrist, out byte fromSquare, out byte toSquare)
        {

            Random bookRnd = new(DateTime.Now.Millisecond * DateTime.Now.Second * DateTime.Now.Minute);
            for (int nn = 0; nn < _bookPositions.Length; nn++)
            {
                if (_bookPositions[nn].Zobrist == zobrist)
                {
                    if (_bookPositions[nn].Moves.Length > 1)
                    {
                        int randomMove = bookRnd.Next(0, _bookPositions[nn].Moves.Length);
                        fromSquare = _bookPositions[nn].Moves[randomMove].From;
                        toSquare = _bookPositions[nn].Moves[randomMove].To;

                    }
                    else
                    {
                        fromSquare = _bookPositions[nn].Moves[0].From;
                        toSquare = _bookPositions[nn].Moves[0].To;
                    }
                    return true;
                }
            }

            fromSquare = 255; 
            toSquare = 255;

            return false;

        }

    }
}
