#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Common
{
    public class LegacyEmailQueueDTO
    {
        public DateTime ToSend { get; set; }
        public required string Email { get; set; }
        public string CC { get; set; }
        public required string Subject { get; set; }
        public required string Body { get; set; }
        public List<LegacyAttachment> Attachments { get; set; } = new();
        public bool IsGxp { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618
