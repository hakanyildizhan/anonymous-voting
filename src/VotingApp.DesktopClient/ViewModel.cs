using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private HubConnection connection;
        private int _voterCount;
        private double _yesPercentage;
        private double _noPercentage;
        private readonly string _clientId;
        private HubConnection _connection;
        private DParamHandler _paramHandler;
        
        public IConfiguration Configuration { get; set; }
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
            switch(state)
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
                    break;
            }
        }

        private void VoteYes()
        {

        }

        private void VoteNo()
        {

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
