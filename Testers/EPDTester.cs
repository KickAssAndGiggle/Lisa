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
            long[] totalReducedBy = new long[6];
            long[] totalVerifiedBy = new long[6];
            long[] totalMissedBy = new long[6];
            long[] totalVerifiedAtQuietIndex = new long[3];
            long[] totalMissedAtQuietIndex = new long[3];
            long totalFailedLow = 0;
            long totalVerifyNodes = 0;
            long[] totalReducedAtDepth = new long[4];
            long[] totalVerifiedAtDepthAndIndex = new long[12];
            long[] totalMissedAtDepthAndIndex = new long[12];
            long[] totalLMPEligible = new long[4 * 31];
            long[] totalLMPPruned = new long[4];
            long[] totalLateByFeatures = new long[4 * 32];
            long[] totalLateBeatByFeatures = new long[4 * 32];
            long[] totalQuietCuts = new long[16];
            long[] totalLMPBeatAlpha = new long[4 * 31];

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
                    OutputLines.Add("QuietCuts by depth <=3 / 4-6 / 7-9 / 10+ (1st|2nd|3rd|4th+): " + QuietCutGridText(Search.InfoQuietCutAtDepthAndPosition));
                    OutputLines.AddRange(LMRVerifyLines(Search));
                    OutputLines.AddRange(LMPShadowLines(Search));
                    for (int nn = 0; nn < 6; nn++)
                    {
                        totalReducedBy[nn] += Search.InfoLMRReducedCountedBy[nn];
                        totalVerifiedBy[nn] += Search.InfoLMRVerifiedBy[nn];
                        totalMissedBy[nn] += Search.InfoLMRMissedAlphaBy[nn];
                    }
                    for (int nn = 0; nn < 3; nn++)
                    {
                        totalVerifiedAtQuietIndex[nn] += Search.InfoLMRVerifiedAtQuietIndex[nn];
                        totalMissedAtQuietIndex[nn] += Search.InfoLMRMissedAlphaAtQuietIndex[nn];
                    }
                    for (int nn = 0; nn < 4; nn++)
                    {
                        totalReducedAtDepth[nn] += Search.InfoLMRReducedAtDepth[nn];
                    }
                    for (int nn = 0; nn < 12; nn++)
                    {
                        totalVerifiedAtDepthAndIndex[nn] += Search.InfoLMRVerifiedAtDepthAndIndex[nn];
                        totalMissedAtDepthAndIndex[nn] += Search.InfoLMRMissedAlphaAtDepthAndIndex[nn];
                    }
                    for (int nn = 0; nn < 4; nn++)
                    {
                        totalLMPPruned[nn] += Search.InfoLMPPruned[nn];
                    }
                    for (int nn = 0; nn < 16; nn++)
                    {
                        totalQuietCuts[nn] += Search.InfoQuietCutAtDepthAndPosition[nn];
                    }
                    for (int nn = 0; nn < 4 * 32; nn++)
                    {
                        totalLateByFeatures[nn] += Search.InfoLMPLateByFeatures[nn];
                        totalLateBeatByFeatures[nn] += Search.InfoLMPLateBeatByFeatures[nn];
                    }
                    for (int nn = 0; nn < 4 * 31; nn++)
                    {
                        totalLMPEligible[nn] += Search.InfoLMPEligible[nn];
                        totalLMPBeatAlpha[nn] += Search.InfoLMPBeatAlpha[nn];
                    }
                    totalFailedLow += Search.InfoLMRFailedLow;
                    totalVerifyNodes += Search.InfoLMRVerifyNodes;
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
            if (LMR_VERIFY)
            {
                long totalReduced = 0;
                long totalVerified = 0;
                long totalMissed = 0;
                for (int nn = 0; nn < 6; nn++)
                {
                    totalReduced += totalReducedBy[nn];
                    totalVerified += totalVerifiedBy[nn];
                    totalMissed += totalMissedBy[nn];
                }
                OutputLines.Add(" ");
                OutputLines.Add("LMR over all positions (checking 1 in " + LMR_VERIFY_EVERY + " fail-lows): reduced " + totalReduced + ", failed low " + totalFailedLow
                    + ", checked " + totalVerified + ", missed alpha raisers " + totalMissed);
                OutputLines.Add("  Missed per checked fail-low: " + Percent(totalMissed, totalVerified)
                    + ", estimated per reduced move: " + EstimatedPercentOfReduced(totalMissed, totalVerified, totalFailedLow, totalReduced));
                for (int nn = 1; nn < 6; nn++)
                {
                    OutputLines.Add("  Reduced by " + nn + ": " + totalMissedBy[nn] + " missed of " + totalVerifiedBy[nn] + " checked (" + Percent(totalMissedBy[nn], totalVerifiedBy[nn]) + ")");
                }
                string[] quietIndexNames = { "2-5", "6-10", "11+" };
                for (int nn = 0; nn < 3; nn++)
                {
                    OutputLines.Add("  Quiet move " + quietIndexNames[nn] + ": " + totalMissedAtQuietIndex[nn] + " missed of " + totalVerifiedAtQuietIndex[nn]
                        + " checked (" + Percent(totalMissedAtQuietIndex[nn], totalVerifiedAtQuietIndex[nn]) + ")");
                }
                string[] depthNames = { "depth <=3", "depth 4-6", "depth 7-9", "depth 10+" };
                OutputLines.Add(" ");
                OutputLines.Add("  Missed/checked (rate) by remaining depth and quiet move:     2-5 | 6-10 | 11+   (reduced at this depth)");
                for (int dd = 0; dd < 4; dd++)
                {
                    string row = "  " + depthNames[dd] + ": ";
                    for (int qq = 0; qq < 3; qq++)
                    {
                        int cell = dd * 3 + qq;
                        row += totalMissedAtDepthAndIndex[cell] + "/" + totalVerifiedAtDepthAndIndex[cell] + " (" + Percent(totalMissedAtDepthAndIndex[cell], totalVerifiedAtDepthAndIndex[cell]) + ")";
                        row += qq < 2 ? " | " : "";
                    }
                    row += "   (" + totalReducedAtDepth[dd] + ")";
                    OutputLines.Add(row);
                }
                OutputLines.Add(" ");
                OutputLines.Add("  Check search nodes (included in node counts): " + totalVerifyNodes);
            }

            OutputLines.Add(" ");
            OutputLines.Add("Quiet-loop cutoffs by remaining depth: which quiet move cut      1st | 2nd | 3rd | 4th+   (cutoffs)");
            string[] cutDepthNames = { "depth <=3", "depth 4-6", "depth 7-9", "depth 10+" };
            for (int dd = 0; dd < 4; dd++)
            {
                long rowTotal = totalQuietCuts[dd * 4] + totalQuietCuts[dd * 4 + 1] + totalQuietCuts[dd * 4 + 2] + totalQuietCuts[dd * 4 + 3];
                OutputLines.Add("  " + cutDepthNames[dd] + ": " + Percent(totalQuietCuts[dd * 4], rowTotal) + " | " + Percent(totalQuietCuts[dd * 4 + 1], rowTotal) + " | "
                    + Percent(totalQuietCuts[dd * 4 + 2], rowTotal) + " | " + Percent(totalQuietCuts[dd * 4 + 3], rowTotal) + "   (" + rowTotal + ")");
            }

            OutputLines.Add(" ");
            OutputLines.Add("LMP pruned at node depth 2/3/4/5 (from quiet move " + LMP_QUIET_MOVES_D2 + "/" + LMP_QUIET_MOVES_D3 + "/" + LMP_QUIET_MOVES_D4 + "/" + LMP_QUIET_MOVES_D5
                + ", 0 = off): " + string.Join("/", totalLMPPruned));

            if (LMP_SHADOW)
            {
                OutputLines.Add(" ");
                OutputLines.Add("LMP shadow over all positions: quiet moves that passed every pruning rule except the move count.");
                OutputLines.Add("Per node depth, by quiet move number: moves | beat alpha (rate) || from this move on: moves | beat alpha (rate)");
                for (int dd = 0; dd < 4; dd++)
                {
                    OutputLines.Add(" ");
                    OutputLines.Add("  Node depth " + (dd + 2) + ":");
                    long tailMoves = 0;
                    long tailBeat = 0;
                    List<string> rows = new();
                    for (int qq = 30; qq >= 1; qq--)
                    {
                        long moves = totalLMPEligible[dd * 31 + qq];
                        long beat = totalLMPBeatAlpha[dd * 31 + qq];
                        tailMoves += moves;
                        tailBeat += beat;
                        string name = qq == 30 ? "30+" : qq.ToString();
                        rows.Add("    " + name.PadLeft(3) + ": " + moves + " | " + beat + " (" + Percent(beat, moves) + ")  ||  " + tailMoves + " | " + tailBeat + " (" + Percent(tailBeat, tailMoves) + ")");
                    }
                    rows.Reverse();
                    OutputLines.AddRange(rows);
                }

                OutputLines.Add(" ");
                OutputLines.Add("LMP shadow, quiet move 10 onwards, by feature mask (1 attacks bigger piece, 2 pawn to 6th/7th, 4 near enemy king, 8 king move, 16 positive history)");
                OutputLines.Add("Per node depth: mask: moves | beat alpha");
                for (int dd = 0; dd < 4; dd++)
                {
                    OutputLines.Add("  Node depth " + (dd + 2) + ":");
                    for (int mm = 0; mm < 32; mm++)
                    {
                        long moves = totalLateByFeatures[dd * 32 + mm];
                        if (moves > 0)
                        {
                            OutputLines.Add("    mask " + mm.ToString().PadLeft(2) + ": " + moves + " | " + totalLateBeatByFeatures[dd * 32 + mm]);
                        }
                    }
                }
            }

            StreamWriter SW = File.CreateText(EPDOutFile);
            foreach (string S in OutputLines)
            {
                SW.WriteLine(S);
            }
            SW.Close();


        }


        //LMR lines for one search. Counts come from the main search only, not from inside the check searches
        internal static List<string> LMRVerifyLines(Searcher search)
        {
            List<string> lines = new();
            int reduced = 0;
            int verified = 0;
            foreach (int count in search.InfoLMRReducedCountedBy)
            {
                reduced += count;
            }
            foreach (int count in search.InfoLMRVerifiedBy)
            {
                verified += count;
            }
            lines.Add("LMR Reduced/FailedLow: " + reduced + "/" + search.InfoLMRFailedLow);
            lines.Add("LMR Reduced at quiet move 2-5/6-10/11+: " + string.Join("/", search.InfoLMRReducedAtQuietIndex));
            lines.Add("LMR Reduced at depth <=3/4-6/7-9/10+: " + string.Join("/", search.InfoLMRReducedAtDepth));
            if (!LMR_VERIFY)
            {
                lines.Add("LMR missed alpha raisers: not measured (set LMRVerify=true)");
                return lines;
            }
            lines.Add("LMR MissedAlphaRaisers: " + search.InfoLMRMissedAlpha + " of " + verified + " checked fail-lows (" + Percent(search.InfoLMRMissedAlpha, verified)
                + "), estimated " + EstimatedPercentOfReduced(search.InfoLMRMissedAlpha, verified, search.InfoLMRFailedLow, reduced) + " of reduced moves");
            int[] missedBy = search.InfoLMRMissedAlphaBy;
            int[] verifiedBy = search.InfoLMRVerifiedBy;
            lines.Add("LMR Missed/Checked by 1/2/3/4/5: " + missedBy[1] + "|" + verifiedBy[1] + " / " + missedBy[2] + "|" + verifiedBy[2] + " / "
                + missedBy[3] + "|" + verifiedBy[3] + " / " + missedBy[4] + "|" + verifiedBy[4] + " / " + missedBy[5] + "|" + verifiedBy[5]);
            int[] missedAt = search.InfoLMRMissedAlphaAtQuietIndex;
            int[] verifiedAt = search.InfoLMRVerifiedAtQuietIndex;
            lines.Add("LMR Missed/Checked at quiet move 2-5/6-10/11+: " + missedAt[0] + "|" + verifiedAt[0] + " / " + missedAt[1] + "|" + verifiedAt[1] + " / " + missedAt[2] + "|" + verifiedAt[2]);
            int[] missedGrid = search.InfoLMRMissedAlphaAtDepthAndIndex;
            int[] verifiedGrid = search.InfoLMRVerifiedAtDepthAndIndex;
            string[] depthNames = { "<=3", "4-6", "7-9", "10+" };
            for (int dd = 0; dd < 4; dd++)
            {
                lines.Add("LMR Missed/Checked at depth " + depthNames[dd] + ", quiet move 2-5/6-10/11+: " + missedGrid[dd * 3] + "|" + verifiedGrid[dd * 3] + " / "
                    + missedGrid[dd * 3 + 1] + "|" + verifiedGrid[dd * 3 + 1] + " / " + missedGrid[dd * 3 + 2] + "|" + verifiedGrid[dd * 3 + 2]);
            }
            lines.Add("LMR Check search nodes (included in TotalNodes): " + search.InfoLMRVerifyNodes);
            return lines;
        }


        //LMP shadow lines for one search: per node depth, moves that passed every rule except the count, and how many beat alpha
        internal static List<string> LMPShadowLines(Searcher search)
        {
            List<string> lines = new();
            lines.Add("LMP pruned at node depth 2/3/4/5: " + string.Join("/", search.InfoLMPPruned));
            if (!LMP_SHADOW)
            {
                return lines;
            }
            for (int dd = 0; dd < 4; dd++)
            {
                int moves = 0;
                int beat = 0;
                for (int qq = 1; qq <= 30; qq++)
                {
                    moves += search.InfoLMPEligible[dd * 31 + qq];
                    beat += search.InfoLMPBeatAlpha[dd * 31 + qq];
                }
                lines.Add("LMP shadow node depth " + (dd + 2) + ": eligible quiet moves " + moves + ", beat alpha " + beat + " (" + Percent(beat, moves) + ")");
            }
            return lines;
        }


        private static string QuietCutGridText(int[] grid)
        {
            List<string> rows = new();
            for (int dd = 0; dd < 4; dd++)
            {
                rows.Add(grid[dd * 4] + "|" + grid[dd * 4 + 1] + "|" + grid[dd * 4 + 2] + "|" + grid[dd * 4 + 3]);
            }
            return string.Join(" / ", rows);
        }


        //Missed rate among checked fail-lows, scaled by the share of reduced moves that failed low
        private static string EstimatedPercentOfReduced(long missed, long verified, long failedLow, long reduced)
        {
            if (verified == 0 || reduced == 0)
            {
                return "0%";
            }
            double estimate = 100.0 * missed / verified * failedLow / reduced;
            return estimate.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }


        private static string Percent(long part, long whole)
        {
            return whole == 0 ? "0%" : (100.0 * part / whole).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }

    }

}
