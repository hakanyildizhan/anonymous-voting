using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using VotingApp.DesktopClient.Crypto;
using VotingApp.DesktopClient.Hub;
using VotingApp.Models;
using VotingApp.Models.JsonHandling;

namespace VotingApp.DesktopClient
{
    internal class ViewModel : BaseViewModel
    {
        private string _question;
        private string _status;
        private string _ticker;
        private double _yesPercentage;
        private double _noPercentage;
        private readonly string _clientId;
        private HubConnection _connection;
        private DParamHandler _paramHandler;
        private State _currentState;
        private bool? _vote;
        private bool _yesButtonIsEnabled = true;
        private bool _noButtonIsEnabled = true;
        private bool _awaitingVote = true;

        public IConfiguration Configuration { get; set; }
        public bool? Vote
        {
            get => _vote;
            set
            {
                if (_vote != value)
                {
                    _vote = value;
                    OnPropertyChanged(nameof(Vote));
                    YesButtonIsEnabled = !_vote.Value;
                    NoButtonIsEnabled = _vote.Value;
                    //OnPropertyChanged(nameof(YesButtonIsEnabled));
                    //OnPropertyChanged(nameof(NoButtonIsEnabled));
                }
            }
        }
        public bool YesButtonIsEnabled 
        {
            get => _yesButtonIsEnabled;
            set
            {
                if (_yesButtonIsEnabled != value)
                {
                    _yesButtonIsEnabled = value;
                    OnPropertyChanged(nameof(YesButtonIsEnabled));
                }
            }
        }
        public bool NoButtonIsEnabled 
        {
            get => _noButtonIsEnabled;
            set
            {
                if (_noButtonIsEnabled != value)
                {
                    _noButtonIsEnabled = value;
                    OnPropertyChanged(nameof(NoButtonIsEnabled));
                }
            }
        }
        public string Status 
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string Ticker
        {
            get => _ticker;
            set
            {
                if (_ticker != value)
                {
                    _ticker = value;
                    OnPropertyChanged(nameof(Ticker));
                }
            }
        }

        public string Question
        {
            get => _question;
            set
            {
                if (_question != value)
                {
                    _question = value;
                    OnPropertyChanged(nameof(Question));
                }
            }
        }

        public double YesPercentage
        {
            get => _yesPercentage;
            set
            {
                if (_yesPercentage != value)
                {
                    _yesPercentage = value;
                    OnPropertyChanged(nameof(YesPercentage));
                }
            }
        }

        public double NoPercentage
        {
            get => _noPercentage;
            set
            {
                if (_noPercentage != value)
                {
                    _noPercentage = value;
                    OnPropertyChanged(nameof(NoPercentage));
                }
            }
        }

        public ICommand VoteYesCommand { get; set; }
        public ICommand VoteNoCommand { get; set; }

        public ViewModel(IConfiguration configuration)
        {
            Configuration = configuration;
            VoteYesCommand = new RelayCommand(() => VoteYes());
            VoteNoCommand = new RelayCommand(() => VoteNo());
            _clientId = Guid.NewGuid().ToString();
            Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            Status = "Waiting to connect to the server..";
            string hubUrl = Configuration.GetSection("AppSettings").GetSection("HubEndpoint").Value;
            _connection = new HubConnectionBuilder()
                .AddNewtonsoftJsonProtocol(JsonHandlingHelpers.GetJsonHandlerOptions())
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new ConnectionRetryPolicy(ConnectionLost, RetryLimitExceeded))
                .Build();
            
            await _connection.StartAsync();
            Status = "Successfully connected to server.";
            await _connection.SendAsync("NotifyConnected", _clientId);
            _connection.On<State>("BroadcastStateToVoters", HandleStatusChange);
            _connection.On<DomainParameters>("BroadcastDomainParameters", HandleBroadcastDomainParameters);
            _connection.On<string>("GetRoundPayloads", HandleGetRoundPayloads);
        }

