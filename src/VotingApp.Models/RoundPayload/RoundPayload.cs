namespace VotingApp.Models
{
    public class RoundPayload
    {
        public string VoterId { get; set; }
        public int Round { get; set; }
        public string Payload { get; set; }
    }
}
