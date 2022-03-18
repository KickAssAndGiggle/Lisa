using static Lisa.Globals;
namespace Lisa
{
    public sealed class OpeningBook
    {

        BookPosition[] BookPositions;

        public OpeningBook(string BookFile)
        {

            string[] Lines = File.ReadAllLines(BookFile);
            List<BookPosition> PosList = new();
            foreach (string Line in Lines)
            {

                string L = Line.Trim();
                if (L != "" && !L.StartsWith("#"))
                {
                    BookPosition Pos = new();
                    string[] Splits = L.Split(Convert.ToChar("="));
                    {
                        Pos.Zobrist = Convert.ToInt64(Splits[0]);
                        string[] Moves = Splits[1].Split(Convert.ToChar("|"));
                        List<BookMove> PosMovesList = new();
                        foreach (string M in Moves)
                        {
                            BookMove Bm = new();
                            string[] Parts = M.Split(Convert.ToChar(","));
                            Bm.From = Convert.ToByte(Parts[0]);
                            Bm.To = Convert.ToByte(Parts[1]);
                            PosMovesList.Add(Bm);
                        }
                        Pos.Moves = PosMovesList.ToArray();
                    }
                    PosList.Add(Pos);
                }

            }

            BookPositions = PosList.ToArray();


        }

        public bool LookInBook(long Zobrist, out byte FromSquare, out byte ToSquare)
        {

            Random BookRnd = new(DateTime.Now.Millisecond * DateTime.Now.Second * DateTime.Now.Minute);
            for (int NN = 0; NN < BookPositions.Length; NN++)
            {
                if (BookPositions[NN].Zobrist == Zobrist)
                {
                    if (BookPositions[NN].Moves.Length > 1)
                    {
                        int RandomMove = BookRnd.Next(0, BookPositions[NN].Moves.Length);
                        FromSquare = BookPositions[NN].Moves[RandomMove].From;
                        ToSquare = BookPositions[NN].Moves[RandomMove].To;

                    }
                    else
                    {
                        FromSquare = BookPositions[NN].Moves[0].From;
                        ToSquare = BookPositions[NN].Moves[0].To;
                    }
                    return true;

                }
            }

            FromSquare = 255; ToSquare = 255;
            return false;

        }

    }
}
