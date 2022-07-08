using LiteDB;

namespace VotingApp.Data
{
    internal class VoterPayload
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string VoterId { get; set; }
        public int Round { get; set; }
        public string Payload { get; set; }
    }
}
