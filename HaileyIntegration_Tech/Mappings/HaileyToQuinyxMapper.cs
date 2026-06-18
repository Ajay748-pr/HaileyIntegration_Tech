using HaileyIntegration.Tech.Models.Hailey_Quinix_Mapping;
using System;

namespace HaileyIntegration.Tech.Mappings
{
    public static class HaileyToQuinyxMapper
    {
        public static UpdateEmployee MapToUpdateEmployee(HaileyEmployee hailey)
        {
            return new UpdateEmployee
            {
                // Employee Number -> badgeNo
                badgeNo = !string.IsNullOrWhiteSpace(hailey?.jobData?.general?.employmentNumber)
                    ? hailey.jobData.general.employmentNumber
                    : "Missing Data",

                // First Name -> givenName
                givenName = !string.IsNullOrWhiteSpace(hailey?.personal?.general?.firstName)
                    ? hailey.personal.general.firstName
                    : "Missing Data",

                // Last Name -> familyName
                familyName = !string.IsNullOrWhiteSpace(hailey?.personal?.general?.lastName)
                    ? hailey.personal.general.lastName
                    : "Missing Data",

                // Company Email -> email
                email = !string.IsNullOrWhiteSpace(hailey?.jobData?.general?.companyEmail)
                    ? hailey.jobData.general.companyEmail
                    : !string.IsNullOrWhiteSpace(hailey?.personal?.contactInformation?.privateEmail)
                        ? hailey.personal.contactInformation.privateEmail
                        : "Missing Data",

                // Work Phone -> phoneNo
                phoneNo = !string.IsNullOrWhiteSpace(hailey?.jobData?.general?.workPhone)
                    ? hailey.jobData.general.workPhone
                    : "Missing Data",

                // Private Phone -> cellPhone
                cellPhone = !string.IsNullOrWhiteSpace(hailey?.personal?.contactInformation?.privatePhone)
                    ? hailey.personal.contactInformation.privatePhone
                    : "Missing Data",

                // Street Address -> address1
                address1 = !string.IsNullOrWhiteSpace(hailey?.personal?.contactInformation?.streetAddress)
                    ? hailey.personal.contactInformation.streetAddress
                    : "Missing Data",

                // Postal Code -> zip
                zip = !string.IsNullOrWhiteSpace(hailey?.personal?.contactInformation?.postalCode)
                    ? hailey.personal.contactInformation.postalCode
                    : "Missing Data",

                // City -> city
                city = !string.IsNullOrWhiteSpace(hailey?.personal?.contactInformation?.city)
                    ? hailey.personal.contactInformation.city
                    : "Missing Data",

                // Manager Employee Id -> reportingTo
                reportingTo = !string.IsNullOrWhiteSpace(hailey?.organizationalBelonging?.managerEmployeeId)
                    ? hailey.organizationalBelonging.managerEmployeeId
                    : "Missing Data"
            };
        }

        public static UpdateAgreementV2 MapToUpdateAgreement(HaileyEmployee hailey)
        {
            return new UpdateAgreementV2
            {
                // Employee Number -> badgeNo
                badgeNo = !string.IsNullOrWhiteSpace(hailey?.jobData?.general?.employmentNumber)
                    ? hailey.jobData.general.employmentNumber
                    : "Missing Data",

                // Missing in Hailey
                extAgreementId = "Missing Data",

                // Missing in Hailey
                extTemplateId = "Missing Data",

                // Date Of Joining -> fromDate
                fromDate = hailey?.jobData?.employment?.dateOfJoining ?? DateTime.MinValue,

                fromDateSpecified = true,

                // Last Day Of Employment -> toDate
                toDate = hailey?.jobData?.employment?.lastDayOfEmployment ?? DateTime.MinValue,

                toDateSpecified = true,

                // Missing in Hailey
                hourly = false,

                hourlySpecified = true,

                // Missing in Hailey
                comment = "Missing Data",

                // Missing in Hailey
                fullEmploymentHrs = 0,

                fullEmploymentHrsSpecified = true,

                // Missing in Hailey
                minHrsWeek = 0,

                minHrsWeekSpecified = true
            };
        }

        public static MoveEmployee MapToMoveEmployee(HaileyEmployee hailey)
        {
            return new MoveEmployee
            {
                // Employee Number -> badgeNo
                badgeNo = !string.IsNullOrWhiteSpace(hailey?.jobData?.general?.employmentNumber)
                    ? hailey.jobData.general.employmentNumber
                    : "Missing Data",

                // Business Area Id -> unitExtCode
                unitExtCode = !string.IsNullOrWhiteSpace(hailey?.organizationalBelonging?.businessAreaId)
                    ? hailey.organizationalBelonging.businessAreaId
                    : "Missing Data",

                // Date Of Joining -> newUnitStartDate
                newUnitStartDate = hailey?.jobData?.employment?.dateOfJoining != null
                    ? hailey.jobData.employment.dateOfJoining.ToString("yyyy-MM-dd")
                    : "Missing Data",

                // Last Day Of Employment -> oldUnitEndShareDate
                oldUnitEndShareDate = hailey?.jobData?.employment?.lastDayOfEmployment != null
                    ? hailey.jobData.employment.lastDayOfEmployment.ToString("yyyy-MM-dd")
                    : "Missing Data",

                // Date Of Joining -> sharableOnNewUnitFrom
                sharableOnNewUnitFrom = hailey?.jobData?.employment?.dateOfJoining != null
                    ? hailey.jobData.employment.dateOfJoining.ToString("yyyy-MM-dd")
                    : "Missing Data",

                // Department -> section
                section = !string.IsNullOrWhiteSpace(hailey?.organizationalBelonging?.departmentId)
                    ? hailey.organizationalBelonging.departmentId
                    : "Missing Data",

                // Cost Center -> costCentre
                costCentre = !string.IsNullOrWhiteSpace(hailey?.organizationalBelonging?.costCenterId)
                    ? hailey.organizationalBelonging.costCenterId
                    : "Missing Data",

                // Manager Employee Id -> reportingTo
                reportingTo = !string.IsNullOrWhiteSpace(hailey?.organizationalBelonging?.managerEmployeeId)
                    ? hailey.organizationalBelonging.managerEmployeeId
                    : "Missing Data"
            };
        }
    }
}