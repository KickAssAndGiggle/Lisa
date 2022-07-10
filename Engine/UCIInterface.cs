using static Lisa.Globals;
namespace Lisa
{
    public class UCIInterface
    {

        private Board _gameBoard;
        private Searcher _search;

        private int _timeInMils = 0;
        private int _oppTimeInMils = 0;
        private List<long> _seenZobrist = new();

        public void InitiateUCI()
        {

            bool exit = false;

            _search = new Searcher();
            _search.BestMoveSelected += Search_BestMoveSelected;
            _search.InfoUpdated += Search_InfoUpdated;
            _search.TablebaseHit += Search_TablebaseHit;

            StreamWriter optWriter = File.CreateText("C:\\PSOutput\\Options\\" + Guid.NewGuid().ToString());
            optWriter.WriteLine("=== OPTIONS ===");

            do
            {

                string command = "";

                do
                {

                    int streamVal = Console.In.Read();
                    if (streamVal != -1)
                    {
                        if (streamVal < 32)
                        {
                            break;
                        }
                        command += Convert.ToString(Convert.ToChar(streamVal));
                    }

                } while (true);

                if (command.StartsWith("uci"))
                {
                    UCIReportID();
                }
                else if (command.Contains("quit"))
                {
                    exit = true;
                }
                else if (command.Contains("isready"))
                {
                    if (_search.IsSearching)
                    {
                        CancelSearch();
                    }
                    ReconfigureAfterOptions();
                    Console.Out.Write("readyok" + Convert.ToChar(10));
                }
                else if (command.StartsWith("setoption"))
                {
                    ProcessOption(command, optWriter);
                }
                else if (command.Contains("new"))
                {

                    if (_search.IsSearching)
                    {
                        CancelSearch();
                    }

                    SetupGame("startpos");

                }
                else if (command.Contains("position"))
                {

                    if (_search.IsSearching)
                    {
                        CancelSearch();
                    }

                    string[] splits = command.Split(Convert.ToChar(" "));
                    string posString = splits[1];

                    if (posString.ToLower() == "fen")
                    {
                        posString = "";
                        int moveStartIndex = 0;
                        for (int nn = 2; nn < splits.Length; nn++)
                        {
                            if (splits[nn].ToLower() != "moves")
                            {
                                posString += splits[nn] + " ";
                            }
                            else
                            {
                                posString = posString.TrimEnd();
                                moveStartIndex = nn + 1;
                                break;
                            }
                        }

                        if (moveStartIndex > 0)
                        {
                            string[] moveARR = new string[splits.Length - moveStartIndex];
                            Array.Copy(splits, moveStartIndex, moveARR, 0, splits.Length - moveStartIndex);
                            SetupGame(posString, moveARR);
                        }
                        else
                        {
                            SetupGame(posString, null);
                        }
                    }
                    else if (posString.ToLower() == "startpos")
                    {
                        if (splits.Length > 2)
                        {
                            string[] moveARR = new string[splits.Length - 3];
                            Array.Copy(splits, 3, moveARR, 0, splits.Length - 3);
                            SetupGame(posString, moveARR);
                        }
                        else
                        {
                            SetupGame(posString, null);
                        }
                    }

                }
                else if (command.StartsWith("go"))
                {

                    if (optWriter != null)
                    {
                        optWriter.WriteLine("=== DONE ===");
                        optWriter.Close();
                        optWriter = null;
                    }

                    string[] splits = command.Split(Convert.ToChar(" "));

                    for (int nn = 0; nn < splits.Length; nn++)
                    {
                        string commandPart = splits[nn];
                        if (commandPart.ToLower() == "wtime" && _gameBoard.OnMove == WHITE)
                        {
                            string msString = splits[nn + 1];
                            _timeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "wtime" && _gameBoard.OnMove == BLACK)
                        {
                            string msString = splits[nn + 1];
                            _oppTimeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "btime" && _gameBoard.OnMove == BLACK)
                        {
                            string msString = splits[nn + 1];
                            _timeInMils = Convert.ToInt32(msString);
                        }
                        else if (commandPart.ToLower() == "btime" && _gameBoard.OnMove == WHITE)
                        {
                            string msString = splits[nn + 1];
                            _oppTimeInMils = Convert.ToInt32(msString);
                        }
                    }

                    Thread searchThread = new(new ThreadStart(SearchPosition));
                    searchThread.Priority = ThreadPriority.Highest;
                    searchThread.Start();

                }
                else if (command.StartsWith("stop"))
                {
                    CancelSearch();
                    UCIMoveMade(_search.BestMove);
                }

            } while (!exit);


        }


