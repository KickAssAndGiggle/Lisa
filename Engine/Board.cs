using static Lisa.Globals;
using System.Runtime.CompilerServices;
namespace Lisa
{
    public sealed class Board
    {

        #region Public visible arrays

        // Having these as just public fields probably wouldn't win any "perfect coding style" prize from
        // Microsoft, but this feels like the easiest way to make these arrays available to the Eval and Search
        // code that will need to use them

        public int[] Piece = new int[64];
        public byte[] Color = new byte[64];
        public int[] SquareColor = new int[64];
        public bool[][] SameDiagonal = new bool[64][];
        public bool[][] KnightDestinations = new bool[64][];
        public bool[][] SameRank = new bool[64][];
        public bool[][] SameFile = new bool[64][];

        public int[] WhitePawnSquares = new int[8];
        public int[] BlackPawnSquares = new int[8];
        public int[][] WhitePassedPawnLookUps = new int[64][];
        public int[][] BlackPassedPawnLookUps = new int[64][];

        public int[] WhiteFilePawns = new int[8];
        public int[] BlackFilePawns = new int[8];

        #endregion


        #region Public visible fields

        // I tried using private _backingVariable with a property get for these, but it was significantly
        // slower than public fields. I'm talking 4 seconds slower to analyse 40 million nodes. In most applications,
        // the nano-second difference per access would not justify this approach, but you can't leave performance on the table in
        // a chess engine.

        public int OnMove;

        public byte EnPasantCapSquare;

        public int PieceCount;
        public int WhiteMaterial;
        public int BlackMaterial;

        public bool WhiteHasLightSquaredBishop = false;
        public bool WhiteHasDarkSquaredBishop = false;
        public bool BlackHasLightSquaredBishop = false;
        public bool BlackHasDarkSquaredBishop = false;

        public byte WhiteRookOneSquare = 255;
        public byte WhiteRookTwoSquare = 255;
        public byte BlackRookOneSquare = 255;
        public byte BlackRookTwoSquare = 255;
        public byte WhiteLightBishopSquare = 255;
        public byte WhiteDarkBishopSquare = 255;
        public byte BlackLightBishopSquare = 255;
        public byte BlackDarkBishopSquare = 255;
        public byte WhiteKnightOneSquare = 255;
        public byte WhiteKnightTwoSquare = 255;
        public byte BlackKnightOneSquare = 255;
        public byte BlackKnightTwoSquare = 255;
        public byte BlackKingSquare = 255;
        public byte WhiteKingSquare = 255;
        public byte BlackQueenSquare = 255;
        public byte WhiteQueenSquare = 255;

        public int WhitePawnsOnLightSquares = 0;
        public int WhitePawnsOnDarkSquares = 0;
        public int BlackPawnsOnLightSquares = 0;
        public int BlackPawnsOnDarkSquares = 0;

        public int WhiteEarlyPSTScore = 0;
        public int WhiteLatePSTScore = 0;
        public int BlackEarlyPSTScore = 0;
        public int BlackLatePSTScore = 0;

        public long CurrentZobrist = 0;
        public long PawnOnlyZobrist = 0;

        public int GamePhase;

        #endregion


        private bool _whiteCanQSideCastle;
        private bool _whiteCanKSideCastle;
        private bool _blackCanQSideCastle;
        private bool _blackCanKSideCastle;

        private readonly int[] _seeUndoSquares = new int[32];
        private readonly int[] _seeUndoPieces = new int[32];
        private readonly byte[] _seeUndoColors = new byte[32];
        private int _seeUndoCount;

        private readonly byte[] _mailbox = new byte[120]
        {
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255,  0,  1,  2,  3,  4,  5,  6,  7, 255,
            255,  8,  9, 10, 11, 12, 13, 14, 15, 255,
            255, 16, 17, 18, 19, 20, 21, 22, 23, 255,
            255, 24, 25, 26, 27, 28, 29, 30, 31, 255,
            255, 32, 33, 34, 35, 36, 37, 38, 39, 255,
            255, 40, 41, 42, 43, 44, 45, 46, 47, 255,
            255, 48, 49, 50, 51, 52, 53, 54, 55, 255,
            255, 56, 57, 58, 59, 60, 61, 62, 63, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255
        };

        private readonly byte[] _mailbox64 = new byte[64]
        {
        21, 22, 23, 24, 25, 26, 27, 28,
        31, 32, 33, 34, 35, 36, 37, 38,
        41, 42, 43, 44, 45, 46, 47, 48,
        51, 52, 53, 54, 55, 56, 57, 58,
        61, 62, 63, 64, 65, 66, 67, 68,
        71, 72, 73, 74, 75, 76, 77, 78,
        81, 82, 83, 84, 85, 86, 87, 88,
        91, 92, 93, 94, 95, 96, 97, 98
        };

        private readonly bool[] _slide = new bool[6] { false, false, true, true, true, false }; // is piece a slider (RQB = yes, PKN = no)
        private readonly int[] _offsets = new int[6] { 0, 8, 4, 4, 8, 8 }; // directions each piece moves in (pawns = 0, knights = 8)
        private readonly int[][] _offset = new int[6][];

        private readonly Move[] _moveList = new Move[150];
        private int _moveListTopIndex = -1;
        private bool _lastMoveWasCastle;

        private readonly int[][][] _squaresBetweenDiagonal = new int[64][][];
        private readonly int[][][] _squaresBetweenVertical = new int[64][][];
        private readonly int[][][] _squaresBetweenHorizontal = new int[64][][];

        private readonly int[][] _earlyWhitePST = new int[6][];
        private readonly int[][] _lateWhitePST = new int[6][];
        private readonly int[][] _earlyBlackPST = new int[6][];
        private readonly int[][] _lateBlackPST = new int[6][];

        private readonly Move[] _undoMoves = new Move[40];
        private readonly int[] _undoCapPiece = new int[40];
        private readonly byte[] _undoCapColor = new byte[40];
        private readonly bool[] _undoCapWasEnPasant = new bool[40];
        private readonly bool[] _undoWhiteCouldCastleKSide = new bool[40];
        private readonly bool[] _undoWhiteCouldCastleQSide = new bool[40];
        private readonly bool[] _undoBlackCouldCastleKSide = new bool[40];
        private readonly bool[] _undoBlackCouldCastleQSide = new bool[40];
        private readonly byte[] _undoEnPasantCapSquare = new byte[40];
        private readonly byte[] _undoBlackKingSquare = new byte[40];
        private readonly byte[] _undoWhiteKingSquare = new byte[40];
        private readonly byte[] _undoBlackQueenSquare = new byte[40];
        private readonly byte[] _undoWhiteQueenSquare = new byte[40];
        private readonly bool[] _undoLastMoveWasCastle = new bool[40];
        private readonly bool[] _undoLastMoveWasNull = new bool[40];
        private readonly bool[] _undoLastMoveWhiteHadLightSquaredBishop = new bool[40];
        private readonly bool[] _undoLastMoveWhiteHadDarkSquaredBishop = new bool[40];
        private readonly bool[] _undoLastMoveBlackHadLightSquaredBishop = new bool[40];
        private readonly bool[] _undoLastMoveBlackHadDarkSquaredBishop = new bool[40];
        private readonly int[] _undoWhiteEarlyPSTScore = new int[40];
        private readonly int[] _undoWhiteLatePSTScore = new int[40];
        private readonly int[] _undoBlackEarlyPSTScore = new int[40];
        private readonly int[] _undoBlackLatePSTScore = new int[40];
        private readonly byte[] _undoWhiteLightBishopSquare = new byte[40];
        private readonly byte[] _undoWhiteDarkBishopSquare = new byte[40];
        private readonly byte[] _undoBlackLightBishopSquare = new byte[40];
        private readonly byte[] _undoBlackDarkBishopSquare = new byte[40];
        private readonly int[] _undoGamePhase = new int[40];

        private readonly byte[] _undoWhiteRookOneSquare = new byte[40];
        private readonly byte[] _undoWhiteRookTwoSquare = new byte[40];
        private readonly byte[] _undoBlackRookOneSquare = new byte[40];
        private readonly byte[] _undoBlackRookTwoSquare = new byte[40];

        private readonly byte[] _undoWhiteKnightOneSquare = new byte[40];
        private readonly byte[] _undoWhiteKnightTwoSquare = new byte[40];
        private readonly byte[] _undoBlackKnightOneSquare = new byte[40];
        private readonly byte[] _undoBlackKnightTwoSquare = new byte[40];

        private readonly int[] _undoWhitePawnsOnLightSquares = new int[40];
        private readonly int[] _undoWhitePawnsOnDarkSquares = new int[40];
        private readonly int[] _undoBlackPawnsOnLightSquares = new int[40];
        private readonly int[] _undoBlackPawnsOnDarkSquares = new int[40];


        private int _undoMoveCount = 0;

        private int _halfMoveClock;
        private int _fullMoveClock;

        private readonly long[] _zobristPieceSquares = new long[1200]; // new long[64][];
        private readonly long[] _zobristEPSquares = new long[64];

        // If you change these numbers, the opening book will not work as it
        // is based on our Zobrist keys
        private readonly long _zobristWhiteCanCastleKSide = 6868868868444599999;
        private readonly long _zobristWhiteCanCastleQSide = 5122222222222333;
        private readonly long _zobristBlackCanCastleKSide = 932939939397979797;
        private readonly long _zobristBlackCanCastleQSide = 123456789012319;
        private readonly long _zobristBlackToMove = 9077444444444444;




        public Board()
        {

            _offset[0] = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            _offset[1] = new int[] { -21, -19, -12, -8, 8, 12, 19, 21 };
            _offset[2] = new int[] { -11, -9, 9, 11, 0, 0, 0, 0 };
            _offset[3] = new int[] { -10, -1, 1, 10, 0, 0, 0, 0 };
            _offset[4] = new int[] { -11, -10, -9, -1, 1, 9, 10, 11 };
            _offset[5] = new int[] { -11, -10, -9, -1, 1, 9, 10, 11 };

            SquareColor[0] = LIGHTSQUARE; SquareColor[1] = DARKSQUARE; SquareColor[2] = LIGHTSQUARE; SquareColor[3] = DARKSQUARE;
            SquareColor[4] = LIGHTSQUARE; SquareColor[5] = DARKSQUARE; SquareColor[6] = LIGHTSQUARE; SquareColor[7] = DARKSQUARE;

            SquareColor[8] = DARKSQUARE; SquareColor[9] = LIGHTSQUARE; SquareColor[10] = DARKSQUARE; SquareColor[11] = LIGHTSQUARE;
            SquareColor[12] = DARKSQUARE; SquareColor[13] = LIGHTSQUARE; SquareColor[14] = DARKSQUARE; SquareColor[15] = LIGHTSQUARE;

            SquareColor[16] = LIGHTSQUARE; SquareColor[17] = DARKSQUARE; SquareColor[18] = LIGHTSQUARE; SquareColor[19] = DARKSQUARE;
            SquareColor[20] = LIGHTSQUARE; SquareColor[21] = DARKSQUARE; SquareColor[22] = LIGHTSQUARE; SquareColor[23] = DARKSQUARE;

            SquareColor[24] = DARKSQUARE; SquareColor[25] = LIGHTSQUARE; SquareColor[26] = DARKSQUARE; SquareColor[27] = LIGHTSQUARE;
            SquareColor[28] = DARKSQUARE; SquareColor[29] = LIGHTSQUARE; SquareColor[30] = DARKSQUARE; SquareColor[31] = LIGHTSQUARE;

            SquareColor[32] = LIGHTSQUARE; SquareColor[33] = DARKSQUARE; SquareColor[34] = LIGHTSQUARE; SquareColor[35] = DARKSQUARE;
            SquareColor[36] = LIGHTSQUARE; SquareColor[37] = DARKSQUARE; SquareColor[38] = LIGHTSQUARE; SquareColor[39] = DARKSQUARE;

            SquareColor[40] = DARKSQUARE; SquareColor[41] = LIGHTSQUARE; SquareColor[42] = DARKSQUARE; SquareColor[43] = LIGHTSQUARE;
            SquareColor[44] = DARKSQUARE; SquareColor[45] = LIGHTSQUARE; SquareColor[46] = DARKSQUARE; SquareColor[47] = LIGHTSQUARE;

            SquareColor[48] = LIGHTSQUARE; SquareColor[49] = DARKSQUARE; SquareColor[50] = LIGHTSQUARE; SquareColor[51] = DARKSQUARE;
            SquareColor[52] = LIGHTSQUARE; SquareColor[53] = DARKSQUARE; SquareColor[54] = LIGHTSQUARE; SquareColor[55] = DARKSQUARE;

            SquareColor[56] = DARKSQUARE; SquareColor[57] = LIGHTSQUARE; SquareColor[58] = DARKSQUARE; SquareColor[59] = LIGHTSQUARE;
            SquareColor[60] = DARKSQUARE; SquareColor[61] = LIGHTSQUARE; SquareColor[62] = DARKSQUARE; SquareColor[63] = LIGHTSQUARE;

            InitZobristKeys();
            InitSameDiagonalLookup();
            InitKnightDestinationLookup();
            InitSameFileLookup();
            InitSameRankLookup();
            InitPassedPawnLookup();

            InitPSTs();

        }


        private void InitPSTs()
        {

            for (int nn = 0; nn <= 5; nn++)
            {
                if (nn == PAWN)
                {

                    _earlyWhitePST[nn] = new int[64];
                    Array.Copy(WhitePawnEarlyPST, 0, _earlyWhitePST[nn], 0, 64);

                    _lateWhitePST[nn] = new int[64];
                    Array.Copy(WhitePawnLatePST, 0, _lateWhitePST[nn], 0, 64);

                    _earlyBlackPST[nn] = new int[64];
                    Array.Copy(BlackPawnEarlyPST, 0, _earlyBlackPST[nn], 0, 64);

                    _lateBlackPST[nn] = new int[64];
                    Array.Copy(BlackPawnLatePST, 0, _lateBlackPST[nn], 0, 64);

                }
                else if (nn == KNIGHT)
                {

                    _earlyWhitePST[nn] = new int[64];
                    Array.Copy(WhiteKnightEarlyPST, 0, _earlyWhitePST[nn], 0, 64);

                    _lateWhitePST[nn] = new int[64];
                    Array.Copy(WhiteKnightLatePST, 0, _lateWhitePST[nn], 0, 64);

                    _earlyBlackPST[nn] = new int[64];
                    Array.Copy(BlackKnightEarlyPST, 0, _earlyBlackPST[nn], 0, 64);

                    _lateBlackPST[nn] = new int[64];
                    Array.Copy(BlackKnightLatePST, 0, _lateBlackPST[nn], 0, 64);

                }
                else if (nn == BISHOP)
                {

                    _earlyWhitePST[nn] = new int[64];
                    Array.Copy(WhiteBishopEarlyPST, 0, _earlyWhitePST[nn], 0, 64);

                    _lateWhitePST[nn] = new int[64];
                    Array.Copy(WhiteBishopLatePST, 0, _lateWhitePST[nn], 0, 64);

                    _earlyBlackPST[nn] = new int[64];
                    Array.Copy(BlackBishopEarlyPST, 0, _earlyBlackPST[nn], 0, 64);

                    _lateBlackPST[nn] = new int[64];
                    Array.Copy(BlackBishopLatePST, 0, _lateBlackPST[nn], 0, 64);

                }
                else if (nn == ROOK)
                {

                    _earlyWhitePST[nn] = new int[64];
                    Array.Copy(WhiteRookEarlyPST, 0, _earlyWhitePST[nn], 0, 64);

                    _lateWhitePST[nn] = new int[64];
                    Array.Copy(WhiteRookLatePST, 0, _lateWhitePST[nn], 0, 64);

                    _earlyBlackPST[nn] = new int[64];
                    Array.Copy(BlackRookEarlyPST, 0, _earlyBlackPST[nn], 0, 64);

                    _lateBlackPST[nn] = new int[64];
                    Array.Copy(BlackRookLatePST, 0, _lateBlackPST[nn], 0, 64);

                }
                else if (nn == QUEEN)
                {

                    _earlyWhitePST[nn] = new int[64];
                    Array.Copy(WhiteQueenEarlyPST, 0, _earlyWhitePST[nn], 0, 64);

                    _lateWhitePST[nn] = new int[64];
                    Array.Copy(WhiteQueenLatePST, 0, _lateWhitePST[nn], 0, 64);

                    _earlyBlackPST[nn] = new int[64];
                    Array.Copy(BlackQueenEarlyPST, 0, _earlyBlackPST[nn], 0, 64);

                    _lateBlackPST[nn] = new int[64];
                    Array.Copy(BlackQueenLatePST, 0, _lateBlackPST[nn], 0, 64);

                }
                else if (nn == KING)
                {

                    _earlyWhitePST[nn] = new int[64];
                    Array.Copy(WhiteKingEarlyPST, 0, _earlyWhitePST[nn], 0, 64);

                    _lateWhitePST[nn] = new int[64];
                    Array.Copy(WhiteKingLatePST, 0, _lateWhitePST[nn], 0, 64);

                    _earlyBlackPST[nn] = new int[64];
                    Array.Copy(BlackKingEarlyPST, 0, _earlyBlackPST[nn], 0, 64);

                    _lateBlackPST[nn] = new int[64];
                    Array.Copy(BlackKingLatePST, 0, _lateBlackPST[nn], 0, 64);

                }
            }

        }


