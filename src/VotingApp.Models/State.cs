namespace VotingApp.Models
{
    public enum State
    {
        WaitingToCommence,
        DistributingDomainParameters,
        Round1,
        Round1ZKPCheck,
        YiCalculation,
        Round2,
        Round2ZKPCheck,
        VotingResultCalculation,
        Finished,
        AlreadyStarted
    }
}
