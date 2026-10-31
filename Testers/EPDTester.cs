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
                    OutputLines.Add(" ");

                    OutputLines.Add("PV: " + ConvertPVToSAN(Search.BestPV, ConstructedFen));

                    OutputLines.Add(" ");
                    OutputLines.Add("Top moves:");
                    if (Search.RootMoves != null && Search.RootPVs != null)
                    {
                        Move[] SortedRootMoves = (Move[])Search.RootMoves.Clone();
                        Array.Sort(SortedRootMoves);
                        for (int NN = 0; NN < SortedRootMoves.Length && NN < 5; NN++)
                        {
                            int MoveKey = SortedRootMoves[NN].From * 100 + SortedRootMoves[NN].To;
                            Move[] MovePV = Search.RootPVs.ContainsKey(MoveKey) ? Search.RootPVs[MoveKey] : new Move[] { SortedRootMoves[NN] };
                            string MovePVString = ConvertPVToSAN(MovePV, ConstructedFen);
                            string FirstMove = MovePVString.Split(' ')[0];
                            OutputLines.Add((NN + 1).ToString() + ") " + FirstMove + "  Score: " + SortedRootMoves[NN].Score.ToString() + "  PV: " + MovePVString);
                        }
                    }

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


        //Converts a PV (stored root move last) to SAN by replaying it on a board set up from the FEN.
        //Each move is matched against the legal moves of the replayed position, and the PV stops at the
        //first one that doesn't match: stored PVs can end with a repeated move.
        private static string ConvertPVToSAN(Move[]? PV, string Fen)
        {
            if (PV == null)
            {
                return "";
            }

            Board PVBoard = new();
            PVBoard.InitialiseFromFEN(Fen);
            string PVString = "";

            for (int NN = PV.Length - 1; NN >= 0; NN--)
            {
                if (PV[NN].From == 0 && PV[NN].To == 0)
                {
                    break;
                }
                if (!TryFindLegalMove(PV[NN], ref PVBoard, out Move LegalMove))
                {
                    break;
                }
                PVString += ConvertMoveToSAN(LegalMove, ref PVBoard) + " ";
                PVBoard.MakeMove(LegalMove, PVBoard.OnMove, false);
            }

            return PVString.Trim();
        }


        private static bool TryFindLegalMove(Move Wanted, ref Board TheBoard, out Move Found)
        {
            Move[] AllMoves = TheBoard.GenerateAllMoves(TheBoard.OnMove);
            foreach (Move M in AllMoves)
            {
                if (M.From == Wanted.From && M.To == Wanted.To && M.PromotionPiece == Wanted.PromotionPiece && TheBoard.MoveIsLegal(M, TheBoard.OnMove))
                {
                    Found = M;
                    return true;
                }
            }
            Found = new Move();
            return false;
        }


        //Full SAN for a move in the position before it is played: adds disambiguation (Nbd7, R1e2),
        //promotion (e8=Q) and check or mate (+, #) to ConvertMoveToAlgebraic's output
        private static string ConvertMoveToSAN(Move TheMove, ref Board TheBoard)
        {
            string San = ConvertMoveToAlgebraic(TheMove, ref TheBoard).Replace("ep", "");

            int MovingPiece = TheBoard.Piece[TheMove.From];
            if (MovingPiece != PAWN && MovingPiece != KING)
            {
                San = San.Substring(0, 1) + Disambiguation(TheMove, ref TheBoard) + San.Substring(1);
            }

            if (TheMove.PromotionPiece != 0)
            {
                San += "=" + "NBRQ"[TheMove.PromotionPiece - 1];
            }

            TheBoard.MakeMove(TheMove, TheBoard.OnMove, false);
            if (TheBoard.IsInCheck(TheBoard.OnMove))
            {
                San += HasLegalMove(ref TheBoard) ? "+" : "#";
            }
            TheBoard.UnmakeLastMove();

            return San;
        }


        //The from-file, from-rank, or both, needed when another piece of the same type can also reach the target square
        private static string Disambiguation(Move TheMove, ref Board TheBoard)
        {
            bool Clash = false; bool SameFile = false; bool SameRank = false;

            Move[] AllMoves = TheBoard.GenerateAllMoves(TheBoard.OnMove);
            foreach (Move M in AllMoves)
            {
                if (M.To == TheMove.To && M.From != TheMove.From && TheBoard.Piece[M.From] == TheBoard.Piece[TheMove.From] && TheBoard.MoveIsLegal(M, TheBoard.OnMove))
                {
                    Clash = true;
                    if (M.From % 8 == TheMove.From % 8)
                    {
                        SameFile = true;
                    }
                    if (M.From / 8 == TheMove.From / 8)
                    {
                        SameRank = true;
                    }
                }
            }

            if (!Clash)
            {
                return "";
            }

            string FromFile = ((char)('a' + TheMove.From % 8)).ToString();
            string FromRank = (8 - TheMove.From / 8).ToString();
            if (!SameFile)
            {
                return FromFile;
            }
            if (!SameRank)
            {
                return FromRank;
            }
            return FromFile + FromRank;
        }


        private static bool HasLegalMove(ref Board TheBoard)
        {
            Move[] AllMoves = TheBoard.GenerateAllMoves(TheBoard.OnMove);
            foreach (Move M in AllMoves)
            {
                if (TheBoard.MoveIsLegal(M, TheBoard.OnMove))
                {
                    return true;
                }
            }
            return false;
        }

    }

}
