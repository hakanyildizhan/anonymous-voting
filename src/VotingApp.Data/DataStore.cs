using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VotingApp.Models;

namespace VotingApp.Data
{
    public static class DataStore
    {
        const string DATABASE_PATH = @".\data.db";
        static LiteDatabase db;
        static DataStore()
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "data.db"));
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "data-log.db"));
            db = new LiteDatabase(DATABASE_PATH);
            var col = db.GetCollection<ServerState>();
            col.Insert(new ServerState()
            {
                State = State.WaitingToCommence
            });
        }

        public static void AddVoter(string voterId, ClientState state)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<VoterState>();

            var voterState = new VoterState
            {
                VoterId = voterId,
                State = state
            };

            col.Insert(voterState);
            col.EnsureIndex(x => x.VoterId);
        }

        public static void SetAllVoterStates(ClientState state)
        {
            var col = db.GetCollection<VoterState>();
            var states = col.FindAll();
            foreach (var item in states)
            {
                item.State = state;
            }
            col.Update(states);
        }

        public static void SetVoterState(string voterId, ClientState state)
        {
            var col = db.GetCollection<VoterState>();
            var voter = col.FindOne(v => v.VoterId.Equals(voterId));
            voter.State = state;
            col.Update(voter);
        }

        public static bool AreAllVotersReady()
        {
            var col = db.GetCollection<VoterState>();
            var voters = col.FindAll();
            foreach (var item in voters)
            {
                if (item.State != ClientState.Ready)
                {
                    return false;
                }
            }
            return true;
        }

        public static int GetVoterCount()
        {
            var col = db.GetCollection<VoterState>();
            return col.Count();
        }

        public static void AddVoterPayload(RoundPayload payload)
        {
            var col = db.GetCollection<VoterPayload>();
            var item = new VoterPayload()
            {
                Payload = payload.Payload,
                VoterId = payload.VoterId,
                Round = payload.Round
            };
            col.Insert(item);
            col.EnsureIndex(x => x.VoterId);
        }

        public static List<RoundPayload> GetVoterPayloads(int round)
        {
            var col = db.GetCollection<VoterPayload>();
            return col.FindAll()
                .Where(p => p.Round == round)
                .Select(p => new RoundPayload()
            {
                VoterId = p.VoterId,
                Payload = p.Payload,
                Round = p.Round
            }).ToList();
        }

        public static void SaveCurrentState(State state)
        {
            var col = db.GetCollection<ServerState>();
            var currState = col.FindAll().First();
            currState.State = state;
            col.Update(currState);
        }

        public static State GetCurrentState()
        {
            var col = db.GetCollection<ServerState>();
            return col.FindAll().First().State;
        }
    }
}
