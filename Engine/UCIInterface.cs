using static Lisa.Globals;
namespace Lisa
{
    public class UCIInterface
    {

        private static TextReader Reader = null;
        private static TextWriter Writer = null;

        private Board GameBoard;
        private Searcher Search;

        private int TimeInMils = 0;
        private int OppTimeInMils = 0;
        private List<long> SeenZobrist = new();

        public void InitiateUCI()
        {

            Reader = Console.In;
            Writer = Console.Out;

            bool Exit = false;

            Search = new Searcher();
            Search.BestMoveSelected += Search_BestMoveSelected;
            Search.InfoUpdated += Search_InfoUpdated;
            Search.TablebaseHit += Search_TablebaseHit;

            StreamWriter optWriter = File.CreateText("C:\\PSOutput\\Options\\" + Guid.NewGuid().ToString());
            optWriter.WriteLine("=== OPTIONS ===");

            do
            {

                string Command = "";

                do
                {

                    int StreamVal = Reader.Read();
                    if (StreamVal != -1)
                    {
                        if (StreamVal < 32)
                        {
                            break;
                        }
                        Command += Convert.ToString(Convert.ToChar(StreamVal));
                    }

                } while (true);

                if (Command.StartsWith("uci"))
                {
                    UCIReportID();
                }
                else if (Command.Contains("quit"))
                {
                    Exit = true;
                }
                else if (Command.Contains("isready"))
                {
                    if (Search.IsSearching)
                    {
                        CancelSearch();
                    }
                    ReconfigureAfterOptions();
                    Writer.Write("readyok" + Convert.ToChar(10));
                }
                else if (Command.StartsWith("setoption"))
                {
                    ProcessOption(Command, optWriter);
                }
                else if (Command.Contains("new"))
                {

                    if (Search.IsSearching)
                    {
                        CancelSearch();
                    }

                    SetupGame("startpos");

                }
                else if (Command.Contains("position"))
                {

                    if (Search.IsSearching)
                    {
                        CancelSearch();
                    }

                    string[] Splits = Command.Split(Convert.ToChar(" "));
                    string PosString = Splits[1];

                    if (PosString.ToLower() == "fen")
                    {
                        PosString = "";
                        int MoveStartIndex = 0;
                        for (int NN = 2; NN < Splits.Length; NN++)
                        {
                            if (Splits[NN].ToLower() != "moves")
                            {
                                PosString += Splits[NN] + " ";
                            }
                            else
                            {
                                PosString = PosString.TrimEnd();
                                MoveStartIndex = NN + 1;
                                break;
                            }
                        }

                        if (MoveStartIndex > 0)
                        {
                            string[] MoveARR = new string[Splits.Length - MoveStartIndex];
                            Array.Copy(Splits, MoveStartIndex, MoveARR, 0, Splits.Length - MoveStartIndex);
                            SetupGame(PosString, MoveARR);
                        }
                        else
                        {
                            SetupGame(PosString, null);
                        }
                    }
                    else if (PosString.ToLower() == "startpos")
                    {
                        if (Splits.Length > 2)
                        {
                            string[] MoveARR = new string[Splits.Length - 3];
                            Array.Copy(Splits, 3, MoveARR, 0, Splits.Length - 3);
                            SetupGame(PosString, MoveARR);
                        }
                        else
                        {
                            SetupGame(PosString, null);
                        }
                    }

                }
                else if (Command.StartsWith("go"))
                {

                    if (optWriter != null)
                    {
                        optWriter.WriteLine("=== DONE ===");
                        optWriter.Close();
                        optWriter = null;
                    }

                    string[] Splits = Command.Split(Convert.ToChar(" "));

                    for (int NN = 0; NN < Splits.Length; NN++)
                    {
                        string S = Splits[NN];
                        if (S.ToLower() == "wtime" && GameBoard.OnMove == WHITE)
                        {
                            string MsString = Splits[NN + 1];
                            TimeInMils = Convert.ToInt32(MsString);
                        }
                        else if (S.ToLower() == "wtime" && GameBoard.OnMove == BLACK)
                        {
                            string MsString = Splits[NN + 1];
                            OppTimeInMils = Convert.ToInt32(MsString);
                        }
                        else if (S.ToLower() == "btime" && GameBoard.OnMove == BLACK)
                        {
                            string MsString = Splits[NN + 1];
                            TimeInMils = Convert.ToInt32(MsString);
                        }
                        else if (S.ToLower() == "btime" && GameBoard.OnMove == WHITE)
                        {
                            string MsString = Splits[NN + 1];
                            OppTimeInMils = Convert.ToInt32(MsString);
                        }
                    }

                    Thread SearchThread = new(new ThreadStart(SearchPosition));
                    SearchThread.Priority = ThreadPriority.Highest;
                    SearchThread.Start();

                }
                else if (Command.StartsWith("stop"))
                {
                    CancelSearch();
                    UCIMoveMade(Search.BestMove);
                }
                else
                {
                    string XYZ = Command;
                }

            } while (!Exit);


        }


