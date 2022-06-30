namespace VotingApp.Models
{
    public enum State
    {
        WaitingToCommence,
        DistributingDomainParameters,
        Round1,
        Round1ZKPBroadcast,
        YiCalculationInProgress,
        YiCalculationComplete,
        Round2,
        Round2ZKPBroadcast,
        VotingResultCalculationInProgress,
        VotingResultCalculationComplete,
        Finished
    }
}