        private void ProcessOption(string optString, StreamWriter sw)
        {

            string[] splits = optString.Split(" ");
            string optName = ""; 
            string optValue = "";

            for (int nn = 0; nn < splits.Length; nn++)
            {
                if (splits[nn] == "name")
                {
                    optName = splits[nn + 1];
                }
                else if (splits[nn] == "value")
                {
                    optValue = splits[nn + 1];
                }
            }

            if (optName != "" && optValue != "")
            {
                switch (optName.ToUpper())
                {

                    case "KNIGHT_VALUE":

                        KNIGHT_VALUE = Convert.ToInt32(optValue);
                        sw.WriteLine("KNIGHT_VALUE = " + KNIGHT_VALUE.ToString());
                        break;

                    case "BISHOP_VALUE":

                        BISHOP_VALUE = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_VALUE = " + BISHOP_VALUE.ToString());
                        break;

                    case "ROOK_VALUE":

                        ROOK_VALUE = Convert.ToInt32(optValue);
                        sw.WriteLine("ROOK_VALUE = " + ROOK_VALUE.ToString());
                        break;

                    case "QUEEN_VALUE":

                        QUEEN_VALUE = Convert.ToInt32(optValue);
                        sw.WriteLine("QUEEN_VALUE = " + QUEEN_VALUE.ToString());
                        break;

                    case "BISHOP_PAIR_BONUS_VALUE":

                        BISHOP_PAIR_BONUS_VALUE = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_PAIR_BONUS_VALUE = " + BISHOP_PAIR_BONUS_VALUE.ToString());
                        break;

                    case "FLANK_BACKWARD_PAWN_PENALTY":

                        FLANK_BACKWARD_PAWN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("FLANK_BACKWARD_PAWN_PENALTY = " + FLANK_BACKWARD_PAWN_PENALTY.ToString());
                        break;

                    case "CENTER_BACKWARD_PAWN_PENALTY":

                        CENTER_BACKWARD_PAWN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("CENTER_BACKWARD_PAWN_PENALTY = " + CENTER_BACKWARD_PAWN_PENALTY.ToString());
                        break;

                    case "BISHOP_PAWN_COLOR_PENALTY":

                        BISHOP_PAWN_COLOR_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_PAWN_COLOR_PENALTY = " + BISHOP_PAWN_COLOR_PENALTY.ToString());
                        break;

                    case "ROOK_ON_SEVENTH_BONUS":

                        ROOK_ON_SEVENTH_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("ROOK_ON_SEVENTH_BONUS = " + ROOK_ON_SEVENTH_BONUS.ToString());
                        break;

                    case "KSIDE_CASTLE_BONUS":

                        KSIDE_CASTLE_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("KSIDE_CASTLE_BONUS = " + KSIDE_CASTLE_BONUS.ToString());
                        break;

                    case "KSIDE_CASTLE_RIGHTS":

                        KSIDE_CASTLE_RIGHTS = Convert.ToInt32(optValue);
                        sw.WriteLine("KSIDE_CASTLE_RIGHTS = " + KSIDE_CASTLE_RIGHTS.ToString());
                        break;

                    case "QSIDE_CASTLE_BONUS":

                        QSIDE_CASTLE_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("QSIDE_CASTLE_BONUS = " + QSIDE_CASTLE_BONUS.ToString());
                        break;

                    case "QSIDE_CASTLE_RIGHTS":

                        QSIDE_CASTLE_RIGHTS = Convert.ToInt32(optValue);
                        sw.WriteLine("QSIDE_CASTLE_RIGHTS = " + QSIDE_CASTLE_RIGHTS.ToString());
                        break;

                    case "DOUBLED_PAWN_PENALTY":

                        DOUBLED_PAWN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("DOUBLED_PAWN_PENALTY = " + DOUBLED_PAWN_PENALTY.ToString());
                        break;

                    case "DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY":

                        DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = " + DOUBLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY.ToString());
                        break;

                    case "TRIPLED_PAWN_PENALTY":

                        TRIPLED_PAWN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("TRIPLED_PAWN_PENALTY = " + TRIPLED_PAWN_PENALTY.ToString());
                        break;

                    case "TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY":

                        TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY = " + TRIPLED_PAWN_ON_CLOSED_FILE_EXTRA_PENALTY.ToString());
                        break;

                    case "EDGE_PAWN_ISOLATED_PENALTY":

                        EDGE_PAWN_ISOLATED_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("EDGE_PAWN_ISOLATED_PENALTY = " + EDGE_PAWN_ISOLATED_PENALTY.ToString());
                        break;

                    case "CENTER_PAWN_ISOLATED_PENALTY":

                        CENTER_PAWN_ISOLATED_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("CENTER_PAWN_ISOLATED_PENALTY = " + CENTER_PAWN_ISOLATED_PENALTY.ToString());
                        break;

                    case "ROOK_ON_SEMI_OPEN_FILE_BONUS":

                        ROOK_ON_SEMI_OPEN_FILE_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("ROOK_ON_SEMI_OPEN_FILE_BONUS = " + ROOK_ON_SEMI_OPEN_FILE_BONUS.ToString());
                        break;

                    case "ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS":

                        ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS = " + ROOK_ON_FULLY_OPEN_FILE_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_BONUS":

                        PASSED_PAWN_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("PASSED_PAWN_BONUS = " + PASSED_PAWN_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS":

                        PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS = " + PASSED_PAWN_HIGHLY_ADVANCED_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS":

                        PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS = " + PASSED_PAWN_SOMEWHAT_ADVANCED_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS":

                        PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS = " + PASSED_PAWN_LITTLE_ADVANCED_ADDITIONAL_BONUS.ToString());
                        break;

                    case "PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY":

                        PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY = " + PAWN_OPPOSITE_FLANK_TO_KING_COWARD_PENALTY.ToString());
                        break;

                    case "BISHOP_ATTACKS_KING_BONUS":

                        BISHOP_ATTACKS_KING_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_ATTACKS_KING_BONUS = " + BISHOP_ATTACKS_KING_BONUS.ToString());
                        break;

                    case "BISHOP_ATTACKS_QUEEN_BONUS":

                        BISHOP_ATTACKS_QUEEN_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_ATTACKS_QUEEN_BONUS = " + BISHOP_ATTACKS_QUEEN_BONUS.ToString());
                        break;

                    case "BISHOP_ATTACKS_ROOK_BONUS":

                        BISHOP_ATTACKS_ROOK_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_ATTACKS_ROOK_BONUS = " + BISHOP_ATTACKS_ROOK_BONUS.ToString());
                        break;

                    case "BISHOP_ATTACKS_KNIGHT_BONUS":

                        BISHOP_ATTACKS_KNIGHT_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_ATTACKS_KNIGHT_BONUS = " + BISHOP_ATTACKS_KNIGHT_BONUS.ToString());
                        break;

                    case "FIANCETTO_IS_GOOD_BONUS":

                        FIANCETTO_IS_GOOD_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("FIANCETTO_IS_GOOD_BONUS = " + FIANCETTO_IS_GOOD_BONUS.ToString());
                        break;

                    case "FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY":

                        FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY = " + FIANCETTO_WITHOUT_BISHOP_AND_KING_THAT_SIDE_ADDITIONAL_PENALTY.ToString());
                        break;

                    case "FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY":

                        FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY = " + FIANCETTO_WITHOUT_BISHOP_BUT_OPP_HAS_ADDITIONAL_PENALTY.ToString());
                        break;

                    case "FIANCETTO_WITHOUT_BISHOP_PENALTY":

                        FIANCETTO_WITHOUT_BISHOP_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("FIANCETTO_WITHOUT_BISHOP_PENALTY = " + FIANCETTO_WITHOUT_BISHOP_PENALTY.ToString());
                        break;

                    case "BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY":

                        BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY = " + BISHOP_CAN_BE_TRAPPED_ON_SEVENTH_PENALTY.ToString());
                        break;

                    case "BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY":

                        BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY = " + BISHOP_IS_TRAPPED_ON_SEVENTH_PENALTY.ToString());
                        break;

                    case "KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY":

                        KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY = " + KING_ADVANCED_EARLY_OPP_QUEEN_ABSENT_PENALTY.ToString());
                        break;

                    case "KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY":

                        KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY = " + KING_ADVANCED_EARLY_OPP_QUEEN_PRESENT_PENALTY.ToString());
                        break;

                    case "KING_STEPPED_UP_EARLY_PENALTY":

                        KING_STEPPED_UP_EARLY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STEPPED_UP_EARLY_PENALTY = " + KING_STEPPED_UP_EARLY_PENALTY.ToString());
                        break;

                    case "KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION":

                        KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION = " + KING_STEPPED_UP_ADJACENT_PAWN_MITIGATION.ToString());
                        break;

                    case "KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION":

                        KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION = " + KING_STEPPED_UP_DIAGONAL_PAWN_SHIELD_MITIGATION.ToString());
                        break;

                    case "KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION":

                        KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION = " + KING_STEPPED_UP_PAWN_SHIELD_ADVANCED_MITIGATION.ToString());
                        break;

                    case "KING_STEPPED_UP_PAWN_SHIELD_MITIGATION":

                        KING_STEPPED_UP_PAWN_SHIELD_MITIGATION = Convert.ToInt32(optValue);
                        sw.WriteLine("KNIGHT_VALUE = " + KING_STEPPED_UP_PAWN_SHIELD_MITIGATION.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_BONUS.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KNIGHT_VALUE.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_HAS_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_NO_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_BONUS.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_HAS_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_BISHOP_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_ROOK_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_DIAG_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_QUEEN_VERTICALLY_SEMI_OPEN_PENALTY.ToString());
                        break;

                    case "KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY":

                        KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY = " + KING_STAYED_BACK_NO_DIAG_PAWN_SHIELD_ATTACKED_BY_KNIGHT_PENALTY.ToString());
                        break;

                    case "OPENING_MOBILITY_PER_MOVE_BONUS":

                        OPENING_MOBILITY_PER_MOVE_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("OPENING_MOBILITY_PER_MOVE_BONUS = " + OPENING_MOBILITY_PER_MOVE_BONUS.ToString());
                        break;

                    case "OPENING_MINOR_PIECE_INFLUENCES_CENTER":

                        OPENING_MINOR_PIECE_INFLUENCES_CENTER = Convert.ToInt32(optValue);
                        sw.WriteLine("OPENING_MINOR_PIECE_INFLUENCES_CENTER = " + OPENING_MINOR_PIECE_INFLUENCES_CENTER.ToString());
                        break;

                    case "ROOK_REDUNDANCY_PENALTY":

                        ROOK_REDUNDANCY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("ROOK_REDUNDANCY_PENALTY = " + ROOK_REDUNDANCY_PENALTY.ToString());
                        break;

                    case "KNIGHT_REDUNDANCY_PENALTY":

                        KNIGHT_REDUNDANCY_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KNIGHT_REDUNDANCY_PENALTY = " + KNIGHT_REDUNDANCY_PENALTY.ToString());
                        break;

                    case "SEMI_OPEN_FILE_TWO_ROOKS_BONUS":

                        SEMI_OPEN_FILE_TWO_ROOKS_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("SEMI_OPEN_FILE_TWO_ROOKS_BONUS = " + SEMI_OPEN_FILE_TWO_ROOKS_BONUS.ToString());
                        break;

                    case "SEMI_OPEN_FILE_ONE_ROOK_BONUS":

                        SEMI_OPEN_FILE_ONE_ROOK_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("SEMI_OPEN_FILE_ONE_ROOK_BONUS = " + SEMI_OPEN_FILE_ONE_ROOK_BONUS.ToString());
                        break;

                    case "TRAPPED_ROOK_PENALTY":

                        TRAPPED_ROOK_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("TRAPPED_ROOK_PENALTY = " + TRAPPED_ROOK_PENALTY.ToString());
                        break;

                    case "TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION":

                        TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = Convert.ToInt32(optValue);
                        sw.WriteLine("TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION = " + TRAPPED_ROOK_PENALTY_B_PAWN_MITIGATION.ToString());
                        break;

                    case "TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION":

                        TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = Convert.ToInt32(optValue);
                        sw.WriteLine("TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION = " + TRAPPED_ROOK_PENALTY_C_PAWN_MITIGATION.ToString());
                        break;

                    case "BLOCKADING_PASSED_PAWN_BONUS":

                        BLOCKADING_PASSED_PAWN_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("BLOCKADING_PASSED_PAWN_BONUS = " + BLOCKADING_PASSED_PAWN_BONUS.ToString());
                        break;

                    case "KNIGHT_OUTPOST_MINOR_BONUS":

                        KNIGHT_OUTPOST_MINOR_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("KNIGHT_OUTPOST_MINOR_BONUS = " + KNIGHT_OUTPOST_MINOR_BONUS.ToString());
                        break;

                    case "KNIGHT_OUTPOST_MAJOR_BONUS":

                        KNIGHT_OUTPOST_MAJOR_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("KNIGHT_OUTPOST_MAJOR_BONUS = " + KNIGHT_OUTPOST_MAJOR_BONUS.ToString());
                        break;

                    case "PAWN_CHAIN_BONUS":

                        PAWN_CHAIN_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("PAWN_CHAIN_BONUS = " + PAWN_CHAIN_BONUS.ToString());
                        break;

                    case "KNIGHT_ON_THE_RIM_IS_DIM_PENALTY":

                        KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = Convert.ToInt32(optValue);
                        sw.WriteLine("KNIGHT_ON_THE_RIM_IS_DIM_PENALTY = " + KNIGHT_ON_THE_RIM_IS_DIM_PENALTY.ToString());
                        break;

                    case "TEMPO_BONUS":

                        TEMPO_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("TEMPO_BONUS = " + TEMPO_BONUS.ToString());
                        break;

                    case "CONNECTED_PASSED_PAWN_BONUS":

                        CONNECTED_PASSED_PAWN_BONUS = Convert.ToInt32(optValue);
                        sw.WriteLine("CONNECTED_PASSED_PAWN_BONUS = " + CONNECTED_PASSED_PAWN_BONUS.ToString());
                        break;

                    case "DEPTH6_FUTILITY_MARGIN":

                        DEPTH6_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH6_FUTILITY_MARGIN = " + DEPTH6_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH5_FUTILITY_MARGIN":

                        DEPTH5_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH5_FUTILITY_MARGIN = " + DEPTH5_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH4_REVERSE_FUTILITY_MARGIN":

                        DEPTH4_REVERSE_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH4_REVERSE_FUTILITY_MARGIN = " + DEPTH4_REVERSE_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH4_FUTILITY_MARGIN":

                        DEPTH4_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH4_FUTILITY_MARGIN = " + DEPTH4_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH3_REVERSE_FUTILITY_MARGIN":

                        DEPTH3_REVERSE_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH3_REVERSE_FUTILITY_MARGIN = " + DEPTH3_REVERSE_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH3_FUTILITY_MARGIN":

                        DEPTH3_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH3_FUTILITY_MARGIN = " + DEPTH3_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH2_REVERSE_FUTILITY_MARGIN":

                        DEPTH2_REVERSE_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH2_REVERSE_FUTILITY_MARGIN = " + DEPTH2_REVERSE_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH2_FUTILITY_MARGIN":

                        DEPTH2_FUTILITY_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH2_FUTILITY_MARGIN = " + DEPTH2_FUTILITY_MARGIN.ToString());
                        break;

                    case "DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF = " + DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF":

                        DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF = Convert.ToInt32(optValue);
                        sw.WriteLine("DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF = " + DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF.ToString());
                        break;

                    case "LAZY_EVAL_QUEENS_OFF_MARGIN":

                        LAZY_EVAL_QUEENS_OFF_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("LAZY_EVAL_QUEENS_OFF_MARGIN = " + LAZY_EVAL_QUEENS_OFF_MARGIN.ToString());
                        break;

                    case "LAZY_EVAL_QUEENS_ON_MARGIN":

                        LAZY_EVAL_QUEENS_ON_MARGIN = Convert.ToInt32(optValue);
                        sw.WriteLine("LAZY_EVAL_QUEENS_ON_MARGIN = " + LAZY_EVAL_QUEENS_ON_MARGIN.ToString());
                        break;

                }

            }


        }

        private void Search_TablebaseHit()
        {
            UCISendToGUI("bestmove " + _search.UCITablebaseHit);
        }

        private void CancelSearch()
        {
            _search.Cancel();
            do
            {
                Thread.Sleep(1);
            } while (!_search.HasCancelled);
            Thread.Sleep(100);
        }


        private void Search_InfoUpdated()
        {

            try
            {

                string infoString = "info seldepth " + _search.InfoCurrentSearchDepth.ToString() + " depth " + _search.InfoCurrentSearchDepth.ToString() + 
                    " time " + (_search.InfoSecondsUsed * 1000).ToString();
                infoString += " nodes " + _search.InfoNodesLookedAt.ToString() + " nps " + _search.InfoNodesPerSecond.ToString();

                Move[] pv = _search.BestPV;
                string pvString = "";
                
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
                infoString += " score cp " + _search.BestScore.ToString() + " pv " + pvString;
                UCISendToGUI(infoString);

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
                if (!_search.HasCancelled)
                {
                    UCIMoveMade(_search.BestMove);
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


        private void UCIMoveMade(Move made)
        {
            string moveString = ConvertMoveToString(made);
            UCISendToGUI("bestmove " + moveString);
        }


        private void SearchPosition()
        {
            _search.Search(ref _gameBoard, 20, _timeInMils, _oppTimeInMils, _seenZobrist);
        }


        private void SetupGame(string startPos, string[] guiMoves = null)
        {

            _gameBoard = new Board();
            if (startPos == "startpos")
            {
                _gameBoard.InitialiseFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            }
            else
            {
                _gameBoard.InitialiseFromFEN(startPos);
            }

            _seenZobrist.Clear();
            _seenZobrist.Add(_gameBoard.CurrentZobrist);
            if (guiMoves != null)
            {
                for (int NN = 0; NN <= guiMoves.Length - 1; NN++)
                {
                    
                    string startBit = guiMoves[NN].Substring(0, 2);
                    string endBit = guiMoves[NN].Substring(2);

                    int startRank = Convert.ToInt32(startBit.Substring(1));
                    int startFile = GetXByColumn(startBit.Substring(0, 1));

                    byte baseStartSquare = 0;
                    if (startRank == 7)
                    {
                        baseStartSquare = 8;
                    }
                    else if (startRank == 6)
                    {
                        baseStartSquare = 16;
                    }
                    else if (startRank == 5)
                    {
                        baseStartSquare = 24;
                    }
                    else if (startRank == 4)
                    {
                        baseStartSquare = 32;
                    }
                    else if (startRank == 3)
                    {
                        baseStartSquare = 40;
                    }
                    else if (startRank == 2)
                    {
                        baseStartSquare = 48;
                    }
                    else if (startRank == 1)
                    {
                        baseStartSquare = 56;
                    }

                    baseStartSquare += (byte)startFile;

                    int endRank = Convert.ToInt32(endBit.Substring(1, 1));
                    int endFile = GetXByColumn(endBit.Substring(0, 1));

                    byte baseEndSquare = 0;
                    if (endRank == 7)
                    {
                        baseEndSquare = 8;
                    }
                    else if (endRank == 6)
                    {
                        baseEndSquare = 16;
                    }
                    else if (endRank == 5)
                    {
                        baseEndSquare = 24;
                    }
                    else if (endRank == 4)
                    {
                        baseEndSquare = 32;
                    }
                    else if (endRank == 3)
                    {
                        baseEndSquare = 40;
                    }
                    else if (endRank == 2)
                    {
                        baseEndSquare = 48;
                    }
                    else if (endRank == 1)
                    {
                        baseEndSquare = 56;
                    }

                    baseEndSquare += (byte)endFile;

                    Move mv = new()
                    {
                        From = baseStartSquare,
                        To = baseEndSquare
                    };

                    if (guiMoves[NN].ToUpper().EndsWith("Q"))
                    {
                        mv.PromotionPiece = QUEEN;
                    }
                    else if (guiMoves[NN].ToUpper().EndsWith("N"))
                    {
                        mv.PromotionPiece = KNIGHT;
                    }
                    else if (guiMoves[NN].ToUpper().EndsWith("R"))
                    {
                        mv.PromotionPiece = ROOK;
                    }
                    else if (guiMoves[NN].ToUpper().EndsWith("B"))
                    {
                        mv.PromotionPiece = BISHOP;
                    }

                    if (_gameBoard.Color[baseEndSquare] != EMPTY)
                    {
                        mv.IsCapture = true;
                    }
                    else if (baseEndSquare == _gameBoard.EnPasantCapSquare && _gameBoard.Piece[baseStartSquare] == PAWN)
                    {
                        mv.IsCapture = true;
                    }

                    _gameBoard.MakeMove(mv, _gameBoard.OnMove, false);
                    _gameBoard.ResetUndoMoves();
                    _seenZobrist.Add(_gameBoard.CurrentZobrist);

                }

            }

        }



        private void UCISendToGUI(string Value)
        {
            Console.Out.Write(Value + Convert.ToChar(10));
        }

    }
}
