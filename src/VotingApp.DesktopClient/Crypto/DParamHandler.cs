using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using VotingApp.Models;

namespace VotingApp.DesktopClient.Crypto
{
    internal partial class DParamHandler
    {
        private ECKeyGenerationParameters _keyGenParams;
        private CurveOperations _curveOperations;
        private BigInteger _r;
        private ECDomainParameters _ecParameters;
        private DomainParameters _parameters;
        private BigInteger _s;
        private Dictionary<string, Round1Payload> _payloads;
        private RoundPayload _round1Payload;
        public RoundPayload Round1Payload
        {
            get
            {
                return _round1Payload;
            }
        }
        public Dictionary<string, Round1Payload> Payloads
        {
            get
            {
                return _payloads;
            }
        }
        public System.Numerics.BigInteger s
        {
            get
            {
                return System.Numerics.BigInteger.Parse(_s.ToString());
            }
        }
        public Point R
        {
            get
            {
                ECPoint r = _ecParameters.G.Multiply(_r).Normalize();
                return new Point()
                {
                    Xstr = r.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = r.AffineYCoord.ToBigInteger().ToString(),
                };
            }
        }

        public DParamHandler(DomainParameters parameters)
        {
            _parameters = parameters;
            var curve = new FpCurve(parameters.Q, parameters.A, parameters.B, parameters.P, parameters.Cofactor);
            ECPoint g = curve.CreatePoint(parameters.Gx, parameters.Gy);
            _ecParameters = new ECDomainParameters(curve, g, parameters.N);
            _keyGenParams = new ECKeyGenerationParameters(_ecParameters, new SecureRandom());
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

        public BigInteger GetPrivateKey()
        {
            return _curveOperations.PrivateKey.D;
        }

        public Point GetPublicKey()
        {
            return new Point()
            {
                Xstr = _curveOperations.PublicKey.Q.AffineXCoord.ToBigInteger().ToString(),
                Ystr = _curveOperations.PublicKey.Q.AffineYCoord.ToBigInteger().ToString()
            };
        }

        public void PickRandomr()
        {
            if (_r != null && _r != BigInteger.Zero)
            {
                return;
            }

            var secureRandom = new SecureRandom();
            do
            {
                _r = new BigInteger(_ecParameters.N.BitLength - secureRandom.Next(0, _ecParameters.N.BitLength/10), secureRandom);
            } while (_r.CompareTo(_ecParameters.N) > 0);
        }

        public void CalculatesForRound1()
        {
            BigInteger c = HashFunction.PBKDF2_SHA512_ComputeHash(_parameters, GetPublicKey(), R).Mod(_ecParameters.Curve.Order);
            // s = r + ac
            _s = _r.Add(GetPrivateKey().Multiply(c).Mod(_ecParameters.Curve.Order)).Mod(_ecParameters.Curve.Order);
        }

        public void CalculateRound1Payload(string clientId)
        {
            _round1Payload = new RoundPayload()
            {
                VoterId = clientId,
                Round = 1,
                Payload = JsonConvert.SerializeObject(new Round1Payload()
                {
                    VotingKey = new Point()
                    {
                        Xstr = GetPublicKey().Xstr,
                        Ystr = GetPublicKey().Ystr,
                    },
                    ZKP = new Round1ZKP()
                    {
                        R = R,
                        s = s.ToString()
                    }
                })
            };
        }

        public void SavePayloads(Dictionary<string, Round1Payload?>? voterPayloads)
        {
            _payloads = voterPayloads;
        }

        public bool CheckZeroKnowledgeProof(Round1Payload payload)
        {
            // first, calculate c = H(A, g, X)
            BigInteger c = HashFunction.PBKDF2_SHA512_ComputeHash(_parameters, payload.VotingKey, payload.ZKP.R).Mod(_ecParameters.Curve.Order);
            // check if g^s = R * A^c
            BigInteger s = new BigInteger(payload.ZKP.s);
            ECPoint gs = _ecParameters.G.Multiply(s).Normalize();
            BigInteger Rx = new BigInteger(payload.ZKP.R.Xstr);
            BigInteger Ry = new BigInteger(payload.ZKP.R.Ystr);
            ECPoint R = _ecParameters.Curve.CreatePoint(Rx, Ry);

            BigInteger Ax = new BigInteger(payload.VotingKey.Xstr);
            BigInteger Ay = new BigInteger(payload.VotingKey.Ystr);
            ECPoint A = _ecParameters.Curve.CreatePoint(Ax, Ay);
            ECPoint RAc = R.Add(A.Multiply(c)).Normalize();

            return gs.Equals(RAc);
        }
    }
}
