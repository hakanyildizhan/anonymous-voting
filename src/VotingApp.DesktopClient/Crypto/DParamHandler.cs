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
    internal partial class DParamHandler
    {
        private ECKeyGenerationParameters _keyGenParams;
        private CurveOperations _curveOperations;

        public DParamHandler(DomainParameters parameters)
        {
            var curve = new FpCurve(parameters.Q, parameters.A, parameters.B, parameters.P, parameters.Cofactor);
            var g = curve.CreatePoint(parameters.Gx, parameters.Gy);
            var domainParams = new ECDomainParameters(curve, g, parameters.N);
            _keyGenParams = new ECKeyGenerationParameters(domainParams, new SecureRandom());
            _curveOperations = new CurveOperations();
        }

        public void GenerateKeys()
        {
            var keyGenerator = new ECKeyPairGenerator();
            keyGenerator.Init(_keyGenParams);
            var keyPair = keyGenerator.GenerateKeyPair();
            _curveOperations.PrivateKey = keyPair.Private as ECPrivateKeyParameters;
            _curveOperations.PublicKey = keyPair.Public as ECPublicKeyParameters;
        }

        public string GetPublicKeyString()
        {
            return _curveOperations.PublicKeyString;
        }

        public string GetPrivateKeyString()
        {
            return _curveOperations.PrivateKeyString;
        }
    }
}
