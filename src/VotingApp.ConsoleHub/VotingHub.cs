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

        public override Task OnConnectedAsync()
        {
            Console.WriteLine("A new client has connected.");
            return base.OnConnectedAsync();
        }
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

        public void NotifyConnected(string voterId)
        {
            if (_started)
            {
                Clients.Caller.BroadcastState(State.AlreadyStarted);
                return;
            }

            Console.WriteLine($"Voter with ID {voterId} is registered.");
            _voters.TryAdd(voterId, new object());

            if (_voters.Count == 3)
            {
                _started = true;
                BroadcastState(State.DistributingDomainParameters);
                DistributeDomainParameters();
            }
        }

        private void DistributeDomainParameters()
        {
            DerObjectIdentifier ecParam = X9ObjectIdentifiers.Prime256v1;
            ECKeyPairGenerator keyGenerator = new ECKeyPairGenerator();
            var parameters = new ECKeyGenerationParameters(ecParam, new SecureRandom());
            BroadcastDomainParameters(new DomainParameters()
            {
                Q = parameters.DomainParameters.Curve.Field.Characteristic,
                A = parameters.DomainParameters.Curve.A.ToBigInteger(),
                B = parameters.DomainParameters.Curve.B.ToBigInteger(),
                G = parameters.DomainParameters.G,
                P = parameters.DomainParameters.Curve.Order,
                N = parameters.DomainParameters.N,
                Cofactor = parameters.DomainParameters.Curve.Cofactor
            });
        }
    }
}
