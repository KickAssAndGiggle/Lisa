using static Lisa.Globals;
namespace Lisa
{
    class Program
    {
        static void Main(string[] args)
        {

            if (RUN_MODE == ProgramMode.Perft)
            {
                InitialiseScorer();
                PerftRunner perfter = new(PERFT_FEN, PERFT_DEPTH, OUTPUT_FILE);
                perfter.DoPerft();
            }
            else if (RUN_MODE == ProgramMode.UCI)
            {
                UCIInterface uci = new();
                uci.InitiateUCI();
            }
            else if (RUN_MODE == ProgramMode.MultiFen)
            {
                if (FENS_TO_TEST != null)
                {
                    InitialiseScorer();
                    FenTester fenner = new(FENS_TO_TEST, OUTPUT_FILE);
                    fenner.Test();
                }
            }
            else if (RUN_MODE == ProgramMode.FenToZobrist)
            {
                InitialiseScorer();
                FenToZobristConverter fenZob = new();
                fenZob.ConvertFenToZobrist(PERFT_FEN, OUTPUT_FILE);
            }
            else if (RUN_MODE == ProgramMode.EPD)
            {
                InitialiseScorer();
                EPDTester epd = new(EPD_INPUT, EPD_OUTPUT);
                epd.AnalyzePositions(MULTI_FEN_DEPTH);
            }
            else if (RUN_MODE == ProgramMode.MakeTexelFile)
            {
                InitialiseScorer();
                TexelFileMaker maker = new(TEXEL_PGN_INPUT, TEXEL_EPD_OUTPUT, TEXEL_POSITIONS_PER_GAME, TEXEL_SKIP_PLIES, TEXEL_MAX_GAMES, TEXEL_GAME_STRIDE);
                maker.MakeFile();
            }
            else if (RUN_MODE == ProgramMode.TexelTune)
            {
                InitialiseScorer();
                TexelTuner tuner = new(TEXEL_TUNE_INPUT, TEXEL_TUNE_RESULTS, TEXEL_TUNE_MAX_POSITIONS, TEXEL_TUNE_THREADS);
                tuner.Tune();
            }
            else if (RUN_MODE == ProgramMode.TexelTunePST)
            {
                InitialiseScorer();
                TexelPSTTuner pstTuner = new(TEXEL_TUNE_INPUT, TEXEL_PST_RESULTS, TEXEL_TUNE_MAX_POSITIONS, TEXEL_TUNE_THREADS, TEXEL_PST_EPOCHS);
                pstTuner.Tune();
            }

        }
    }
}