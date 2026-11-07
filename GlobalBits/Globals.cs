using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lisa
{
    public static partial class Globals
    {

        public static bool USE_TABLEBASE;

        public static OpeningBook OPENING_BOOK;

        public const byte TT_POS_TYPE_EXACT = 0;
        public const byte TT_POS_TYPE_ALPHA_NOT_BEATEN = 1;
        public const byte TT_POS_TYPE_BETA_CUT = 2;

        public const byte EMPTY = 0;
        public const byte WHITE = 1;
        public const byte BLACK = 2;

        public const byte PAWN = 0;
        public const byte KNIGHT = 1;
        public const byte BISHOP = 2;
        public const byte ROOK = 3;
        public const byte QUEEN = 4;
        public const byte KING = 5;

        public const byte DARKSQUARE = 0;
        public const byte LIGHTSQUARE = 1;

        public static ProgramMode RUN_MODE = ProgramMode.Unknown;

        public static string PERFT_FEN = "";
        public static int PERFT_DEPTH = 0;
        public static byte MULTI_FEN_DEPTH = 0;
        public static string OUTPUT_FILE = "";
        public static string[]? FENS_TO_TEST;
        public static string EPD_INPUT = "";
        public static string EPD_OUTPUT = "";

        public static string TEXEL_PGN_INPUT = "";
        public static string TEXEL_EPD_OUTPUT = "";
        public static int TEXEL_POSITIONS_PER_GAME = 10;
        public static int TEXEL_SKIP_PLIES = 16;
        public static int TEXEL_MAX_GAMES = 100000;
        public static int TEXEL_GAME_STRIDE = 25;

        public static string TEXEL_TUNE_INPUT = "";
        public static string TEXEL_TUNE_RESULTS = "";
        public static int TEXEL_TUNE_MAX_POSITIONS = 0;
        public static int TEXEL_TUNE_THREADS = 0;
        public static string TEXEL_PST_RESULTS = "";
        public static int TEXEL_PST_EPOCHS = 2000;

        public static string BOOK_PGN_INPUT = "";
        public static string BOOK_OUTPUT = "";
        public static int BOOK_MAX_PLIES = 16;
        public static int BOOK_MIN_GAMES = 50;
        public static int BOOK_MIN_SCORE_PERCENT = 40;
        public static string BOOK_FIRST_MOVES = "e4,d4,c4,Nf3,b3";

        public static int TT_HASH_SIZE_MB = 128;
        public static int PAWN_STRUCTURE_HASH_SIZE_MB = 64;
        public static int POSITION_SCORE_HASH_SIZE_MB = 64;

        //Search threads, the main search included (Lazy SMP). Threads= in Settings.ini, or the UCI Threads option
        public static int SEARCH_THREADS = 4;

        private static string _bookFile = "";

        static Globals()
        {

            LoadSettings();

            MATERIAL = Array.Empty<int>();
            SEE_MATERIAL = Array.Empty<int>();
            KING_ATTACK_UNITS = Array.Empty<int>();
            KING_DEFENCE_UNITS = Array.Empty<int>();

            OPENING_BOOK = new OpeningBook(_bookFile);

        }


        private static void LoadSettings()
        {

            string AppFolder = AppDomain.CurrentDomain.BaseDirectory;
            if (!AppFolder.EndsWith("\\"))
            {
                AppFolder += "\\";
            }

            string SettingsFile = AppFolder + "Settings.ini";
            if (!File.Exists(SettingsFile))
            {
                SettingsFile = AppFolder + "GlobalBits\\Settings.ini";
            }

            if (File.Exists(AppFolder + "Book.ini"))
            {
                _bookFile = AppFolder + "Book.ini";
            }
            else if (File.Exists(AppFolder + "GlobalBits\\Book.ini"))
            {
                _bookFile = AppFolder + "GlobalBits\\Book.ini";
            }

            List<string> MultiFenList = new();
            if (File.Exists(SettingsFile))
            {

                string[] Lines = File.ReadAllLines(SettingsFile);
                foreach (string Line in Lines)
                {
                    string L = Line.Trim();
                    if (!L.StartsWith("#"))
                    {
                        if (L.ToLower().StartsWith("mode="))
                        {
                            if (L.ToLower().Contains("=uci"))
                            {
                                RUN_MODE = ProgramMode.UCI;
                            }
                            else if (L.ToLower().Contains("=perftfen"))
                            {
                                RUN_MODE = ProgramMode.Perft;
                            }
                            else if (L.ToLower().Contains("=multifen"))
                            {
                                RUN_MODE = ProgramMode.MultiFen;
                            }
                            else if (L.ToLower().Contains("=fentozobrist"))
                            {
                                RUN_MODE = ProgramMode.FenToZobrist;
                            }
                            else if (L.ToLower().Contains("=epd"))
                            {
                                RUN_MODE = ProgramMode.EPD;
                            }
                            else if (L.ToLower().Contains("=maketexelfile"))
                            {
                                RUN_MODE = ProgramMode.MakeTexelFile;
                            }
                            else if (L.ToLower().Contains("=makebook"))
                            {
                                RUN_MODE = ProgramMode.MakeBook;
                            }
                            else if (L.ToLower().Contains("=texeltunepst"))
                            {
                                RUN_MODE = ProgramMode.TexelTunePST;
                            }
                            else if (L.ToLower().Contains("=texeltune"))
                            {
                                RUN_MODE = ProgramMode.TexelTune;
                            }
                        }
                        else if (L.ToLower().StartsWith("usetablebase"))
                        {
                            USE_TABLEBASE = (L.ToLower().Contains("true"));
                        }
                    }
                }

                //Mode-specific keys: each is read only when Mode is one of those listed
                SettingKey[] modeKeys =
                {
                    new("fen=", new[] { ProgramMode.Perft, ProgramMode.FenToZobrist }, value => PERFT_FEN = value),
                    new("perftdepth=", new[] { ProgramMode.Perft }, value => PERFT_DEPTH = Convert.ToInt32(value)),
                    new("multifen_", new[] { ProgramMode.MultiFen }, value => MultiFenList.Add(value)),
                    new("threads=", new[] { ProgramMode.UCI, ProgramMode.MultiFen, ProgramMode.EPD },
                        value => SEARCH_THREADS = Math.Max(1, Convert.ToInt32(value))),
                    new("multifendepth=", new[] { ProgramMode.MultiFen, ProgramMode.EPD },
                        value => MULTI_FEN_DEPTH = Convert.ToByte(value)),
                    new("epdinputfile=", new[] { ProgramMode.EPD }, value => EPD_INPUT = value),
                    new("epdoutputfile=", new[] { ProgramMode.EPD }, value => EPD_OUTPUT = value),
                    new("texelpgninput=", new[] { ProgramMode.MakeTexelFile }, value => TEXEL_PGN_INPUT = value),
                    new("texelepdoutput=", new[] { ProgramMode.MakeTexelFile }, value => TEXEL_EPD_OUTPUT = value),
                    new("texelpositionspergame=", new[] { ProgramMode.MakeTexelFile },
                        value => TEXEL_POSITIONS_PER_GAME = Convert.ToInt32(value)),
                    new("texelskipplies=", new[] { ProgramMode.MakeTexelFile },
                        value => TEXEL_SKIP_PLIES = Convert.ToInt32(value)),
                    new("texelmaxgames=", new[] { ProgramMode.MakeTexelFile },
                        value => TEXEL_MAX_GAMES = Convert.ToInt32(value)),
                    new("texelgamestride=", new[] { ProgramMode.MakeTexelFile },
                        value => TEXEL_GAME_STRIDE = Convert.ToInt32(value)),
                    new("texeltuneinput=", new[] { ProgramMode.TexelTune, ProgramMode.TexelTunePST },
                        value => TEXEL_TUNE_INPUT = value),
                    new("texeltuneresults=", new[] { ProgramMode.TexelTune }, value => TEXEL_TUNE_RESULTS = value),
                    new("texeltunemaxpositions=", new[] { ProgramMode.TexelTune, ProgramMode.TexelTunePST },
                        value => TEXEL_TUNE_MAX_POSITIONS = Convert.ToInt32(value)),
                    new("texeltunethreads=", new[] { ProgramMode.TexelTune, ProgramMode.TexelTunePST },
                        value => TEXEL_TUNE_THREADS = Convert.ToInt32(value)),
                    new("texelpstresults=", new[] { ProgramMode.TexelTunePST }, value => TEXEL_PST_RESULTS = value),
                    new("texelpstepochs=", new[] { ProgramMode.TexelTunePST },
                        value => TEXEL_PST_EPOCHS = Convert.ToInt32(value)),
                    new("bookpgninput=", new[] { ProgramMode.MakeBook }, value => BOOK_PGN_INPUT = value),
                    new("bookoutput=", new[] { ProgramMode.MakeBook }, value => BOOK_OUTPUT = value),
                    new("bookmaxplies=", new[] { ProgramMode.MakeBook },
                        value => BOOK_MAX_PLIES = Convert.ToInt32(value)),
                    new("bookmingames=", new[] { ProgramMode.MakeBook },
                        value => BOOK_MIN_GAMES = Convert.ToInt32(value)),
                    new("bookminscorepercent=", new[] { ProgramMode.MakeBook },
                        value => BOOK_MIN_SCORE_PERCENT = Convert.ToInt32(value)),
                    new("bookfirstmoves=", new[] { ProgramMode.MakeBook }, value => BOOK_FIRST_MOVES = value)
                };

                foreach (string Line in Lines)
                {
                    string L = Line.Trim();
                    foreach (SettingKey key in modeKeys)
                    {
                        if (Array.IndexOf(key.Modes, RUN_MODE) >= 0 && L.ToLower().StartsWith(key.Prefix))
                        {
                            string[] Splits = L.Split(Convert.ToChar("="));
                            key.Apply(Splits[1]);
                        }
                    }
                    if (L.ToLower().Contains("outputfile") && !L.ToLower().Contains("epdoutputfile"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        OUTPUT_FILE = Splits[1];
                    }
                    if (L.ToLower().Contains("transtablehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        TT_HASH_SIZE_MB = Convert.ToInt32(Splits[1]);
                    }
                    if (L.ToLower().Contains("pawnstructurehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        PAWN_STRUCTURE_HASH_SIZE_MB = Convert.ToInt32(Splits[1]);
                    }
                    if (L.ToLower().Contains("positionscorehashsize"))
                    {
                        string[] Splits = L.Split(Convert.ToChar("="));
                        POSITION_SCORE_HASH_SIZE_MB = Convert.ToInt32(Splits[1]);
                    }
                }
            }

            FENS_TO_TEST = MultiFenList.ToArray();

        }


        //A Settings.ini key: a line starting with Prefix (any case) has the text after its "=" passed to Apply
        private sealed class SettingKey
        {
            public readonly string Prefix;
            public readonly ProgramMode[] Modes;
            public readonly Action<string> Apply;

            public SettingKey(string prefix, ProgramMode[] modes, Action<string> apply)
            {
                Prefix = prefix;
                Modes = modes;
                Apply = apply;
            }
        }


        #region Helper functions used in many places


        /// <summary>
        /// Gets numeric column by its letter
        /// </summary>
        public static int GetXByColumn(string Col)
        {

            switch (Col.ToLower())
            {
                case "a":
                    return 0;
                case "b":
                    return 1;
                case "c":
                    return 2;
                case "d":
                    return 3;
                case "e":
                    return 4;
                case "f":
                    return 5;
                case "g":
                    return 6;
                case "h":
                    return 7;
                default:
                    break;
            }

            return -1;

        }


        public static string ConvertMoveToAlgebraic(Move Made, ref Board theBoard)
        {


            string PieceOnSquare = ""; string ToRank = ""; string ToFile = ""; bool IsEP = false;

            if (theBoard.Piece[Made.From] == KNIGHT)
            {
                PieceOnSquare = "N";
            }
            else if (theBoard.Piece[Made.From] == BISHOP)
            {
                PieceOnSquare = "B";
            }
            else if (theBoard.Piece[Made.From] == ROOK)
            {
                PieceOnSquare = "R";
            }
            else if (theBoard.Piece[Made.From] == QUEEN)
            {
                PieceOnSquare = "Q";
            }
            else if (theBoard.Piece[Made.From] == KING)
            {
                if (Made.To == Made.From - 2)
                {
                    return "O-O-O";
                }
                else if (Made.To == Made.From + 2)
                {
                    return "O-O";
                }
                else
                {
                    PieceOnSquare = "K";
                }
            }
            else if (theBoard.Piece[Made.From] == PAWN && Made.IsCapture)
            {
                if (Made.From == 0 || Made.From % 8 == 0)
                {
                    PieceOnSquare = "a";
                }
                else if (Made.From == 1 || Made.From % 8 == 1)
                {
                    PieceOnSquare = "b";
                }
                else if (Made.From == 2 || Made.From % 8 == 2)
                {
                    PieceOnSquare = "c";
                }
                else if (Made.From == 3 || Made.From % 8 == 3)
                {
                    PieceOnSquare = "d";
                }
                else if (Made.From == 4 || Made.From % 8 == 4)
                {
                    PieceOnSquare = "e";
                }
                else if (Made.From == 5 || Made.From % 8 == 5)
                {
                    PieceOnSquare = "f";
                }
                else if (Made.From == 6 || Made.From % 8 == 6)
                {
                    PieceOnSquare = "g";
                }
                else if (Made.From == 7 || Made.From % 8 == 7)
                {
                    PieceOnSquare = "h";
                }
                if (Made.To == theBoard.EnPasantCapSquare)
                {
                    IsEP = true;
                }
            }


            if (Made.To == 0 || Made.To % 8 == 0)
            {
                ToFile = "a";
            }
            else if (Made.To == 1 || Made.To % 8 == 1)
            {
                ToFile = "b";
            }
            else if (Made.To == 2 || Made.To % 8 == 2)
            {
                ToFile = "c";
            }
            else if (Made.To == 3 || Made.To % 8 == 3)
            {
                ToFile = "d";
            }
            else if (Made.To == 4 || Made.To % 8 == 4)
            {
                ToFile = "e";
            }
            else if (Made.To == 5 || Made.To % 8 == 5)
            {
                ToFile = "f";
            }
            else if (Made.To == 6 || Made.To % 8 == 6)
            {
                ToFile = "g";
            }
            else if (Made.To == 7 || Made.To % 8 == 7)
            {
                ToFile = "h";
            }

            if (Made.To <= 7)
            {
                ToRank = "8";
            }
            else if (Made.To >= 8 && Made.To <= 15)
            {
                ToRank = "7";
            }
            else if (Made.To >= 16 && Made.To <= 23)
            {
                ToRank = "6";
            }
            else if (Made.To >= 24 && Made.To <= 31)
            {
                ToRank = "5";
            }
            else if (Made.To >= 32 && Made.To <= 39)
            {
                ToRank = "4";
            }
            else if (Made.To >= 40 && Made.To <= 47)
            {
                ToRank = "3";
            }
            else if (Made.To >= 48 && Made.To <= 55)
            {
                ToRank = "2";
            }
            else if (Made.To >= 56 && Made.To <= 63)
            {
                ToRank = "1";
            }

            string Ret = PieceOnSquare + (Made.IsCapture ? "x" : "") + ToFile + ToRank + (IsEP ? "ep" : "");
            return Ret;

        }


        public static string ConvertMoveToString(Move Made)
        {

            string RetVal;
            string FromFile = "";
            string ToFile = "";

            if (Made.From == 0 || Made.From % 8 == 0)
            {
                FromFile = "a";
            }
            else if (Made.From == 1 || Made.From % 8 == 1)
            {
                FromFile = "b";
            }
            else if (Made.From == 2 || Made.From % 8 == 2)
            {
                FromFile = "c";
            }
            else if (Made.From == 3 || Made.From % 8 == 3)
            {
                FromFile = "d";
            }
            else if (Made.From == 4 || Made.From % 8 == 4)
            {
                FromFile = "e";
            }
            else if (Made.From == 5 || Made.From % 8 == 5)
            {
                FromFile = "f";
            }
            else if (Made.From == 6 || Made.From % 8 == 6)
            {
                FromFile = "g";
            }
            else if (Made.From == 7 || Made.From % 8 == 7)
            {
                FromFile = "h";
            }

            if (Made.To == 0 || Made.To % 8 == 0)
            {
                ToFile = "a";
            }
            else if (Made.To == 1 || Made.To % 8 == 1)
            {
                ToFile = "b";
            }
            else if (Made.To == 2 || Made.To % 8 == 2)
            {
                ToFile = "c";
            }
            else if (Made.To == 3 || Made.To % 8 == 3)
            {
                ToFile = "d";
            }
            else if (Made.To == 4 || Made.To % 8 == 4)
            {
                ToFile = "e";
            }
            else if (Made.To == 5 || Made.To % 8 == 5)
            {
                ToFile = "f";
            }
            else if (Made.To == 6 || Made.To % 8 == 6)
            {
                ToFile = "g";
            }
            else if (Made.To == 7 || Made.To % 8 == 7)
            {
                ToFile = "h";
            }

            string FromRank = ""; string ToRank = "";

            if (Made.From <= 7)
            {
                FromRank = "8";
            }
            else if (Made.From >= 8 && Made.From <= 15)
            {
                FromRank = "7";
            }
            else if (Made.From >= 16 && Made.From <= 23)
            {
                FromRank = "6";
            }
            else if (Made.From >= 24 && Made.From <= 31)
            {
                FromRank = "5";
            }
            else if (Made.From >= 32 && Made.From <= 39)
            {
                FromRank = "4";
            }
            else if (Made.From >= 40 && Made.From <= 47)
            {
                FromRank = "3";
            }
            else if (Made.From >= 48 && Made.From <= 55)
            {
                FromRank = "2";
            }
            else if (Made.From >= 56 && Made.From <= 63)
            {
                FromRank = "1";
            }

            if (Made.To <= 7)
            {
                ToRank = "8";
            }
            else if (Made.To >= 8 && Made.To <= 15)
            {
                ToRank = "7";
            }
            else if (Made.To >= 16 && Made.To <= 23)
            {
                ToRank = "6";
            }
            else if (Made.To >= 24 && Made.To <= 31)
            {
                ToRank = "5";
            }
            else if (Made.To >= 32 && Made.To <= 39)
            {
                ToRank = "4";
            }
            else if (Made.To >= 40 && Made.To <= 47)
            {
                ToRank = "3";
            }
            else if (Made.To >= 48 && Made.To <= 55)
            {
                ToRank = "2";
            }
            else if (Made.To >= 56 && Made.To <= 63)
            {
                ToRank = "1";
            }

            RetVal = FromFile + FromRank + ToFile + ToRank;

            if (Made.PromotionPiece == KNIGHT)
            {
                RetVal += "n";
            }
            else if (Made.PromotionPiece == BISHOP)
            {
                RetVal += "b";
            }
            else if (Made.PromotionPiece == ROOK)
            {
                RetVal += "r";
            }
            else if (Made.PromotionPiece == QUEEN)
            {
                RetVal += "q";
            }

            return RetVal;

        }


        #endregion


    }
}
