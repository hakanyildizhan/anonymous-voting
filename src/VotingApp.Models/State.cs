namespace VotingApp.Models
{
    public enum State
    {
        WaitingToCommence,
        DistributingDomainParameters,
        Round1,
        Round1PayloadBroadcast,
        Round1GetPayloads,
        Round1ZKPCheck,
        Round2,
        Round2PayloadBroadcast,
        Round2GetPayloads,
        Round2ZKPCheck,
        VotingResultCalculation,
        Finished,
        AlreadyStarted
    }
}
