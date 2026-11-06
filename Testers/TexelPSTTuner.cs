using System.Diagnostics;
using System.Globalization;
using static Lisa.Globals;
namespace Lisa
{
    //Texel tuning for the piece-square tables, all entries at once by gradient descent.
    //The PSTs enter the eval linearly: each piece adds its early entry weighted (256 - phase) / 256 and its late entry
    //weighted phase / 256, White's pieces plus and Black's minus at the mirrored square. So one normal pass records,
    //per position, the eval without the PSTs plus where the pieces stand, and after that any set of tables can be
    //scored without a Board or the Scorer.
    //Only White's tables are tuned; MirrorPSTsForBlack builds Black's from them.
    public sealed class TexelPSTTuner
    {

        private const int TABLE_SIZE = 384;                         //6 pieces x 64 squares, per phase
        private const int PARAMETER_COUNT = TABLE_SIZE * 2;         //early tables, then late tables
        private const int MAX_PIECES = 32;
        //every 10th position is held out of the tuning, to check for overfitting
        private const int HOLDOUT_EVERY = 10;
        private const int LOG_EVERY_EPOCHS = 100;
        //centipawns: an eval this close to 0 counts as calling a draw
        private const int DRAW_BAND = 40;
        private const int CHUNKS_PER_THREAD = 8;

        //Pull towards the starting tables, so an entry only moves when the data consistently asks for it.
        //Roughly: a square seen in a couple of thousand positions that all lean the same way moves about 10 centipawns
        private const double PULL_TOWARDS_START = 5e-9;

        private const double LEARNING_RATE_START = 2.0;
        private const double LEARNING_RATE_END = 0.05;
        private const double ADAM_BETA1 = 0.9;
        private const double ADAM_BETA2 = 0.999;
        private const double ADAM_EPSILON = 1e-12;

        private static readonly string[] PIECE_NAMES = { "Pawn", "Knight", "Bishop", "Rook", "Queen", "King" };

        private readonly string _epdFile;
        private readonly string _resultsFile;
        private readonly string _logFile;
        private readonly int _maxPositions;
        private readonly int _threads;
        private readonly int _epochs;
        private readonly ParallelOptions _parallelOptions;
        private readonly Stopwatch _timer = new();

        private string[] _fens = Array.Empty<string>();
        private double[] _results = Array.Empty<double>();

        //Per position: the eval with the PSTs taken out, the phase, and one feature per piece. A feature is the table
        //index (piece * 64 + White's square) plus one, negated for a Black piece
        private double[] _evalWithoutPST = Array.Empty<double>();
        private int[] _phase = Array.Empty<int>();
        private short[] _features = Array.Empty<short>();
        private byte[] _featureCount = Array.Empty<byte>();

        private readonly double[] _startTables = new double[PARAMETER_COUNT];
        private readonly double[] _tables = new double[PARAMETER_COUNT];
        private readonly bool[] _isTunable = new bool[PARAMETER_COUNT];

        private double _k;

        public TexelPSTTuner(string epdFile, string resultsFile, int maxPositions, int threads, int epochs)
        {
            _epdFile = epdFile;
            _resultsFile = resultsFile;
            _logFile = resultsFile + ".log.txt";
            _maxPositions = maxPositions;
            _threads = threads > 0 ? threads : Environment.ProcessorCount;
            _epochs = epochs;
            _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _threads };
        }


