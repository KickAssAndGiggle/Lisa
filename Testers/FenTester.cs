using static Lisa.Globals;
namespace Lisa
{
    public sealed class FenTester
    {

        private string[] _fens;
        private string _fenOutputFile;

        public FenTester(string[] FENsToTest, string OutputFile)
        {
            _fens = FENsToTest;
            _fenOutputFile = OutputFile;
        }


        public void Test()
        {

            List<string> linesForFile = new();

            foreach (string fen in _fens)
            {

                int startTicks = System.Environment.TickCount;

                Board fenBoard = new();
                fenBoard.InitialiseFromFEN(fen);

                Searcher search = new();
                search.Search(ref fenBoard, MULTI_FEN_DEPTH);
                Move bestMove = search.BestMove;

                int endTicks = System.Environment.TickCount;

                linesForFile.Add("FEN: " + fen);
                linesForFile.Add("Move: " + Globals.ConvertMoveToString(bestMove));
                linesForFile.Add("Time: " + (endTicks - startTicks).ToString());
                linesForFile.Add(" ");
                linesForFile.Add("TotalNodes: " + search.InfoNodesLookedAt.ToString());
                linesForFile.Add("WithoutQuiesce: " + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("Quiesced: " + search.InfoNodesQuiesced.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("PVNodesFromTT: " + search.InfoPVNodesFoundInTT.ToString());
                linesForFile.Add("CutNodesFromTT: " + search.InfoCutNodesFoundInTT.ToString());
                linesForFile.Add("AllNodesFromTT: " + search.InfoAllNodesFoundInTT.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("CutOffWithPVFromTT: " + search.InfoPVTTCutoffs.ToString());
                linesForFile.Add("CutOffWithGreaterThanBetaFromTT: " + search.InfoBetaTTCutoffs.ToString());
                linesForFile.Add("CutOffWithLessThanBetaFromTT: " + search.InfoAlphaTTCutoffs.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("FutilityCutoffD4: " + search.InfoFutilityD4.ToString());
                linesForFile.Add("FutilityCutoffD3: " + search.InfoFutilityD3.ToString());
                linesForFile.Add("FutilityCutoffD2: " + search.InfoFutilityD2.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("ReverseFutilityCutoffD4: " + search.InfoReverseFutilityD4.ToString());
                linesForFile.Add("ReverseFutilityCutoffD3: " + search.InfoReverseFutilityD3.ToString());
                linesForFile.Add("ReverseFutilityCutoffD2: " + search.InfoReverseFutilityD2.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("NullMoveCuts/Attempts: " + search.InfoNullMoveCutOffs.ToString() + "/" + search.InfoNullMoveAttempts.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("ProbcutCuts/Attempts: " + search.InfoProbCutCutOffs.ToString() + "/" + search.InfoProbCutAttempts.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithPVMoveNoGenNeeded: " + search.InfoCutOffWithPVMoveOnly.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithKillerOneMoveNoGenNeeded: " + search.InfoCutOffUsingKillerOne.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithKillerTwoMoveNoGenNeeded: " + search.InfoCutOffUsingKillerTwo.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithRefutationNoGenNeeded: " + search.InfoCutOffUsingRefutation.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("CutWithOnlyWinningCaptures: " + search.InfoCutOffOnlyUsingWinningCaps.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutWithOnlyLosingCaptures: " + search.InfoCutOffOnlyUsingLosingCaps.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("CutOffWithFirstSortedMove: " + search.InfoNodesCutOffWithFirstSortedMove.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithSecondSortedMove: " + search.InfoNodesCutoffWithSecondSortedMove.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithThirdSortedMove: " + search.InfoNodesCutOffWithThirdSortedMove.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithLaterSortedMove: " + search.InfoNodesCutOffWithLaterSortedMove.ToString() + "/" + search.InfoNodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add(" ");

                Move[]? pv = search.BestPV;
                string pvString = "";

                if (pv != null)
                {                    
                    for (int nn = pv.Length - 1; nn >= 0; nn--)
                    {
                        if (!(pv[nn].From == 0 && pv[nn].To == 0))
                        {
                            pvString += ConvertMoveToString(pv[nn]) + " ";
                        }
                        else
                        {
                            break;
                        }
                    }
                    pvString = pvString.Trim();
                }

                linesForFile.Add("PV: " + pvString);
                linesForFile.Add(" ");
                linesForFile.Add(" ");
                linesForFile.Add(" ");
                linesForFile.Add(" ");
                linesForFile.Add(" ");

            }

            StreamWriter sw = File.CreateText(_fenOutputFile);
            foreach (string line in linesForFile)
            {
                sw.WriteLine(line);
            }
            sw.Close();


        }


    }
}
