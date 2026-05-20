using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaileyIntegration.Tech.Models.Hailey_Quinix_Mapping
{
        public class HaileyEmployee
        {
            public string employeeId { get; set; }

            public Personal personal { get; set; }

            public JobData jobData { get; set; }

            public OrganizationalBelonging organizationalBelonging { get; set; }
        }

        public class Personal
        {
            public PersonalGeneral general { get; set; }

            public ContactInformation contactInformation { get; set; }
        }

        public class PersonalGeneral
        {
            public string firstName { get; set; }

            public string lastName { get; set; }
        }

        public class ContactInformation
        {
            public string privateEmail { get; set; }

            public string privatePhone { get; set; }

            public string streetAddress { get; set; }

            public string postalCode { get; set; }

            public string city { get; set; }
        }

        public class JobData
        {
            public Employment employment { get; set; }

            public General general { get; set; }
        }

        public class General
        {
            public string workPhone { get; set; }

            public string companyEmail { get; set; }

            public string employmentNumber { get; set; }
        }

        public class Employment
        {
            public DateTime dateOfJoining { get; set; }

            public DateTime lastDayOfEmployment { get; set; }
        }

    public class OrganizationalBelonging
    {
        public string managerEmployeeId { get; set; }

        public string businessAreaId { get; set; }

        public string costCenterId { get; set; }

        public string departmentId { get; set; }
    }
}

