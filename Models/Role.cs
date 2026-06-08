using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
