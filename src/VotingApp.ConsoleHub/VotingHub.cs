using Microsoft.AspNetCore.SignalR;
using VotingApp.Models;

namespace VotingApp.ConsoleHub
{
    public class VotingHub : Hub<IVotingClient>
    {
        public void BroadcastRoundPayload(RoundPayload payload)
        {
            Clients.All.BroadcastRoundPayload(payload);
        }

        public void BroadcastState(State state)
        {
            Clients.All.BroadcastState(state);
        }

        public void BroadcastDomainParameters(DomainParameters parameters)
        {
            Clients.All.BroadcastDomainParameters(parameters);
        }

        public void BroadcastQuestion(string question)
        {
            Clients.All.BroadcastQuestion(question);
        }
    }
}