        public void Tune()
        {

            _timer.Start();
            File.WriteAllText(_logFile, "");

            if (!LoadPositions())
            {
                return;
            }

            ReadStartTables();
            ExtractFeatures();

            double[] startEvals = ModelEvals(_tables);
            _k = FitK(startEvals);
            double startError = Error(startEvals, false, false);
            Log("K fitted at " + _k.ToString("F4", CultureInfo.InvariantCulture) + ", starting error " +
                FormatError(startError)
                + " (tuned positions " + FormatError(Error(startEvals, true, false)) + ", held out " +
                    FormatError(Error(startEvals, false, true)) + ")");
            int startCorrect = CountCalledCorrectly(startEvals, false, out int allConsidered);
            int startHeldOutCorrect = CountCalledCorrectly(startEvals, true, out int heldOutConsidered);
            Log("Called correctly (draw within " + DRAW_BAND + "): " + CalledCorrectlyText(startCorrect, allConsidered)
                + ", held out " + CalledCorrectlyText(startHeldOutCorrect, heldOutConsidered));

            RunAdam();

            for (int nn = 0; nn < PARAMETER_COUNT; nn++)
            {
                _tables[nn] = Math.Round(_tables[nn]);
            }

            double[] finalEvals = ModelEvals(_tables);
            double modelError = Error(finalEvals, false, false);
            Log("Rounded tables, model error " + FormatError(modelError)
                + " (tuned positions " + FormatError(Error(finalEvals, true, false)) + ", held out " +
                    FormatError(Error(finalEvals, false, true)) + ")");
            int finalCorrect = CountCalledCorrectly(finalEvals, false, out _);
            int finalHeldOutCorrect = CountCalledCorrectly(finalEvals, true, out _);
            string[] calledCorrectlyLines =
            {
                "Called correctly (draw within " + DRAW_BAND + "): " +
                    CalledCorrectlyChangeText(startCorrect, finalCorrect, allConsidered),
                "Called correctly, held out: " + CalledCorrectlyChangeText(startHeldOutCorrect, finalHeldOutCorrect,
                    heldOutConsidered)
            };
            Log(calledCorrectlyLines[0]);
            Log(calledCorrectlyLines[1]);

            double realError = RealErrorWithTables();
            Log("Real eval with the new tables, error " + FormatError(realError) +
                " (should match the model error above)");

            WriteResults(startError, modelError, realError, calledCorrectlyLines);
            Log("Finished.");

        }


        //Full-batch Adam: every epoch computes the exact gradient over all tuned positions, adds the pull towards the
        //starting tables, and moves every tunable entry. The step size falls linearly over the run so the entries
        //settle
        private void RunAdam()
        {

            double[] firstMoment = new double[PARAMETER_COUNT];
            double[] secondMoment = new double[PARAMETER_COUNT];

            for (int epoch = 1; epoch <= _epochs; epoch++)
            {

                double[] gradient = Gradient(_tables, out double tunedError);

                double learningRate = LEARNING_RATE_START +
                    (LEARNING_RATE_END - LEARNING_RATE_START) * (epoch - 1) / Math.Max(1, _epochs - 1);
                double firstCorrection = 1.0 - Math.Pow(ADAM_BETA1, epoch);
                double secondCorrection = 1.0 - Math.Pow(ADAM_BETA2, epoch);

                for (int nn = 0; nn < PARAMETER_COUNT; nn++)
                {
                    if (!_isTunable[nn])
                    {
                        continue;
                    }

                    double g = gradient[nn] + 2.0 * PULL_TOWARDS_START * (_tables[nn] - _startTables[nn]);
                    firstMoment[nn] = ADAM_BETA1 * firstMoment[nn] + (1.0 - ADAM_BETA1) * g;
                    secondMoment[nn] = ADAM_BETA2 * secondMoment[nn] + (1.0 - ADAM_BETA2) * g * g;
                    _tables[nn] -= learningRate * (firstMoment[nn] / firstCorrection)
                        / (Math.Sqrt(secondMoment[nn] / secondCorrection) + ADAM_EPSILON);
                }

                if (epoch % LOG_EVERY_EPOCHS == 0 || epoch == _epochs)
                {
                    double[] evals = ModelEvals(_tables);
                    Log("Epoch " + epoch + ": tuned positions " + FormatError(tunedError) + ", held out " +
                        FormatError(Error(evals, false, true)));
                    WriteResults(double.NaN, double.NaN, double.NaN, Array.Empty<string>());
                }
            }

        }


