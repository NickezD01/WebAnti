using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.CompanyResponse
{
    public class CompanyResponse
    {
        // Khớp chính xác với CompanyId trong DB
        public int CompanyId { get; set; }

        // Khớp chính xác với CompanyName trong DB
        public string CompanyName { get; set; }

        public string Domain { get; set; }

        public string LogoUrl { get; set; }
    }
}
