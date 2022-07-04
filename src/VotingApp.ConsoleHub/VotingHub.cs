using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Collections.Concurrent;
using VotingApp.Models;

namespace VotingApp.ConsoleHub
{
    public class VotingHub : Hub<IVotingClient>
    {
        private ConcurrentDictionary<string, ClientState> _voters = new ConcurrentDictionary<string, ClientState>();
        private ConcurrentBag<RoundPayload> _roundPayloads = new ConcurrentBag<RoundPayload>();
        private bool _started;
        private static State _currentState = State.WaitingToCommence;

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine("A new client has connected.");
            await base.OnConnectedAsync();
        }

        public async Task BroadcastRoundPayload(RoundPayload payload)
        {
            _voters[payload.VoterId] = ClientState.Ready;
            _roundPayloads.Add(payload);

            if (AreAllVotersReady())
            {
                await Clients.All.GetRoundPayloads(_roundPayloads.ToList());
            }
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
            _voters.TryAdd(voterId, ClientState.Ready);
            await Clients.Caller.BroadcastStateToVoters(State.WaitingToCommence);

            if (SessionIsStarted())
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                _started = true;
                await AdvanceToNextStage();
            }
        }

        private async Task AdvanceToNextStage()
        {
            await Clients.All.BroadcastStateToVoters(++_currentState);

            switch (_currentState)
            {
                case State.DistributingDomainParameters:
                    await DistributeDomainParameters();
                    SetAllVoterStates(ClientState.Busy);
                    break;
                case State.Round1:
                    await Clients.Caller.BroadcastStateToVoters(_currentState);
                    SetAllVoterStates(ClientState.Busy);
                    break;
            }
        }

        private void SetAllVoterStates(ClientState state)
        {
            foreach (var keyValuePair in _voters)
            {
                _voters[keyValuePair.Key] = state;
            }
        }

        public async Task VoterIsReady(string voterId)
        {
            if (!_voters.ContainsKey(voterId))
            {
                // TODO: handle
                return;
            }

            _voters[voterId] = ClientState.Ready;

            if (AreAllVotersReady())
            {
                await AdvanceToNextStage();
            }
        }

        private bool AreAllVotersReady()
        {
            return !_voters.Any(v => v.Value != ClientState.Ready);
        }

        private bool SessionIsStarted()
        {
            return _voters.Count == 1;
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
