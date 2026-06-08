using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class Institution
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}