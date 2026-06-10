namespace NasIndexer.Dtos
{
    public class NasServerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastScan { get; set; }
        public int ScanJobCount { get; set; }
        public int ManagedAdminCount { get; set; }
    }
}
