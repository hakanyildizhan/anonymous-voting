using LiteDB;
using VotingApp.Models;

namespace VotingApp.Data
{
    public class VoterState
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string VoterId { get; set; }
        public ClientState State { get; set; }
    }
}