        private void ProcessOption(string OptString, StreamWriter sw)
        {

            string[] Splits = OptString.Split(" ");
            string OptName = ""; string OptValue = "";

            for (int NN = 0; NN < Splits.Length; NN++)
            {
                if (Splits[NN] == "name")
                {
                    OptName = Splits[NN + 1];
                }
                else if (Splits[NN] == "value")
                {
                    OptValue = Splits[NN + 1];
                }
            }

            if (OptName != "" && OptValue != "")
            {
                switch (OptName.ToUpper())
                {

                    case "KNIGHT_VALUE":

                        KNIGHT_VALUE = Convert.ToInt32(OptValue);
                        sw.WriteLine("KNIGHT_VALUE = " + KNIGHT_VALUE.ToString());
                        break;

                    case "BISHOP_VALUE":

                        BISHOP_VALUE = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_VALUE = " + BISHOP_VALUE.ToString());
                        break;

                    case "ROOK_VALUE":

                        ROOK_VALUE = Convert.ToInt32(OptValue);
                        sw.WriteLine("ROOK_VALUE = " + ROOK_VALUE.ToString());
                        break;

                    case "QUEEN_VALUE":

                        QUEEN_VALUE = Convert.ToInt32(OptValue);
                        sw.WriteLine("QUEEN_VALUE = " + QUEEN_VALUE.ToString());
                        break;

                    case "BISHOP_PAIR_BONUS_VALUE":

                        BISHOP_PAIR_BONUS_VALUE = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_PAIR_BONUS_VALUE = " + BISHOP_PAIR_BONUS_VALUE.ToString());
                        break;

                    case "FLANK_BACKWARD_PAWN_PENALTY":

                        FLANK_BACKWARD_PAWN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("FLANK_BACKWARD_PAWN_PENALTY = " + FLANK_BACKWARD_PAWN_PENALTY.ToString());
                        break;

                    case "CENTER_BACKWARD_PAWN_PENALTY":

                        CENTER_BACKWARD_PAWN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("CENTER_BACKWARD_PAWN_PENALTY = " + CENTER_BACKWARD_PAWN_PENALTY.ToString());
                        break;

                    case "BISHOP_PAWN_COLOR_PENALTY":

                        BISHOP_PAWN_COLOR_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_PAWN_COLOR_PENALTY = " + BISHOP_PAWN_COLOR_PENALTY.ToString());
                        break;

                    case "ROOK_ON_SEVENTH_BONUS":

                        ROOK_ON_SEVENTH_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("ROOK_ON_SEVENTH_BONUS = " + ROOK_ON_SEVENTH_BONUS.ToString());
                        break;

                    case "KSIDE_CASTLE_BONUS":

                        KSIDE_CASTLE_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("KSIDE_CASTLE_BONUS = " + KSIDE_CASTLE_BONUS.ToString());
                        break;

                    case "KSIDE_CASTLE_RIGHTS":

                        KSIDE_CASTLE_RIGHTS = Convert.ToInt32(OptValue);
                        sw.WriteLine("KSIDE_CASTLE_RIGHTS = " + KSIDE_CASTLE_RIGHTS.ToString());
                        break;

                    case "QSIDE_CASTLE_BONUS":

                        QSIDE_CASTLE_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("QSIDE_CASTLE_BONUS = " + QSIDE_CASTLE_BONUS.ToString());
                        break;

                    case "QSIDE_CASTLE_RIGHTS":

                        QSIDE_CASTLE_RIGHTS = Convert.ToInt32(OptValue);
                        sw.WriteLine("QSIDE_CASTLE_RIGHTS = " + QSIDE_CASTLE_RIGHTS.ToString());
                        break;

                    case "DOUBLED_PAWN_PENALTY":

                        DOUBLED_PAWN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("DOUBLED_PAWN_PENALTY = " + DOUBLED_PAWN_PENALTY.ToString());
                        break;

                    case "DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY":

                        DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = " + DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY.ToString());
                        break;

                    case "TRIPLED_PAWN_PENALTY":

                        TRIPLED_PAWN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("TRIPLED_PAWN_PENALTY = " + TRIPLED_PAWN_PENALTY.ToString());
                        break;

                    case "TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY":

                        TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = " + TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY.ToString());
                        break;

                    case "EDGE_PAWN_ISOLATED_PENALTY":

                        EDGE_PAWN_ISOLATED_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("EDGE_PAWN_ISOLATED_PENALTY = " + EDGE_PAWN_ISOLATED_PENALTY.ToString());
                        break;

                    case "CENTER_PAWN_ISOLATED_PENALTY":

                        CENTER_PAWN_ISOLATED_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("CENTER_PAWN_ISOLATED_PENALTY = " + CENTER_PAWN_ISOLATED_PENALTY.ToString());
                        break;

                    case "ROOK_ON_SEMI_OPEN_FILE_BONUS":

                        ROOK_ON_SEMI_OPEN_FILE_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("ROOK_ON_SEMI_OPEN_FILE_BONUS = " + ROOK_ON_SEMI_OPEN_FILE_BONUS.ToString());
                        break;

                    case "ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS":

                        ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = " + ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_BONUS":

                        PASSED_PAWN_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("PASSED_PAWN_BONUS = " + PASSED_PAWN_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS":

                        PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = " + PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS":

                        PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = " + PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS":

                        PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = " + PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY":

                        PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = " + PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY.ToString());
                        break;

                    case "BISHOP_ATTACKS_KING_BONUS":

                        BISHOP_ATTACKS_KING_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_ATTACKS_KING_BONUS = " + BISHOP_ATTACKS_KING_BONUS.ToString());
                        break;

                    case "BISHOP_ATTACKS_QUEEN_BONUS":

                        BISHOP_ATTACKS_QUEEN_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_ATTACKS_QUEEN_BONUS = " + BISHOP_ATTACKS_QUEEN_BONUS.ToString());
                        break;

                    case "BISHOP_ATTACKS_ROOK_BONUS":

                        BISHOP_ATTACKS_ROOK_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_ATTACKS_ROOK_BONUS = " + BISHOP_ATTACKS_ROOK_BONUS.ToString());
                        break;

                    case "BISHOP_ATTACKS_KNIGHT_BONUS":

                        BISHOP_ATTACKS_KNIGHT_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_ATTACKS_KNIGHT_BONUS = " + BISHOP_ATTACKS_KNIGHT_BONUS.ToString());
                        break;

                    case "FIANCETTO_IS_GOOD_BONUS":

                        FIANCETTO_IS_GOOD_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("FIANCETTO_IS_GOOD_BONUS = " + FIANCETTO_IS_GOOD_BONUS.ToString());
                        break;

                    case "FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY":

                        FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY = " + FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY.ToString());
                        break;

                    case "FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY":

                        FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY = " + FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY.ToString());
                        break;

                    case "FIANCETTO_WITHOUT_BISHOP_PENALTY":

                        FIANCETTO_WITHOUT_BISHOP_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("FIANCETTO_WITHOUT_BISHOP_PENALTY = " + FIANCETTO_WITHOUT_BISHOP_PENALTY.ToString());
                        break;

                    case "BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY":

                        BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY = " + BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY.ToString());
                        break;

                    case "BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY":

                        BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY = " + BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY.ToString());
                        break;

                    case "KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY":

                        KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = " + KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY.ToString());
                        break;

                    case "KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY":

                        KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = " + KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY.ToString());
                        break;

                    case "KING_STEPPED_UP_EARLY_PENALTY":

                        KING_STEPPED_UP_EARLY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STEPPED_UP_EARLY_PENALTY = " + KING_STEPPED_UP_EARLY_PENALTY.ToString());
                        break;

                    case "KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION":

                        KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION = " + KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION.ToString());
                        break;

                    case "KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION":

                        KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION = " + KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION.ToString());
                        break;

                    case "KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION":

                        KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION = " + KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION.ToString());
                        break;

                    case "KING_STEPPED_UP_PAWN_SHIELD_MITIGATION":

                        KING_STEPPED_UP_PAWN_SHIELD_MITIGATION = Convert.ToInt32(OptValue);
                        sw.WriteLine("KNIGHT_VALUE = " + KING_STEPPED_UP_PAWN_SHIELD_MITIGATION.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KNIGHT_VALUE.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "OPENING_MOBILITY_PER_MOVE_BONUS":

                        OPENING_MOBILITY_PER_MOVE_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("OPENING_MOBILITY_PER_MOVE_BONUS = " + OPENING_MOBILITY_PER_MOVE_BONUS.ToString());
                        break;

                    case "OPENING_MINOR_PIECE_INFLUENCES_CENTER":

                        OPENING_MINOR_PIECE_INFLUENCES_CENTER = Convert.ToInt32(OptValue);
                        sw.WriteLine("OPENING_MINOR_PIECE_INFLUENCES_CENTER = " + OPENING_MINOR_PIECE_INFLUENCES_CENTER.ToString());
                        break;

                    case "ROOK_REDUNDANCY_PENALTY":

                        ROOK_REDUNDANCY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("ROOK_REDUNDANCY_PENALTY = " + ROOK_REDUNDANCY_PENALTY.ToString());
                        break;

                    case "KNIGHT_REDUNDANCY_PENALTY":

                        KNIGHT_REDUNDANCY_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KNIGHT_REDUNDANCY_PENALTY = " + KNIGHT_REDUNDANCY_PENALTY.ToString());
                        break;

                    case "SEMI_OPEN_FILE_TWO_ROOKS_BONUS":

                        SEMI_OPEN_FILE_TWO_ROOKS_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("SEMI_OPEN_FILE_TWO_ROOKS_BONUS = " + SEMI_OPEN_FILE_TWO_ROOKS_BONUS.ToString());
                        break;

                    case "SEMI_OPEN_FILE_ONE_ROOK_BONUS":

                        SEMI_OPEN_FILE_ONE_ROOK_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("SEMI_OPEN_FILE_ONE_ROOK_BONUS = " + SEMI_OPEN_FILE_ONE_ROOK_BONUS.ToString());
                        break;

                    case "TRAPPED_ROOK_PENALTY":

                        TRAPPED_ROOK_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("TRAPPED_ROOK_PENALTY = " + TRAPPED_ROOK_PENALTY.ToString());
                        break;

                    case "TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION":

                        TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = Convert.ToInt32(OptValue);
                        sw.WriteLine("TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = " + TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION.ToString());
                        break;

                    case "TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION":

                        TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = Convert.ToInt32(OptValue);
                        sw.WriteLine("TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = " + TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION.ToString());
                        break;

                    case "BLOCKADING_PASSED_PAWN_BONUS":

                        BLOCKADING_PASSED_PAWN_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("BLOCKADING_PASSED_PAWN_BONUS = " + BLOCKADING_PASSED_PAWN_BONUS.ToString());
                        break;

                    case "KNIGHT_OUTPOST_MINOR_BONUS":

                        KNIGHT_OUTPOST_MINOR_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("KNIGHT_OUTPOST_MINOR_BONUS = " + KNIGHT_OUTPOST_MINOR_BONUS.ToString());
                        break;

                    case "KNIGHT_OUTPOST_MAJOR_BONUS":

                        KNIGHT_OUTPOST_MAJOR_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("KNIGHT_OUTPOST_MAJOR_BONUS = " + KNIGHT_OUTPOST_MAJOR_BONUS.ToString());
                        break;

                    case "PAWN_CHAIN_BONUS":

                        PAWN_CHAIN_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("PAWN_CHAIN_BONUS = " + PAWN_CHAIN_BONUS.ToString());
                        break;

                    case "KNIGHT_ON_THE_RIM_IS_DIM_PENALTY":

                        KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = Convert.ToInt32(OptValue);
                        sw.WriteLine("KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = " + KNIGHT_ON_THE_RIM_IS_DIM_PENALTY.ToString());
                        break;

                    case "TEMPO_BONUS":

                        TEMPO_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("TEMPO_BONUS = " + TEMPO_BONUS.ToString());
                        break;

                    case "CONNECTED_PASSED_PAWN_BONUS":

                        CONNECTED_PASSED_PAWN_BONUS = Convert.ToInt32(OptValue);
                        sw.WriteLine("CONNECTED_PASSED_PAWN_BONUS = " + CONNECTED_PASSED_PAWN_BONUS.ToString());
                        break;

                    case "DEPTH6_FUTILITY_MARGIN":

                        DEPTH6_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH6_FUTILITY_MARGIN = " + DEPTH6_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH5_FUTILITY_MARGIN":

                        DEPTH5_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH5_FUTILITY_MARGIN = " + DEPTH5_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH4_REVERSE_FUTILITY_MARGIN":

                        DEPTH4_REVERSE_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH4_REVERSE_FUTILITY_MARGIN = " + DEPTH4_REVERSE_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH4_FUTILITY_MARGIN":

                        DEPTH4_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH4_FUTILITY_MARGIN = " + DEPTH4_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH3_REVERSE_FUTILITY_MARGIN":

                        DEPTH3_REVERSE_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH3_REVERSE_FUTILITY_MARGIN = " + DEPTH3_REVERSE_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH3_FUTILITY_MARGIN":

                        DEPTH3_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH3_FUTILITY_MARGIN = " + DEPTH3_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH2_REVERSE_FUTILITY_MARGIN":

                        DEPTH2_REVERSE_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH2_REVERSE_FUTILITY_MARGIN = " + DEPTH2_REVERSE_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH2_FUTILITY_MARGIN":

                        DEPTH2_FUTILITY_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH2_FUTILITY_MARGIN = " + DEPTH2_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(OptValue);
                        sw.WriteLine("DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "LAZY_EVAL_QUEENS_OFF_MARGIN":

                        LAZY_EVAL_QUEENS_OFF_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("LAZY_EVAL_QUEENS_OFF_MARGIN = " + LAZY_EVAL_QUEENS_OFF_MARGIN.ToString());
                        break;

                    case "LAZY_EVAL_QUEENS_ON_MARGIN":

                        LAZY_EVAL_QUEENS_ON_MARGIN = Convert.ToInt32(OptValue);
                        sw.WriteLine("LAZY_EVAL_QUEENS_ON_MARGIN = " + LAZY_EVAL_QUEENS_ON_MARGIN.ToString());
                        break;

                }

            }


        }

        private void Search_TablebaseHit()
        {
            UCISendToGUI("bestmove " + Search.UCITablebaseHit);
        }

        private void CancelSearch()
        {
            Search.Cancel();
            do
            {
                Thread.Sleep(1);
            } while (!Search.HasCancelled);
            Thread.Sleep(100);
        }


        private void Search_InfoUpdated()
        {

            try
            {

                string InfoString = "info seldepth " + Search.InfoCurrentSearchDepth.ToString() + " depth " + Search.InfoCurrentSearchDepth.ToString() + " time " + (Search.InfoSecondsUsed * 1000).ToString();
                InfoString += " nodes " + Search.InfoNodesLookedAt.ToString() + " nps " + Search.InfoNodesPerSecond.ToString();
                Move[] PV = Search.BestPV;
                string PVString = "";
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
                InfoString += " score cp " + Search.BestScore.ToString() + " pv " + PVString;

                UCISendToGUI(InfoString);

            }
            catch (Exception Ex)
            {
                string Err = Ex.Message;
            }

        }


        private void Search_BestMoveSelected()
        {
            try
            {
                if (!Search.HasCancelled)
                {
                    UCIMoveMade(Search.BestMove);
                }
            }
            catch (Exception Ex)
            {
                string Err = Ex.Message;
            }
        }


        private void UCIReportID()
        {
            UCISendToGUI("id name PawnStorm");
            UCISendToGUI("id author Russell Lambert");
            UCISendToGUI("id country ENG");
            UCISendToGUI("uciok");
        }


        private void UCIMoveMade(Move Made)
        {
            string MoveString = ConvertMoveToString(Made);
            UCISendToGUI("bestmove " + MoveString);
        }


        private void SearchPosition()
        {
            Search.Search(ref GameBoard, 20, TimeInMils, OppTimeInMils, SeenZobrist);
        }


        private void SetupGame(string StartPos, string[] GUIMoves = null)
        {

            GameBoard = new Board();
            if (StartPos == "startpos")
            {
                GameBoard.InitialiseFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            }
            else
            {
                GameBoard.InitialiseFromFEN(StartPos);
            }

            SeenZobrist.Clear();
            SeenZobrist.Add(GameBoard.CurrentZobrist);
            if (GUIMoves != null)
            {
                for (int NN = 0; NN <= GUIMoves.Length - 1; NN++)
                {
                    Move M = new();
                    string StartBit = GUIMoves[NN].Substring(0, 2);
                    string EndBit = GUIMoves[NN].Substring(2);

                    int StartRank = Convert.ToInt32(StartBit.Substring(1));
                    int StartFile = GetXByColumn(StartBit.Substring(0, 1));

                    byte BaseStartSquare = 0;
                    if (StartRank == 7)
                    {
                        BaseStartSquare = 8;
                    }
                    else if (StartRank == 6)
                    {
                        BaseStartSquare = 16;
                    }
                    else if (StartRank == 5)
                    {
                        BaseStartSquare = 24;
                    }
                    else if (StartRank == 4)
                    {
                        BaseStartSquare = 32;
                    }
                    else if (StartRank == 3)
                    {
                        BaseStartSquare = 40;
                    }
                    else if (StartRank == 2)
                    {
                        BaseStartSquare = 48;
                    }
                    else if (StartRank == 1)
                    {
                        BaseStartSquare = 56;
                    }

                    BaseStartSquare += (byte)StartFile;
                    M.From = BaseStartSquare;

                    int EndRank = Convert.ToInt32(EndBit.Substring(1, 1));
                    int EndFile = GetXByColumn(EndBit.Substring(0, 1));

                    byte BaseEndSquare = 0;
                    if (EndRank == 7)
                    {
                        BaseEndSquare = 8;
                    }
                    else if (EndRank == 6)
                    {
                        BaseEndSquare = 16;
                    }
                    else if (EndRank == 5)
                    {
                        BaseEndSquare = 24;
                    }
                    else if (EndRank == 4)
                    {
                        BaseEndSquare = 32;
                    }
                    else if (EndRank == 3)
                    {
                        BaseEndSquare = 40;
                    }
                    else if (EndRank == 2)
                    {
                        BaseEndSquare = 48;
                    }
                    else if (EndRank == 1)
                    {
                        BaseEndSquare = 56;
                    }

                    BaseEndSquare += (byte)EndFile;
                    M.To = BaseEndSquare;

                    if (GUIMoves[NN].ToUpper().EndsWith("Q"))
                    {
                        M.PromotionPiece = QUEEN;
                    }
                    else if (GUIMoves[NN].ToUpper().EndsWith("N"))
                    {
                        M.PromotionPiece = KNIGHT;
                    }
                    else if (GUIMoves[NN].ToUpper().EndsWith("R"))
                    {
                        M.PromotionPiece = ROOK;
                    }
                    else if (GUIMoves[NN].ToUpper().EndsWith("B"))
                    {
                        M.PromotionPiece = BISHOP;
                    }

                    if (GameBoard.Color[BaseEndSquare] != EMPTY)
                    {
                        M.IsCapture = true;
                    }
                    else if (BaseEndSquare == GameBoard.EnPasantCapSquare && GameBoard.Piece[BaseStartSquare] == PAWN)
                    {
                        M.IsCapture = true;
                    }

                    GameBoard.MakeMove(M, GameBoard.OnMove, false);
                    GameBoard.ResetUndoMoves();
                    SeenZobrist.Add(GameBoard.CurrentZobrist);

                }

            }

        }



        private void UCISendToGUI(string Value)
        {
            Writer.Write(Value + Convert.ToChar(10));
        }

    }
}