        //Gradient of the mean squared error over the tuned positions with respect to every table entry. For one
        //position: d(error)/d(eval) = -2 (result - expected) * expected * (1 - expected) * c, where expected is the
        //sigmoid of c * eval and c = K * ln(10) / 400. d(eval)/d(entry) is +/- the entry's phase weight. Each chunk
        //sums into its own array and the chunks are added in a fixed order, so every run gives identical numbers
        private double[] Gradient(double[] tables, out double tunedError)
        {

            int count = _fens.Length;
            int chunkCount = _threads * CHUNKS_PER_THREAD;
            double[][] chunkGradients = new double[chunkCount][];
            double[] chunkErrors = new double[chunkCount];
            int[] chunkPositions = new int[chunkCount];
            double c = _k * Math.Log(10.0) / 400.0;

            Parallel.For(0, chunkCount, _parallelOptions, chunk =>
            {
                double[] gradient = new double[PARAMETER_COUNT];
                double errorSum = 0;
                int positions = 0;

                int start = (int)((long)chunk * count / chunkCount);
                int end = (int)((long)(chunk + 1) * count / chunkCount);

                for (int i = start; i < end; i++)
                {
                    if (IsHeldOut(i))
                    {
                        continue;
                    }

                    double eval = ModelEval(tables, i);
                    double expected = 1.0 / (1.0 + Math.Exp(-c * eval));
                    double difference = _results[i] - expected;
                    errorSum += difference * difference;
                    positions += 1;

                    double slope = -2.0 * difference * expected * (1.0 - expected) * c;
                    double earlyWeight = slope * (256 - _phase[i]) / 256.0;
                    double lateWeight = slope * _phase[i] / 256.0;

                    int featureStart = i * MAX_PIECES;
                    for (int f = 0; f < _featureCount[i]; f++)
                    {
                        int feature = _features[featureStart + f];
                        if (feature > 0)
                        {
                            gradient[feature - 1] += earlyWeight;
                            gradient[TABLE_SIZE + feature - 1] += lateWeight;
                        }
                        else
                        {
                            gradient[-feature - 1] -= earlyWeight;
                            gradient[TABLE_SIZE - feature - 1] -= lateWeight;
                        }
                    }
                }

                chunkGradients[chunk] = gradient;
                chunkErrors[chunk] = errorSum;
                chunkPositions[chunk] = positions;
            });

            double[] total = new double[PARAMETER_COUNT];
            double totalError = 0;
            long totalPositions = 0;
            for (int chunk = 0; chunk < chunkCount; chunk++)
            {
                for (int nn = 0; nn < PARAMETER_COUNT; nn++)
                {
                    total[nn] += chunkGradients[chunk][nn];
                }
                totalError += chunkErrors[chunk];
                totalPositions += chunkPositions[chunk];
            }

            for (int nn = 0; nn < PARAMETER_COUNT; nn++)
            {
                total[nn] /= totalPositions;
            }
            tunedError = totalError / totalPositions;
            return total;

        }


        private double ModelEval(double[] tables, int position)
        {

            double early = 0;
            double late = 0;
            int featureStart = position * MAX_PIECES;
            for (int f = 0; f < _featureCount[position]; f++)
            {
                int feature = _features[featureStart + f];
                if (feature > 0)
                {
                    early += tables[feature - 1];
                    late += tables[TABLE_SIZE + feature - 1];
                }
                else
                {
                    early -= tables[-feature - 1];
                    late -= tables[TABLE_SIZE - feature - 1];
                }
            }
            return _evalWithoutPST[position] + (early * (256 - _phase[position]) + late * _phase[position]) / 256.0;

        }


        private double[] ModelEvals(double[] tables)
        {
            double[] evals = new double[_fens.Length];
            Parallel.For(0, _fens.Length, _parallelOptions, i =>
            {
                evals[i] = ModelEval(tables, i);
            });
            return evals;
        }


        private bool IsHeldOut(int position)
        {
            return position % HOLDOUT_EVERY == HOLDOUT_EVERY - 1;
        }


