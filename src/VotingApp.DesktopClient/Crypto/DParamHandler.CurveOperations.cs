using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VotingApp.DesktopClient.Crypto
{
    internal partial class DParamHandler
    {
        private class CurveOperations
        {
            internal ECPrivateKeyParameters PrivateKey { get; set; }
            internal ECPublicKeyParameters PublicKey { get; set; }

            internal string PrivateKeyString 
            { 
                get 
                {  
                    if (PrivateKey == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return BitConverter.ToString(PrivateKey.D.ToByteArrayUnsigned()).Replace("-", "");
                    }
                }
            }
            internal string PublicKeyString 
            { 
                get
                {
                    if (PublicKey == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return BitConverter.ToString(PublicKey.Q.GetEncoded()).Replace("-", "");
                    }
                }
            }
        }
    }
}
