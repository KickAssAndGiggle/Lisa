using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using static Lisa.Globals;
namespace Lisa
{
    //Texel tuning: finds the eval weights that best predict the results of real games.
    //Each position's static eval (White's view, in centipawns) is turned into an expected score between 0 and 1
    //with a sigmoid, and compared with the game's actual result (1, 0.5 or 0). The average squared difference
    //over every position is the error. Weights are nudged one at a time, keeping any nudge that lowers the error.
    public sealed class TexelTuner
    {

        //Weights the tuner may change. Comment a line out to leave that weight alone.
        //Pawn material is fixed at 100 (in InitialiseScorer) so the scale stays in centipawns.
        private static readonly string[] TUNABLE_WEIGHTS =
        {
            "KNIGHT_VALUE",
            "BISHOP_VALUE",
            "ROOK_VALUE",
            "QUEEN_VALUE",
            "BISHOP_PAIR_BONUS_VALUE",
            "FLANK_BACKWARD_PAWN_PENALTY",
            "CENTER_BACKWARD_PAWN_PENALTY",
            "ROOK_ON_SEVENTH_BONUS",
            "BISHOP_PAWN_COLOR_PENALTY",
            "KSIDE_CASTLE_BONUS",
            "QSIDE_CASTLE_BONUS",
            "KSIDE_CASTLE_RIGHTS",
            "QSIDE_CASTLE_RIGHTS",
            "DOUBLED_PAWN_PENALTY",
            "DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY",
            "TRIPLED_PAWN_PENALTY",
            "TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY",
            "EDGE_PAWN_ISOLATED_PENALTY",
            "CENTER_PAWN_ISOLATED_PENALTY",
            "ROOK_ON_SEMI_OPEN_FILE_BONUS",
            "ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS",
            "PASSED_PAWN_BONUS",
            "PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS",
            "PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS",
            "PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS",
            "PASSED_PAWN_BLOCKADED_KEEP_PERCENT",
            "PASSED_PAWN_OWN_KING_DISTANCE_PENALTY",
            "PASSED_PAWN_ENEMY_KING_DISTANCE_BONUS",
            "PASSED_PAWN_FREE_PATH_BONUS",
            "PASSED_PAWN_SAFE_STEP_BONUS",
            "PASSED_PAWN_ROOK_BEHIND_BONUS",
            "PASSED_PAWN_ENEMY_ROOK_BEHIND_PENALTY",
            "BISHOP_ATTACKS_QUEEN_BONUS",
            "BISHOP_ATTACKS_ROOK_BONUS",
            "BISHOP_ATTACKS_KNIGHT_BONUS",
            "KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY",
            "KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY",
            "KING_STEPPED_UP_EARLY_PENALTY",
            "KING_FILE_SHELTER_PAWN_BONUS",
            "KING_FILE_NO_SHELTER_PAWN_PENALTY",
            "SIDE_FILE_SHELTER_PAWN_BONUS",
            "SIDE_FILE_NO_SHELTER_PAWN_PENALTY",
            "KING_NEAR_HALF_OPEN_FILE_PENALTY",
            "KING_NEAR_OPEN_FILE_ADDITIONAL_PENALTY",
            "FIXED_PAWN_ON_ENEMY_BISHOP_COLOUR_PENALTY",
            //The attack and defence units pick an entry from KING_ATTACK_PENALTY rather than adding centipawns, and raising
            //the attack units far enough would index past the end of that table, so they are left to hand tuning
            //"KING_ATTACK_KNIGHT_UNITS",
            //"KING_ATTACK_BISHOP_UNITS",
            //"KING_ATTACK_ROOK_UNITS",
            //"KING_ATTACK_QUEEN_UNITS",
            //"KING_DEFENCE_KNIGHT_UNITS",
            //"KING_DEFENCE_BISHOP_UNITS",
            //"KING_DEFENCE_ROOK_UNITS",
            //"KING_DEFENCE_QUEEN_UNITS",
            "OPENING_MOBILITY_PER_KNIGHT_MOVE_BONUS",
            "OPENING_MOBILITY_PER_BISHOP_MOVE_BONUS",
            "OPENING_MOBILITY_PER_ROOK_MOVE_BONUS",
            "OPENING_MOBILITY_PER_QUEEN_MOVE_BONUS",
            "ENDGAME_MOBILITY_PER_KNIGHT_MOVE_BONUS",
            "ENDGAME_MOBILITY_PER_BISHOP_MOVE_BONUS",
            "ENDGAME_MOBILITY_PER_ROOK_MOVE_BONUS",
            "ENDGAME_MOBILITY_PER_QUEEN_MOVE_BONUS",
            "OPENING_MINOR_PIECE_INFLUENCES_CENTER",
            "ROOK_REDUNDANCY_PENALTY",
            "EXCHANGE_IMBALANCE_BONUS",
            "KNIGHT_REDUNDANCY_PENALTY",
            "SEMI_OPEN_FILE_TWO_ROOKS_BONUS",
            "SEMI_OPEN_FILE_ONE_ROOK_BONUS",
            "TRAPPED_ROOK_PENALTY",
            "TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION",
            "TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION",
            "BLOCKADING_PASSED_PAWN_BONUS",
            "KNIGHT_OUTPOST_MINOR_BONUS",
            "KNIGHT_OUTPOST_MAJOR_BONUS",
            "PAWN_CHAIN_BONUS",
            "KNIGHT_ON_THE_RIM_IS_DIM_PENALTY",
            "TEMPO_BONUS",
            "CONNECTED_PASSED_PAWN_BONUS"
        };

