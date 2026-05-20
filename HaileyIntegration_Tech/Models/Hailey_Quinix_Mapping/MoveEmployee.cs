using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaileyIntegration.Tech.Models.Hailey_Quinix_Mapping
{
    public class MoveEmployee
    {
        public string badgeNo { get; set; }
        public string unitExtCode { get; set; }
        public string newUnitStartDate { get; set; }
        public string oldUnitEndShareDate { get; set; }
        public string sharableOnNewUnitFrom { get; set; }
        public string section { get; set; }
        public string costCentre { get; set; }
        public string reportingTo { get; set; }
    }
}
