using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VotingApp.Models
{
    public class Round2ZKP
    {
        public Point Byes { get; set; }
        public Point Bno { get; set; }
        public Point a1yes { get; set; }
        public Point a2yes { get; set; }
        public Point a1no { get; set; }
        public Point a2no { get; set; }
        public string d1 { get; set; }
        public string d2 { get; set; }
        public string r1 { get; set; }
        public string r2 { get; set; }

    }
}