        //Mean of (result - expected)^2 over all positions, or only the tuned ones, or only the held-out ones
        private double Error(double[] evals, bool tunedOnly, bool heldOutOnly)
        {

            double scale = -_k * Math.Log(10.0) / 400.0;
            double total = 0;
            long positions = 0;

            for (int i = 0; i < evals.Length; i++)
            {
                bool heldOut = IsHeldOut(i);
                if ((tunedOnly && heldOut) || (heldOutOnly && !heldOut))
                {
                    continue;
                }
                double expected = 1.0 / (1.0 + Math.Exp(scale * evals[i]));
                double difference = _results[i] - expected;
                total += difference * difference;
                positions += 1;
            }

            return total / positions;

        }


        //Golden-section search for the K with the lowest error over all positions, using the starting tables
        private double FitK(double[] evals)
        {

            double low = 0.1;
            double high = 4.0;
            double ratio = (Math.Sqrt(5.0) - 1.0) / 2.0;

            double a = high - ratio * (high - low);
            double b = low + ratio * (high - low);
            _k = a;
            double errorA = Error(evals, false, false);
            _k = b;
            double errorB = Error(evals, false, false);

            while (high - low > 0.0001)
            {
                if (errorA < errorB)
                {
                    high = b;
                    b = a;
                    errorB = errorA;
                    a = high - ratio * (high - low);
                    _k = a;
                    errorA = Error(evals, false, false);
                }
                else
                {
                    low = a;
                    a = b;
                    errorA = errorB;
                    b = low + ratio * (high - low);
                    _k = b;
                    errorB = Error(evals, false, false);
                }
            }

            return (low + high) / 2.0;

        }


        //One normal pass with the real eval. Each position stores its White-view eval minus the PST part it got from
        //the starting tables, so the model reproduces the real eval exactly at the starting tables
        private void ExtractFeatures()
        {

            int count = _fens.Length;
            _evalWithoutPST = new double[count];
            _phase = new int[count];
            _features = new short[count * MAX_PIECES];
            _featureCount = new byte[count];

            int chunkCount = _threads * CHUNKS_PER_THREAD;
            Parallel.For(0, chunkCount, _parallelOptions, chunk =>
            {
                Board theBoard = new();
                TranspositionTable evalTable = new(true);

                int start = (int)((long)chunk * count / chunkCount);
                int end = (int)((long)(chunk + 1) * count / chunkCount);

                for (int i = start; i < end; i++)
                {
                    theBoard.InitialiseFromFEN(_fens[i]);
                    int score = Scorer.ScorePosition(ref theBoard, ref evalTable, -5000, 5000, false, 0);
                    int whiteEval = theBoard.OnMove == BLACK ? score : -score;

                    int phase = (theBoard.GamePhase * 256 + 12) / 24;
                    _phase[i] = phase;

                    int featureStart = i * MAX_PIECES;
                    int features = 0;
                    for (int sq = 0; sq < 64; sq++)
                    {
                        if (theBoard.Color[sq] == WHITE)
                        {
                            _features[featureStart + features] = (short)(theBoard.Piece[sq] * 64 + sq + 1);
                            features += 1;
                        }
                        else if (theBoard.Color[sq] == BLACK)
                        {
                            _features[featureStart + features] = (short)(-(theBoard.Piece[sq] * 64 + (sq ^ 56) + 1));
                            features += 1;
                        }
                    }
                    _featureCount[i] = (byte)features;

                    double pstPart = ModelEval(_startTables, i);
                    _evalWithoutPST[i] = whiteEval - pstPart;
                }
            });

            Log("Features extracted for " + count + " positions");

        }


        //The tables are laid out early then late, piece by piece in PAWN..KING order, 64 squares each in Lisa's square
        //order (a8 = 0).
        //A pawn can never stand on the first or last rank, so those entries are never tuned
        private void ReadStartTables()
        {
            for (int piece = PAWN; piece <= KING; piece++)
            {
                int[] early = WhiteTable(piece, true);
                int[] late = WhiteTable(piece, false);
                for (int sq = 0; sq < 64; sq++)
                {
                    _startTables[piece * 64 + sq] = early[sq];
                    _startTables[TABLE_SIZE + piece * 64 + sq] = late[sq];
                    bool tunable = piece != PAWN || (sq >= 8 && sq <= 55);
                    _isTunable[piece * 64 + sq] = tunable;
                    _isTunable[TABLE_SIZE + piece * 64 + sq] = tunable;
                }
            }
            Array.Copy(_startTables, _tables, PARAMETER_COUNT);
        }


