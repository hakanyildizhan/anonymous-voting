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
        private ConcurrentDictionary<string, object> _voters = new ConcurrentDictionary<string,object>();
        private bool _started;

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine("A new client has connected.");
            await base.OnConnectedAsync();
        }
        public async Task BroadcastRoundPayload(RoundPayload payload)
        {
            await Clients.All.BroadcastRoundPayload(payload);
        }

        public async Task BroadcastState(State state)
        {
            await Clients.All.BroadcastState(state);
        }

        public async Task BroadcastDomainParameters(DomainParameters parameters)
        {
            await Clients.All.BroadcastDomainParameters(parameters);
        }

        public async Task BroadcastQuestion(string question)
        {
            await Clients.All.BroadcastQuestion(question);
        }

        public async Task NotifyConnected(string voterId)
        {
            if (_started)
            {
                await Clients.Caller.BroadcastState(State.AlreadyStarted);
                return;
            }

            Console.WriteLine($"Voter with ID {voterId} is registered.");
            _voters.TryAdd(voterId, new object());

            if (SessionIsStarted())
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                _started = true;
                await BroadcastState(State.DistributingDomainParameters);
                await DistributeDomainParameters();
            }
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
            await BroadcastDomainParameters(new DomainParameters()
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
