namespace NasIndexer.Dtos
{
    public class SystemAdminDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastLogin { get; set; }
        public List<int> ManagedNasServerIds { get; set; } = new();
        public List<NasServerSummaryDto> ManagedNasServers { get; set; } = new();
    }
}
