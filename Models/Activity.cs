using System;
using StudyGo.Enums;

namespace StudyGo.Models
{
    public abstract class Activity
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ActivityState State { get; set; }

        // Campo nuevo para la fecha límite de la entrega
        public DateTime? DueDate { get; set; }

        public Course Course { get; set; }
    }
}