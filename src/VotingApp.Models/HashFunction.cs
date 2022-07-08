using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace VotingApp.Models
{
    public class HashFunction
    {
        /// <summary>
        /// The number of times to encrypt the password
        /// </summary>
        const int ITERATIONS = 250000;

        /// <summary>
        /// The salt size
        /// </summary>
        const int SALT_BYTE_SIZE = 128;

        /// <summary>
        /// The final hash size.
        /// </summary>
        const int HASH_BYTE_SIZE = 256;

        /// <summary>
        /// PBKDF2 applies a pseudorandom function, such as hash-based message authentication code (HMAC), to the input password or passphrase along with a salt value and repeats the process many times to produce a derived key, which can then be used as a cryptographic key in subsequent operations. The added computational work makes password cracking much more difficult, and is known as key stretching.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="A"></param>
        /// <param name="R"></param>
        /// <returns></returns>
        public static BigInteger PBKDF2_SHA512_ComputeHash(DomainParameters parameters, Point A, Point R)
        {
            var curve = new FpCurve(parameters.Q, parameters.A, parameters.B, parameters.P, parameters.Cofactor);
            ECPoint G = curve.CreatePoint(parameters.Gx, parameters.Gy);
            BigInteger Ax = new BigInteger(A.Xstr);
            BigInteger Ay = new BigInteger(A.Ystr);
            ECPoint APoint = curve.CreatePoint(Ax, Ay);
            BigInteger Rx = new BigInteger(R.Xstr);
            BigInteger Ry = new BigInteger(R.Ystr);
            ECPoint RPoint = curve.CreatePoint(Rx, Ry);
            // H = g + 2*A - 3*R
            ECPoint s = G.Add(APoint.Multiply(BigInteger.Two).Normalize()).Normalize().Subtract(RPoint.Multiply(BigInteger.Three).Normalize()).Normalize();

            var pdb = new Pkcs5S2ParametersGenerator(new Org.BouncyCastle.Crypto.Digests.Sha512Digest());
            pdb.Init(PbeParametersGenerator.Pkcs5PasswordToBytes(s.ToString().ToCharArray()), CreateSalt(APoint, RPoint), ITERATIONS);
            var key = (KeyParameter)pdb.GenerateDerivedMacParameters(HASH_BYTE_SIZE * 8);
            var hash = key.GetKey();
            return ToBigInt(hash);
        }

        private static byte[] CreateSalt(ECPoint A, ECPoint R)
        {
            byte[] salt = new byte[SALT_BYTE_SIZE];

            for (int i = 0; i < SALT_BYTE_SIZE; i++)
            {
                if (i % 4 == 0)
                {
                    salt[i] = byte.Parse((A.AffineXCoord.ToBigInteger().ToString()[i % A.AffineXCoord.ToBigInteger().ToString().Length]).ToString());
                }
                if (i % 3 == 0)
                {
                    salt[i] = byte.Parse((A.AffineYCoord.ToBigInteger().ToString()[i % A.AffineYCoord.ToBigInteger().ToString().Length]).ToString());
                }
                else if (i % 2 == 0)
                {
                    salt[i] = byte.Parse((R.AffineXCoord.ToBigInteger().ToString()[i % R.AffineXCoord.ToBigInteger().ToString().Length]).ToString());
                }
                else
                {
                    salt[i] = byte.Parse((R.AffineYCoord.ToBigInteger().ToString()[i % R.AffineYCoord.ToBigInteger().ToString().Length]).ToString());
                }
            }

            return salt;
        }

        private static BigInteger ToBigInt(byte[] arr)
        {
            byte[] rev = new byte[arr.Length + 1];
            for (int i = 0; i < arr.Length; i++)
                rev[i + 1] = arr[i];
            return new BigInteger(rev);
        }
    }
}
