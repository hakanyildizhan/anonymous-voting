using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace VotingApp.Models
{
    public class DomainParameters
    {
        public ECPoint G { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger A { get; set; }
        public BigInteger B { get; set; }
        public BigInteger P { get; set; }
        public BigInteger N { get; set; }
        public BigInteger Cofactor { get; set; }
    }
}