        private async Task HandleGetRoundPayloads(string payloads)
        {
            IList<RoundPayload> payloadList = JsonConvert.DeserializeObject<IList<RoundPayload>>(payloads);
            switch(_currentState)
            {
                case State.Round1PayloadBroadcast:
                    var voterPayloads = payloadList.Select(p => new { p.VoterId, p.Payload });
                    var payloadDict = voterPayloads
                        .ToDictionary(p => p.VoterId, p => JsonConvert.DeserializeObject<Round1Payload>(p.Payload));
                    _paramHandler.SavePayloads(payloadDict);
                    break;
                case State.Round2PayloadBroadcast:
                    var round2Payloads = payloadList.Select(p => new { p.VoterId, p.Payload })
                        .ToDictionary(p => p.VoterId, p => JsonConvert.DeserializeObject<Round2Payload>(p.Payload));
                    _paramHandler.SavePayloads(round2Payloads);
                    break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            Status = "Got all payloads.";
            Ticker = $"Got payloads from other {payloadList.Count-1} voters.";
            await _connection.SendAsync("VoterIsReady", _clientId);
        }

        private async Task HandleBroadcastDomainParameters(DomainParameters parameters)
        {
            Status = "Generating keys..";
            _paramHandler = new DParamHandler(parameters);
            _paramHandler.GenerateKeys();
            Ticker = "Your private key: " + _paramHandler.GetPrivateKeyString();
            await Task.Delay(TimeSpan.FromSeconds(1));
            Ticker += "\r\nYour public key: " + _paramHandler.GetPublicKeyString();
            Status = "Generated private and voting key successfully.";
            await _connection.SendAsync("VoterIsReady", _clientId);
        }

        private async Task HandleStatusChange(State state)
        {
            Trace.WriteLine($"Current state was {_currentState.ToString()}");
            Trace.WriteLine($"New state is {state.ToString()}");
            if (_currentState == state)
            {
                return;
            }

            _currentState = state;
            switch (_currentState)
            {
                case State.WaitingToCommence:
                    Status = "Waiting for voting to start..";
                    break;
                case State.AlreadyStarted:
                    Status = "Voting has already started. Going offline..";
                    await _connection.StopAsync();
                    break;
                case State.DistributingDomainParameters:
                    Status = "Getting domain parameters..";
                    break;
                case State.Round1:
                    Status = "Round 1 in progress.";
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    _paramHandler.PickRandomr();
                    _paramHandler.CalculatesForRound1();
                    _paramHandler.CalculateRound1Payload(_clientId);
                    Status = "Calculated Round 1 payload & zero-knowledge proof.";
                    await _connection.SendAsync("VoterIsReady", _clientId);
                    break;
                case State.Round1PayloadBroadcast:
                    await _connection.SendAsync("BroadcastRoundPayload", _paramHandler.Round1Payload);
                    Status = "Sent Round 1 payload & zero-knowledge proof.";
                    await Task.Delay(TimeSpan.FromSeconds(4));
                    await _connection.SendAsync("VoterIsReady", _clientId);
                    break;
                case State.Round1ZKPCheck:
                    Status = "All voter payloads are now available. Checking proofs of zero knowledge..";

                    foreach (var voterPayload in _paramHandler.Round1Payloads)
                    {
                        if (voterPayload.Key.Equals(_clientId))
                        {
                            continue;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        Ticker = $"Checking zero knowledge proof for voter {voterPayload.Key}:";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        Ticker += $"\r\nVoter public key: {voterPayload.Value.VotingKey.ToPublicKeyFormat()}";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        Ticker += $"\r\nVoter g^r (R): {voterPayload.Value.ZKP.R}";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        Ticker += $"\r\nVoter s: {voterPayload.Value.ZKP.s}";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        bool checkResult = _paramHandler.CheckZeroKnowledgeProof(voterPayload.Value);

                        if (checkResult)
                        {
                            Ticker += "\r\n\r\nZero knowledge proof HOLDS.";
                        }
                        else
                        {
                            Ticker += "\r\n\r\nZero knowledge proof DOES NOT HOLD.";
                            throw new Exception();
                        }
                    }
                    Status = "Finished checking proofs of zero knowledge.";
                    await _connection.SendAsync("VoterIsReady", _clientId);
                    break;
                case State.Round2:
                    Status = "Round 2 in progress.";
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await _paramHandler.CalculateY();
                    Ticker = "Yi calculation complete.";
                    
                    if (!Vote.HasValue)
                    {
                        Status = "You have not voted yet. Please vote.";
                        Ticker += "\r\nAwaiting your vote..";
                    }
                    else
                    {
                        await CalculateRound2Payload();
                    }
                    break;
                case State.Round2PayloadBroadcast:
                    await _connection.SendAsync("BroadcastRoundPayload", _paramHandler.Round2Payload);
                    Status = "Sent Round 2 payload & zero-knowledge proof.";
                    await Task.Delay(TimeSpan.FromSeconds(4));
                    await _connection.SendAsync("VoterIsReady", _clientId);
                    break;
                case State.Round2ZKPCheck:
                    Status = "All voter payloads are now available. Checking proofs of zero knowledge..";

                    foreach (var voterPayload in _paramHandler.Round2Payloads)
                    {
                        if (voterPayload.Key.Equals(_clientId))
                        {
                            continue;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        Ticker = $"Checking zero knowledge proof for voter {voterPayload.Key}:";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        var votingKey = _paramHandler.Round1Payloads[voterPayload.Key].VotingKey;
                        Ticker += $"\r\nVoter public key (X): {votingKey.ToPublicKeyFormat()}";
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        bool checkResult = _paramHandler.CheckZeroKnowledgeProof(votingKey, voterPayload.Value);

                        if (checkResult)
                        {
                            Ticker += "\r\n\r\nZero knowledge proof HOLDS.";
                        }
                        else
                        {
                            Ticker += "\r\n\r\nZero knowledge proof DOES NOT HOLD.";
                            throw new Exception();
                        }
                    }
                    Status = "Finished checking proofs of zero knowledge.";
                    await _connection.SendAsync("VoterIsReady", _clientId);
                    break;
                case State.VotingResultCalculation:
                    var numberOfYesVotes = _paramHandler.CalculateYesVotes();
                    YesPercentage = ((numberOfYesVotes * 1.0d) / _paramHandler.Round2Payloads.Count) * 100;
                    NoPercentage = 100.0 - YesPercentage;
                    Status = "Voting complete, results are available.";
                    break;
            }
        }

        private async Task VoteYes()
        {
            Vote = true;
            if (_awaitingVote && _currentState == State.Round2)
            {
                await CalculateRound2Payload();
            }
        }

        private async Task VoteNo()
        {
            Vote = false;
            if (_awaitingVote && _currentState == State.Round2)
            {
                await CalculateRound2Payload();
            }
        }

        private async Task CalculateRound2Payload()
        {
            _awaitingVote = false;
            YesButtonIsEnabled = false;
            NoButtonIsEnabled = false;
            await _paramHandler.CalculateRound2Payload(Vote.Value);
            Ticker = "Gxyv and CDS ZKP is calculated.";
            Status = "Calculated Round 2 payload & zero-knowledge proof (CDS).";
            await _connection.SendAsync("VoterIsReady", _clientId);
        }

        private void RetryLimitExceeded(object? sender, EventArgs e)
        {
            Status = "Connection retries failed. Connection is lost.";
        }

        private void ConnectionLost(object? sender, EventArgs e)
        {
            Status = "Connection lost. Retrying..";
        }
    }
}
