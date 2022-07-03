using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VotingApp.Models
{
    public  class VoterState
    {
        public string VoterId { get; set; }
        public ClientState State { get; set; }
    }
}