        private const int PERCENT_WEIGHT_MAXIMUM = 100;
        private const int WEIGHT_MAXIMUM = 5000;
        private const int INITIAL_STEP_DIVISOR = 16;
        private const int CHUNKS_PER_THREAD = 8;
        private const int DRAW_BAND = 40;                           //centipawns: an eval this close to 0 counts as calling a draw

        private sealed class Weight
        {
            public string Name = "";
            public FieldInfo Field = null!;
            public int StartValue;
            public int Step;
            public int Maximum;
            public bool AffectsEval = true;
        }

        private readonly string _epdFile;
        private readonly string _resultsFile;
        private readonly string _logFile;
        private readonly int _maxPositions;
        private readonly ParallelOptions _parallelOptions;
        private readonly int _threads;

        private string[] _fens = Array.Empty<string>();
        private double[] _results = Array.Empty<double>();
        private int[] _whiteEvals = Array.Empty<int>();

        private readonly List<Weight> _weights = new();
        private double _k;
        private double _startError;
        private double _bestError;
        private int _startCorrect;
        private int _finalCorrect = -1;
        private int _iteration;
        private long _passes;
        private readonly Stopwatch _timer = new();

        public TexelTuner(string epdFile, string resultsFile, int maxPositions, int threads)
        {
            _epdFile = epdFile;
            _resultsFile = resultsFile;
            _logFile = resultsFile + ".log.txt";
            _maxPositions = maxPositions;
            _threads = threads > 0 ? threads : Environment.ProcessorCount;
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
            if (!FindWeights())
            {
                return;
            }

            EvaluateAllPositions();
            _k = FitK();
            _startError = ErrorFromEvals(_k);
            _bestError = _startError;
            Log("K fitted at " + _k.ToString("F4", CultureInfo.InvariantCulture) + ", starting error " + FormatError(_bestError));
            _startCorrect = CountCalledCorrectly();
            Log("Called correctly (draw within " + DRAW_BAND + "): " + CalledCorrectlyText(_startCorrect, _fens.Length));
            WriteResults(false);

            bool converged = false;
            while (!converged)
            {

                _iteration += 1;
                bool anyStepAboveOne = false;
                foreach (Weight weight in _weights)
                {
                    if (weight.AffectsEval && weight.Step > 1)
                    {
                        anyStepAboveOne = true;
                    }
                }
                int improvedWeights = 0;

                foreach (Weight weight in _weights)
                {
                    if (!weight.AffectsEval)
                    {
                        continue;
                    }

                    if (TuneWeight(weight))
                    {
                        improvedWeights += 1;
                        WriteResults(false);
                    }
                    else if (weight.Step > 1)
                    {
                        weight.Step /= 2;
                    }
                }

                Log("Iteration " + _iteration + " done: " + improvedWeights + " weights moved, error " + FormatError(_bestError));
                WriteResults(false);

                converged = improvedWeights == 0 && !anyStepAboveOne;
            }

            //The last pass evaluated a rejected trial value, so evaluate again with the tuned weights before counting
            EvaluateAllPositions();
            _finalCorrect = CountCalledCorrectly();
            Log("Called correctly (draw within " + DRAW_BAND + "): " + CalledCorrectlyChangeText(_startCorrect, _finalCorrect, _fens.Length));

            WriteResults(true);
            Log("Finished.");

        }


