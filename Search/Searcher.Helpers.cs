using static Lisa.Globals;
namespace Lisa
{
    public sealed partial class Searcher
    {

        //Lazy SMP. While the main searcher searches, SEARCH_THREADS - 1 helper searchers search the same root position
        //on their own threads, sharing its transposition table. Only the main searcher chooses the move: the helpers
        //exist to fill the shared table, so the main search finds more of its positions already searched.
        //Each helper has its own board, move ordering (Sorter) and counters.

        private const int HELPER_STACK_BYTES = 16 * 1024 * 1024;

        //Helpers keep going past the main search's maximum depth, up to this, so no core sits idle while the main
        //search finishes its last iteration. Board's undo stack and Sorter's per-ply arrays limit how deep it can be
        private const int HELPER_MAX_DEPTH = 20;

        private Searcher[] _helpers = Array.Empty<Searcher>();
        private readonly List<Thread> _helperThreads = new();
        private Move[] _helperRootMoves = Array.Empty<Move>();


        //A helper: shares the main searcher's transposition table, and never reports to the GUI
        private Searcher(TranspositionTable sharedTable)
        {
            _transTable = sharedTable;
            _theBoard = new Board();
            _secondTimer = new System.Timers.Timer();
        }


        //Starts the helpers on the root position. Every second helper searches one ply deeper than the main search at
        //each step, so the shared table holds deeper results than the main search has reached
        private void StartHelpers(Move[] rootMoves, byte maxDepth)
        {

            if (_helpers.Length != SEARCH_THREADS - 1)
            {
                Searcher[] helpers = new Searcher[SEARCH_THREADS - 1];
                for (int hh = 0; hh < helpers.Length; hh++)
                {
                    helpers[hh] = new Searcher(_transTable);
                }
                _helpers = helpers;
            }

            string rootFen = _theBoard.GenerateFen();
            _helperThreads.Clear();

            for (int hh = 0; hh < _helpers.Length; hh++)
            {
                Searcher helper = _helpers[hh];
                helper.PrepareAsHelper(rootFen, rootMoves, _gameZobrists, _endGame);
                int depthOffset = hh % 2 == 0 ? 1 : 0;

                byte helperMaxDepth = (byte)Math.Max((int)maxDepth, HELPER_MAX_DEPTH);
                Thread thread = new(() => helper.SearchAsHelper(depthOffset, helperMaxDepth), HELPER_STACK_BYTES);
                thread.IsBackground = true;
                //Normal priority: above it, helpers on a busy machine can starve the thread that reads "stop"
                thread.Priority = ThreadPriority.Normal;
                _helperThreads.Add(thread);
                thread.Start();
            }

        }


        //Stops the helpers and waits for them, so nothing touches the shared table once this returns
        private void StopHelpers()
        {
            for (int hh = 0; hh < _helpers.Length; hh++)
            {
                _helpers[hh]._cancel = true;
            }
            foreach (Thread thread in _helperThreads)
            {
                thread.Join();
            }
            _helperThreads.Clear();
        }


        //Nodes searched by each thread, main search first, joined with "/" for the tester output files
        public string NodesLookedAtPerThread()
        {
            string perThread = _stats.NodesLookedAt.ToString();
            Searcher[] helpers = _helpers;
            for (int hh = 0; hh < helpers.Length; hh++)
            {
                perThread += "/" + helpers[hh]._stats.NodesLookedAt;
            }
            return perThread;
        }


        public long NodesLookedAtAllThreads()
        {
            long total = _stats.NodesLookedAt;
            Searcher[] helpers = _helpers;
            for (int hh = 0; hh < helpers.Length; hh++)
            {
                total += helpers[hh]._stats.NodesLookedAt;
            }
            return total;
        }


        private void PrepareAsHelper(string rootFen, Move[] rootMoves, HashSet<long> gameZobrists, bool endGame)
        {

            _theBoard = new Board();
            _theBoard.InitialiseFromFEN(rootFen);
            _theBoard.RecalcPSTScores();

            _helperRootMoves = (Move[])rootMoves.Clone();
            _gameZobrists = gameZobrists;
            _endGame = endGame;
            _cancel = false;
            _stats = new SearchStats();

            _sorter.Reset();
            _sorter.SetRootMoves(_helperRootMoves);

            _ply = 0;
            _inCheckAtPly[0] = _theBoard.IsInCheck(_theBoard.OnMove);
            _pathZobrist[0] = _theBoard.CurrentZobrist;
            _repetitionFloor = 0;

        }


        //The main search's own root search, on this helper's copy of the root moves, with no time limit
        private void SearchAsHelper(int depthOffset, byte maxDepth)
        {
            SearchRoot(_helperRootMoves, (byte)(1 + depthOffset), maxDepth, int.MaxValue,
                new Dictionary<int, Move[]>());
        }

    }
}
