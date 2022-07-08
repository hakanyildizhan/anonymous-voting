using VotingApp.Models;

namespace VotingApp.ConsoleHub
{
    public interface IVotingClient
    {
        Task BroadcastRoundPayload(RoundPayload payload);
        Task GetRoundPayloads(string payloads);
        Task BroadcastStateToVoters(State state);
        Task VoterIsReady(string voterId);
        Task BroadcastDomainParameters(DomainParameters parameters);
        Task BroadcastQuestion(string question);
        Task NotifyConnected(string voterId);
    }
}