        //Tries the weight one step up, then one step down. Whichever direction helps is followed for as long as it keeps helping.
        //Returns true if the weight moved.
        private bool TuneWeight(Weight weight)
        {

            int original = GetValue(weight);

            double upError = double.MaxValue;
            if (original + weight.Step <= weight.Maximum)
            {
                upError = ErrorWithValue(weight, original + weight.Step);
                if (upError < _bestError)
                {
                    FollowDirection(weight, original + weight.Step, upError, weight.Step);
                    return true;
                }
            }

            double downError = double.MaxValue;
            if (original - weight.Step >= 0)
            {
                downError = ErrorWithValue(weight, original - weight.Step);
                if (downError < _bestError)
                {
                    FollowDirection(weight, original - weight.Step, downError, -weight.Step);
                    return true;
                }
            }

            SetValue(weight, original);

            //Neither direction changed a single eval, so the weight is not used by the evaluation
            if (_iteration == 1 && upError == _bestError && downError == _bestError)
            {
                weight.AffectsEval = false;
                Log(weight.Name + " has no effect on any position; it won't be tried again");
            }

            return false;

        }


        private void FollowDirection(Weight weight, int value, double error, int step)
        {

            _bestError = error;
            int bestValue = value;

            while (bestValue + step >= 0 && bestValue + step <= weight.Maximum)
            {
                double nextError = ErrorWithValue(weight, bestValue + step);
                if (nextError >= _bestError)
                {
                    break;
                }
                _bestError = nextError;
                bestValue += step;
            }

            SetValue(weight, bestValue);
            Log(weight.Name + " " + weight.StartValue + " -> " + bestValue + " (step " + weight.Step + "), error " + FormatError(_bestError));

        }


        private double ErrorWithValue(Weight weight, int value)
        {
            SetValue(weight, value);
            EvaluateAllPositions();
            return ErrorFromEvals(_k);
        }


