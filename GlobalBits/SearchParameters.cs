namespace Lisa
{
    public static partial class Globals
    {

        #region Search parameters

        public static int DEPTH2_FUTILITY_MARGIN = 155;
        public static int DEPTH3_FUTILITY_MARGIN = 375;
        public static int DEPTH4_FUTILITY_MARGIN = 605;
        public static int DEPTH2_REVERSE_FUTILITY_MARGIN = 110;
        public static int DEPTH3_REVERSE_FUTILITY_MARGIN = 270;
        public static int DEPTH4_REVERSE_FUTILITY_MARGIN = 475;

        public static int DEPTH5_FUTILITY_MARGIN = 670;
        public static int DEPTH6_FUTILITY_MARGIN = 930;
        public static int DEPTH5_REVERSE_FUTILITY_MARGIN = 600;
        public static int DEPTH6_REVERSE_FUTILITY_MARGIN = 800;

        public static int DEPTH6_EARLY_SELECTIVITY_SCORE_CUTOFF = 370;
        public static int DEPTH7_EARLY_SELECTIVITY_SCORE_CUTOFF = 360;
        public static int DEPTH8_EARLY_SELECTIVITY_SCORE_CUTOFF = 220;
        public static int DEPTH9_EARLY_SELECTIVITY_SCORE_CUTOFF = 175;
        public static int DEPTH10_EARLY_SELECTIVITY_SCORE_CUTOFF = 160;
        public static int DEPTH_OVER_10_EARLY_SELECTIVITY_SCORE_CUTOFF = 130;

        public static int DEPTH6_LATE_SELECTIVITY_SCORE_CUTOFF = 470;
        public static int DEPTH7_LATE_SELECTIVITY_SCORE_CUTOFF = 460;
        public static int DEPTH8_LATE_SELECTIVITY_SCORE_CUTOFF = 270;
        public static int DEPTH9_LATE_SELECTIVITY_SCORE_CUTOFF = 250;
        public static int DEPTH10_LATE_SELECTIVITY_SCORE_CUTOFF = 220;
        public static int DEPTH_OVER_10_LATE_SELECTIVITY_SCORE_CUTOFF = 190;

        public static int LAZY_EVAL_QUEENS_OFF_MARGIN = 1143;
        public static int LAZY_EVAL_QUEENS_ON_MARGIN = 1348;

        //Late move pruning: at a node this many plies from the leaves, quiet moves from this number on are skipped (0 =
        //off)
        public static int LMP_QUIET_MOVES_D2 = 0;
        public static int LMP_QUIET_MOVES_D3 = 8;
        public static int LMP_QUIET_MOVES_D4 = 8;
        public static int LMP_QUIET_MOVES_D5 = 8;

        #endregion

    }
}
