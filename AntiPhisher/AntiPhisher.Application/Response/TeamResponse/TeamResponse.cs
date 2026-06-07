using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.TeamResponse
{
    public class TeamResponse
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int MemberCount { get; set; }
    }
}
