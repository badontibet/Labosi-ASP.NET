using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NasIndexer.Model
{
    public class NasServer
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastScan { get; set; }
        public virtual ICollection<ScanJob> ScanJobs { get; set; } = new List<ScanJob>();
        public virtual ICollection<SystemAdmin> ManagedAdmins { get; set; } = new List<SystemAdmin>();
    }
}
