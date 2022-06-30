using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VotingApp.Models;

namespace VotingApp.DesktopClient
{
    internal class ViewModel : BaseViewModel
    {
        private string _question;
        private string _status;
        private HubConnection connection;
        private int _voterCount;
        private double _yesPercentage;
        private double _noPercentage;
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
            Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            string hubUrl = Configuration.GetSection("AppSettings").GetSection("HubEndpoint").Value;
            connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            connection.On<State>("BroadcastState", HandleStatusChange);

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
        }

        private async Task HandleStatusChange(State state)
        {

        }

        private void VoteYes()
        {

        }

        private void VoteNo()
        {

        }
    }
}
