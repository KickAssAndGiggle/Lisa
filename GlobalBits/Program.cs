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

        }
    }
}