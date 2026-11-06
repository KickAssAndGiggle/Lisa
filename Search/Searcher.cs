using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
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

        //Ply of the current node, and whether the side to move was in check at each ply
        private int _ply = 0;
        private bool[] _inCheckAtPly = new bool[128];

        //Repetition detection: the Zobrist of the position at each ply of the current line, the lowest ply a repetition
        //can be found at (raised by captures, pawn moves and null moves, which no position before them can recur
        //across), and every position from the game itself
        private long[] _pathZobrist = new long[128];
        private int _repetitionFloor = 0;
        private HashSet<long> _gameZobrists = new();

        private System.Timers.Timer _secondTimer;

        private SearchStats _stats = new();

        public ref readonly SearchStats Stats => ref _stats;


        #region Interaction with caller properties

        private Move _bestMove;
        private int _bestScore;
        private Move[]? _bestPV;
        private Move[]? _rootMoves;
        private Dictionary<int, Move[]>? _rootPVs;

        private bool _cancel = false;
        private bool _hasCancelled = false;
        private bool _isSearching = false;

        public string _uciTablebaseHit = "";

        public bool HasCancelled => _hasCancelled;
        public bool IsSearching => _isSearching;
        public Move BestMove => _bestMove;
        public int BestScore => _bestScore;
        public Move[]? BestPV => _bestPV;
        public Move[]? RootMoves => _rootMoves;
        public Dictionary<int, Move[]>? RootPVs => _rootPVs;
        public string UCITablebaseHit => _uciTablebaseHit;

        #endregion

        private bool _endGame = false;

        private int _startingWhiteMaterial;
        private int _startingBlackMaterial;

        private Move _phantomMove = new();

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


        //Captures, pawn moves and promotions: no position from before one can ever occur again
        private bool MoveIsIrreversible(Move move)
        {
            return move.IsCapture || move.PromotionPiece != 0 || _theBoard.Piece[move.From] == PAWN;
        }


        //True if the position at this ply already occurred earlier in the line (same side to move, so every second ply,
        //down to the repetition floor), or, with nothing irreversible in the line, anywhere in the game
        private bool IsRepetition(int positionPly)
        {
            long zobrist = _pathZobrist[positionPly];
            for (int ply = positionPly - 2; ply >= _repetitionFloor; ply -= 2)
            {
                if (_pathZobrist[ply] == zobrist)
                {
                    return true;
                }
            }
            return _repetitionFloor == 0 && _gameZobrists.Contains(zobrist);
        }


        //A repetition scores as a draw, held inside the window as the search is fail-hard
        private static int DrawScoreInWindow(int alpha, int beta)
        {
            if (alpha >= 0)
            {
                return alpha;
            }
            if (beta <= 0)
            {
                return beta;
            }
            return 0;
        }


        #region Minor private helper functions


        private void SecondTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_cancel && _isSearching)
            {
                _stats.SecondsUsed += 1;
                _stats.NodesPerSecond = _stats.NodesLookedAt / _stats.SecondsUsed;
                InfoUpdated?.Invoke();
            }
        }


        private void ResetStatTrackers()
        {

            _isSearching = true;
            _hasCancelled = false;
            _cancel = false;

            _bestScore = 0;
            _stats.SecondsUsed = 0;
            _stats.NodesPerSecond = 0;
            _stats.NodesLookedAt = 0;
            _stats.CurrentSearchDepth = 0;

            //Create a timer for one second so we can periodically update whichever UCI GUI/Command line is controlling
            //us
            _secondTimer.Interval = 1000;
            _secondTimer.Start();

        }


        #endregion

    }
}
