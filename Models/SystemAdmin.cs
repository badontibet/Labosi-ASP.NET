using System;
using System.Collections.Generic;

namespace NasIndexer.Model
{
    public class SystemAdmin
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastLogin { get; set; }
        public List<NasServer> ManagedServers { get; set; } = new List<NasServer>();
    }
}