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
                    bool Correct = (BestMoves.Contains(AlgebraicMoveString) || BestMoves.Contains(AlgebraicMoveString + "+")); //(AlgebraicMoveString == MoveWeWant.Replace("+", ""));

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
                    OutputLines.Add("TotalNodes: " + Search.InfoNodesLookedAt.ToString());
                    OutputLines.Add("WithoutQuiesce: " + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("Quiesced: " + Search.InfoNodesQuiesced.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("PVNodesFromTT: " + Search.InfoPVNodesFoundInTT.ToString());
                    OutputLines.Add("CutNodesFromTT: " + Search.InfoCutNodesFoundInTT.ToString());
                    OutputLines.Add("AllNodesFromTT: " + Search.InfoAllNodesFoundInTT.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("CutOffWithPVFromTT: " + Search.InfoPVTTCutoffs.ToString());
                    OutputLines.Add("CutOffWithGreaterThanBetaFromTT: " + Search.InfoBetaTTCutoffs.ToString());
                    OutputLines.Add("CutOffWithLessThanBetaFromTT: " + Search.InfoAlphaTTCutoffs.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("FutilityCutoffD4: " + Search.InfoFutilityD4.ToString());
                    OutputLines.Add("FutilityCutoffD3: " + Search.InfoFutilityD3.ToString());
                    OutputLines.Add("FutilityCutoffD2: " + Search.InfoFutilityD2.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("ReverseFutilityCutoffD4: " + Search.InfoReverseFutilityD4.ToString());
                    OutputLines.Add("ReverseFutilityCutoffD3: " + Search.InfoReverseFutilityD3.ToString());
                    OutputLines.Add("ReverseFutilityCutoffD2: " + Search.InfoReverseFutilityD2.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("NullMoveCuts/Attempts: " + Search.InfoNullMoveCutOffs.ToString() + "/" + Search.InfoNullMoveAttempts.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("ProbcutCuts/Attempts: " + Search.InfoProbCutCutOffs.ToString() + "/" + Search.InfoProbCutAttempts.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithPVMoveNoGenNeeded: " + Search.InfoCutOffWithPVMoveOnly.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithKillerOneMoveNoGenNeeded: " + Search.InfoCutOffUsingKillerOne.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithKillerTwoMoveNoGenNeeded: " + Search.InfoCutOffUsingKillerTwo.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithRefutationNoGenNeeded: " + Search.InfoCutOffUsingRefutation.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("CutWithOnlyWinningCaptures: " + Search.InfoCutOffOnlyUsingWinningCaps.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutWithOnlyLosingCaptures: " + Search.InfoCutOffOnlyUsingLosingCaps.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("CutOffWithFirstSortedMove: " + Search.InfoNodesCutOffWithFirstSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithSecondSortedMove: " + Search.InfoNodesCutoffWithSecondSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithThirdSortedMove: " + Search.InfoNodesCutOffWithThirdSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add("CutOffWithLaterSortedMove: " + Search.InfoNodesCutOffWithLaterSortedMove.ToString() + "/" + Search.InfoNodesLookedAtWithoutQuiesce.ToString());
                    OutputLines.Add(" ");
                    OutputLines.Add("LMR ReSearches: " + Search.InfoLMRReSearches.ToString());
                    OutputLines.Add("LMR NotReduced/ReducedBy0/1/2/3/4/5: " + Search.InfoLMRNotReduced.ToString() + "/" + string.Join("/", Search.InfoLMRReducedBy));
                    OutputLines.Add("LMR ReSearched Cut/Raised/Failed: " + Search.InfoLMRReSearchCut.ToString() + "/" + Search.InfoLMRReSearchRaised.ToString() + "/" + Search.InfoLMRReSearchFailed.ToString());
                    OutputLines.Add("LMR ReSearch Nodes: " + Search.InfoLMRReSearchNodes.ToString());
                    OutputLines.Add("LMR ReSearched Cut by 0/1/2/3/4/5: " + string.Join("/", Search.InfoLMRReSearchCutBy));
                    OutputLines.Add("LMR ReSearched Raised by 0/1/2/3/4/5: " + string.Join("/", Search.InfoLMRReSearchRaisedBy));
                    OutputLines.Add("LMR ReSearched Failed by 0/1/2/3/4/5: " + string.Join("/", Search.InfoLMRReSearchFailedBy));
                    OutputLines.Add(" ");
                    OutputLines.Add("KillerStage SkippedAtAll: " + Search.InfoKillerStageSkippedAtAll.ToString());
                    OutputLines.Add("RefutationTried/Cut: " + Search.InfoRefutationTried.ToString() + "/" + Search.InfoCutOffUsingRefutation.ToString());
                    OutputLines.Add("KillerOne Tried/Cut/AlphaRaised: " + Search.InfoKillerOneTried.ToString() + "/" + Search.InfoCutOffUsingKillerOne.ToString() + "/" + Search.InfoKillerOneAlphaRaised.ToString());
                    OutputLines.Add("KillerOne NotTried EmptyOrTT/SameAsRefutation/Invalid: " + Search.InfoKillerOneEmptyOrTT.ToString() + "/" + Search.InfoKillerOneSameAsRefutation.ToString() + "/" + Search.InfoKillerOneInvalid.ToString());
                    OutputLines.Add("KillerOne Captures Tried/Cut: " + Search.InfoKillerOneCaptureTried.ToString() + "/" + Search.InfoKillerOneCaptureCut.ToString());
                    OutputLines.Add("KillerOne AtAllNodes Tried/Cut: " + Search.InfoKillerOneTriedAtAll.ToString() + "/" + Search.InfoKillerOneCutAtAll.ToString());
                    OutputLines.Add("KillerOne AtUnknownNodes Tried/Cut: " + Search.InfoKillerOneTriedAtUnknown.ToString() + "/" + Search.InfoKillerOneCutAtUnknown.ToString());
                    OutputLines.Add("KillerOne Nodes Cut/Failed: " + Search.InfoKillerOneNodesCut.ToString() + "/" + Search.InfoKillerOneNodesFailed.ToString());
                    OutputLines.Add("KillerTwo Tried/Cut/AlphaRaised: " + Search.InfoKillerTwoTried.ToString() + "/" + Search.InfoCutOffUsingKillerTwo.ToString() + "/" + Search.InfoKillerTwoAlphaRaised.ToString());
                    OutputLines.Add("KillerTwo NotTried EmptyOrTT/SameAsRefutation/Invalid: " + Search.InfoKillerTwoEmptyOrTT.ToString() + "/" + Search.InfoKillerTwoSameAsRefutation.ToString() + "/" + Search.InfoKillerTwoInvalid.ToString());
                    OutputLines.Add("KillerTwo Captures Tried/Cut: " + Search.InfoKillerTwoCaptureTried.ToString() + "/" + Search.InfoKillerTwoCaptureCut.ToString());
                    OutputLines.Add("KillerTwo AtAllNodes Tried/Cut: " + Search.InfoKillerTwoTriedAtAll.ToString() + "/" + Search.InfoKillerTwoCutAtAll.ToString());
                    OutputLines.Add("KillerTwo AtUnknownNodes Tried/Cut: " + Search.InfoKillerTwoTriedAtUnknown.ToString() + "/" + Search.InfoKillerTwoCutAtUnknown.ToString());
                    OutputLines.Add("KillerTwo Nodes Cut/Failed: " + Search.InfoKillerTwoNodesCut.ToString() + "/" + Search.InfoKillerTwoNodesFailed.ToString());
                    OutputLines.Add(" ");

                    OutputLines.Add("PV: " + PVReport.ConvertPVToSAN(Search.BestPV, ConstructedFen, null));

                    OutputLines.Add(" ");
                    OutputLines.AddRange(PVReport.TopMoveLines(Search, ConstructedFen, 5));

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

            StreamWriter SW = File.CreateText(EPDOutFile);
            foreach (string S in OutputLines)
            {
                SW.WriteLine(S);
            }
            SW.Close();


        }

    }

}
