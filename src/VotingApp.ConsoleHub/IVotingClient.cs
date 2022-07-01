using VotingApp.Models;

namespace VotingApp.ConsoleHub
{
    public interface IVotingClient
    {
        void BroadcastRoundPayload(RoundPayload voter);
        void BroadcastState(State state);
        void BroadcastDomainParameters(DomainParameters parameters);
        void BroadcastQuestion(string question);
        void NotifyConnected(string voterId);
    }
}