        //Static eval of every position from White's point of view. Each chunk gets its own Board and its own
        //one-entry eval cache, made fresh for this pass so no score worked out under the old weights is reused.
        private void EvaluateAllPositions()
        {

            InitialiseScorer();

            int count = _fens.Length;
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
                    _whiteEvals[i] = theBoard.OnMove == BLACK ? score : -score;
                }
            });

            _passes += 1;

        }


        //Mean of (result - expected score)^2, where expected score = 1 / (1 + 10^(-k * eval / 400))
        private double ErrorFromEvals(double k)
        {

            double scale = -k * Math.Log(10.0) / 400.0;
            double total = 0;

            for (int i = 0; i < _whiteEvals.Length; i++)
            {
                double expected = 1.0 / (1.0 + Math.Exp(scale * _whiteEvals[i]));
                double difference = _results[i] - expected;
                total += difference * difference;
            }

            return total / _whiteEvals.Length;

        }


        //K converts centipawns into expected score. It is fitted once, to the starting weights, and then held still
        //so that the weights do the moving. Golden-section search: the error has a single low point in K.
        private double FitK()
        {

            double low = 0.1;
            double high = 4.0;
            double ratio = (Math.Sqrt(5.0) - 1.0) / 2.0;

            double a = high - ratio * (high - low);
            double b = low + ratio * (high - low);
            double errorA = ErrorFromEvals(a);
            double errorB = ErrorFromEvals(b);

            while (high - low > 0.0001)
            {
                if (errorA < errorB)
                {
                    high = b;
                    b = a;
                    errorB = errorA;
                    a = high - ratio * (high - low);
                    errorA = ErrorFromEvals(a);
                }
                else
                {
                    low = a;
                    a = b;
                    errorA = errorB;
                    b = low + ratio * (high - low);
                    errorB = ErrorFromEvals(b);
                }
            }

            return (low + high) / 2.0;

        }


        //Each line: <fen> c9 "1-0";  The clocks are added because InitialiseFromFEN expects six fields.
        private bool LoadPositions()
        {

            if (!File.Exists(_epdFile))
            {
                Log("EPD file not found: " + _epdFile);
                return false;
            }

            List<string> fens = new();
            List<double> results = new();
            int unreadable = 0;

            foreach (string line in File.ReadLines(_epdFile))
            {

                if (_maxPositions > 0 && fens.Count >= _maxPositions)
                {
                    break;
                }

                int marker = line.IndexOf(" c9 ", StringComparison.Ordinal);
                if (marker < 0)
                {
                    unreadable += 1;
                    continue;
                }

                string resultText = line.Substring(marker + 4);
                double result;
                if (resultText.Contains("1/2-1/2"))
                {
                    result = 0.5;
                }
                else if (resultText.Contains("1-0"))
                {
                    result = 1.0;
                }
                else if (resultText.Contains("0-1"))
                {
                    result = 0.0;
                }
                else
                {
                    unreadable += 1;
                    continue;
                }

                fens.Add(line.Substring(0, marker) + " 0 1");
                results.Add(result);
            }

            _fens = fens.ToArray();
            _results = results.ToArray();
            _whiteEvals = new int[_fens.Length];

            Log("Loaded " + _fens.Length + " positions from " + _epdFile + (unreadable > 0 ? " (" + unreadable + " unreadable lines skipped)" : "") + ", using " + _threads + " threads");
            return _fens.Length > 0;

        }


        private bool FindWeights()
        {

            foreach (string name in TUNABLE_WEIGHTS)
            {

                FieldInfo? field = typeof(Globals).GetField(name, BindingFlags.Public | BindingFlags.Static);
                if (field == null || field.FieldType != typeof(int) || field.IsInitOnly || field.IsLiteral)
                {
                    Log("Weight " + name + " is not a public static int in Globals - fix the list in TexelTuner");
                    return false;
                }

                int value = (int)field.GetValue(null)!;
                _weights.Add(new Weight
                {
                    Name = name,
                    Field = field,
                    StartValue = value,
                    Step = Math.Max(1, Math.Abs(value) / INITIAL_STEP_DIVISOR),
                    Maximum = name.EndsWith("_PERCENT") ? PERCENT_WEIGHT_MAXIMUM : WEIGHT_MAXIMUM
                });
            }

            Log("Tuning " + _weights.Count + " weights");
            return true;

        }


        private static int GetValue(Weight weight)
        {
            return (int)weight.Field.GetValue(null)!;
        }


        private static void SetValue(Weight weight, int value)
        {
            weight.Field.SetValue(null, value);
        }


        //Rewritten after every improvement, so stopping Lisa at any point leaves the best weights found so far.
        //The lines are in the same form as Globals.cs so they can be pasted straight over the old ones.
        private void WriteResults(bool finished)
        {

            List<string> lines = new()
            {
                finished ? "Texel tuning finished" : "Texel tuning in progress (best so far)",
                "Positions: " + _fens.Length + ", K: " + _k.ToString("F4", CultureInfo.InvariantCulture),
                "Error: " + FormatError(_startError) + " -> " + FormatError(_bestError),
                "Called correctly (draw within " + DRAW_BAND + "): " + (_finalCorrect >= 0
                    ? CalledCorrectlyChangeText(_startCorrect, _finalCorrect, _fens.Length)
                    : CalledCorrectlyText(_startCorrect, _fens.Length) + " at the start"),
                "Iteration: " + _iteration + ", evaluation passes: " + _passes + ", time: " + FormatTime(_timer.Elapsed),
                ""
            };

            foreach (Weight weight in _weights)
            {
                int value = GetValue(weight);
                string line = "        public static int " + weight.Name + " = " + value + ";";
                if (value != weight.StartValue)
                {
                    line += " //was " + weight.StartValue;
                }
                if (!weight.AffectsEval)
                {
                    line += " //no effect on any position";
                }
                lines.Add(line);
            }

            File.WriteAllLines(_resultsFile, lines);

        }


        //A position is called correctly when its eval points the way the game went: above DRAW_BAND for a White win, below
        //-DRAW_BAND for a Black win, within DRAW_BAND of 0 for a draw. Only reported, to read alongside the error; never tuned on.
        //Counts from the evals of the latest EvaluateAllPositions
        private int CountCalledCorrectly()
        {
            int correct = 0;
            for (int i = 0; i < _whiteEvals.Length; i++)
            {
                if (IsCalledCorrectly(_whiteEvals[i], _results[i]))
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
            return correct + " of " + considered + " (" + (100.0 * correct / considered).ToString("F3", CultureInfo.InvariantCulture) + "%)";
        }


        private static string CalledCorrectlyChangeText(int before, int after, int considered)
        {
            return before + " -> " + after + " of " + considered + " (" + (100.0 * before / considered).ToString("F3", CultureInfo.InvariantCulture)
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
            return ((int)elapsed.TotalHours).ToString("00") + ":" + elapsed.Minutes.ToString("00") + ":" + elapsed.Seconds.ToString("00");
        }

    }
}
