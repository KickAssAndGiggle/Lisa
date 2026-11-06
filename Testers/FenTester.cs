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
                linesForFile.Add("Score: " + search.BestScore.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("TotalNodes: " + search.Stats.NodesLookedAt.ToString());
                linesForFile.Add("WithoutQuiesce: " + search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("Quiesced: " + search.Stats.NodesQuiesced.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("PVNodesFromTT: " + search.Stats.PVNodesFoundInTT.ToString());
                linesForFile.Add("CutNodesFromTT: " + search.Stats.CutNodesFoundInTT.ToString());
                linesForFile.Add("AllNodesFromTT: " + search.Stats.AllNodesFoundInTT.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("CutOffWithPVFromTT: " + search.Stats.PVTTCutoffs.ToString());
                linesForFile.Add("CutOffWithGreaterThanBetaFromTT: " + search.Stats.BetaTTCutoffs.ToString());
                linesForFile.Add("CutOffWithLessThanBetaFromTT: " + search.Stats.AlphaTTCutoffs.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("FutilityCutoffD4: " + search.Stats.FutilityD4.ToString());
                linesForFile.Add("FutilityCutoffD3: " + search.Stats.FutilityD3.ToString());
                linesForFile.Add("FutilityCutoffD2: " + search.Stats.FutilityD2.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("ReverseFutilityCutoffD4: " + search.Stats.ReverseFutilityD4.ToString());
                linesForFile.Add("ReverseFutilityCutoffD3: " + search.Stats.ReverseFutilityD3.ToString());
                linesForFile.Add("ReverseFutilityCutoffD2: " + search.Stats.ReverseFutilityD2.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("NullMoveCuts/Attempts: " + search.Stats.NullMoveCutOffs.ToString() + "/" +
                    search.Stats.NullMoveAttempts.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("ProbcutCuts/Attempts: " + search.Stats.ProbCutCutOffs.ToString() + "/" +
                    search.Stats.ProbCutAttempts.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithPVMoveNoGenNeeded: " + search.Stats.CutOffWithPVMoveOnly.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithKillerOneMoveNoGenNeeded: " + search.Stats.KillerOne.CutOffs.ToString() +
                    "/" + search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithKillerTwoMoveNoGenNeeded: " + search.Stats.KillerTwo.CutOffs.ToString() +
                    "/" + search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithRefutationNoGenNeeded: " + search.Stats.CutOffUsingRefutation.ToString() +
                    "/" + search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("CutWithOnlyWinningCaptures: " + search.Stats.CutOffOnlyUsingWinningCaps.ToString() +
                    "/" + search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutWithOnlyLosingCaptures: " + search.Stats.CutOffOnlyUsingLosingCaps.ToString() +
                    "/" + search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("CutOffWithFirstSortedMove: " +
                    search.Stats.NodesCutOffWithFirstSortedMove.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithSecondSortedMove: " +
                    search.Stats.NodesCutoffWithSecondSortedMove.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithThirdSortedMove: " +
                    search.Stats.NodesCutOffWithThirdSortedMove.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add("CutOffWithLaterSortedMove: " +
                    search.Stats.NodesCutOffWithLaterSortedMove.ToString() + "/" +
                    search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                linesForFile.Add(" ");
                linesForFile.Add("LMP pruned at node depth 2/3/4/5: " + string.Join("/", search.Stats.LMPPruned));
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
                linesForFile.AddRange(PVReport.TopMoveLines(search, fen, 5));
                linesForFile.Add(" ");
                linesForFile.AddRange(Scorer.EvalBreakdownLines(fen));
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
