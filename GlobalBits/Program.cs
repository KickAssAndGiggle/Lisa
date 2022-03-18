using static Lisa.Globals;
namespace Lisa
{
    class Program
    {
        static void Main(string[] args)
        {

            Globals.Initialise();

            if (Globals.Mode == ProgramMode.Perft)
            {
                Globals.ReconfigureAfterOptions();
                PerftRunner Perfter = new(Globals.PerftFen, Globals.PerftDepth, Globals.OutputFile);
                Perfter.DoPerft();
            }
            else if (Globals.Mode == ProgramMode.UCI)
            {
                UCIInterface UCI = new();
                UCI.InitiateUCI();
            }
            else if (Globals.Mode == ProgramMode.MultiFen)
            {
                Globals.ReconfigureAfterOptions();
                FenTester Fenner = new(Globals.FensToTest, Globals.OutputFile);
                Fenner.Test();
            }
            else if (Globals.Mode == ProgramMode.FenToZobrist)
            {
                Globals.ReconfigureAfterOptions();
                FenToZobristConverter FenZob = new();
                FenZob.ConvertFenToZobrist(Globals.PerftFen, Globals.OutputFile);
            }
            else if (Globals.Mode == ProgramMode.EPD)
            {
                Globals.ReconfigureAfterOptions();
                EPDTester EPD = new(EPDInput, EPDOutput);
                EPD.AnalyzePositions(MultiFenDepth);
            }

        }
    }
}