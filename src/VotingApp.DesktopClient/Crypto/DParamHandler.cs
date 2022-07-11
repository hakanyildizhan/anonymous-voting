using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private Dictionary<string, Round1Payload> _round1Payloads;
        private Dictionary<string, Round2Payload> _round2Payloads;
        private RoundPayload _round1Payload;
        private RoundPayload _round2Payload;
        private Point _y;
        public string ClientId { get; private set; }
        public ECPoint Y { get; private set; }
        public RoundPayload Round1Payload
        {
            get
            {
                return _round1Payload;
            }
        }
        public RoundPayload Round2Payload
        {
            get
            {
                return _round2Payload;
            }
        }
        public Dictionary<string, Round1Payload> Round1Payloads
        {
            get
            {
                return _round1Payloads;
            }
        }
        public Dictionary<string, Round2Payload> Round2Payloads
        {
            get
            {
                return _round2Payloads;
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
                _r = new BigInteger(_ecParameters.N.BitLength - secureRandom.Next(0, _ecParameters.N.BitLength / 10), secureRandom);
            } while (_r.CompareTo(_ecParameters.Curve.Order.Subtract(BigInteger.One)) > 0);
        }

        public void CalculatesForRound1()
        {
            BigInteger c = HashFunction.PBKDF2_SHA512_ComputeHash(_parameters, GetPublicKey(), R).Mod(_ecParameters.Curve.Order);
            // s = r + ac
            _s = _r.Add(GetPrivateKey().Multiply(c).Mod(_ecParameters.Curve.Order)).Mod(_ecParameters.Curve.Order);
        }

        public void CalculateRound1Payload(string clientId)
        {
            ClientId = clientId;
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
            _round1Payloads = voterPayloads;
        }

        public void SavePayloads(Dictionary<string, Round2Payload?>? voterPayloads)
        {
            _round2Payloads = voterPayloads;
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

        public Task CalculateY()
        {
            var previousVoterKeys = new List<ECPoint>();
            var followingVoterKeys = new List<ECPoint>();

            foreach (var payload in _round1Payloads)
            {
                // if this is our payload, skip it
                if (payload.Key == ClientId)
                {
                    continue;
                }

                var keyPoint = _ecParameters.Curve.CreatePoint(new BigInteger(payload.Value.VotingKey.Xstr), new BigInteger(payload.Value.VotingKey.Ystr));

                if (string.Compare(payload.Key, ClientId) == -1)
                {
                    previousVoterKeys.Add(keyPoint);
                }
                else if (string.Compare(payload.Key, ClientId) == 1)
                {
                    followingVoterKeys.Add(keyPoint);
                }
            }

            ECPoint dividend = !previousVoterKeys.Any() ? null : previousVoterKeys.Aggregate((result, element) => result.Add(element)).Normalize();
            ECPoint divisor = !followingVoterKeys.Any() ? null : followingVoterKeys.Aggregate((result, element) => result.Add(element)).Normalize().Negate();

            if (dividend == null)
            {
                Y = divisor;
            }
            else if (divisor == null)
            {
                Y = dividend;
            }
            else
            {
                Y = dividend.Add(divisor).Normalize();
            }
            return Task.CompletedTask;
        }

        internal Task CalculateRound2Payload(bool vote)
        {
            // calculate g^(xy) * g^v = Y^x * g^v
            var payload = Y.Multiply(GetPrivateKey()).Normalize();
            payload = vote ? payload.Add(_ecParameters.G).Normalize() : payload;// payload.Add(_ecParameters.G.Negate()).Normalize();

            // calculate inputs for CDS ZKP
            // pick r1, r2, w1, w2, d1, d2
            var r1 = PickRandomNumber();
            var r2 = PickRandomNumber();
            var w1 = PickRandomNumber();
            var w2 = PickRandomNumber();
            var d1 = PickRandomNumber();
            var d2 = PickRandomNumber();

            // for v = 1
            // B = g^(xy) * g
            var Byes = Y.Multiply(GetPrivateKey()).Add(_ecParameters.G).Normalize();

            // a1 = g^r1 * (Y*g)^-d1
            var a1yes = _ecParameters.G.Multiply(r1).Add(Byes.Add(_ecParameters.G).Multiply(d1.Negate())).Normalize();

            // a2 = g^w2
            var a2yes = _ecParameters.G.Multiply(w2).Normalize();

            // c = H(B,a1,a2)
            var cyes = HashFunction.PBKDF2_SHA512_ComputeHash(_parameters,
                new Point
                {
                    Xstr = Byes.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = Byes.AffineYCoord.ToBigInteger().ToString(),
                },
                new Point
                {
                    Xstr = a1yes.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = a1yes.AffineYCoord.ToBigInteger().ToString(),
                },
                new Point
                {
                    Xstr = a2yes.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = a2yes.AffineYCoord.ToBigInteger().ToString(),
                });

            // for v = -1
            // B = g^(xy) * g^-1
            var Bno = Y.Multiply(GetPrivateKey()).Add(_ecParameters.G.Negate()).Normalize();

            // a1 = g^w1
            var a1no = _ecParameters.G.Multiply(w1).Normalize();

            // a2 = g^r2 * (Y * g^-1)^-d2
            var a2no = _ecParameters.G.Multiply(r2).Add(Bno.Add(_ecParameters.G.Negate()).Multiply(d2.Negate())).Normalize();

            // c = H(B,a1,a2)
            var cno = HashFunction.PBKDF2_SHA512_ComputeHash(_parameters,
                new Point
                {
                    Xstr = Bno.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = Bno.AffineYCoord.ToBigInteger().ToString(),
                },
                new Point
                {
                    Xstr = a1no.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = a1no.AffineYCoord.ToBigInteger().ToString(),
                },
                new Point
                {
                    Xstr = a2no.AffineXCoord.ToBigInteger().ToString(),
                    Ystr = a2no.AffineYCoord.ToBigInteger().ToString(),
                });


            _round2Payload = new RoundPayload()
            {
                VoterId = ClientId,
                Round = 2,
                Payload = JsonConvert.SerializeObject(new Round2Payload()
                {
                    Gxyv = new Point()
                    {
                        Xstr = payload.AffineXCoord.ToBigInteger().ToString(),
                        Ystr = payload.AffineYCoord.ToBigInteger().ToString(),
                    },
                    ZKP = new Round2ZKP()
                    {
                        Bno = new Point()
                        {
                            Xstr = Bno.AffineXCoord.ToBigInteger().ToString(),
                            Ystr = Bno.AffineYCoord.ToBigInteger().ToString(),
                        },
                        Byes = new Point()
                        {
                            Xstr = Byes.AffineXCoord.ToBigInteger().ToString(),
                            Ystr = Byes.AffineYCoord.ToBigInteger().ToString(),
                        },
                        a1yes = new Point()
                        {
                            Xstr = a1yes.AffineXCoord.ToBigInteger().ToString(),
                            Ystr = a1yes.AffineYCoord.ToBigInteger().ToString(),
                        },
                        a2yes = new Point()
                        {
                            Xstr = a2yes.AffineXCoord.ToBigInteger().ToString(),
                            Ystr = a2yes.AffineYCoord.ToBigInteger().ToString(),
                        },
                        a1no = new Point()
                        {
                            Xstr = a1no.AffineXCoord.ToBigInteger().ToString(),
                            Ystr = a1no.AffineYCoord.ToBigInteger().ToString(),
                        },
                        a2no = new Point()
                        {
                            Xstr = a2no.AffineXCoord.ToBigInteger().ToString(),
                            Ystr = a2no.AffineYCoord.ToBigInteger().ToString(),
                        },
                        d1 = d1.ToString(),
                        d2 = d2.ToString(),
                        r1 = r1.ToString(),
                        r2 = r2.ToString(),
                    }
                })
            };

            return Task.CompletedTask;
        }

        private BigInteger PickRandomNumber()
        {
            var secureRandom = new SecureRandom();
            var n = BigInteger.Zero;
            do
            {
                n = new BigInteger(_ecParameters.N.BitLength - secureRandom.Next(0, _ecParameters.N.BitLength / 10), secureRandom);
            } while (n.CompareTo(_ecParameters.Curve.Order.Subtract(BigInteger.One)) > 0);
            return n;
        }

        public bool CheckZeroKnowledgeProof(Point h, Round2Payload payload)
        {
            // TODO: figure it out (CDS)
            return true;
        }

        internal int CalculateYesVotes()
        {
            var result = Round2Payloads.ToList()
                .Select(p => _ecParameters.Curve.CreatePoint(new BigInteger(p.Value.Gxyv.Xstr), new BigInteger(p.Value.Gxyv.Ystr)))
                .Aggregate((result, element) =>
                    result.Add(element))
                .Normalize();

            for (int i = 1; i <= Round2Payloads.Count; i++)
            {
                var possibleResult = _ecParameters.G.Multiply(new BigInteger(i.ToString())).Normalize();
                if (possibleResult.AffineXCoord.Equals(result.AffineXCoord) && possibleResult.AffineYCoord.Equals(result.AffineYCoord))
                {
                    return i;
                }
            }

            throw new Exception();
        }
    }
}
