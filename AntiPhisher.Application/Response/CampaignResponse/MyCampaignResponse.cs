namespace AntiPhisher.Application.Response.CampaignResponse
{
    public class MyCampaignResponse
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? CompanyId { get; set; }
    }
}
