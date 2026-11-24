using System.ComponentModel.DataAnnotations;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.BatchIntermediate
{
    public class LegacyBatchIntermediateDTO
    {
        public int ID { get; set; }
        public int SiteId { get; set; }
        [StringLength(50)]
        public string? BatchID { get; set; }
        [StringLength(50)]
        public string? BatchStartTime { get; set; }

        public string? JBatchValues { get; set; }
        [StringLength(50)]
        public string? EventFrameId { get; set; }

        public string? DeviceWebId { get; set; }
        /// <summary>
        /// True if at application has detected   "Good": false inside JBatchValues
        ///  {
        ///  "Items": [
        ///    {
        ///      "Name": "BatchID",
        ///      "Value": {
        ///        "Value": "2019-06-06 15:52:22",
        ///        "Good": true
        ///      }
        ///      },
        ///    {
        ///      "Name": "Program",
        ///      "Value": {
        ///        "Value": {
        ///          "Name": "No Data",
        ///          "Value": 248,
        ///          "IsSystem": true
        ///        },
        ///        "Good": false
        ///      }
        ///    },
        ///    {
        ///      "Name": "Station",
        ///      "Value": {
        ///        "Value": {
        ///          "Name": "?11",
        ///          "Value": 11,
        ///          "IsSystem": true
        ///        },
        ///        "Good": false
        ///      }
        ///    }
        ///  ]
        ///}
        /// </summary>
        public bool BadValue { get; set; }
        /// <summary>
        /// 10 attempts(in App.config) as limit. If we are exceeding this limit we stop update Attempts and we should be notified via mail.
        /// </summary>
        public int UpdateAttems { get; set; }
        /// <summary>
        /// True if notification via email is send in case of 10 attempts with bad values
        /// </summary>
        public bool IsNotifiedSend { get; set; }
        public bool IsNewSignUpCreated { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618