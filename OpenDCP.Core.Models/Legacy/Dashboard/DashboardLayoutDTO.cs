namespace OpenDCP.Core.Models.Legacy.Dashboard
{
    [Obsolete]
    public class DashboardLayoutDTO
    {
        public Guid DashboardTabId { get; set; }
        public List<DashboardLayoutDetailsDTO> DashboardLayoutDetails { get; set; }
        public int CurrentUserId { get; set; }
        public bool IsGxp { get; set; }
    }
}
