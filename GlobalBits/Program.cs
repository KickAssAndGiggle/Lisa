using static Lisa.Globals;
namespace Lisa
{
    class Program
    {
        static void Main(string[] args)
        {

            Initialise();

            if (Mode == ProgramMode.Perft)
            {
                ReconfigureAfterOptions();
                PerftRunner perfter = new(PerftFen, PerftDepth, OutputFile);
                perfter.DoPerft();
            }
            else if (Mode == ProgramMode.UCI)
            {
                UCIInterface uci = new();
                uci.InitiateUCI();
            }
            else if (Mode == ProgramMode.MultiFen)
            {
                if (FensToTest != null)
                {
                    ReconfigureAfterOptions();
                    FenTester fenner = new(FensToTest, OutputFile);
                    fenner.Test();
                }
            }
            else if (Mode == ProgramMode.FenToZobrist)
            {
                ReconfigureAfterOptions();
                FenToZobristConverter fenZob = new();
                fenZob.ConvertFenToZobrist(PerftFen, OutputFile);
            }
            else if (Mode == ProgramMode.EPD)
            {
                ReconfigureAfterOptions();
                EPDTester epd = new(EPDInput, EPDOutput);
                epd.AnalyzePositions(MultiFenDepth);
            }

        }
    }
}