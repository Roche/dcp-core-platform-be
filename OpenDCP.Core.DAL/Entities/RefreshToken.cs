namespace OpenDCP.Core.DAL.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string? UserIdentifier { get; set; }
        public string? Token { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsRevoked { get; set; }
    }
}
