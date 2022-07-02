using Org.BouncyCastle.Math;

namespace VotingApp.Models
{
    public class DomainParameters
    {
        public BigInteger Gx { get; set; }
        public BigInteger Gy { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger A { get; set; }
        public BigInteger B { get; set; }
        public BigInteger P { get; set; }
        public BigInteger N { get; set; }
        public BigInteger Cofactor { get; set; }
    }
}
