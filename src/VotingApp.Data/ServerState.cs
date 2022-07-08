using LiteDB;
using VotingApp.Models;

namespace VotingApp.Data
{
    internal class ServerState
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public State State { get; set; }
    }
}