        public void RecalcPSTScores()
        {

            WhiteEarlyPSTScore = 0; BlackEarlyPSTScore = 0;
            WhiteLatePSTScore = 0; BlackLatePSTScore = 0;

            for (int nn = 0; nn <= 63; nn++)
            {
                if (Color[nn] == WHITE)
                {
                    WhiteEarlyPSTScore += _earlyWhitePST[Piece[nn]][nn];
                    WhiteLatePSTScore += _lateWhitePST[Piece[nn]][nn];
                }
                else if (Color[nn] == BLACK)
                {
                    BlackEarlyPSTScore += _earlyBlackPST[Piece[nn]][nn];
                    BlackLatePSTScore += _lateBlackPST[Piece[nn]][nn];
                }
            }


        }


        private void InitZobristKeys()
        {

            // Seed must remain constant as the opening book uses our zobrist keys
            Random rnd = new(1762353731);

            for (int sq = 0; sq <= 63; sq++)
            {
                //_zobristPieceSquares[sq] = new long[12];
                for (int cp = 0; cp <= 11; cp++)
                {
                    byte[] randBytes = new byte[8];
                    rnd.NextBytes(randBytes);
                    _zobristPieceSquares[cp * 100 + sq] = BitConverter.ToInt64(randBytes, 0);
                }
            }

            for (int sq = 0; sq <= 63; sq++)
            {
                byte[] randBytes = new byte[8];
                rnd.NextBytes(randBytes);
                _zobristEPSquares[sq] = BitConverter.ToInt64(randBytes, 0);
            }

        }


        private void InitKnightDestinationLookup()
        {

            for (int from = 0; from <= 63; from++)
            {

                bool[] toSquares = new bool[64];

                Piece[from] = KNIGHT;
                Color[from] = WHITE;

                Move[] knightMoves = GenerateNonCaptureMoves(WHITE);
                for (int to = 0; to < knightMoves.Length; to++)
                {
                    toSquares[knightMoves[to].To] = true;
                }

                Piece[from] = -1;
                Color[from] = EMPTY;
                KnightDestinations[from] = toSquares;

            }

        }


        private void InitSameDiagonalLookup()
        {

            for (int from = 0; from <= 63; from++)
            {

                bool[] ToSquares = new bool[64];
                _squaresBetweenDiagonal[from] = new int[64][];

                Piece[from] = BISHOP;
                Color[from] = WHITE;

                Move[] BishMoves = GenerateNonCaptureMoves(WHITE);
                List<int> Betweens = new();
                int currentCompass = 0;
                for (int to = 0; to < BishMoves.Length; to++)
                {
                    int compass = 0;
                    int fromY = from / 8;
                    int fromX = from % 8;
                    int toY = BishMoves[to].To / 8;
                    int toX = BishMoves[to].To % 8;

                    if (fromY < toY)
                    {
                        if (fromX < toX)
                        {
                            compass = 1;
                        }
                        else if (fromX > toX)
                        {
                            compass = 2;
                        }
                    }
                    else if (fromY > toY)
                    {
                        if (fromX < toX)
                        {
                            compass = 3;
                        }
                        else if (fromX > toX)
                        {
                            compass = 4;
                        }
                    }

                    if (currentCompass != 0 && currentCompass != compass)
                    {
                        Betweens.Clear();
                    }
                    currentCompass = compass;

                    ToSquares[BishMoves[to].To] = true;
                    _squaresBetweenDiagonal[from][BishMoves[to].To] = Betweens.ToArray();
                    Betweens.Add(BishMoves[to].To);
                }

                ToSquares[from] = true;
                Piece[from] = -1;
                Color[from] = EMPTY;
                SameDiagonal[from] = ToSquares;

            }

        }


        private void InitSameRankLookup()
        {

            for (int from = 0; from <= 63; from++)
            {
                SameRank[from] = new bool[64];
                _squaresBetweenHorizontal[from] = new int[64][];
                List<int> Betweens = new();
                for (int to = 0; to <= 63; to++)
                {
                    if (from / 8 == to / 8)
                    {
                        SameRank[from][to] = true;
                        if (to > from)
                        {
                            _squaresBetweenHorizontal[from][to] = Betweens.ToArray();
                            Betweens.Add(to);
                        }
                    }
                }
                Betweens = new List<int>();
                for (int to = 63; to >= 0; to--)
                {
                    if (from / 8 == to / 8)
                    {
                        if (to < from)
                        {
                            _squaresBetweenHorizontal[from][to] = Betweens.ToArray();
                            Betweens.Add(to);
                        }
                    }
                }
            }

        }


        private void InitSameFileLookup()
        {

            for (int from = 0; from <= 63; from++)
            {
                SameFile[from] = new bool[64];
                _squaresBetweenVertical[from] = new int[64][];
                List<int> Betweens = new();
                for (int to = 0; to <= 63; to++)
                {
                    if (from % 8 == to % 8)
                    {
                        SameFile[from][to] = true;
                        if (to > from)
                        {
                            _squaresBetweenVertical[from][to] = Betweens.ToArray();
                            Betweens.Add(to);
                        }
                    }
                }
                Betweens = new List<int>();
                for (int to = 63; to >= 0; to--)
                {
                    if (from % 8 == to % 8)
                    {
                        if (to < from)
                        {
                            //Betweens.AddRange(_squaresBetweenVertical[from][to]);
                            _squaresBetweenVertical[from][to] = Betweens.ToArray();
                            Betweens.Add(to);
                        }
                    }
                }

            }

        }


        private void InitPassedPawnLookup()
        {

            for (int nn = 8; nn <= 55; nn++)
            {
                List<int> passSquares = new();
                if (nn % 8 > 0)
                {
                    for (int sq = nn - 9; sq >= 8; sq--)
                    {
                        if (sq % 8 == (nn - 9) % 8)
                        {
                            passSquares.Add(sq);
                        }
                    }
                }
                if (nn % 8 < 7)
                {
                    for (int sq = nn - 7; sq >= 8; sq--)
                    {
                        if (sq % 8 == (nn - 7) % 8)
                        {
                            passSquares.Add(sq);
                        }
                    }
                }
                for (int sq = nn - 8; sq >= 8; sq--)
                {
                    if (sq % 8 == nn % 8)
                    {
                        passSquares.Add(sq);
                    }
                }
                WhitePassedPawnLookUps[nn] = passSquares.ToArray();
                passSquares = new List<int>();
                if (nn % 8 > 0)
                {
                    for (int sq = nn + 7; sq <= 55; sq++)
                    {
                        if (sq % 8 == (nn + 7) % 8)
                        {
                            passSquares.Add(sq);
                        }
                    }
                }
                if (nn % 8 < 7)
                {
                    for (int sq = nn + 9; sq <= 55; sq++)
                    {
                        if (sq % 8 == (nn + 9) % 8)
                        {
                            passSquares.Add(sq);
                        }
                    }
                }
                for (int sq = nn + 8; sq <= 55; sq++)
                {
                    if (sq % 8 == nn % 8)
                    {
                        passSquares.Add(sq);
                    }
                }
                BlackPassedPawnLookUps[nn] = passSquares.ToArray();
            }
        }


        private void SetInitialZobrist()
        {

            for (int sq = 0; sq <= 63; sq++)
            {
                if (Color[sq] == WHITE)
                {
                    CurrentZobrist ^= _zobristPieceSquares[Piece[sq] * 100 + sq];
                    if (Piece[sq] == PAWN)
                    {
                        PawnOnlyZobrist ^= _zobristPieceSquares[PAWN * 100 + sq];
                    }
                }
                else if (Color[sq] == BLACK)
                {
                    CurrentZobrist ^= _zobristPieceSquares[(Piece[sq] + 6) * 100 + sq];
                    if (Piece[sq] == PAWN)
                    {
                        PawnOnlyZobrist ^= _zobristPieceSquares[(PAWN + 6) * 100 + sq];
                    }
                }
            }

            if (EnPasantCapSquare != 255)
            {
                CurrentZobrist ^= _zobristEPSquares[EnPasantCapSquare];
            }

            if (OnMove == BLACK)
            {
                CurrentZobrist ^= _zobristBlackToMove;
            }

            if (_whiteCanKSideCastle)
            {
                CurrentZobrist ^= _zobristWhiteCanCastleKSide;
            }

            if (_whiteCanQSideCastle)
            {
                CurrentZobrist ^= _zobristWhiteCanCastleQSide;
            }

            if (_blackCanKSideCastle)
            {
                CurrentZobrist ^= _zobristBlackCanCastleKSide;
            }

            if (_blackCanQSideCastle)
            {
                CurrentZobrist ^= _zobristBlackCanCastleQSide;
            }

        }


        private void ToggleZobristWhiteCanCastleKSide()
        {
            CurrentZobrist ^= _zobristWhiteCanCastleKSide;
        }


        private void ToggleZobristWhiteCanCastleQSide()
        {
            CurrentZobrist ^= _zobristWhiteCanCastleQSide;
        }


        private void ToggleZobristBlackCanCastleKSide()
        {
            CurrentZobrist ^= _zobristBlackCanCastleKSide;
        }


