namespace OpenDCP.Core.Models.Legacy.Common
{

#pragma warning disable CS1591
#pragma warning disable CS8618
    public class LegacyAttachment
    {
        public string FileName { get; set; }
        public bool IsEmbedded { get; set; }
        /// <summary>
        /// File extension
        /// </summary>
        public string MediaType { get; set; }
        public string Data { get; set; }
        public Guid ContentId { get; set; }
    }

#pragma warning restore CS1591
#pragma warning restore CS8618
}