        //Puts the (rounded) tuned tables into Globals and runs the real eval over every position, as a check on the
        //model
        private double RealErrorWithTables()
        {

            for (int piece = PAWN; piece <= KING; piece++)
            {
                int[] early = WhiteTable(piece, true);
                int[] late = WhiteTable(piece, false);
                for (int sq = 0; sq < 64; sq++)
                {
                    early[sq] = (int)_tables[piece * 64 + sq];
                    late[sq] = (int)_tables[TABLE_SIZE + piece * 64 + sq];
                }
            }
            InitialiseScorer();

            int count = _fens.Length;
            double[] evals = new double[count];
            int chunkCount = _threads * CHUNKS_PER_THREAD;
            Parallel.For(0, chunkCount, _parallelOptions, chunk =>
            {
                Board theBoard = new();
                TranspositionTable evalTable = new(true);

                int start = (int)((long)chunk * count / chunkCount);
                int end = (int)((long)(chunk + 1) * count / chunkCount);

                for (int i = start; i < end; i++)
                {
                    theBoard.InitialiseFromFEN(_fens[i]);
                    int score = Scorer.ScorePosition(ref theBoard, ref evalTable, -5000, 5000, false, 0);
                    evals[i] = theBoard.OnMove == BLACK ? score : -score;
                }
            });

            return Error(evals, false, false);

        }


        private static int[] WhiteTable(int piece, bool early)
        {
            switch (piece)
            {
                case PAWN:
                    return early ? WhitePawnEarlyPST : WhitePawnLatePST;
                case KNIGHT:
                    return early ? WhiteKnightEarlyPST : WhiteKnightLatePST;
                case BISHOP:
                    return early ? WhiteBishopEarlyPST : WhiteBishopLatePST;
                case ROOK:
                    return early ? WhiteRookEarlyPST : WhiteRookLatePST;
                case QUEEN:
                    return early ? WhiteQueenEarlyPST : WhiteQueenLatePST;
                default:
                    return early ? WhiteKingEarlyPST : WhiteKingLatePST;
            }
        }


        //Each line: <fen> c9 "1-0";  The clocks are added because InitialiseFromFEN expects six fields
        private bool LoadPositions()
        {

            if (!File.Exists(_epdFile))
            {
                Log("EPD file not found: " + _epdFile);
                return false;
            }

            List<string> fens = new();
            List<double> results = new();

            foreach (string line in File.ReadLines(_epdFile))
            {

                if (_maxPositions > 0 && fens.Count >= _maxPositions)
                {
                    break;
                }

                int marker = line.IndexOf(" c9 ", StringComparison.Ordinal);
                if (marker < 0)
                {
                    continue;
                }

                string resultText = line.Substring(marker + 4);
                if (resultText.Contains("1/2-1/2"))
                {
                    results.Add(0.5);
                }
                else if (resultText.Contains("1-0"))
                {
                    results.Add(1.0);
                }
                else if (resultText.Contains("0-1"))
                {
                    results.Add(0.0);
                }
                else
                {
                    continue;
                }

                fens.Add(line.Substring(0, marker) + " 0 1");
            }

            _fens = fens.ToArray();
            _results = results.ToArray();

            Log("Loaded " + _fens.Length + " positions from " + _epdFile + ", using " + _threads + " threads, " +
                _epochs + " epochs");
            return _fens.Length > 0;

        }


