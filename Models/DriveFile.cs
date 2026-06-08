using System;

namespace StudyGo.Models
{
    public class DriveFile
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public Guid CourseId { get; set; }
        public string DriveFileId { get; set; }
        public string Url { get; set; }

        public User Owner { get; set; }
        public Course Course { get; set; }
    }
}