using System;
using System.Collections.Generic;

namespace NasIndexer.Model
{
    public class NasServer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastScan { get; set; }
        public List<ScanJob> ScanJobs { get; set; } = new List<ScanJob>();
    }
}