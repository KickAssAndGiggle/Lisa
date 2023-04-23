using static Lisa.Globals;
namespace Lisa
{
    public sealed class FenTester
    {

        private string[] FENs;
        private string FenOutputFile;

        public FenTester(string[] FENsToTest, string OutputFile)
        {
            FENs = FENsToTest;
            FenOutputFile = OutputFile;
        }


        public void Test()
        {

            List<string> LinesForFile = new();

            foreach (string FEN in FENs)
            {

                int StartTicks = System.Environment.TickCount;

                Board FenBoard = new();
                FenBoard.InitialiseFromFEN(FEN);
                string OurFen = FenBoard.GenerateFen();

                Searcher Search = new();
                Search.Search(ref FenBoard, MULTI_FEN_DEPTH);
                Move BestMove = Search.BestMove;

                int EndTicks = System.Environment.TickCount;

                LinesForFile.Add("FEN: " + FEN);
                LinesForFile.Add("Move: " + Globals.ConvertMoveToString(BestMove));
                LinesForFile.Add("Time: " + (EndTicks - StartTicks).ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("TotalNodes: " + Search.InfoNodesLookedAt.ToString());
                LinesForFile.Add("WithoutQuiesce: " + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("Quiesced: " + Search.InfoNodesQuiesced.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("PVNodesFromTT: " + Search.InfoPVNodesFoundInTT.ToString());
                LinesForFile.Add("CutNodesFromTT: " + Search.InfoCutNodesFoundInTT.ToString());
                LinesForFile.Add("AllNodesFromTT: " + Search.InfoAllNodesFoundInTT.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("CutOffWithPVFromTT: " + Search.InfoPVTTCutoffs.ToString());
                LinesForFile.Add("CutOffWithGreaterThanBetaFromTT: " + Search.InfoBetaTTCutoffs.ToString());
                LinesForFile.Add("CutOffWithLessThanBetaFromTT: " + Search.InfoAlphaTTCutoffs.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("FutilityCutoffD4: " + Search.InfoFutilityD4.ToString());
                LinesForFile.Add("FutilityCutoffD3: " + Search.InfoFutilityD3.ToString());
                LinesForFile.Add("FutilityCutoffD2: " + Search.InfoFutilityD2.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("ReverseFutilityCutoffD4: " + Search.InfoReverseFutilityD4.ToString());
                LinesForFile.Add("ReverseFutilityCutoffD3: " + Search.InfoReverseFutilityD3.ToString());
                LinesForFile.Add("ReverseFutilityCutoffD2: " + Search.InfoReverseFutilityD2.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("NullMoveCuts/Attempts: " + Search.InfoNullMoveCutOffs.ToString() + "/" + Search.InfoNullMoveAttempts.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("ProbcutCuts/Attempts: " + Search.InfoProbCutCutOffs.ToString() + "/" + Search.InfoProbCutAttempts.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithPVMoveNoGenNeeded: " + Search.InfoCutOffWithPVMoveOnly.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithKillerOneMoveNoGenNeeded: " + Search.InfoCutOffUsingKillerOne.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithKillerTwoMoveNoGenNeeded: " + Search.InfoCutOffUsingKillerTwo.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithRefutationNoGenNeeded: " + Search.InfoCutOffUsingRefutation.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("CutWithOnlyWinningCaptures: " + Search.InfoCutOffOnlyUsingWinningCaps.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutWithOnlyLosingCaptures: " + Search.InfoCutOffOnlyUsingLosingCaps.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add(" ");
                LinesForFile.Add("CutOffWithFirstSortedMove: " + Search.InfoNodesCutOffWithFirstSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithSecondSortedMove: " + Search.InfoNodesCutoffWithSecondSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithThirdSortedMove: " + Search.InfoNodesCutOffWithThirdSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add("CutOffWithLaterSortedMove: " + Search.InfoNodesCutOffWithLaterSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                LinesForFile.Add(" ");

                Move[]? PV = Search.BestPV;
                string PVString = "";
                if (PV != null)
                {                    
                    for (int NN = PV.Length - 1; NN >= 0; NN--)
                    {
                        if (!(PV[NN].From == 0 && PV[NN].To == 0))
                        {
                            PVString += ConvertMoveToString(PV[NN]) + " ";
                        }
                        else
                        {
                            break;
                        }
                    }
                    PVString = PVString.Trim();
                }
                LinesForFile.Add("PV: " + PVString);
                LinesForFile.Add(" ");
                LinesForFile.Add(" ");
                LinesForFile.Add(" ");
                LinesForFile.Add(" ");
                LinesForFile.Add(" ");

            }

            StreamWriter SW = File.CreateText(FenOutputFile);
            foreach (string Line in LinesForFile)
            {
                SW.WriteLine(Line);
            }
            SW.Close();


        }


    }
}
