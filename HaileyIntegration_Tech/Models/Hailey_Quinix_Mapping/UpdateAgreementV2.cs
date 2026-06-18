using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaileyIntegration.Tech.Models.Hailey_Quinix_Mapping
{
    public class UpdateAgreementV2
    {
        public string badgeNo { get; set; }
        public string extAgreementId { get; set; }
        public string extTemplateId { get; set; }

        public DateTime fromDate { get; set; }
        public bool fromDateSpecified { get; set; }

        public DateTime toDate { get; set; }
        public bool toDateSpecified { get; set; }

        public bool hourly { get; set; }
        public bool hourlySpecified { get; set; }

        public string comment { get; set; }

        public decimal fullEmploymentHrs { get; set; }
        public bool fullEmploymentHrsSpecified { get; set; }

        public decimal minHrsWeek { get; set; }
        public bool minHrsWeekSpecified { get; set; }
    }
}