        private void ToggleZobristBlackCanCastleQSide()
        {
            CurrentZobrist ^= _zobristBlackCanCastleQSide;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToggleZobristPieceOnSquare(int pSquare, int pColor, int pPiece)
        {
            if (pColor == WHITE)
            {
                CurrentZobrist ^= _zobristPieceSquares[pPiece * 100 + pSquare];
                if (pPiece == PAWN)
                {
                    PawnOnlyZobrist ^= _zobristPieceSquares[pPiece * 100 + pSquare];
                }
            }
            else
            {
                CurrentZobrist ^= _zobristPieceSquares[(pPiece + 6) * 100 + pSquare];
                if (pPiece == PAWN)
                {
                    PawnOnlyZobrist ^= _zobristPieceSquares[(pPiece + 6) * 100 + pSquare];
                }
            }
        }


        private void ToggleZobristOnMove()
        {
            CurrentZobrist ^= _zobristBlackToMove;
        }


        private void ToggleZobristEnPasantSquare(int PSquare)
        {
            CurrentZobrist ^= _zobristEPSquares[PSquare];
        }


        public string GenerateFen()
        {

            string Fen = "";
            string CastleString = "";
            if (!_whiteCanKSideCastle && !_whiteCanQSideCastle && !_blackCanKSideCastle && !_blackCanQSideCastle)
            {
                CastleString = "-";
            }
            else
            {
                if (_whiteCanKSideCastle)
                {
                    CastleString += "K";
                }
                if (_whiteCanQSideCastle)
                {
                    CastleString += "Q";
                }
                if (_blackCanKSideCastle)
                {
                    CastleString += "k";
                }
                if (_blackCanQSideCastle)
                {
                    CastleString += "q";
                }
            }

            int EmptyCount = 0;
            for (int nn = 0; nn <= 63; nn++)
            {
                if (nn != 0 && (nn % 8 == 0))
                {
                    if (EmptyCount > 0)
                    {
                        Fen += EmptyCount.ToString() + "/";
                        EmptyCount = 0;
                    }
                    else
                    {
                        Fen += "/";
                    }
                }
                if (Color[nn] == EMPTY)
                {
                    EmptyCount += 1;
                }
                else
                {
                    if (EmptyCount != 0)
                    {
                        Fen += EmptyCount.ToString();
                        EmptyCount = 0;
                    }
                    switch (Piece[nn])
                    {

                        case PAWN:

                            Fen += (Color[nn] == WHITE ? "P" : "p");
                            break;

                        case KNIGHT:

                            Fen += (Color[nn] == WHITE ? "N" : "n");
                            break;

                        case BISHOP:

                            Fen += (Color[nn] == WHITE ? "B" : "b");
                            break;

                        case ROOK:

                            Fen += (Color[nn] == WHITE ? "R" : "r");
                            break;

                        case QUEEN:

                            Fen += (Color[nn] == WHITE ? "Q" : "q");
                            break;

                        case KING:

                            Fen += (Color[nn] == WHITE ? "K" : "k");
                            break;
                    }
                }
            }

            if (EmptyCount != 0)
            {
                Fen += EmptyCount.ToString();
            }

            if (OnMove == WHITE)
            {
                Fen += " w ";
            }
            else
            {
                Fen += " b ";
            }

            Fen += CastleString;

            if (EnPasantCapSquare != 255)
            {
                switch (EnPasantCapSquare)
                {

                    case 16:

                        Fen += " a6 ";
                        break;

                    case 17:

                        Fen += " b6 ";
                        break;

                    case 18:

                        Fen += " c6 ";
                        break;

                    case 19:

                        Fen += " d6 ";
                        break;

                    case 20:

                        Fen += " e6 ";
                        break;

                    case 21:

                        Fen += " f6 ";
                        break;

                    case 22:

                        Fen += " g6 ";
                        break;

                    case 23:

                        Fen += " h6 ";
                        break;

                    case 40:

                        Fen += " a3 ";
                        break;

                    case 41:

                        Fen += " b3 ";
                        break;

                    case 42:

                        Fen += " c3 ";
                        break;

                    case 43:

                        Fen += " d3 ";
                        break;

                    case 44:

                        Fen += " e3 ";
                        break;

                    case 45:

                        Fen += " f3 ";
                        break;

                    case 46:

                        Fen += " g3 ";
                        break;

                    case 47:

                        Fen += " h3 ";
                        break;
                }
            }
            else
            {
                Fen += " - ";
            }

            Fen = Fen.Trim();
            return Fen;

        }


        public void InitialiseFromFEN(string fen)
        {

            for (int nn = 0; nn <= 63; nn++)
            {
                Piece[nn] = -1;
            }

            for (int nn = 0; nn <= 7; nn++)
            {
                WhitePawnSquares[nn] = -1;
                BlackPawnSquares[nn] = -1;
            }

            string[] fenSplits = fen.Split(Convert.ToChar("/"));
            string Info = fenSplits[7].Substring(fenSplits[7].IndexOf(" ")).Trim();
            fenSplits[7] = fenSplits[7].Substring(0, fenSplits[7].IndexOf(" ")).Trim();
            string[] InfoSplits = Info.Split(Convert.ToChar(" "));

            _whiteCanKSideCastle = (Info.Contains('K'));
            _whiteCanQSideCastle = (Info.Contains('Q'));
            _blackCanKSideCastle = (Info.Contains('k'));
            _blackCanQSideCastle = (Info.Contains('q'));

            try
            {
                _halfMoveClock = Convert.ToInt32(InfoSplits[3]);
            }
            catch { }

            try
            {
                _fullMoveClock = Convert.ToInt32(InfoSplits[4]);
            }
            catch { }

            if (Info.StartsWith("w"))
            {
                OnMove = WHITE;
            }
            else
            {
                OnMove = BLACK;
            }

            if (InfoSplits[2] != "-")
            {

                //ep cap is available
                int fileOffset = 0;
                if (InfoSplits[2].StartsWith("b"))
                {
                    fileOffset = 1;
                }
                else if (InfoSplits[2].StartsWith("c"))
                {
                    fileOffset = 2;
                }
                else if (InfoSplits[2].StartsWith("d"))
                {
                    fileOffset = 3;
                }
                else if (InfoSplits[2].StartsWith("e"))
                {
                    fileOffset = 4;
                }
                else if (InfoSplits[2].StartsWith("f"))
                {
                    fileOffset = 5;
                }
                else if (InfoSplits[2].StartsWith("g"))
                {
                    fileOffset = 6;
                }
                else if (InfoSplits[2].StartsWith("h"))
                {
                    fileOffset = 7;
                }

                int AlgRank = Convert.ToInt32(InfoSplits[2].Substring(1));
                int ConvRank = 8 - AlgRank;
                EnPasantCapSquare = (byte)((ConvRank * 8) + fileOffset);

            }
            else
            {
                EnPasantCapSquare = 255;
            }

            PieceCount = 0;

            int indent = 0;
            int line = 0;
            int whitePawnIndex = 0;
            int blackPawnIndex = 0;

            for (int rank = 0; rank < 63; rank += 8)
            {

                do
                {
                    string CharVal = fenSplits[line].Substring(0, 1);
                    if (CharVal == "r")
                    {
                        Piece[rank + indent] = ROOK;
                        Color[rank + indent] = BLACK;
                        PieceCount += 1;
                        BlackMaterial += Material[ROOK];
                        if (BlackRookOneSquare == 255)
                        {
                            BlackRookOneSquare = (byte)(rank + indent);
                        }
                        else
                        {
                            BlackRookTwoSquare = (byte)(rank + indent);
                        }
                        BlackEarlyPSTScore += _earlyBlackPST[ROOK][rank + indent];
                        BlackLatePSTScore += _lateBlackPST[ROOK][rank + indent];
                    }
                    else if (CharVal == "n")
                    {
                        Piece[rank + indent] = KNIGHT;
                        Color[rank + indent] = BLACK;
                        PieceCount += 1;
                        BlackMaterial += Material[KNIGHT];
                        BlackEarlyPSTScore += _earlyBlackPST[KNIGHT][rank + indent];
                        BlackLatePSTScore += _lateBlackPST[KNIGHT][rank + indent];
                        if (BlackKnightOneSquare == 255)
                        {
                            BlackKnightOneSquare = (byte)(rank + indent);
                        }
                        else
                        {
                            BlackKnightTwoSquare = (byte)(rank + indent);
                        }
                    }
                    else if (CharVal == "b")
                    {
                        Piece[rank + indent] = BISHOP;
                        Color[rank + indent] = BLACK;
                        PieceCount += 1;
                        BlackMaterial += Material[BISHOP];
                        if (SquareColor[rank + indent] == DARKSQUARE)
                        {
                            BlackHasDarkSquaredBishop = true;
                            BlackDarkBishopSquare = (byte)(rank + indent);
                        }
                        else
                        {
                            BlackHasLightSquaredBishop = true;
                            BlackLightBishopSquare = (byte)(rank + indent);
                        }
                        BlackEarlyPSTScore += _earlyBlackPST[BISHOP][rank + indent];
                        BlackLatePSTScore += _lateBlackPST[BISHOP][rank + indent];
                    }
                    else if (CharVal == "q")
                    {
                        Piece[rank + indent] = QUEEN;
                        Color[rank + indent] = BLACK;
                        PieceCount += 1;
                        BlackMaterial += Material[QUEEN];
                        BlackQueenSquare = (byte)(rank + indent);
                        BlackEarlyPSTScore += _earlyBlackPST[QUEEN][rank + indent];
                        BlackLatePSTScore += _lateBlackPST[QUEEN][rank + indent];
                    }
                    else if (CharVal == "k")
                    {
                        Piece[rank + indent] = KING;
                        Color[rank + indent] = BLACK;
                        BlackKingSquare = (byte)(rank + indent);
                        PieceCount += 1;
                        BlackEarlyPSTScore += _earlyBlackPST[KING][rank + indent];
                        BlackLatePSTScore += _lateBlackPST[KING][rank + indent];
                    }
                    else if (CharVal == "p")
                    {
                        Piece[rank + indent] = PAWN;
                        Color[rank + indent] = BLACK;
                        PieceCount += 1;
                        BlackMaterial += Material[PAWN];
                        BlackFilePawns[indent] += 1;
                        BlackEarlyPSTScore += _earlyBlackPST[PAWN][rank + indent];
                        BlackLatePSTScore += _lateBlackPST[PAWN][rank + indent];
                        if (SquareColor[rank + indent] == DARKSQUARE)
                        {
                            BlackPawnsOnDarkSquares += 1;
                        }
                        else
                        {
                            BlackPawnsOnLightSquares += 1;
                        }
                        BlackPawnSquares[blackPawnIndex] = rank + indent;
                        blackPawnIndex += 1;
                    }
                    else if (CharVal == "R")
                    {
                        Piece[rank + indent] = ROOK;
                        Color[rank + indent] = WHITE;
                        PieceCount += 1;
                        WhiteMaterial += Material[ROOK];
                        if (WhiteRookOneSquare == 255)
                        {
                            WhiteRookOneSquare = (byte)(rank + indent);
                        }
                        else
                        {
                            WhiteRookTwoSquare = (byte)(rank + indent);
                        }
                        WhiteEarlyPSTScore += _earlyWhitePST[ROOK][rank + indent];
                        WhiteLatePSTScore += _lateWhitePST[ROOK][rank + indent];
                    }
                    else if (CharVal == "N")
                    {
                        Piece[rank + indent] = KNIGHT;
                        Color[rank + indent] = WHITE;
                        PieceCount += 1;
                        WhiteMaterial += Material[KNIGHT];
                        WhiteEarlyPSTScore += _earlyWhitePST[KNIGHT][rank + indent];
                        WhiteLatePSTScore += _lateWhitePST[KNIGHT][rank + indent];
                        if (WhiteKnightOneSquare == 255)
                        {
                            WhiteKnightOneSquare = (byte)(rank + indent);
                        }
                        else
                        {
                            WhiteKnightTwoSquare = (byte)(rank + indent);
                        }
                    }
                    else if (CharVal == "B")
                    {
                        Piece[rank + indent] = BISHOP;
                        Color[rank + indent] = WHITE;
                        PieceCount += 1;
                        WhiteMaterial += Material[BISHOP];
                        if (SquareColor[rank + indent] == DARKSQUARE)
                        {
                            WhiteHasDarkSquaredBishop = true;
                            WhiteDarkBishopSquare = (byte)(rank + indent);
                        }
                        else
                        {
                            WhiteHasLightSquaredBishop = true;
                            WhiteLightBishopSquare = (byte)(rank + indent);
                        }
                        WhiteEarlyPSTScore += _earlyWhitePST[BISHOP][rank + indent];
                        WhiteLatePSTScore += _lateWhitePST[BISHOP][rank + indent];
                    }
                    else if (CharVal == "Q")
                    {
                        Piece[rank + indent] = QUEEN;
                        Color[rank + indent] = WHITE;
                        PieceCount += 1;
                        WhiteMaterial += Material[QUEEN];
                        WhiteQueenSquare = (byte)(rank + indent);
                        WhiteEarlyPSTScore += _earlyWhitePST[QUEEN][rank + indent];
                        WhiteLatePSTScore += _lateWhitePST[QUEEN][rank + indent];
                    }
                    else if (CharVal == "K")
                    {
                        Piece[rank + indent] = KING;
                        Color[rank + indent] = WHITE;
                        WhiteKingSquare = (byte)(rank + indent);
                        PieceCount += 1;
                        WhiteEarlyPSTScore += _earlyWhitePST[KING][rank + indent];
                        WhiteLatePSTScore += _lateWhitePST[KING][rank + indent];
                    }
                    else if (CharVal == "P")
                    {
                        Piece[rank + indent] = PAWN;
                        Color[rank + indent] = WHITE;
                        PieceCount += 1;
                        WhiteMaterial += Material[PAWN];
                        WhiteFilePawns[indent] += 1;
                        WhiteEarlyPSTScore += _earlyWhitePST[PAWN][rank + indent];
                        WhiteLatePSTScore += _lateWhitePST[PAWN][rank + indent];
                        if (SquareColor[rank + indent] == DARKSQUARE)
                        {
                            WhitePawnsOnDarkSquares += 1;
                        }
                        else
                        {
                            WhitePawnsOnLightSquares += 1;
                        }
                        WhitePawnSquares[whitePawnIndex] = rank + indent;
                        whitePawnIndex += 1;
                    }
                    else
                    {
                        indent += Convert.ToInt32(CharVal) - 1;
                    }

                    if (fenSplits[line].Length == 1)
                    {
                        break;
                    }

                    fenSplits[line] = fenSplits[line].Substring(1);
                    indent += 1;

                } while (true);

                indent = 0;
                line += 1;
            }

            SetInitialZobrist();

            GamePhase = 24;
            if (BlackHasLightSquaredBishop)
            {
                GamePhase -= 1;
            }
            if (BlackHasDarkSquaredBishop)
            {
                GamePhase -= 1;
            }
            if (WhiteHasLightSquaredBishop)
            {
                GamePhase -= 1;
            }
            if (WhiteHasDarkSquaredBishop)
            {
                GamePhase -= 1;
            }

            if (WhiteKnightOneSquare != 255)
            {
                GamePhase -= 1;
            }
            if (WhiteKnightTwoSquare != 255)
            {
                GamePhase -= 1;
            }
            if (BlackKnightOneSquare != 255)
            {
                GamePhase -= 1;
            }
            if (BlackKnightTwoSquare != 255)
            {
                GamePhase -= 1;
            }

            if (WhiteRookOneSquare != 255)
            {
                GamePhase -= 2;
            }
            if (WhiteRookTwoSquare != 255)
            {
                GamePhase -= 2;
            }
            if (BlackRookOneSquare != 255)
            {
                GamePhase -= 2;
            }
            if (BlackRookTwoSquare != 255)
            {
                GamePhase -= 2;
            }

            if (WhiteQueenSquare != 255)
            {
                GamePhase -= 4;
            }
            if (BlackQueenSquare != 255)
            {
                GamePhase -= 4;
            }

        }


        private void AddMoveToList(byte from, byte to, bool Cap, byte PromPiece = 0)
        {

            _moveListTopIndex += 1;
            _moveList[_moveListTopIndex].From = from;
            _moveList[_moveListTopIndex].To = to;
            _moveList[_moveListTopIndex].IsCapture = Cap;
            _moveList[_moveListTopIndex].PromotionPiece = PromPiece;

        }

        private void AddMoveToListWithScore(byte from, byte to, bool Cap, byte PromPiece = 0, bool IsEP = false)
        {

            _moveListTopIndex += 1;
            _moveList[_moveListTopIndex].From = from;
            _moveList[_moveListTopIndex].To = to;
            _moveList[_moveListTopIndex].IsCapture = Cap;
            _moveList[_moveListTopIndex].PromotionPiece = PromPiece;
            if (IsEP)
            {
                _moveList[_moveListTopIndex].Score = 4020;
            }
            else
            {
                _moveList[_moveListTopIndex].Score = 4000 + (Material[Piece[to]] - Material[Piece[from]]);
            }
            if (_moveList[_moveListTopIndex].Score == 4000)
            {
                _moveList[_moveListTopIndex].Score += (10 - Piece[from]);
            }

        }


        public bool IsInCheck(int whichSide)
        {

            int kingSquare = whichSide == WHITE ? WhiteKingSquare : BlackKingSquare;
            Move[] oppMoves = GenerateAllMoves(whichSide == WHITE ? BLACK : WHITE, true, kingSquare, false);
            for (int nn = 0; nn < oppMoves.Length; nn++)
            {
                if (oppMoves[nn].To == kingSquare)
                {
                    return true;
                }
            }

            return false;

        }


        public Move[] GenerateNonCaptureMoves(int toMove)
        {

            Span<byte> localColor = new(Color, 0, 64);
            _moveListTopIndex = -1;

            for (byte sq = 0; sq < 64; sq++) //Loop each square
            {

                if (localColor[sq] == toMove) //sq has a piece of right color for toMove
                {

                    int P = Piece[sq];

                    if (P != PAWN) //It's not a pawn
                    {

                        for (int off = 0; off < _offsets[P]; off++)
                        {

                            for (byte nextSq = sq; ;)
                            {

                                nextSq = _mailbox[_mailbox64[nextSq] + _offset[P][off]]; // next square along the ray

                                if (nextSq == 255) //off the board
                                {
                                    break;
                                }
                                if (localColor[nextSq] != EMPTY)
                                {
                                    break;
                                }

                                AddMoveToList(sq, nextSq, false);

                                if (!_slide[P])
                                {
                                    break;
                                }

                            }

                        }

                        if (P == KING)
                        {
                            if (toMove == WHITE)
                            {
                                if (_whiteCanKSideCastle)
                                {
                                    if (localColor[62] == EMPTY && localColor[61] == EMPTY)
                                    {
                                        AddMoveToList(60, 62, false);
                                    }
                                }
                                if (_whiteCanQSideCastle)
                                {
                                    if (localColor[59] == EMPTY && localColor[58] == EMPTY && localColor[57] == EMPTY)
                                    {
                                        AddMoveToList(60, 58, false);
                                    }
                                }
                            }
                            else
                            {
                                if (_blackCanKSideCastle)
                                {
                                    if (localColor[5] == EMPTY && localColor[6] == EMPTY)
                                    {
                                        AddMoveToList(4, 6, false);
                                    }
                                }
                                if (_blackCanQSideCastle)
                                {
                                    if (localColor[3] == EMPTY && localColor[2] == EMPTY && localColor[1] == EMPTY)
                                    {
                                        AddMoveToList(4, 2, false);
                                    }
                                }
                            }
                        }

                    }
                    else
                    {

                        if (toMove == WHITE)
                        {

                            if (localColor[(byte)(sq - 8)] == EMPTY)
                            {
                                if ((byte)(sq - 8) <= 7)
                                {
                                    AddMoveToList(sq, (byte)(sq - 8), false, QUEEN);
                                    AddMoveToList(sq, (byte)(sq - 8), false, ROOK);
                                    AddMoveToList(sq, (byte)(sq - 8), false, BISHOP);
                                    AddMoveToList(sq, (byte)(sq - 8), false, KNIGHT);
                                }
                                else
                                {
                                    AddMoveToList(sq, (byte)(sq - 8), false);
                                }

                                if (sq >= 48 && sq <= 55)
                                {
                                    if (localColor[(byte)(sq - 16)] == EMPTY)
                                    {
                                        AddMoveToList(sq, (byte)(sq - 16), false);
                                    }
                                }
                            }

                        }
                        else
                        {

                            if (localColor[(byte)(sq + 8)] == EMPTY)
                            {
                                if ((byte)(sq + 8) >= 56)
                                {
                                    AddMoveToList(sq, (byte)(sq + 8), false, QUEEN);
                                    AddMoveToList(sq, (byte)(sq + 8), false, ROOK);
                                    AddMoveToList(sq, (byte)(sq + 8), false, BISHOP);
                                    AddMoveToList(sq, (byte)(sq + 8), false, KNIGHT);
                                }
                                else
                                {
                                    AddMoveToList(sq, (byte)(sq + 8), false);
                                }
                                if (sq >= 8 && sq <= 15)
                                {
                                    if (localColor[(byte)(sq + 16)] == EMPTY)
                                    {
                                        AddMoveToList(sq, (byte)(sq + 16), false);
                                    }
                                }
                            }

                        }

                    }

                }

            }

            Span<Move> ret = new(_moveList, 0, _moveListTopIndex + 1);
            return ret.ToArray();


        }


        public Move[] GenerateCapsToSquare(int toMove, int ToSquare)
        {

            Span<byte> localColor = new(Color, 0, 64);
            _moveListTopIndex = -1;

            for (byte sq = 0; sq < 64; sq++) //Loop each square
            {

                if (localColor[sq] == toMove) //sq has a piece of right color for toMove
                {

                    int P = Piece[sq];

                    if (P != PAWN) //It's not a pawn
                    {

                        if (P == ROOK)
                        {
                            if (sq % 8 != ToSquare % 8 && sq / 8 != ToSquare / 8)
                            {
                                //Legality check and rook not on same rank/file as enemy king
                                continue;
                            }
                        }
                        else if (P == BISHOP)
                        {
                            if (!SameDiagonal[sq][ToSquare])
                            {
                                //Legality check and bishop not on same diagonal as enemy king
                                continue;
                            }
                        }
                        else if (P == QUEEN)
                        {
                            if (sq % 8 != ToSquare % 8 && sq / 8 != ToSquare / 8 && !SameDiagonal[sq][ToSquare])
                            {
                                //Legality check and queen not on same rank/file as enemy king and also not on same diagonal
                                continue;
                            }
                        }
                        else if (P == KNIGHT)
                        {
                            if (!KnightDestinations[sq][ToSquare])
                            {
                                //Legality check and knight too far away
                                continue;
                            }
                        }
                        else if (P == KING)
                        {
                            if (sq != ToSquare - 1 && sq != ToSquare + 1 && sq != ToSquare + 8 && sq != ToSquare - 8 &&
                                sq != ToSquare - 7 && sq != ToSquare - 9 && sq != ToSquare + 7 && sq != ToSquare + 9)
                            {
                                continue;
                            }
                        }

                        for (int off = 0; off < _offsets[P]; off++)
                        {

                            for (byte nextSq = sq; ;)
                            {

                                nextSq = _mailbox[_mailbox64[nextSq] + _offset[P][off]]; // next square along the ray

                                if (nextSq == 255) //off the board
                                {
                                    break;
                                }

                                if (localColor[nextSq] != EMPTY)
                                {
                                    if (nextSq == ToSquare && localColor[nextSq] != toMove)
                                    {
                                        if (Piece[nextSq] != KING) //Capturing the king is not actually a move...
                                        {
                                            AddMoveToList(sq, nextSq, true);
                                        }
                                    }
                                    break;
                                }

                                if (!_slide[P])
                                {
                                    break;
                                }

                            }

                        }

                    }
                    else
                    {

                        if (toMove == WHITE)
                        {

                            if ((byte)(sq - 7) == ToSquare && sq % 8 != 7)
                            {
                                if (localColor[(byte)(sq - 7)] != EMPTY && localColor[(byte)(sq - 7)] != toMove)
                                {
                                    if ((byte)(sq - 7) <= 7)
                                    {
                                        AddMoveToList(sq, (byte)(sq - 7), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq - 7), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq - 7), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq - 7), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq - 7), true);
                                    }
                                }
                                else if ((byte)(sq - 7) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }

                            if ((byte)(sq - 9) == ToSquare && sq % 8 != 0)
                            {
                                if (localColor[(byte)(sq - 9)] != EMPTY && localColor[(byte)(sq - 9)] != toMove)
                                {
                                    if ((byte)(sq - 9) <= 7)
                                    {
                                        AddMoveToList(sq, (byte)(sq - 9), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq - 9), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq - 9), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq - 9), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq - 9), true);
                                    }
                                }
                                else if ((byte)(sq - 9) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }


                        }
                        else
                        {

                            if ((byte)(sq + 7) == ToSquare && sq % 8 != 0)
                            {
                                if (localColor[(byte)(sq + 7)] != EMPTY && localColor[(byte)(sq + 7)] != toMove)
                                {
                                    if ((byte)(sq + 7) >= 56)
                                    {
                                        AddMoveToList(sq, (byte)(sq + 7), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq + 7), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq + 7), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq + 7), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq + 7), true);
                                    }
                                }
                                else if ((byte)(sq + 7) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }

                            if ((byte)(sq + 9) == ToSquare && sq % 8 != 7)
                            {
                                if (localColor[(byte)(sq + 9)] != EMPTY && localColor[(byte)(sq + 9)] != toMove)
                                {
                                    if ((byte)(sq + 9) >= 56)
                                    {
                                        AddMoveToList(sq, (byte)(sq + 9), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq + 9), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq + 9), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq + 9), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq + 9), true);
                                    }
                                }
                                else if ((byte)(sq + 9) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }

                        }

                    }

                }

            }

            Span<Move> ret = new(_moveList, 0, _moveListTopIndex + 1);
            return ret.ToArray();

        }


        public Move[] GenerateCaptureMoves(int toMove)
        {

            Span<byte> localColor = new(Color, 0, 64);
            _moveListTopIndex = -1;

            for (byte sq = 0; sq < 64; sq++) //Loop each square
            {

                if (localColor[sq] == toMove) //sq has a piece of right color for toMove
                {

                    int P = Piece[sq];

                    if (P != PAWN) //It's not a pawn
                    {

                        for (int off = 0; off < _offsets[P]; off++)
                        {

                            for (byte nextSq = sq; ;)
                            {

                                nextSq = _mailbox[_mailbox64[nextSq] + _offset[P][off]]; // next square along the ray

                                if (nextSq == 255) //off the board
                                {
                                    break;
                                }

                                if (localColor[nextSq] != EMPTY)
                                {
                                    if (localColor[nextSq] != toMove)
                                    {
                                        if (Piece[nextSq] != KING) //Capturing the king is not actually a move...
                                        {
                                            AddMoveToList(sq, nextSq, true);
                                        }
                                    }
                                    break;
                                }

                                if (!_slide[P])
                                {
                                    break;
                                }

                            }

                        }

                    }
                    else
                    {

                        if (toMove == WHITE)
                        {

                            if (sq % 8 != 7)
                            {
                                if (localColor[(byte)(sq - 7)] != EMPTY && localColor[(byte)(sq - 7)] != toMove)
                                {
                                    if ((byte)(sq - 7) <= 7)
                                    {
                                        AddMoveToList(sq, (byte)(sq - 7), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq - 7), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq - 7), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq - 7), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq - 7), true);
                                    }
                                }
                                else if ((byte)(sq - 7) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }

                            if (sq % 8 != 0)
                            {
                                if (localColor[(byte)(sq - 9)] != EMPTY && localColor[(byte)(sq - 9)] != toMove)
                                {
                                    if ((byte)(sq - 9) <= 7)
                                    {
                                        AddMoveToList(sq, (byte)(sq - 9), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq - 9), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq - 9), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq - 9), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq - 9), true);
                                    }
                                }
                                else if ((byte)(sq - 9) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }


                        }
                        else
                        {

                            if (sq % 8 != 0)
                            {
                                if (localColor[(byte)(sq + 7)] != EMPTY && localColor[(byte)(sq + 7)] != toMove)
                                {
                                    if ((byte)(sq + 7) >= 56)
                                    {
                                        AddMoveToList(sq, (byte)(sq + 7), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq + 7), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq + 7), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq + 7), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq + 7), true);
                                    }
                                }
                                else if ((byte)(sq + 7) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }

                            if (sq % 8 != 7)
                            {
                                if (localColor[(byte)(sq + 9)] != EMPTY && localColor[(byte)(sq + 9)] != toMove)
                                {
                                    if ((byte)(sq + 9) >= 56)
                                    {
                                        AddMoveToList(sq, (byte)(sq + 9), true, QUEEN);
                                        AddMoveToList(sq, (byte)(sq + 9), true, ROOK);
                                        AddMoveToList(sq, (byte)(sq + 9), true, BISHOP);
                                        AddMoveToList(sq, (byte)(sq + 9), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq + 9), true);
                                    }
                                }
                                else if ((byte)(sq + 9) == EnPasantCapSquare)
                                {
                                    AddMoveToList(sq, EnPasantCapSquare, true);
                                }
                            }

                        }

                    }

                }

            }

            Span<Move> ret = new(_moveList, 0, _moveListTopIndex + 1);
            return ret.ToArray();

        }


        public Move[] GenerateCaptureMovesWithScore(int toMove)
        {

            Span<byte> localColor = new(Color, 0, 64);
            _moveListTopIndex = -1;

            for (byte sq = 0; sq < 64; sq++) //Loop each square
            {

                if (localColor[sq] == toMove) //sq has a piece of right color for toMove
                {

                    int P = Piece[sq];

                    if (P != PAWN) //It's not a pawn
                    {

                        for (int off = 0; off < _offsets[P]; off++)
                        {

                            for (byte nextSq = sq; ;)
                            {

                                nextSq = _mailbox[_mailbox64[nextSq] + _offset[P][off]]; // next square along the ray

                                if (nextSq == 255) //off the board
                                {
                                    break;
                                }

                                if (localColor[nextSq] != EMPTY)
                                {
                                    if (localColor[nextSq] != toMove)
                                    {
                                        if (Piece[nextSq] != KING) //Capturing the king is not actually a move...
                                        {
                                            AddMoveToListWithScore(sq, nextSq, true);
                                        }
                                    }
                                    break;
                                }

                                if (!_slide[P])
                                {
                                    break;
                                }

                            }

                        }

                    }
                    else
                    {

                        if (toMove == WHITE)
                        {

                            if (sq % 8 != 7)
                            {
                                if (localColor[(byte)(sq - 7)] != EMPTY && localColor[(byte)(sq - 7)] != toMove)
                                {
                                    if ((byte)(sq - 7) <= 7)
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq - 7), true, QUEEN);
                                        AddMoveToListWithScore(sq, (byte)(sq - 7), true, ROOK);
                                        AddMoveToListWithScore(sq, (byte)(sq - 7), true, BISHOP);
                                        AddMoveToListWithScore(sq, (byte)(sq - 7), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq - 7), true);
                                    }
                                }
                                else if ((byte)(sq - 7) == EnPasantCapSquare)
                                {
                                    AddMoveToListWithScore(sq, EnPasantCapSquare, true, 0, true);
                                }
                            }

                            if (sq % 8 != 0)
                            {
                                if (localColor[(byte)(sq - 9)] != EMPTY && localColor[(byte)(sq - 9)] != toMove)
                                {
                                    if ((byte)(sq - 9) <= 7)
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq - 9), true, QUEEN);
                                        AddMoveToListWithScore(sq, (byte)(sq - 9), true, ROOK);
                                        AddMoveToListWithScore(sq, (byte)(sq - 9), true, BISHOP);
                                        AddMoveToListWithScore(sq, (byte)(sq - 9), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq - 9), true);
                                    }
                                }
                                else if ((byte)(sq - 9) == EnPasantCapSquare)
                                {
                                    AddMoveToListWithScore(sq, EnPasantCapSquare, true, 0, true);
                                }
                            }


                        }
                        else
                        {

                            if (sq % 8 != 0)
                            {
                                if (localColor[(byte)(sq + 7)] != EMPTY && localColor[(byte)(sq + 7)] != toMove)
                                {
                                    if ((byte)(sq + 7) >= 56)
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq + 7), true, QUEEN);
                                        AddMoveToListWithScore(sq, (byte)(sq + 7), true, ROOK);
                                        AddMoveToListWithScore(sq, (byte)(sq + 7), true, BISHOP);
                                        AddMoveToListWithScore(sq, (byte)(sq + 7), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq + 7), true);
                                    }
                                }
                                else if ((byte)(sq + 7) == EnPasantCapSquare)
                                {
                                    AddMoveToListWithScore(sq, EnPasantCapSquare, true, 0, true);
                                }
                            }

                            if (sq % 8 != 7)
                            {
                                if (localColor[(byte)(sq + 9)] != EMPTY && localColor[(byte)(sq + 9)] != toMove)
                                {
                                    if ((byte)(sq + 9) >= 56)
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq + 9), true, QUEEN);
                                        AddMoveToListWithScore(sq, (byte)(sq + 9), true, ROOK);
                                        AddMoveToListWithScore(sq, (byte)(sq + 9), true, BISHOP);
                                        AddMoveToListWithScore(sq, (byte)(sq + 9), true, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToListWithScore(sq, (byte)(sq + 9), true);
                                    }
                                }
                                else if ((byte)(sq + 9) == EnPasantCapSquare)
                                {
                                    AddMoveToListWithScore(sq, EnPasantCapSquare, true, 0, true);
                                }
                            }

                        }

                    }

                }

            }

            Span<Move> ret = new(_moveList, 0, _moveListTopIndex + 1);
            Move[] sorted = ret.ToArray();
            Array.Sort(sorted);
            return sorted;

        }


        public Move[] GeneratePieceMovesWithoutKing(int toMove)
        {

            Span<byte> localColor = new(Color, 0, 64);
            _moveListTopIndex = -1;

            for (byte sq = 0; sq < 64; sq++) //Loop each square
            {

                if (localColor[sq] == toMove) //sq has a piece of right color for toMove
                {

                    int P = Piece[sq];

                    if (P != PAWN && P != KING) //It's not a pawn or king
                    {

                        for (int off = 0; off < _offsets[P]; off++)
                        {

                            for (byte nextSq = sq; ;)
                            {

                                nextSq = _mailbox[_mailbox64[nextSq] + _offset[P][off]]; // next square along the ray

                                if (nextSq == 255) //off the board
                                {
                                    break;
                                }

                                if (localColor[nextSq] != EMPTY)
                                {
                                    if (localColor[nextSq] != toMove)
                                    {
                                        if (Piece[nextSq] != KING) //If we are not doing a check test, capturing the king is not actually a move...
                                        {
                                            AddMoveToList(sq, nextSq, true);
                                        }
                                    }
                                    break;
                                }

                                AddMoveToList(sq, nextSq, false);

                                if (!_slide[P])
                                {
                                    break;
                                }

                            }

                        }

                    }

                }

            }

            Span<Move> ret = new(_moveList, 0, _moveListTopIndex + 1);
            return ret.ToArray();

        }


        public Move[] GenerateAllMoves(int toMove, bool isLegalityCheck = false, int legalityCheckKingSquare = -1, bool legalityCheckLastMoveWasCastle = false)
        {

            Span<byte> localColor = new(Color, 0, 64);
            _moveListTopIndex = -1;

            for (byte sq = 0; sq < 64; sq++) //Loop each square
            {

                if (localColor[sq] == toMove) //sq has a piece of right color for toMove
                {

                    int P = Piece[sq];

                    if (P != PAWN) //It's not a pawn
                    {

                        if (P == ROOK && !legalityCheckLastMoveWasCastle)
                        {
                            if (isLegalityCheck && sq % 8 != legalityCheckKingSquare % 8 && sq / 8 != legalityCheckKingSquare / 8)
                            {
                                //Legality check and rook not on same rank/file as enemy king
                                continue;
                            }
                        }
                        else if (P == BISHOP && !legalityCheckLastMoveWasCastle)
                        {
                            if (isLegalityCheck && !SameDiagonal[sq][legalityCheckKingSquare])
                            {
                                //Legality check and bishop not on same diagonal as enemy king
                                continue;
                            }
                        }
                        else if (P == QUEEN && !legalityCheckLastMoveWasCastle)
                        {
                            if (isLegalityCheck && sq % 8 != legalityCheckKingSquare % 8 && sq / 8 != legalityCheckKingSquare / 8 && !SameDiagonal[sq][legalityCheckKingSquare])
                            {
                                //Legality check and queen not on same rank/file as enemy king and also not on same diagonal
                                continue;
                            }
                        }
                        else if (P == KNIGHT && !legalityCheckLastMoveWasCastle)
                        {
                            if (isLegalityCheck && !KnightDestinations[sq][legalityCheckKingSquare])
                            {
                                //Legality check and knight too far away
                                continue;
                            }
                        }
                        else if (P == KING)
                        {
                            if (isLegalityCheck && sq != legalityCheckKingSquare - 1 && sq != legalityCheckKingSquare + 1 && sq != legalityCheckKingSquare + 8 && sq != legalityCheckKingSquare - 8 &&
                                sq != legalityCheckKingSquare - 7 && sq != legalityCheckKingSquare - 9 && sq != legalityCheckKingSquare + 7 && sq != legalityCheckKingSquare + 9)
                            {
                                continue;
                            }
                        }

                        for (int off = 0; off < _offsets[P]; off++)
                        {

                            for (byte nextSq = sq; ;)
                            {

                                nextSq = _mailbox[_mailbox64[nextSq] + _offset[P][off]]; // next square along the ray

                                if (nextSq == 255) //off the board
                                {
                                    break;
                                }

                                if (localColor[nextSq] != EMPTY)
                                {
                                    if (localColor[nextSq] != toMove)
                                    {
                                        if (Piece[nextSq] != KING || isLegalityCheck) //If we are not doing a check test, capturing the king is not actually a move...
                                        {
                                            AddMoveToList(sq, nextSq, true);
                                        }
                                    }
                                    break;
                                }

                                if (!isLegalityCheck || legalityCheckLastMoveWasCastle)
                                {
                                    AddMoveToList(sq, nextSq, false); //If checking for legality, only need non-captures to check castling through check
                                }

                                if (!_slide[P])
                                {
                                    break;
                                }

                            }

                        }

                        if (P == KING && !isLegalityCheck)
                        {
                            if (toMove == WHITE)
                            {
                                if (_whiteCanKSideCastle)
                                {
                                    if (Color[62] == EMPTY && localColor[61] == EMPTY)
                                    {
                                        AddMoveToList(60, 62, false);
                                    }
                                }
                                if (_whiteCanQSideCastle)
                                {
                                    if (Color[59] == EMPTY && localColor[58] == EMPTY && localColor[57] == EMPTY)
                                    {
                                        AddMoveToList(60, 58, false);
                                    }
                                }
                            }
                            else
                            {
                                if (_blackCanKSideCastle)
                                {
                                    if (Color[5] == EMPTY && localColor[6] == EMPTY)
                                    {
                                        AddMoveToList(4, 6, false);
                                    }
                                }
                                if (_blackCanQSideCastle)
                                {
                                    if (Color[3] == EMPTY && localColor[2] == EMPTY && localColor[1] == EMPTY)
                                    {
                                        AddMoveToList(4, 2, false);
                                    }
                                }
                            }
                        }

                    }
                    else
                    {

                        if (toMove == WHITE)
                        {

                            if (!isLegalityCheck || sq / 8 == legalityCheckKingSquare / 8 + 1)
                            {
                                if (localColor[(byte)(sq - 8)] == EMPTY && !isLegalityCheck)
                                {
                                    if ((byte)(sq - 8) <= 7)
                                    {
                                        AddMoveToList(sq, (byte)(sq - 8), false, QUEEN);
                                        AddMoveToList(sq, (byte)(sq - 8), false, ROOK);
                                        AddMoveToList(sq, (byte)(sq - 8), false, BISHOP);
                                        AddMoveToList(sq, (byte)(sq - 8), false, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq - 8), false);
                                    }

                                    if (sq >= 48 && sq <= 55)
                                    {
                                        if (localColor[(byte)(sq - 16)] == EMPTY)
                                        {
                                            AddMoveToList(sq, (byte)(sq - 16), false);
                                        }
                                    }
                                }

                                if (sq % 8 != 7)
                                {
                                    if (localColor[(byte)(sq - 7)] != EMPTY && localColor[(byte)(sq - 7)] != toMove)
                                    {
                                        if ((byte)(sq - 7) <= 7 && !isLegalityCheck)
                                        {
                                            AddMoveToList(sq, (byte)(sq - 7), true, QUEEN);
                                            AddMoveToList(sq, (byte)(sq - 7), true, ROOK);
                                            AddMoveToList(sq, (byte)(sq - 7), true, BISHOP);
                                            AddMoveToList(sq, (byte)(sq - 7), true, KNIGHT);
                                        }
                                        else
                                        {
                                            AddMoveToList(sq, (byte)(sq - 7), true);
                                        }
                                    }
                                    else if ((byte)(sq - 7) == EnPasantCapSquare && !isLegalityCheck)
                                    {
                                        AddMoveToList(sq, EnPasantCapSquare, true);
                                    }
                                }

                                if (sq % 8 != 0)
                                {
                                    if (localColor[(byte)(sq - 9)] != EMPTY && localColor[(byte)(sq - 9)] != toMove)
                                    {
                                        if ((byte)(sq - 9) <= 7 && !isLegalityCheck)
                                        {
                                            AddMoveToList(sq, (byte)(sq - 9), true, QUEEN);
                                            AddMoveToList(sq, (byte)(sq - 9), true, ROOK);
                                            AddMoveToList(sq, (byte)(sq - 9), true, BISHOP);
                                            AddMoveToList(sq, (byte)(sq - 9), true, KNIGHT);
                                        }
                                        else
                                        {
                                            AddMoveToList(sq, (byte)(sq - 9), true);
                                        }
                                    }
                                    else if ((byte)(sq - 9) == EnPasantCapSquare && !isLegalityCheck)
                                    {
                                        AddMoveToList(sq, EnPasantCapSquare, true);
                                    }
                                }
                            }

                        }
                        else
                        {

                            if (!isLegalityCheck || sq / 8 == legalityCheckKingSquare / 8 - 1)
                            {
                                if (localColor[(byte)(sq + 8)] == EMPTY && !isLegalityCheck)
                                {
                                    if ((byte)(sq + 8) >= 56)
                                    {
                                        AddMoveToList(sq, (byte)(sq + 8), false, QUEEN);
                                        AddMoveToList(sq, (byte)(sq + 8), false, ROOK);
                                        AddMoveToList(sq, (byte)(sq + 8), false, BISHOP);
                                        AddMoveToList(sq, (byte)(sq + 8), false, KNIGHT);
                                    }
                                    else
                                    {
                                        AddMoveToList(sq, (byte)(sq + 8), false);
                                    }
                                    if (sq >= 8 && sq <= 15)
                                    {
                                        if (localColor[(byte)(sq + 16)] == EMPTY)
                                        {
                                            AddMoveToList(sq, (byte)(sq + 16), false);
                                        }
                                    }
                                }

                                if (sq % 8 != 0)
                                {
                                    if (localColor[(byte)(sq + 7)] != EMPTY && localColor[(byte)(sq + 7)] != toMove)
                                    {
                                        if ((byte)(sq + 7) >= 56 && !isLegalityCheck)
                                        {
                                            AddMoveToList(sq, (byte)(sq + 7), true, QUEEN);
                                            AddMoveToList(sq, (byte)(sq + 7), true, ROOK);
                                            AddMoveToList(sq, (byte)(sq + 7), true, BISHOP);
                                            AddMoveToList(sq, (byte)(sq + 7), true, KNIGHT);
                                        }
                                        else
                                        {
                                            AddMoveToList(sq, (byte)(sq + 7), true);
                                        }
                                    }
                                    else if ((byte)(sq + 7) == EnPasantCapSquare && !isLegalityCheck)
                                    {
                                        AddMoveToList(sq, EnPasantCapSquare, true);
                                    }
                                }

                                if (sq % 8 != 7)
                                {
                                    if (localColor[(byte)(sq + 9)] != EMPTY && localColor[(byte)(sq + 9)] != toMove)
                                    {
                                        if ((byte)(sq + 9) >= 56 && !isLegalityCheck)
                                        {
                                            AddMoveToList(sq, (byte)(sq + 9), true, QUEEN);
                                            AddMoveToList(sq, (byte)(sq + 9), true, ROOK);
                                            AddMoveToList(sq, (byte)(sq + 9), true, BISHOP);
                                            AddMoveToList(sq, (byte)(sq + 9), true, KNIGHT);
                                        }
                                        else
                                        {
                                            AddMoveToList(sq, (byte)(sq + 9), true);
                                        }
                                    }
                                    else if ((byte)(sq + 9) == EnPasantCapSquare && !isLegalityCheck)
                                    {
                                        AddMoveToList(sq, EnPasantCapSquare, true);
                                    }
                                }

                            }
                        }

                    }

                }

            }

            Span<Move> ret = new(_moveList, 0, _moveListTopIndex + 1);
            return ret.ToArray();

        }      


        public bool MoveIsLegal(Move theMove, int toMove, bool isKillerValidCheck = false)
        {

            if (!isKillerValidCheck)
            {
                if (toMove == WHITE)
                {
                    if (Color[theMove.From] == EMPTY || Color[theMove.From] == BLACK)
                    {
                        return false;
                    }
                    if (theMove.IsCapture && Color[theMove.To] != BLACK)
                    {
                        if (Piece[theMove.From] != PAWN)
                        {
                            return false;
                        }
                        else
                        {
                            if (!(Piece[theMove.To + 8] == PAWN && Color[theMove.To + 8] == BLACK && theMove.To == EnPasantCapSquare))
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    if (Color[theMove.From] == EMPTY || Color[theMove.From] == WHITE)
                    {
                        return false;
                    }
                    if (theMove.IsCapture && Color[theMove.To] != WHITE)
                    {
                        if (Piece[theMove.From] != PAWN)
                        {
                            return false;
                        }
                        else
                        {
                            if (!(Piece[theMove.To - 8] == PAWN && Color[theMove.To - 8] == WHITE && theMove.To == EnPasantCapSquare))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            else
            {

                if (!theMove.IsCapture && Color[theMove.To] != EMPTY)
                {
                    return false;
                }
                else if (theMove.IsCapture)
                {
                    if (Color[theMove.To] == OnMove)
                    {
                        return false;
                    }
                    else if (Color[theMove.To] == EMPTY && theMove.To != EnPasantCapSquare)
                    {
                        return false;
                    }
                }

                if (Piece[theMove.From] == KING)
                {
                    if (theMove.To == theMove.From - 2)
                    {
                        if (toMove == WHITE && !_whiteCanQSideCastle)
                        {
                            return false;
                        }
                        if (toMove == BLACK && !_blackCanQSideCastle)
                        {
                            return false;
                        }
                        if (Color[theMove.From - 1] != EMPTY)
                        {
                            return false;
                        }
                        if (Color[theMove.From - 2] != EMPTY)
                        {
                            return false;
                        }
                        if (Color[theMove.From - 3] != EMPTY)
                        {
                            return false;
                        }
                    }
                    else if (theMove.To == theMove.From + 2)
                    {
                        if (toMove == WHITE && !_whiteCanKSideCastle)
                        {
                            return false;
                        }
                        if (toMove == BLACK && !_blackCanKSideCastle)
                        {
                            return false;
                        }
                        if (Color[theMove.From + 1] != EMPTY)
                        {
                            return false;
                        }
                        if (Color[theMove.From + 2] != EMPTY)
                        {
                            return false;
                        }
                    }
                }

                if (toMove == WHITE)
                {
                    if (Color[theMove.From] == EMPTY || Color[theMove.From] == BLACK)
                    {
                        return false;
                    }
                    if (theMove.IsCapture && Color[theMove.To] != BLACK)
                    {
                        if (Piece[theMove.From] != PAWN)
                        {
                            return false;
                        }
                        else
                        {
                            if (!(Piece[theMove.To + 8] == PAWN && theMove.To >= 16 && theMove.To <= 23))
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    if (Color[theMove.From] == EMPTY || Color[theMove.From] == WHITE)
                    {
                        return false;
                    }
                    if (theMove.IsCapture && Color[theMove.To] != WHITE)
                    {
                        if (Piece[theMove.From] != PAWN)
                        {
                            return false;
                        }
                        else
                        {
                            if (!(Piece[theMove.To - 8] == PAWN && theMove.To >= 40 && theMove.To <= 47))
                            {
                                return false;
                            }
                        }
                    }
                }

                if ((Piece[theMove.From] == BISHOP || Piece[theMove.From] == QUEEN) && SameDiagonal[theMove.From][theMove.To])
                {
                    for (int nn = 0; nn < _squaresBetweenDiagonal[theMove.From][theMove.To].Length; nn++)
                    {
                        if (Color[_squaresBetweenDiagonal[theMove.From][theMove.To][nn]] != EMPTY)
                        {
                            return false;
                        }
                    }
                }
                else if ((Piece[theMove.From] == ROOK || Piece[theMove.From] == QUEEN) && theMove.From % 8 == theMove.To % 8)
                {
                    for (int nn = 0; nn < _squaresBetweenVertical[theMove.From][theMove.To].Length; nn++)
                    {
                        if (Color[_squaresBetweenVertical[theMove.From][theMove.To][nn]] != EMPTY)
                        {
                            return false;
                        }
                    }
                }
                else if ((Piece[theMove.From] == ROOK || Piece[theMove.From] == QUEEN) && theMove.From / 8 == theMove.To / 8)
                {
                    for (int nn = 0; nn < _squaresBetweenHorizontal[theMove.From][theMove.To].Length; nn++)
                    {
                        if (Color[_squaresBetweenHorizontal[theMove.From][theMove.To][nn]] != EMPTY)
                        {
                            return false;
                        }
                    }
                }
                else if (Piece[theMove.From] == PAWN && theMove.To == theMove.From - 16)
                {
                    if (Color[theMove.From - 8] != EMPTY)
                    {
                        return false;
                    }
                    if (Color[theMove.From] != WHITE)
                    {
                        return false;
                    }
                }
                else if (Piece[theMove.From] == PAWN && theMove.To == theMove.From + 16)
                {
                    if (Color[theMove.From + 8] != EMPTY)
                    {
                        return false;
                    }
                    if (Color[theMove.From] != BLACK)
                    {
                        return false;
                    }
                }
                else if (Piece[theMove.From] == PAWN && theMove.To < theMove.From)
                {
                    if (Color[theMove.From] != WHITE)
                    {
                        return false;
                    }
                    if (theMove.To != theMove.From - 8 && theMove.To != theMove.From - 7 && theMove.To != theMove.From - 9)
                    {
                        return false;
                    }
                }
                else if (Piece[theMove.From] == PAWN && theMove.To > theMove.From)
                {
                    if (Color[theMove.From] != BLACK)
                    {
                        return false;
                    }
                    if (theMove.To != theMove.From + 8 && theMove.To != theMove.From + 7 && theMove.To != theMove.From + 9)
                    {
                        return false;
                    }
                }

            }

            MakeMove(theMove, toMove, false);
            int kingSquare = toMove == WHITE ? WhiteKingSquare : BlackKingSquare;

            if (_lastMoveWasCastle)
            {
                if (theMove.To == 6)
                {
                    //Black just castled K side: check pawns stopping castling
                    if ((Color[11] == WHITE && Piece[11] == PAWN) ||
                        (Color[12] == WHITE && Piece[12] == PAWN) ||
                        (Color[13] == WHITE && Piece[13] == PAWN) ||
                        (Color[14] == WHITE && Piece[14] == PAWN) ||
                        (Color[15] == WHITE && Piece[15] == PAWN))
                    {
                        UnmakeLastMove();
                        return false;
                    }
                }
                if (theMove.To == 2)
                {
                    //Black just castled Q side: check pawns stopping castling
                    if ((Color[9] == WHITE && Piece[9] == PAWN) ||
                        (Color[10] == WHITE && Piece[10] == PAWN) ||
                        (Color[11] == WHITE && Piece[11] == PAWN) ||
                        (Color[12] == WHITE && Piece[12] == PAWN) ||
                        (Color[13] == WHITE && Piece[13] == PAWN))
                    {
                        UnmakeLastMove();
                        return false;
                    }
                }
                if (theMove.To == 62)
                {
                    //White just castled K side: check pawns stopping castling
                    if ((Color[51] == BLACK && Piece[51] == PAWN) ||
                        (Color[52] == BLACK && Piece[52] == PAWN) ||
                        (Color[53] == BLACK && Piece[53] == PAWN) ||
                        (Color[54] == BLACK && Piece[54] == PAWN) ||
                        (Color[55] == BLACK && Piece[55] == PAWN))
                    {
                        UnmakeLastMove();
                        return false;
                    }
                }
                if (theMove.To == 58)
                {
                    //White just castled Q side: check pawns stopping castling
                    if ((Color[49] == BLACK && Piece[49] == PAWN) ||
                        (Color[50] == BLACK && Piece[50] == PAWN) ||
                        (Color[51] == BLACK && Piece[51] == PAWN) ||
                        (Color[52] == BLACK && Piece[52] == PAWN) ||
                        (Color[53] == BLACK && Piece[53] == PAWN))
                    {
                        UnmakeLastMove();
                        return false;
                    }
                }
            }

            Move[] oppMoves = GenerateAllMoves(toMove == WHITE ? BLACK : WHITE, true, kingSquare, _lastMoveWasCastle);
            for (int nn = 0; nn < oppMoves.Length; nn++)
            {
                if (oppMoves[nn].To == kingSquare)
                {
                    UnmakeLastMove();
                    return false;
                }
                if (_lastMoveWasCastle)
                {
                    if (theMove.To == 6)
                    {
                        //move is black k side castle, check its not through check
                        if (oppMoves[nn].To == 4 || oppMoves[nn].To == 5 || oppMoves[nn].To == 6)
                        {
                            UnmakeLastMove();
                            return false;
                        }
                    }
                    else if (theMove.To == 2)
                    {
                        //move is black q side castle, check its not through check
                        if (oppMoves[nn].To == 4 || oppMoves[nn].To == 3 || oppMoves[nn].To == 2)
                        {
                            UnmakeLastMove();
                            return false;
                        }
                    }
                    else if (theMove.To == 62)
                    {
                        //move is white k side castle, check its not through check
                        if (oppMoves[nn].To == 60 || oppMoves[nn].To == 61 || oppMoves[nn].To == 62)
                        {
                            UnmakeLastMove();
                            return false;
                        }
                    }
                    else if (theMove.To == 58)
                    {
                        //move is white q side castle, check its not through check
                        if (oppMoves[nn].To == 60 || oppMoves[nn].To == 59 || oppMoves[nn].To == 58)
                        {
                            UnmakeLastMove();
                            return false;
                        }
                    }
                }

            }

            UnmakeLastMove();
            return true;


        }


        public void MakeMove(Move theMove, int toMove, bool NullMove)
        {

            bool MoveWasCastle = false;

            _undoMoves[_undoMoveCount] = theMove;

            _undoBlackCouldCastleKSide[_undoMoveCount] = _blackCanKSideCastle;
            _undoBlackCouldCastleQSide[_undoMoveCount] = _blackCanQSideCastle;
            _undoWhiteCouldCastleKSide[_undoMoveCount] = _whiteCanKSideCastle;
            _undoWhiteCouldCastleQSide[_undoMoveCount] = _whiteCanQSideCastle;
            _undoEnPasantCapSquare[_undoMoveCount] = EnPasantCapSquare;
            _undoBlackKingSquare[_undoMoveCount] = BlackKingSquare;
            _undoWhiteKingSquare[_undoMoveCount] = WhiteKingSquare;
            _undoBlackQueenSquare[_undoMoveCount] = BlackQueenSquare;
            _undoWhiteQueenSquare[_undoMoveCount] = WhiteQueenSquare;
            _undoLastMoveWasCastle[_undoMoveCount] = _lastMoveWasCastle;
            _undoLastMoveWhiteHadDarkSquaredBishop[_undoMoveCount] = WhiteHasDarkSquaredBishop;
            _undoLastMoveWhiteHadLightSquaredBishop[_undoMoveCount] = WhiteHasLightSquaredBishop;
            _undoLastMoveBlackHadDarkSquaredBishop[_undoMoveCount] = BlackHasDarkSquaredBishop;
            _undoLastMoveBlackHadLightSquaredBishop[_undoMoveCount] = BlackHasLightSquaredBishop;
            _undoWhiteEarlyPSTScore[_undoMoveCount] = WhiteEarlyPSTScore;
            _undoWhiteLatePSTScore[_undoMoveCount] = WhiteLatePSTScore;
            _undoBlackEarlyPSTScore[_undoMoveCount] = BlackEarlyPSTScore;
            _undoBlackLatePSTScore[_undoMoveCount] = BlackLatePSTScore;
            _undoWhiteLightBishopSquare[_undoMoveCount] = WhiteLightBishopSquare;
            _undoWhiteDarkBishopSquare[_undoMoveCount] = WhiteDarkBishopSquare;
            _undoBlackLightBishopSquare[_undoMoveCount] = BlackLightBishopSquare;
            _undoBlackDarkBishopSquare[_undoMoveCount] = BlackDarkBishopSquare;
            _undoWhitePawnsOnDarkSquares[_undoMoveCount] = WhitePawnsOnDarkSquares;
            _undoWhitePawnsOnLightSquares[_undoMoveCount] = WhitePawnsOnLightSquares;
            _undoBlackPawnsOnDarkSquares[_undoMoveCount] = BlackPawnsOnDarkSquares;
            _undoBlackPawnsOnLightSquares[_undoMoveCount] = BlackPawnsOnLightSquares;
            _undoWhiteKnightOneSquare[_undoMoveCount] = WhiteKnightOneSquare;
            _undoWhiteKnightTwoSquare[_undoMoveCount] = WhiteKnightTwoSquare;
            _undoBlackKnightOneSquare[_undoMoveCount] = BlackKnightOneSquare;
            _undoBlackKnightTwoSquare[_undoMoveCount] = BlackKnightTwoSquare;
            _undoWhiteRookOneSquare[_undoMoveCount] = WhiteRookOneSquare;
            _undoWhiteRookTwoSquare[_undoMoveCount] = WhiteRookTwoSquare;
            _undoBlackRookOneSquare[_undoMoveCount] = BlackRookOneSquare;
            _undoBlackRookTwoSquare[_undoMoveCount] = BlackRookTwoSquare;
            _undoGamePhase[_undoMoveCount] = GamePhase;

            _undoLastMoveWasNull[_undoMoveCount] = NullMove;

            if (!NullMove)
            {

                if (Piece[theMove.From] == PAWN)
                {
                    if (Color[theMove.From] == WHITE)
                    {
                        if (SquareColor[theMove.From] == DARKSQUARE)
                        {
                            WhitePawnsOnDarkSquares -= 1;
                        }
                        else
                        {
                            WhitePawnsOnLightSquares -= 1;
                        }
                        if (theMove.PromotionPiece == 0)
                        {
                            if (SquareColor[theMove.To] == DARKSQUARE)
                            {
                                WhitePawnsOnDarkSquares += 1;
                            }
                            else
                            {
                                WhitePawnsOnLightSquares += 1;
                            }
                        }
                        for (int pp = 0; pp <= 7; pp++)
                        {
                            if (WhitePawnSquares[pp] == theMove.From)
                            {
                                if (theMove.To < 8)
                                {
                                    WhitePawnSquares[pp] = -1;
                                }
                                else
                                {
                                    WhitePawnSquares[pp] = theMove.To;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (SquareColor[theMove.From] == DARKSQUARE)
                        {
                            BlackPawnsOnDarkSquares -= 1;
                        }
                        else
                        {
                            BlackPawnsOnLightSquares -= 1;
                        }
                        if (theMove.PromotionPiece == 0)
                        {
                            if (SquareColor[theMove.To] == DARKSQUARE)
                            {
                                BlackPawnsOnDarkSquares += 1;
                            }
                            else
                            {
                                BlackPawnsOnLightSquares += 1;
                            }
                        }
                        for (int pp = 0; pp <= 7; pp++)
                        {
                            if (BlackPawnSquares[pp] == theMove.From)
                            {
                                if (theMove.To > 55)
                                {
                                    BlackPawnSquares[pp] = -1;
                                }
                                else
                                {
                                    BlackPawnSquares[pp] = theMove.To;
                                }
                                break;
                            }
                        }
                    }
                }

                if (Piece[theMove.From] == KING)
                {
                    if (Color[theMove.From] == WHITE)
                    {
                        WhiteKingSquare = theMove.To;
                    }
                    else
                    {
                        BlackKingSquare = theMove.To;
                    }
                }

                if (Piece[theMove.From] == QUEEN)
                {
                    if (Color[theMove.From] == WHITE)
                    {
                        if (theMove.From == WhiteQueenSquare)
                        {
                            WhiteQueenSquare = theMove.To;
                        }
                    }
                    else
                    {
                        if (theMove.From == BlackQueenSquare)
                        {
                            BlackQueenSquare = theMove.To;
                        }
                    }
                }

                if (Piece[theMove.From] == BISHOP)
                {
                    if (Color[theMove.From] == WHITE)
                    {
                        if (theMove.From == WhiteDarkBishopSquare)
                        {
                            WhiteDarkBishopSquare = theMove.To;
                        }
                        else if (theMove.From == WhiteLightBishopSquare)
                        {
                            WhiteLightBishopSquare = theMove.To;
                        }
                    }
                    else
                    {
                        if (theMove.From == BlackDarkBishopSquare)
                        {
                            BlackDarkBishopSquare = theMove.To;
                        }
                        else if (theMove.From == BlackLightBishopSquare)
                        {
                            BlackLightBishopSquare = theMove.To;
                        }
                    }
                }

                if (Piece[theMove.From] == KNIGHT)
                {
                    if (Color[theMove.From] == BLACK)
                    {
                        if (theMove.From == BlackKnightOneSquare)
                        {
                            BlackKnightOneSquare = theMove.To;
                        }
                        else if (theMove.From == BlackKnightTwoSquare)
                        {
                            BlackKnightTwoSquare = theMove.To;
                        }
                    }
                    else
                    {
                        if (theMove.From == WhiteKnightOneSquare)
                        {
                            WhiteKnightOneSquare = theMove.To;
                        }
                        else if (theMove.From == WhiteKnightTwoSquare)
                        {
                            WhiteKnightTwoSquare = theMove.To;
                        }
                    }
                }

                if (Piece[theMove.From] == ROOK)
                {
                    if (theMove.From == 0)
                    {
                        if (_blackCanQSideCastle)
                        {
                            ToggleZobristBlackCanCastleQSide();
                        }
                        _blackCanQSideCastle = false;
                    }
                    else if (theMove.From == 7)
                    {
                        if (_blackCanKSideCastle)
                        {
                            ToggleZobristBlackCanCastleKSide();
                        }
                        _blackCanKSideCastle = false;
                    }
                    else if (theMove.From == 56)
                    {
                        if (_whiteCanQSideCastle)
                        {
                            ToggleZobristWhiteCanCastleQSide();
                        }
                        _whiteCanQSideCastle = false;
                    }
                    else if (theMove.From == 63)
                    {
                        if (_whiteCanKSideCastle)
                        {
                            ToggleZobristWhiteCanCastleKSide();
                        }
                        _whiteCanKSideCastle = false;
                    }
                    if (Color[theMove.From] == BLACK)
                    {
                        if (theMove.From == BlackRookOneSquare)
                        {
                            BlackRookOneSquare = theMove.To;
                        }
                        else if (theMove.From == BlackRookTwoSquare)
                        {
                            BlackRookTwoSquare = theMove.To;
                        }
                    }
                    else
                    {
                        if (theMove.From == WhiteRookOneSquare)
                        {
                            WhiteRookOneSquare = theMove.To;
                        }
                        else if (theMove.From == WhiteRookTwoSquare)
                        {
                            WhiteRookTwoSquare = theMove.To;
                        }
                    }
                }

                if (theMove.To == 0)
                {
                    if (_blackCanQSideCastle)
                    {
                        ToggleZobristBlackCanCastleQSide();
                    }
                    _blackCanQSideCastle = false;
                }
                if (theMove.To == 7)
                {
                    if (_blackCanKSideCastle)
                    {
                        ToggleZobristBlackCanCastleKSide();
                    }
                    _blackCanKSideCastle = false;
                }
                if (theMove.To == 56)
                {
                    if (_whiteCanQSideCastle)
                    {
                        ToggleZobristWhiteCanCastleQSide();
                    }
                    _whiteCanQSideCastle = false;
                }
                if (theMove.To == 63)
                {
                    if (_whiteCanKSideCastle)
                    {
                        ToggleZobristWhiteCanCastleKSide();
                    }
                    _whiteCanKSideCastle = false;
                }

                if (theMove.IsCapture)
                {
                    PieceCount -= 1;
                    if (theMove.To == EnPasantCapSquare)
                    {
                        _undoCapPiece[_undoMoveCount] = PAWN;
                        if (toMove == WHITE)
                        {
                            _undoCapColor[_undoMoveCount] = BLACK;
                            Color[theMove.To + 8] = EMPTY;
                            Piece[theMove.To + 8] = -1;
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (BlackPawnSquares[pp] == theMove.To + 8)
                                {
                                    BlackPawnSquares[pp] = -1;
                                }
                            }
                            if (SquareColor[EnPasantCapSquare + 8] == DARKSQUARE)
                            {
                                BlackPawnsOnDarkSquares -= 1;
                            }
                            else
                            {
                                BlackPawnsOnLightSquares -= 1;
                            }
                            ToggleZobristPieceOnSquare(theMove.To + 8, BLACK, PAWN);
                            BlackMaterial -= Material[PAWN];
                            WhiteFilePawns[theMove.From % 8] -= 1;
                            WhiteFilePawns[theMove.To % 8] += 1;
                            BlackFilePawns[theMove.To % 8] -= 1;
                            BlackEarlyPSTScore -= _earlyBlackPST[PAWN][theMove.To + 8];
                            BlackLatePSTScore -= _lateBlackPST[PAWN][theMove.To + 8];
                            WhiteEarlyPSTScore -= _earlyWhitePST[PAWN][theMove.From];
                            WhiteLatePSTScore -= _lateWhitePST[PAWN][theMove.From];
                            WhiteEarlyPSTScore += _earlyWhitePST[PAWN][theMove.To];
                            WhiteLatePSTScore += _lateWhitePST[PAWN][theMove.To];
                        }
                        else
                        {
                            _undoCapColor[_undoMoveCount] = WHITE;
                            Color[theMove.To - 8] = EMPTY;
                            Piece[theMove.To - 8] = -1;
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (WhitePawnSquares[pp] == theMove.To - 8)
                                {
                                    WhitePawnSquares[pp] = -1;
                                }
                            }
                            if (SquareColor[EnPasantCapSquare - 8] == DARKSQUARE)
                            {
                                WhitePawnsOnDarkSquares -= 1;
                            }
                            else
                            {
                                WhitePawnsOnLightSquares -= 1;
                            }
                            ToggleZobristPieceOnSquare(theMove.To - 8, WHITE, PAWN);
                            WhiteMaterial -= Material[PAWN];
                            BlackFilePawns[theMove.From % 8] -= 1;
                            BlackFilePawns[theMove.To % 8] += 1;
                            WhiteFilePawns[theMove.To % 8] -= 1;
                            WhiteEarlyPSTScore -= _earlyWhitePST[PAWN][theMove.To + 8];
                            WhiteLatePSTScore -= _lateWhitePST[PAWN][theMove.To + 8];
                            BlackEarlyPSTScore -= _earlyBlackPST[PAWN][theMove.From];
                            BlackLatePSTScore -= _lateBlackPST[PAWN][theMove.From];
                            BlackEarlyPSTScore += _earlyBlackPST[PAWN][theMove.To];
                            BlackLatePSTScore += _lateBlackPST[PAWN][theMove.To];
                        }
                        _undoCapWasEnPasant[_undoMoveCount] = true;
                        ToggleZobristPieceOnSquare(theMove.From, Color[theMove.From], Piece[theMove.From]);
                        Color[theMove.To] = Color[theMove.From];
                        Piece[theMove.To] = Piece[theMove.From];
                        Color[theMove.From] = EMPTY;
                        Piece[theMove.From] = -1;
                        ToggleZobristPieceOnSquare(theMove.To, Color[theMove.To], Piece[theMove.To]);
                    }
                    else
                    {
                        _undoCapWasEnPasant[_undoMoveCount] = false;
                        _undoCapPiece[_undoMoveCount] = Piece[theMove.To];
                        _undoCapColor[_undoMoveCount] = Color[theMove.To];

                        if (Piece[theMove.To] == ROOK)
                        {
                            if (Color[theMove.To] == WHITE)
                            {
                                if (theMove.To == WhiteRookOneSquare)
                                {
                                    WhiteRookOneSquare = 255;
                                    GamePhase += 2;
                                }
                                else if (theMove.To == WhiteRookTwoSquare)
                                {
                                    WhiteRookTwoSquare = 255;
                                    GamePhase += 2;
                                }
                            }
                            else
                            {
                                if (theMove.To == BlackRookOneSquare)
                                {
                                    BlackRookOneSquare = 255;
                                    GamePhase += 2;
                                }
                                else if (theMove.To == BlackRookTwoSquare)
                                {
                                    BlackRookTwoSquare = 255;
                                    GamePhase += 2;
                                }
                            }
                        }
                        else if (Piece[theMove.To] == KNIGHT)
                        {
                            if (Color[theMove.To] == WHITE)
                            {
                                if (theMove.To == WhiteKnightOneSquare)
                                {
                                    WhiteKnightOneSquare = 255;
                                    GamePhase += 1;
                                }
                                else if (theMove.To == WhiteKnightTwoSquare)
                                {
                                    WhiteKnightTwoSquare = 255;
                                    GamePhase += 1;
                                }
                            }
                            else
                            {
                                if (theMove.To == BlackKnightOneSquare)
                                {
                                    BlackKnightOneSquare = 255;
                                    GamePhase += 1;
                                }
                                else if (theMove.To == BlackKnightTwoSquare)
                                {
                                    BlackKnightTwoSquare = 255;
                                    GamePhase += 1;
                                }
                            }
                        }
                        else if (Piece[theMove.To] == QUEEN)
                        {
                            if (Color[theMove.To] == WHITE)
                            {
                                if (WhiteQueenSquare == theMove.To)
                                {
                                    WhiteQueenSquare = 255;
                                    GamePhase += 4;
                                }
                            }
                            else
                            {
                                if (BlackQueenSquare == theMove.To)
                                {
                                    BlackQueenSquare = 255;
                                    GamePhase += 4;
                                }
                            }
                        }
                        else if (Piece[theMove.To] == PAWN)
                        {
                            if (Color[theMove.To] == WHITE)
                            {
                                for (int pp = 0; pp <= 7; pp++)
                                {
                                    if (WhitePawnSquares[pp] == theMove.To)
                                    {
                                        WhitePawnSquares[pp] = -1;
                                    }
                                }
                                if (SquareColor[theMove.To] == DARKSQUARE)
                                {
                                    WhitePawnsOnDarkSquares -= 1;
                                }
                                else
                                {
                                    WhitePawnsOnLightSquares -= 1;
                                }
                            }
                            else
                            {
                                for (int pp = 0; pp <= 7; pp++)
                                {
                                    if (BlackPawnSquares[pp] == theMove.To)
                                    {
                                        BlackPawnSquares[pp] = -1;
                                    }
                                }
                                if (SquareColor[theMove.To] == DARKSQUARE)
                                {
                                    BlackPawnsOnDarkSquares -= 1;
                                }
                                else
                                {
                                    BlackPawnsOnLightSquares -= 1;
                                }
                            }
                        }

                        if (toMove == WHITE)
                        {
                            BlackMaterial -= Material[Piece[theMove.To]];
                            BlackEarlyPSTScore -= _earlyBlackPST[Piece[theMove.To]][theMove.To];
                            BlackLatePSTScore -= _lateBlackPST[Piece[theMove.To]][theMove.To];
                            WhiteEarlyPSTScore -= _earlyWhitePST[Piece[theMove.From]][theMove.From];
                            WhiteLatePSTScore -= _lateWhitePST[Piece[theMove.From]][theMove.From];
                            WhiteEarlyPSTScore += _earlyWhitePST[Piece[theMove.From]][theMove.To];
                            WhiteLatePSTScore += _lateWhitePST[Piece[theMove.From]][theMove.To];
                            if (Piece[theMove.From] == PAWN)
                            {
                                WhiteFilePawns[theMove.From % 8] -= 1;
                                WhiteFilePawns[theMove.To % 8] += 1;
                            }
                            if (Piece[theMove.To] == PAWN)
                            {
                                BlackFilePawns[theMove.To % 8] -= 1;
                            }
                            else if (Piece[theMove.To] == BISHOP)
                            {
                                if (theMove.To == BlackDarkBishopSquare)
                                {
                                    BlackHasDarkSquaredBishop = false;
                                    BlackDarkBishopSquare = 255;
                                    GamePhase += 1;

                                }
                                else if (theMove.To == BlackLightBishopSquare)
                                {
                                    BlackHasLightSquaredBishop = false;
                                    BlackLightBishopSquare = 255;
                                    GamePhase += 1;
                                }
                            }
                        }
                        else
                        {
                            WhiteMaterial -= Material[Piece[theMove.To]];
                            WhiteEarlyPSTScore -= _earlyWhitePST[Piece[theMove.To]][theMove.To];
                            WhiteLatePSTScore -= _lateWhitePST[Piece[theMove.To]][theMove.To];
                            BlackEarlyPSTScore -= _earlyBlackPST[Piece[theMove.From]][theMove.From];
                            BlackLatePSTScore -= _lateBlackPST[Piece[theMove.From]][theMove.From];
                            BlackEarlyPSTScore += _earlyBlackPST[Piece[theMove.From]][theMove.To];
                            BlackLatePSTScore += _lateBlackPST[Piece[theMove.From]][theMove.To];
                            if (Piece[theMove.From] == PAWN)
                            {
                                BlackFilePawns[theMove.From % 8] -= 1;
                                BlackFilePawns[theMove.To % 8] += 1;
                            }
                            if (Piece[theMove.To] == PAWN)
                            {
                                WhiteFilePawns[theMove.To % 8] -= 1;
                            }
                            else if (Piece[theMove.To] == BISHOP)
                            {
                                if (theMove.To == WhiteDarkBishopSquare)
                                {
                                    WhiteHasDarkSquaredBishop = false;
                                    WhiteDarkBishopSquare = 255;
                                    GamePhase += 1;
                                }
                                else if (theMove.To == WhiteLightBishopSquare)
                                {
                                    WhiteHasLightSquaredBishop = false;
                                    WhiteLightBishopSquare = 255;
                                    GamePhase += 1;
                                }
                            }
                        }

                        ToggleZobristPieceOnSquare(theMove.To, Color[theMove.To], Piece[theMove.To]);
                        Color[theMove.To] = Color[theMove.From];
                        Piece[theMove.To] = Piece[theMove.From];
                        ToggleZobristPieceOnSquare(theMove.To, Color[theMove.To], Piece[theMove.To]);
                        ToggleZobristPieceOnSquare(theMove.From, Color[theMove.To], Piece[theMove.To]);
                        Color[theMove.From] = EMPTY;
                        Piece[theMove.From] = -1;
                    }

                    if (EnPasantCapSquare != 255)
                    {
                        ToggleZobristEnPasantSquare(EnPasantCapSquare);
                    }
                    EnPasantCapSquare = 255;
                    if (Piece[theMove.To] == KING)
                    {
                        if (Color[theMove.To] == WHITE)
                        {
                            if (_whiteCanKSideCastle)
                            {
                                ToggleZobristWhiteCanCastleKSide();
                            }
                            if (_whiteCanQSideCastle)
                            {
                                ToggleZobristWhiteCanCastleQSide();
                            }
                            _whiteCanKSideCastle = false;
                            _whiteCanQSideCastle = false;
                        }
                        else
                        {
                            if (_blackCanKSideCastle)
                            {
                                ToggleZobristBlackCanCastleKSide();
                            }
                            if (_blackCanQSideCastle)
                            {
                                ToggleZobristBlackCanCastleQSide();
                            }
                            _blackCanKSideCastle = false;
                            _blackCanQSideCastle = false;
                        }
                    }

                }
                else
                {

                    ToggleZobristPieceOnSquare(theMove.From, Color[theMove.From], Piece[theMove.From]);
                    if (Color[theMove.From] == WHITE)
                    {
                        WhiteEarlyPSTScore -= _earlyWhitePST[Piece[theMove.From]][theMove.From];
                        WhiteLatePSTScore -= _lateWhitePST[Piece[theMove.From]][theMove.From];
                        WhiteEarlyPSTScore += _earlyWhitePST[Piece[theMove.From]][theMove.To];
                        WhiteLatePSTScore += _lateWhitePST[Piece[theMove.From]][theMove.To];
                    }
                    else
                    {
                        BlackEarlyPSTScore -= _earlyBlackPST[Piece[theMove.From]][theMove.From];
                        BlackLatePSTScore -= _lateBlackPST[Piece[theMove.From]][theMove.From];
                        BlackEarlyPSTScore += _earlyBlackPST[Piece[theMove.From]][theMove.To];
                        BlackLatePSTScore += _lateBlackPST[Piece[theMove.From]][theMove.To];
                    }
                    Color[theMove.To] = Color[theMove.From];
                    Piece[theMove.To] = Piece[theMove.From];
                    Color[theMove.From] = EMPTY;
                    Piece[theMove.From] = -1;
                    ToggleZobristPieceOnSquare(theMove.To, Color[theMove.To], Piece[theMove.To]);

                    if (Piece[theMove.To] == KING)
                    {
                        if (Color[theMove.To] == WHITE)
                        {
                            if (_whiteCanKSideCastle)
                            {
                                ToggleZobristWhiteCanCastleKSide();
                            }
                            if (_whiteCanQSideCastle)
                            {
                                ToggleZobristWhiteCanCastleQSide();
                            }
                            _whiteCanKSideCastle = false;
                            _whiteCanQSideCastle = false;
                        }
                        else
                        {
                            if (_blackCanKSideCastle)
                            {
                                ToggleZobristBlackCanCastleKSide();
                            }
                            if (_blackCanQSideCastle)
                            {
                                ToggleZobristBlackCanCastleQSide();
                            }
                            _blackCanKSideCastle = false;
                            _blackCanQSideCastle = false;
                        }
                        if (theMove.To == theMove.From + 2)
                        {
                            //Castle Kingside
                            if (theMove.From == 60)
                            {
                                //white;
                                Color[61] = WHITE;
                                Piece[61] = ROOK;
                                Color[63] = EMPTY;
                                Piece[63] = -1;
                                WhiteEarlyPSTScore -= _earlyWhitePST[ROOK][63];
                                WhiteLatePSTScore -= _lateWhitePST[ROOK][63];
                                WhiteEarlyPSTScore += _earlyWhitePST[ROOK][61];
                                WhiteLatePSTScore += _lateWhitePST[ROOK][61];
                                ToggleZobristPieceOnSquare(61, WHITE, ROOK);
                                ToggleZobristPieceOnSquare(63, WHITE, ROOK);
                                if (WhiteRookOneSquare == 63)
                                {
                                    WhiteRookOneSquare = 61;
                                }
                                else if (WhiteRookTwoSquare == 63)
                                {
                                    WhiteRookTwoSquare = 61;
                                }
                            }
                            else
                            {
                                //black;
                                Color[5] = BLACK;
                                Piece[5] = ROOK;
                                Color[7] = EMPTY;
                                Piece[7] = -1;
                                BlackEarlyPSTScore -= _earlyBlackPST[ROOK][7];
                                BlackLatePSTScore -= _lateBlackPST[ROOK][7];
                                BlackEarlyPSTScore += _earlyBlackPST[ROOK][5];
                                BlackLatePSTScore += _lateBlackPST[ROOK][5];
                                ToggleZobristPieceOnSquare(5, BLACK, ROOK);
                                ToggleZobristPieceOnSquare(7, BLACK, ROOK);
                                if (BlackRookOneSquare == 7)
                                {
                                    BlackRookOneSquare = 5;
                                }
                                else if (BlackRookTwoSquare == 7)
                                {
                                    BlackRookTwoSquare = 5;
                                }
                            }
                            MoveWasCastle = true;
                        }
                        else if (theMove.To == theMove.From - 2)
                        {
                            //Castle Queenside
                            if (theMove.From == 60)
                            {
                                //white;
                                Color[59] = WHITE;
                                Piece[59] = ROOK;
                                Color[56] = EMPTY;
                                Piece[56] = -1;
                                WhiteEarlyPSTScore -= _earlyWhitePST[ROOK][56];
                                WhiteLatePSTScore -= _lateWhitePST[ROOK][56];
                                WhiteEarlyPSTScore += _earlyWhitePST[ROOK][59];
                                WhiteLatePSTScore += _lateWhitePST[ROOK][59];
                                ToggleZobristPieceOnSquare(59, WHITE, ROOK);
                                ToggleZobristPieceOnSquare(56, WHITE, ROOK);
                                if (WhiteRookOneSquare == 56)
                                {
                                    WhiteRookOneSquare = 59;
                                }
                                else if (WhiteRookTwoSquare == 56)
                                {
                                    WhiteRookTwoSquare = 59;
                                }
                            }
                            else
                            {
                                //black;
                                Color[3] = BLACK;
                                Piece[3] = ROOK;
                                Color[0] = EMPTY;
                                Piece[0] = -1;
                                BlackEarlyPSTScore -= _earlyBlackPST[ROOK][0];
                                BlackLatePSTScore -= _lateBlackPST[ROOK][0];
                                BlackEarlyPSTScore += _earlyBlackPST[ROOK][3];
                                BlackLatePSTScore += _lateBlackPST[ROOK][3];
                                ToggleZobristPieceOnSquare(3, BLACK, ROOK);
                                ToggleZobristPieceOnSquare(0, BLACK, ROOK);
                                if (BlackRookOneSquare == 0)
                                {
                                    BlackRookOneSquare = 3;
                                }
                                else if (BlackRookTwoSquare == 0)
                                {
                                    BlackRookTwoSquare = 3;
                                }
                            }
                            MoveWasCastle = true;
                        }

                        if (EnPasantCapSquare != 255)
                        {
                            ToggleZobristEnPasantSquare(EnPasantCapSquare);
                        }
                        EnPasantCapSquare = 255;

                    }
                    else if (Piece[theMove.To] == PAWN)
                    {
                        if (EnPasantCapSquare != 255)
                        {
                            ToggleZobristEnPasantSquare(EnPasantCapSquare);
                        }
                        if (theMove.To - 16 == theMove.From)
                        {
                            //white, enpasant move
                            EnPasantCapSquare = (byte)(theMove.To - 8);
                            ToggleZobristEnPasantSquare(EnPasantCapSquare);
                        }
                        else if (theMove.To + 16 == theMove.From)
                        {
                            //black, enpasant move
                            EnPasantCapSquare = (byte)(theMove.To + 8);
                            ToggleZobristEnPasantSquare(EnPasantCapSquare);
                        }
                        else
                        {
                            EnPasantCapSquare = 255;
                        }
                    }
                    else
                    {
                        if (EnPasantCapSquare != 255)
                        {
                            ToggleZobristEnPasantSquare(EnPasantCapSquare);
                        }
                        EnPasantCapSquare = 255;
                    }

                }

                if (theMove.PromotionPiece > 0)
                {
                    if (toMove == WHITE)
                    {
                        WhiteMaterial -= Material[PAWN];
                        WhiteMaterial += Material[theMove.PromotionPiece];
                        if (!theMove.IsCapture)
                        {
                            WhiteFilePawns[theMove.From % 8] -= 1;
                        }
                        else
                        {
                            WhiteFilePawns[theMove.To % 8] -= 1;
                        }
                        WhiteEarlyPSTScore -= _earlyWhitePST[PAWN][theMove.To];
                        WhiteLatePSTScore -= _lateWhitePST[PAWN][theMove.To];
                        WhiteEarlyPSTScore += _earlyWhitePST[theMove.PromotionPiece][theMove.To];
                        WhiteLatePSTScore += _lateWhitePST[theMove.PromotionPiece][theMove.To];
                    }
                    else
                    {
                        BlackMaterial -= Material[PAWN];
                        BlackMaterial += Material[theMove.PromotionPiece];
                        if (!theMove.IsCapture)
                        {
                            BlackFilePawns[theMove.From % 8] -= 1;
                        }
                        else
                        {
                            BlackFilePawns[theMove.To % 8] -= 1;
                        }
                        BlackEarlyPSTScore -= _earlyBlackPST[PAWN][theMove.To];
                        BlackLatePSTScore -= _lateBlackPST[PAWN][theMove.To];
                        BlackEarlyPSTScore += _earlyBlackPST[theMove.PromotionPiece][theMove.To];
                        BlackLatePSTScore += _lateBlackPST[theMove.PromotionPiece][theMove.To];
                    }
                    ToggleZobristPieceOnSquare(theMove.To, Color[theMove.To], PAWN);
                    Piece[theMove.To] = theMove.PromotionPiece;
                    ToggleZobristPieceOnSquare(theMove.To, Color[theMove.To], theMove.PromotionPiece);
                }

            }
            else
            {
                if (EnPasantCapSquare != 255)
                {
                    ToggleZobristEnPasantSquare(EnPasantCapSquare);
                }
                EnPasantCapSquare = 255;
            }

            _undoMoveCount += 1;
            _lastMoveWasCastle = MoveWasCastle;
            OnMove = (OnMove == WHITE ? BLACK : WHITE);
            ToggleZobristOnMove();

        }


        public void UnmakeLastMove()
        {

            _undoMoveCount -= 1;

            if (!_undoLastMoveWasNull[_undoMoveCount])
            {
                Move toUndo = _undoMoves[_undoMoveCount];
                if (_blackCanKSideCastle != _undoBlackCouldCastleKSide[_undoMoveCount])
                {
                    ToggleZobristBlackCanCastleKSide();
                }
                _blackCanKSideCastle = _undoBlackCouldCastleKSide[_undoMoveCount];
                if (_blackCanQSideCastle != _undoBlackCouldCastleQSide[_undoMoveCount])
                {
                    ToggleZobristBlackCanCastleQSide();
                }
                _blackCanQSideCastle = _undoBlackCouldCastleQSide[_undoMoveCount];
                if (_whiteCanKSideCastle != _undoWhiteCouldCastleKSide[_undoMoveCount])
                {
                    ToggleZobristWhiteCanCastleKSide();
                }
                _whiteCanKSideCastle = _undoWhiteCouldCastleKSide[_undoMoveCount];
                if (_whiteCanQSideCastle != _undoWhiteCouldCastleQSide[_undoMoveCount])
                {
                    ToggleZobristWhiteCanCastleQSide();
                }
                _whiteCanQSideCastle = _undoWhiteCouldCastleQSide[_undoMoveCount];

                if (EnPasantCapSquare != 255)
                {
                    ToggleZobristEnPasantSquare(EnPasantCapSquare);
                }
                EnPasantCapSquare = _undoEnPasantCapSquare[_undoMoveCount];
                if (EnPasantCapSquare != 255)
                {
                    ToggleZobristEnPasantSquare(EnPasantCapSquare);
                }

                _lastMoveWasCastle = _undoLastMoveWasCastle[_undoMoveCount];

                WhiteKingSquare = _undoWhiteKingSquare[_undoMoveCount];
                BlackKingSquare = _undoBlackKingSquare[_undoMoveCount];
                WhiteQueenSquare = _undoWhiteQueenSquare[_undoMoveCount];
                BlackQueenSquare = _undoBlackQueenSquare[_undoMoveCount];

                WhiteDarkBishopSquare = _undoWhiteDarkBishopSquare[_undoMoveCount];
                WhiteLightBishopSquare = _undoWhiteLightBishopSquare[_undoMoveCount];
                BlackDarkBishopSquare = _undoBlackDarkBishopSquare[_undoMoveCount];
                BlackLightBishopSquare = _undoBlackLightBishopSquare[_undoMoveCount];

                WhiteRookOneSquare = _undoWhiteRookOneSquare[_undoMoveCount];
                BlackRookOneSquare = _undoBlackRookOneSquare[_undoMoveCount];
                WhiteRookTwoSquare = _undoWhiteRookTwoSquare[_undoMoveCount];
                BlackRookTwoSquare = _undoBlackRookTwoSquare[_undoMoveCount];

                WhiteKnightOneSquare = _undoWhiteKnightOneSquare[_undoMoveCount];
                BlackKnightOneSquare = _undoBlackKnightOneSquare[_undoMoveCount];
                WhiteKnightTwoSquare = _undoWhiteKnightTwoSquare[_undoMoveCount];
                BlackKnightTwoSquare = _undoBlackKnightTwoSquare[_undoMoveCount];

                WhiteHasLightSquaredBishop = _undoLastMoveWhiteHadLightSquaredBishop[_undoMoveCount];
                WhiteHasDarkSquaredBishop = _undoLastMoveWhiteHadDarkSquaredBishop[_undoMoveCount];
                BlackHasDarkSquaredBishop = _undoLastMoveBlackHadDarkSquaredBishop[_undoMoveCount];
                BlackHasLightSquaredBishop = _undoLastMoveBlackHadLightSquaredBishop[_undoMoveCount];

                WhiteEarlyPSTScore = _undoWhiteEarlyPSTScore[_undoMoveCount];
                WhiteLatePSTScore = _undoWhiteLatePSTScore[_undoMoveCount];
                BlackEarlyPSTScore = _undoBlackEarlyPSTScore[_undoMoveCount];
                BlackLatePSTScore = _undoBlackLatePSTScore[_undoMoveCount];

                WhitePawnsOnDarkSquares = _undoWhitePawnsOnDarkSquares[_undoMoveCount];
                WhitePawnsOnLightSquares = _undoWhitePawnsOnLightSquares[_undoMoveCount];
                BlackPawnsOnDarkSquares = _undoBlackPawnsOnDarkSquares[_undoMoveCount];
                BlackPawnsOnLightSquares = _undoBlackPawnsOnLightSquares[_undoMoveCount];

                GamePhase = _undoGamePhase[_undoMoveCount];

                if (toUndo.IsCapture)
                {
                    PieceCount += 1;
                    ToggleZobristPieceOnSquare(toUndo.To, Color[toUndo.To], Piece[toUndo.To]);
                    Color[toUndo.From] = Color[toUndo.To];
                    Piece[toUndo.From] = Piece[toUndo.To];
                    if (Piece[toUndo.From] == PAWN)
                    {
                        if (Color[toUndo.From] == WHITE)
                        {
                            WhiteFilePawns[toUndo.From % 8] += 1;
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (WhitePawnSquares[pp] == toUndo.To)
                                {
                                    WhitePawnSquares[pp] = toUndo.From;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            BlackFilePawns[toUndo.From % 8] += 1;
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (BlackPawnSquares[pp] == toUndo.To)
                                {
                                    BlackPawnSquares[pp] = toUndo.From;
                                    break;
                                }
                            }
                        }
                    }
                    ToggleZobristPieceOnSquare(toUndo.From, Color[toUndo.From], Piece[toUndo.From]);
                    if (_undoCapWasEnPasant[_undoMoveCount])
                    {
                        Color[toUndo.To] = EMPTY;
                        Piece[toUndo.To] = -1;
                        if (_undoCapColor[_undoMoveCount] == WHITE)
                        {
                            Color[toUndo.To - 8] = WHITE;
                            Piece[toUndo.To - 8] = PAWN;
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (WhitePawnSquares[pp] == -1)
                                {
                                    WhitePawnSquares[pp] = toUndo.To - 8;
                                    break;
                                }
                            }
                            ToggleZobristPieceOnSquare(toUndo.To - 8, WHITE, PAWN);
                            WhiteMaterial += Material[PAWN];
                            WhiteFilePawns[toUndo.To % 8] += 1;
                            BlackFilePawns[toUndo.To % 8] -= 1;
                        }
                        else
                        {
                            Color[toUndo.To + 8] = BLACK;
                            Piece[toUndo.To + 8] = PAWN;
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (BlackPawnSquares[pp] == -1)
                                {
                                    BlackPawnSquares[pp] = toUndo.To + 8;
                                    break;
                                }
                            }
                            ToggleZobristPieceOnSquare(toUndo.To + 8, BLACK, PAWN);
                            BlackMaterial += Material[PAWN];
                            BlackFilePawns[toUndo.To % 8] += 1;
                            WhiteFilePawns[toUndo.To % 8] -= 1;
                        }
                    }
                    else
                    {
                        if (Piece[toUndo.From] == PAWN)
                        {
                            if (Color[toUndo.From] == WHITE)
                            {
                                WhiteFilePawns[toUndo.To % 8] -= 1;
                            }
                            else
                            {
                                BlackFilePawns[toUndo.To % 8] -= 1;
                            }
                        }
                        Color[toUndo.To] = _undoCapColor[_undoMoveCount];
                        Piece[toUndo.To] = _undoCapPiece[_undoMoveCount];
                        if (Piece[toUndo.To] == PAWN)
                        {
                            if (Color[toUndo.To] == WHITE)
                            {
                                WhiteFilePawns[toUndo.To % 8] += 1;
                                for (int pp = 0; pp <= 7; pp++)
                                {
                                    if (WhitePawnSquares[pp] == -1)
                                    {
                                        WhitePawnSquares[pp] = toUndo.To;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                BlackFilePawns[toUndo.To % 8] += 1;
                                for (int pp = 0; pp <= 7; pp++)
                                {
                                    if (BlackPawnSquares[pp] == -1)
                                    {
                                        BlackPawnSquares[pp] = toUndo.To;
                                        break;
                                    }
                                }
                            }
                        }
                        ToggleZobristPieceOnSquare(toUndo.To, Color[toUndo.To], Piece[toUndo.To]);
                        if (Color[toUndo.To] == WHITE)
                        {
                            WhiteMaterial += Material[Piece[toUndo.To]];
                        }
                        else
                        {
                            BlackMaterial += Material[Piece[toUndo.To]];
                        }
                    }
                }
                else
                {

                    ToggleZobristPieceOnSquare(toUndo.To, Color[toUndo.To], Piece[toUndo.To]);
                    Color[toUndo.From] = Color[toUndo.To];
                    Piece[toUndo.From] = Piece[toUndo.To];
                    if (Piece[toUndo.From] == PAWN)
                    {
                        if (Color[toUndo.To] == WHITE)
                        {
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (WhitePawnSquares[pp] == toUndo.To)
                                {
                                    WhitePawnSquares[pp] = toUndo.From;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int pp = 0; pp <= 7; pp++)
                            {
                                if (BlackPawnSquares[pp] == toUndo.To)
                                {
                                    BlackPawnSquares[pp] = toUndo.From;
                                    break;
                                }
                            }
                        }
                    }
                    Color[toUndo.To] = EMPTY;
                    Piece[toUndo.To] = -1;
                    ToggleZobristPieceOnSquare(toUndo.From, Color[toUndo.From], Piece[toUndo.From]);

                    if (Piece[toUndo.From] == KING)
                    {

                        if (toUndo.To == toUndo.From + 2)
                        {
                            //Castle kingside
                            if (toUndo.From == 60)
                            {
                                //white;
                                Color[63] = WHITE;
                                Piece[63] = ROOK;
                                Color[61] = EMPTY;
                                Piece[61] = -1;
                                ToggleZobristPieceOnSquare(61, WHITE, ROOK);
                                ToggleZobristPieceOnSquare(63, WHITE, ROOK);
                            }
                            else
                            {
                                //black;
                                Color[7] = BLACK;
                                Piece[7] = ROOK;
                                Color[5] = EMPTY;
                                Piece[5] = -1;
                                ToggleZobristPieceOnSquare(5, BLACK, ROOK);
                                ToggleZobristPieceOnSquare(7, BLACK, ROOK);
                            }

                        }
                        else if (toUndo.To == toUndo.From - 2)
                        {
                            //Castle Queenside
                            if (toUndo.From == 60)
                            {
                                //white;
                                Color[56] = WHITE;
                                Piece[56] = ROOK;
                                Color[59] = EMPTY;
                                Piece[59] = -1;
                                ToggleZobristPieceOnSquare(59, WHITE, ROOK);
                                ToggleZobristPieceOnSquare(56, WHITE, ROOK);
                            }
                            else
                            {
                                //black;
                                Color[0] = BLACK;
                                Piece[0] = ROOK;
                                Color[3] = EMPTY;
                                Piece[3] = -1;
                                ToggleZobristPieceOnSquare(3, BLACK, ROOK);
                                ToggleZobristPieceOnSquare(0, BLACK, ROOK);
                            }
                        }
                    }
                }

                if (toUndo.PromotionPiece > 0)
                {
                    if (Color[toUndo.From] == WHITE)
                    {
                        WhiteMaterial -= Material[Piece[toUndo.From]];
                        WhiteMaterial += Material[PAWN];
                        WhiteFilePawns[toUndo.From % 8] += 1;
                        for (int pp = 0; pp <= 7; pp++)
                        {
                            if (WhitePawnSquares[pp] == -1)
                            {
                                WhitePawnSquares[pp] = toUndo.From;
                                break;
                            }
                        }
                    }
                    else
                    {
                        BlackMaterial -= Material[Piece[toUndo.From]];
                        BlackMaterial += Material[PAWN];
                        BlackFilePawns[toUndo.From % 8] += 1;
                        for (int pp = 0; pp <= 7; pp++)
                        {
                            if (BlackPawnSquares[pp] == -1)
                            {
                                BlackPawnSquares[pp] = toUndo.From;
                                break;
                            }
                        }
                    }
                    ToggleZobristPieceOnSquare(toUndo.From, Color[toUndo.From], Piece[toUndo.From]);
                    Piece[toUndo.From] = PAWN;
                    ToggleZobristPieceOnSquare(toUndo.From, Color[toUndo.From], PAWN);
                }

            }
            else
            {
                EnPasantCapSquare = _undoEnPasantCapSquare[_undoMoveCount];
                if (EnPasantCapSquare != 255)
                {
                    ToggleZobristEnPasantSquare(EnPasantCapSquare);
                }
            }

            OnMove = (OnMove == WHITE ? BLACK : WHITE);
            ToggleZobristOnMove();

        }


        public int See(int toSquare)
        {

            if (Piece[toSquare] == -1)
            {
                //en-pasant capture
                return 1;
            }

            int whiteGains = 0;
            int blackGains = 0;
            int whosMove = Color[toSquare] == WHITE ? BLACK : WHITE;
            int startingSide = whosMove;

            Move[] capsToSquare = GenerateCapsToSquare(whosMove, toSquare);
            if (capsToSquare.Length == 0)
            {
                return 0;
            }

            _seeUndoCount = 0;
            _seeUndoColors[0] = Color[toSquare];
            _seeUndoPieces[0] = Piece[toSquare];
            _seeUndoSquares[0] = toSquare;

            int bestSee = 0;
            bool canStop = true;

            while (capsToSquare.Length > 0)
            {

                int lowestValPieceScore = 5000;
                int lowestCap = -1;

                for (int nn = 0; nn < capsToSquare.Length; nn++)
                {
                    if (Material[Piece[capsToSquare[nn].From]] < lowestValPieceScore)
                    {
                        lowestValPieceScore = Material[Piece[capsToSquare[nn].From]];
                        lowestCap = nn;
                    }
                }

                _seeUndoCount += 1;
                _seeUndoColors[_seeUndoCount] = Color[capsToSquare[lowestCap].From];
                _seeUndoPieces[_seeUndoCount] = Piece[capsToSquare[lowestCap].From];
                _seeUndoSquares[_seeUndoCount] = capsToSquare[lowestCap].From;

                if (Color[toSquare] == BLACK)
                {
                    whiteGains += SeeMaterial[Piece[toSquare]];
                }
                else
                {
                    blackGains += SeeMaterial[Piece[toSquare]];
                }

                Piece[toSquare] = Piece[capsToSquare[lowestCap].From];
                Color[toSquare] = Color[capsToSquare[lowestCap].From];
                Piece[capsToSquare[lowestCap].From] = -1;
                Color[capsToSquare[lowestCap].From] = EMPTY;

                canStop = !canStop;
                if (canStop)
                {
                    if (startingSide == WHITE)
                    {
                        if (whiteGains - blackGains > bestSee)
                        {
                            bestSee = whiteGains - blackGains;
                        }
                    }
                    else
                    {
                        if (blackGains - whiteGains > bestSee)
                        {
                            bestSee = blackGains - whiteGains;
                        }
                    }
                }

                if (whosMove == WHITE)
                {
                    whosMove = BLACK;
                }
                else
                {
                    whosMove = WHITE;
                }

                capsToSquare = GenerateCapsToSquare(whosMove, toSquare);

                if (capsToSquare.Length == 0)
                {
                    if (startingSide == WHITE)
                    {
                        if (whiteGains - blackGains > bestSee)
                        {
                            bestSee = whiteGains - blackGains;
                        }
                    }
                    else
                    {
                        if (blackGains - whiteGains > bestSee)
                        {
                            bestSee = blackGains - whiteGains;
                        }
                    }
                }

            };

            for (int nn = 0; nn <= _seeUndoCount; nn++)
            {
                Piece[_seeUndoSquares[nn]] = _seeUndoPieces[nn];
                Color[_seeUndoSquares[nn]] = _seeUndoColors[nn];
            }

            return bestSee;

        }

        public void ResetUndoMoves()
        {
            _undoMoveCount = 0;
        }


    }


}
