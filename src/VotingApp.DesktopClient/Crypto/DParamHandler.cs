using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VotingApp.Models;

namespace VotingApp.DesktopClient.Crypto
{
    internal class DParamHandler
    {
        private ECKeyGenerationParameters _keyGenParams;

        public DParamHandler(DomainParameters parameters)
        {
            var curve = new FpCurve(parameters.Q, parameters.A, parameters.B, parameters.P, parameters.Cofactor);
            var domainParams = new ECDomainParameters(curve, parameters.G, parameters.N);
            _keyGenParams = new ECKeyGenerationParameters(domainParams, new SecureRandom());
        }

        public void Selectxi()
        {
            var keyGenerator = new ECKeyPairGenerator();
            keyGenerator.Init(_keyGenParams);
            var keyPair = keyGenerator.GenerateKeyPair();
            var privateKey = keyPair.Private as ECPrivateKeyParameters;
            var publicKey = keyPair.Public as ECPublicKeyParameters;
        }
    }
}
