using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using VotingApp.Data;
using VotingApp.Models;

namespace VotingApp.ConsoleHub
{
    public class VotingHub : Hub<IVotingClient>
    {
        private static bool _started;

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine("A new client has connected.");
            await base.OnConnectedAsync();
        }

        public Task BroadcastRoundPayload(RoundPayload payload)
        {
            DataStore.AddVoterPayload(payload);
            Console.WriteLine($"Got the payload from voter {payload.VoterId}");
            return Task.CompletedTask;
        }

        public async Task BroadcastQuestion(string question)
        {
            await Clients.All.BroadcastQuestion(question);
        }

        public async Task NotifyConnected(string voterId)
        {
            if (_started)
            {
                await Clients.Caller.BroadcastStateToVoters(State.AlreadyStarted);
                return;
            }

            Console.WriteLine($"Voter with ID {voterId} is registered.");
            DataStore.AddVoter(voterId, ClientState.Busy);
            await Clients.Caller.BroadcastStateToVoters(State.WaitingToCommence);

            if (SessionIsStarted())
            {
                _started = true;
                await AdvanceToNextStage();
            }
        }

        private async Task AdvanceToNextStage()
        {
            State currentState = DataStore.GetCurrentState();
            Console.WriteLine($"State change: {currentState} > {++currentState}");
            DataStore.SaveCurrentState(currentState);
            switch (currentState)
            {
                case State.WaitingToCommence:
                    await Clients.All.BroadcastStateToVoters(currentState);
                    break;
                case State.DistributingDomainParameters:
                    await Clients.All.BroadcastStateToVoters(currentState);
                    await DistributeDomainParameters();
                    SetAllVoterStates(ClientState.Busy);
                    break;
                case State.Round1:
                    await Clients.All.BroadcastStateToVoters(currentState);
                    SetAllVoterStates(ClientState.Busy);
                    break;
                case State.Round1PayloadBroadcast:
                    SetAllVoterStates(ClientState.Busy);
                    await Clients.All.BroadcastStateToVoters(currentState);
                    break;
                case State.Round1GetPayloads:
                    var payloads = DataStore.GetVoterPayloads();
                    if (payloads != null && payloads.Any())
                    {
                        Console.WriteLine($"{payloads.Count} payloads are being sent to voters");
                        SetAllVoterStates(ClientState.Busy);
                        await Clients.All.GetRoundPayloads(JsonConvert.SerializeObject(payloads));
                    }

                    break;
                case State.Round1ZKPCheck:
                    SetAllVoterStates(ClientState.Busy);
                    await Clients.All.BroadcastStateToVoters(currentState);
                    break;
            }
        }

        private void SetAllVoterStates(ClientState state)
        {
            DataStore.SetAllVoterStates(state);
        }

        public async Task VoterIsReady(string voterId)
        {
            DataStore.SetVoterState(voterId, ClientState.Ready);

            if (AreAllVotersReady())
            {
                Console.WriteLine("Advancing to next stage");
                DataStore.SetVoterState(voterId, ClientState.Busy);
                await AdvanceToNextStage();
            }
        }

        private bool AreAllVotersReady()
        {
            return DataStore.AreAllVotersReady();
        }

        private bool SessionIsStarted()
        {
            var voterCount = DataStore.GetVoterCount();
            Console.WriteLine($"There are currently {voterCount} voters");
            return voterCount == 3;
        }

        private async Task DistributeDomainParameters()
        {
            DerObjectIdentifier ecParam = X9ObjectIdentifiers.Prime256v1;
            ECKeyPairGenerator keyGenerator = new ECKeyPairGenerator();
            var parameters = new ECKeyGenerationParameters(ecParam, new SecureRandom());
            await Clients.All.BroadcastDomainParameters(new DomainParameters()
            {
                Q = parameters.DomainParameters.Curve.Field.Characteristic,
                A = parameters.DomainParameters.Curve.A.ToBigInteger(),
                B = parameters.DomainParameters.Curve.B.ToBigInteger(),
                Gx = parameters.DomainParameters.G.AffineXCoord.ToBigInteger(),
                Gy = parameters.DomainParameters.G.AffineYCoord.ToBigInteger(),
                P = parameters.DomainParameters.Curve.Order,
                N = parameters.DomainParameters.N,
                Cofactor = parameters.DomainParameters.Curve.Cofactor
            });
        }
    }
}
