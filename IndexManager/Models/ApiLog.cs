using System;
using System.ComponentModel.DataAnnotations;

namespace IndexManager.Models
{
    public class ApiLog
    {
        /*
        public long ApiLogId { get; set; }

        public int? SiteUserId { get; set; }

        public DateTime RequestAt { get; set; }

        public DateTime ResponseAt { get; set; }

        [StringLength(500)]
        public string Url { get; set; }

        [StringLength(100)]
        public string Method { get; set; }

        [StringLength(20)]
        public string RequestIp { get; set; }

        [StringLength(255)]
        public string CredentialLogin { get; set; }

        public string RequestBody { get; set; }

        public int? ResponseHttpStatus { get; set; }

        public string ResponseBody { get; set; }

        //public virtual SiteUser SiteUser { get; set; }
        */
        public string Id { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        public int? SiteUserId { get; set; }
        public string AuthorizationType { get; set; }
        public int? CompanyInformationId { get; set; }
        public string UserName { get; set; }
        public string UserRoles { get; set; }

        public string ApiName { get; set; }
        public string Source { get; set; }
        public string QueryStringParameters { get; set; }

        public string RequestIp { get; set; }
        public DateTime? RequestOn { get; set; }
        public string RequestBody { get; set; }

        public DateTime? ResponseOn { get; set; }
        public string ResponseBody { get; set; }
        public int? HttpCode { get; set; }

        public double Timestamp { get; set; }
        public string ServerName { get; set; }

        public double ExpiredOn { get; set; }
    }
}
