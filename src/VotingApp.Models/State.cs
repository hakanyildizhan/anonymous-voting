namespace VotingApp.Models
{
    public enum State
    {
        WaitingToCommence,
        DistributingDomainParameters,
        Round1,
        Round1ZKPBroadcast,
        YiCalculation,
        Round2,
        Round2ZKPBroadcast,
        VotingResultCalculation,
        Finished,
        AlreadyStarted
    }
}
