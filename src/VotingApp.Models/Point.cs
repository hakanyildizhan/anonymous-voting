using System.Numerics;

namespace VotingApp.Models
{
    public class Point
    {
        public string Xstr { get; set; }
        public string Ystr { get; set; }

        public BigInteger X()
        {
            return BigInteger.Parse(Xstr);
        }

        public BigInteger Y()
        {
            return BigInteger.Parse(Ystr);
        }

        public string ToPublicKeyFormat()
        {
            return (BitConverter.ToString(X().ToByteArray()) + BitConverter.ToString(Y().ToByteArray()))
                .Replace("-","");
        }

        public override string ToString()
        {
            return $"({Xstr},{Ystr})";
        }
    }
}
