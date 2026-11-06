using static Lisa.Globals;
namespace Lisa
{
    public class EPDTester
    {

        private readonly string EPDInFile = "";
        private readonly string EPDOutFile = "";

        public EPDTester(string InputFilename, string OutputFileName)
        {
            EPDInFile = InputFilename;
            EPDOutFile = OutputFileName;
        }


        public void AnalyzePositions(byte Depth)
        {


            string[] Lines = File.ReadAllLines(EPDInFile);
            List<string> OutputLines = new();
            int Total = 0; int CorrectSolves = 0; int TotalTicks = 0;
            long[] totalLMPPruned = new long[4];

            foreach (string S in Lines)
            {

                if (!S.Trim().StartsWith("#"))
                {
                    string Fen = "";
                    if (S.Contains(';'))
                    {
                        string[] Splits = S.Split(Convert.ToChar(";"));
                        Fen = Splits[0];
                    }
                    else
                    {
                        Fen = S;
                    }

                    string[] FenSplits = Fen.Split(Convert.ToChar(" "));
                    string ConstructedFen = ""; bool Best = false; bool Avoid = false; List<string> BestMoves = new();
                    for (int NN = 0; NN < FenSplits.Length; NN++)
                    {
                        if (FenSplits[NN] == "bm")
                        {
                            Best = true;
                        }
                        else if (FenSplits[NN] == "am")
                        {
                            Avoid = true;
                        }
                        else
                        {
                            if (Best || Avoid)
                            {
                                BestMoves.Add(FenSplits[NN]);
                            }
                            else
                            {
                                ConstructedFen += FenSplits[NN] + " ";
                            }
                        }
                    }

                    string AcceptableMoves = "";
                    foreach (string BMs in BestMoves)
                    {
                        AcceptableMoves += BMs + " ";
                    }

                    if (ConstructedFen.EndsWith(" "))
                    {
                        ConstructedFen = ConstructedFen.TrimEnd();
                    }

                    Board TestBoard = new();
                    TestBoard.InitialiseFromFEN(ConstructedFen);

                    Searcher Search = new();

                    int StartTicks = System.Environment.TickCount;
                    Search.Search(ref TestBoard, Depth);
                    int EndTicks = System.Environment.TickCount;

                    TotalTicks += (EndTicks - StartTicks);

                    Move Chosen = Search.BestMove;
                    Total += 1;
                    string Movestring = Globals.ConvertMoveToString(Chosen);
                    string AlgebraicMoveString = Globals.ConvertMoveToAlgebraic(Chosen, ref TestBoard);
                    //(AlgebraicMoveString == MoveWeWant.Replace("+", ""));
                    bool Correct = (BestMoves.Contains(AlgebraicMoveString) ||
                        BestMoves.Contains(AlgebraicMoveString + "+"));

                    if (Avoid)
                    {
                        Correct = !Correct;
                    }
                    if (Correct)
                    {
                        CorrectSolves += 1;
                    }

                    OutputLines.Add("#" + Total.ToString());
                    OutputLines.Add("Fen: " + ConstructedFen);
                    OutputLines.Add("Move we chose: " + AlgebraicMoveString + " (" + Movestring + ")");
                    if (Avoid)
                    {
                        OutputLines.Add("We were to avoid: " + AcceptableMoves);
                    }
                    else if (Best)
                    {
                        OutputLines.Add("We were to choose: " + AcceptableMoves);
                    }

                    if (Correct)
                    {
                        OutputLines.Add("This was CORRECT");
                    }
                    else
                    {
                        OutputLines.Add("This was WRONG");
                    }

                    OutputLines.Add(" ");
                    OutputLines.Add("FEN: " + ConstructedFen);
                    OutputLines.Add("Move: " + AlgebraicMoveString + " (" + Movestring + ")");
                    OutputLines.Add("Time: " + (EndTicks - StartTicks).ToString());
                    OutputLines.Add("Score: " + Search.BestScore.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("TotalNodes: " + Search.Stats.NodesLookedAt.ToString());
                    OutputLines.Add("WithoutQuiesce: " + Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("Quiesced: " + Search.Stats.NodesQuiesced.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("PVNodesFromTT: " + Search.Stats.PVNodesFoundInTT.ToString());
                    OutputLines.Add("CutNodesFromTT: " + Search.Stats.CutNodesFoundInTT.ToString());
                    OutputLines.Add("AllNodesFromTT: " + Search.Stats.AllNodesFoundInTT.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("CutOffWithPVFromTT: " + Search.Stats.PVTTCutoffs.ToString());
                    OutputLines.Add("CutOffWithGreaterThanBetaFromTT: " + Search.Stats.BetaTTCutoffs.ToString());
                    OutputLines.Add("CutOffWithLessThanBetaFromTT: " + Search.Stats.AlphaTTCutoffs.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("FutilityCutoffD4: " + Search.Stats.FutilityD4.ToString());
                    OutputLines.Add("FutilityCutoffD3: " + Search.Stats.FutilityD3.ToString());
                    OutputLines.Add("FutilityCutoffD2: " + Search.Stats.FutilityD2.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("ReverseFutilityCutoffD4: " + Search.Stats.ReverseFutilityD4.ToString());
                    OutputLines.Add("ReverseFutilityCutoffD3: " + Search.Stats.ReverseFutilityD3.ToString());
                    OutputLines.Add("ReverseFutilityCutoffD2: " + Search.Stats.ReverseFutilityD2.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("NullMoveCuts/Attempts: " + Search.Stats.NullMoveCutOffs.ToString() + "/" +
                        Search.Stats.NullMoveAttempts.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("ProbcutCuts/Attempts: " + Search.Stats.ProbCutCutOffs.ToString() + "/" +
                        Search.Stats.ProbCutAttempts.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithPVMoveNoGenNeeded: " + Search.Stats.CutOffWithPVMoveOnly.ToString() +
                        "/" + Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithKillerOneMoveNoGenNeeded: " + Search.Stats.KillerOne.CutOffs.ToString() +
                        "/" + Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithKillerTwoMoveNoGenNeeded: " + Search.Stats.KillerTwo.CutOffs.ToString() +
                        "/" + Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithRefutationNoGenNeeded: " +
                        Search.Stats.CutOffUsingRefutation.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("CutWithOnlyWinningCaptures: " +
                        Search.Stats.CutOffOnlyUsingWinningCaps.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutWithOnlyLosingCaptures: " + Search.Stats.CutOffOnlyUsingLosingCaps.ToString() +
                        "/" + Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("CutOffWithFirstSortedMove: " +
                        Search.Stats.NodesCutOffWithFirstSortedMove.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithSecondSortedMove: " +
                        Search.Stats.NodesCutoffWithSecondSortedMove.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithThirdSortedMove: " +
                        Search.Stats.NodesCutOffWithThirdSortedMove.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithLaterSortedMove: " +
                        Search.Stats.NodesCutOffWithLaterSortedMove.ToString() + "/" +
                        Search.Stats.NodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("LMR ReSearches: " + Search.Stats.LMRReSearches.ToString());
                    OutputLines.Add("LMR NotReduced/ReducedBy0/1/2/3/4/5: " + Search.Stats.LMRNotReduced.ToString() +
                        "/" + string.Join("/", Search.Stats.LMRReducedBy));
                    OutputLines.Add("LMR ReSearched Cut/Raised/Failed: " + Search.Stats.LMRReSearchCut.ToString() +
                        "/" + Search.Stats.LMRReSearchRaised.ToString() + "/" +
                        Search.Stats.LMRReSearchFailed.ToString());
                    OutputLines.Add("LMR ReSearch Nodes: " + Search.Stats.LMRReSearchNodes.ToString());
                    OutputLines.Add("LMR ReSearched Cut by 0/1/2/3/4/5: " +
                        string.Join("/", Search.Stats.LMRReSearchCutBy));
                    OutputLines.Add("LMR ReSearched Raised by 0/1/2/3/4/5: " +
                        string.Join("/", Search.Stats.LMRReSearchRaisedBy));
                    OutputLines.Add("LMR ReSearched Failed by 0/1/2/3/4/5: " +
                        string.Join("/", Search.Stats.LMRReSearchFailedBy));
                    OutputLines.Add("LMP pruned at node depth 2/3/4/5: " + string.Join("/", Search.Stats.LMPPruned));
                    for (int nn = 0; nn < 4; nn++)
                    {
                        totalLMPPruned[nn] += Search.Stats.LMPPruned[nn];
                    }
                    OutputLines.Add(" ");
                    OutputLines.Add("KillerStage SkippedAtAll: " + Search.Stats.KillerStageSkippedAtAll.ToString());
                    OutputLines.Add("RefutationTried/Cut: " + Search.Stats.RefutationTried.ToString() + "/" +
                        Search.Stats.CutOffUsingRefutation.ToString());
                    OutputLines.Add("KillerOne Tried/Cut/AlphaRaised: " + Search.Stats.KillerOne.Tried.ToString() +
                        "/" + Search.Stats.KillerOne.CutOffs.ToString() + "/" +
                        Search.Stats.KillerOne.AlphaRaised.ToString());
                    OutputLines.Add("KillerOne NotTried EmptyOrTT/SameAsRefutation/Invalid: " +
                        Search.Stats.KillerOne.EmptyOrTT.ToString() + "/" +
                        Search.Stats.KillerOne.SameAsRefutation.ToString() + "/" +
                        Search.Stats.KillerOne.Invalid.ToString());
                    OutputLines.Add("KillerOne Captures Tried/Cut: " + Search.Stats.KillerOne.CaptureTried.ToString() +
                        "/" + Search.Stats.KillerOne.CaptureCut.ToString());
                    OutputLines.Add("KillerOne AtAllNodes Tried/Cut: " + Search.Stats.KillerOne.TriedAtAll.ToString() +
                        "/" + Search.Stats.KillerOne.CutAtAll.ToString());
                    OutputLines.Add("KillerOne AtUnknownNodes Tried/Cut: " +
                        Search.Stats.KillerOne.TriedAtUnknown.ToString() + "/" +
                        Search.Stats.KillerOne.CutAtUnknown.ToString());
                    OutputLines.Add("KillerOne Nodes Cut/Failed: " + Search.Stats.KillerOne.NodesCut.ToString() + "/" +
                        Search.Stats.KillerOne.NodesFailed.ToString());
                    OutputLines.Add("KillerTwo Tried/Cut/AlphaRaised: " + Search.Stats.KillerTwo.Tried.ToString() +
                        "/" + Search.Stats.KillerTwo.CutOffs.ToString() + "/" +
                        Search.Stats.KillerTwo.AlphaRaised.ToString());
                    OutputLines.Add("KillerTwo NotTried EmptyOrTT/SameAsRefutation/Invalid: " +
                        Search.Stats.KillerTwo.EmptyOrTT.ToString() + "/" +
                        Search.Stats.KillerTwo.SameAsRefutation.ToString() + "/" +
                        Search.Stats.KillerTwo.Invalid.ToString());
                    OutputLines.Add("KillerTwo Captures Tried/Cut: " + Search.Stats.KillerTwo.CaptureTried.ToString() +
                        "/" + Search.Stats.KillerTwo.CaptureCut.ToString());
                    OutputLines.Add("KillerTwo AtAllNodes Tried/Cut: " + Search.Stats.KillerTwo.TriedAtAll.ToString() +
                        "/" + Search.Stats.KillerTwo.CutAtAll.ToString());
                    OutputLines.Add("KillerTwo AtUnknownNodes Tried/Cut: " +
                        Search.Stats.KillerTwo.TriedAtUnknown.ToString() + "/" +
                        Search.Stats.KillerTwo.CutAtUnknown.ToString());
                    OutputLines.Add("KillerTwo Nodes Cut/Failed: " + Search.Stats.KillerTwo.NodesCut.ToString() + "/" +
                        Search.Stats.KillerTwo.NodesFailed.ToString());
                    OutputLines.Add(" ");

                    OutputLines.Add("PV: " + PVReport.ConvertPVToSAN(Search.BestPV, ConstructedFen, null));

                    OutputLines.Add(" ");
                    OutputLines.AddRange(PVReport.TopMoveLines(Search, ConstructedFen, 5));
                    OutputLines.Add(" ");
                    OutputLines.AddRange(Scorer.EvalBreakdownLines(ConstructedFen));

                    OutputLines.Add(" ");
                    OutputLines.Add(" ");
                    OutputLines.Add(" ");
                    OutputLines.Add(" ");
                    OutputLines.Add(" ");

                }

            }

            OutputLines.Add("=========================================================================");
            OutputLines.Add(" ");
            OutputLines.Add("Total moves checked: " + Total.ToString());
            OutputLines.Add("We got correct: " + CorrectSolves.ToString());
            OutputLines.Add("Time taken: " + TotalTicks.ToString());
            OutputLines.Add("Depth used: " + Depth.ToString());
            OutputLines.Add(" ");
            OutputLines.Add("LMP pruned at node depth 2/3/4/5 (from quiet move " + LMP_QUIET_MOVES_D2 + "/" +
                LMP_QUIET_MOVES_D3 + "/" + LMP_QUIET_MOVES_D4 + "/" + LMP_QUIET_MOVES_D5
                + ", 0 = off): " + string.Join("/", totalLMPPruned));

            StreamWriter SW = File.CreateText(EPDOutFile);
            foreach (string S in OutputLines)
            {
                SW.WriteLine(S);
            }
            SW.Close();


        }

    }

}
