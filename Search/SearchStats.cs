namespace Lisa
{
    //The search's counters, read by the UCI interface and the testers. A struct, held as a field of the Searcher, so
    //the counters sit inside the Searcher itself and an increment in the search costs no more than a plain field would.
    //Must be created with new(), not default, or the arrays are null
    public struct SearchStats
    {

        public int SecondsUsed;
        public int NodesLookedAt;
        public int NodesPerSecond;
        public byte CurrentSearchDepth;
        public byte MaxSearchDepth;
        public int PVNodesFoundInTT;
        public int CutNodesFoundInTT;
        public int AllNodesFoundInTT;
        public int NullMoveCutOffs;
        public int NullMoveAttempts;
        public int ProbCutCutOffs;
        public int ProbCutAttempts;
        public int CutOffWithPVMoveOnly;
        public int NodesLookedAtWithoutQuiesce;
        public int NodesCutOffWithFirstSortedMove;
        public int NodesCutoffWithSecondSortedMove;
        public int NodesCutOffWithThirdSortedMove;
        public int NodesCutOffWithLaterSortedMove;
        public int CutOffUsingRefutation;
        public int CutOffOnlyUsingWinningCaps;
        public int CutOffOnlyUsingLosingCaps;
        public int NodesQuiesced;
        public int FutilityD4;
        public int FutilityD3;
        public int FutilityD2;
        public int ReverseFutilityD4;
        public int ReverseFutilityD3;
        public int ReverseFutilityD2;
        public int PVTTCutoffs;
        public int BetaTTCutoffs;
        public int AlphaTTCutoffs;
        public int LMRReSearches;
        public int LMRNotReduced;
        public int[] LMRReducedBy;
        public int LMRReSearchCut;
        public int LMRReSearchRaised;
        public int LMRReSearchFailed;
        public int LMRReSearchNodes;
        public int[] LMRReSearchCutBy;
        public int[] LMRReSearchRaisedBy;
        public int[] LMRReSearchFailedBy;
        public int[] LMPPruned;
        public int RefutationTried;
        public int KillerStageSkippedAtAll;
        public KillerStats KillerOne;
        public KillerStats KillerTwo;

        public SearchStats()
        {
            LMRReducedBy = new int[6];
            LMRReSearchCutBy = new int[6];
            LMRReSearchRaisedBy = new int[6];
            LMRReSearchFailedBy = new int[6];
            LMPPruned = new int[4];
        }

    }


    //Counters for one killer slot
    public struct KillerStats
    {
        public int Tried;
        public int CutOffs;
        public int AlphaRaised;
        public int Invalid;
        public int EmptyOrTT;
        public int SameAsRefutation;
        public int CaptureTried;
        public int CaptureCut;
        public int TriedAtAll;
        public int TriedAtUnknown;
        public int CutAtAll;
        public int CutAtUnknown;
        public int NodesCut;
        public int NodesFailed;
    }
}
