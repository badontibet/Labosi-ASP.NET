using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NasIndexer.Model
{
    public class SystemAdmin
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastLogin { get; set; }
        public virtual ICollection<NasServer> ManagedServers { get; set; } = new List<NasServer>();
    }
}