        //Rewritten every LOG_EVERY_EPOCHS epochs (with the unrounded tables, rounded for display) and once more at the
        //end.
        //The tables are in the same layout as EvalWeights.cs, so each can be pasted over the old one
        private void WriteResults(double startError, double modelError, double realError, string[] calledCorrectlyLines)
        {

            List<string> lines = new();
            if (double.IsNaN(realError))
            {
                lines.Add("PST tuning in progress (current tables, rounded)");
            }
            else
            {
                lines.Add("PST tuning finished");
                lines.Add("Positions: " + _fens.Length + ", K: " + _k.ToString("F4", CultureInfo.InvariantCulture));
                lines.Add("Error: " + FormatError(startError) + " -> " + FormatError(modelError) + " (model), " +
                    FormatError(realError) + " (real eval)");
                lines.AddRange(calledCorrectlyLines);
            }
            lines.Add("Time: " + FormatTime(_timer.Elapsed));
            lines.Add("");

            for (int half = 0; half < 2; half++)
            {
                for (int piece = PAWN; piece <= KING; piece++)
                {
                    int offset = half * TABLE_SIZE + piece * 64;
                    double shift = 0;
                    int tunable = 0;
                    for (int sq = 0; sq < 64; sq++)
                    {
                        if (_isTunable[offset + sq])
                        {
                            shift += Math.Round(_tables[offset + sq]) - _startTables[offset + sq];
                            tunable += 1;
                        }
                    }

                    string name = "White" + PIECE_NAMES[piece] + (half == 0 ? "Early" : "Late") + "PST";
                    lines.Add("        //" + name + ": average change " +
                        (shift / tunable).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture));
                    lines.Add("        public static int[] " + name + " = new int[64]");
                    lines.Add("        {");
                    for (int rank = 0; rank < 8; rank++)
                    {
                        List<string> values = new();
                        for (int file = 0; file < 8; file++)
                        {
                            values.Add(((int)Math.Round(_tables[offset + rank * 8 +
                                file])).ToString(CultureInfo.InvariantCulture));
                        }
                        lines.Add("            " + string.Join(", ", values) + (rank < 7 ? "," : ""));
                    }
                    lines.Add("        };");
                    lines.Add("");
                }
            }

            File.WriteAllLines(_resultsFile, lines);

        }


        //A position is called correctly when its eval points the way the game went: above DRAW_BAND for a White win,
        //below -DRAW_BAND for a Black win, within DRAW_BAND of 0 for a draw. Only reported, to read alongside the
        //error; never tuned on
        private int CountCalledCorrectly(double[] evals, bool heldOutOnly, out int considered)
        {
            int correct = 0;
            considered = 0;
            for (int i = 0; i < evals.Length; i++)
            {
                if (heldOutOnly && !IsHeldOut(i))
                {
                    continue;
                }
                considered += 1;
                if (IsCalledCorrectly(evals[i], _results[i]))
                {
                    correct += 1;
                }
            }
            return correct;
        }


        private static bool IsCalledCorrectly(double whiteEval, double result)
        {
            if (result == 1.0)
            {
                return whiteEval > DRAW_BAND;
            }
            if (result == 0.0)
            {
                return whiteEval < -DRAW_BAND;
            }
            return Math.Abs(whiteEval) <= DRAW_BAND;
        }


        private static string CalledCorrectlyText(int correct, int considered)
        {
            return correct + " of " + considered + " (" + (100.0 * correct / considered).ToString("F3",
                CultureInfo.InvariantCulture) + "%)";
        }


        private static string CalledCorrectlyChangeText(int before, int after, int considered)
        {
            return before + " -> " + after + " of " + considered + " (" +
                (100.0 * before / considered).ToString("F3", CultureInfo.InvariantCulture)
                + "% -> " + (100.0 * after / considered).ToString("F3", CultureInfo.InvariantCulture) + "%)";
        }


        private void Log(string message)
        {
            File.AppendAllText(_logFile, FormatTime(_timer.Elapsed) + "  " + message + "\r\n");
        }


        private static string FormatError(double error)
        {
            return error.ToString("F8", CultureInfo.InvariantCulture);
        }


        private static string FormatTime(TimeSpan elapsed)
        {
            return ((int)elapsed.TotalHours).ToString("00") + ":" + elapsed.Minutes.ToString("00") + ":" +
                elapsed.Seconds.ToString("00");
        }

    }
}
