using VotingApp.Models;

namespace VotingApp.ConsoleHub
{
    public interface IVotingClient
    {
        Task BroadcastRoundPayload(RoundPayload voter);
        Task BroadcastState(State state);
        Task BroadcastDomainParameters(DomainParameters parameters);
        Task BroadcastQuestion(string question);
        Task NotifyConnected(string voterId);
    }
}
