using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels.Calificaciones
{
    public class EstudianteCalificacionesViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public string StudentName { get; set; }
        public decimal AverageScore { get; set; }
        public int CompletedTasksCount { get; set; }
        public int TotalTasksCount { get; set; }

        public List<EstudianteCalificacionItemViewModel> Grades { get; set; } = new List<EstudianteCalificacionItemViewModel>();
    }

    public class EstudianteCalificacionItemViewModel
    {
        public Guid ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public string ActivityType { get; set; } // "Tarea" o "Quiz"
        public decimal? Score { get; set; }
        public string Status { get; set; } // "Calificada", "Pendiente", "Sin Entregar"
        public DateTime? GradedAt { get; set; }
        public string Feedback { get; set; }
    }

    public class DocenteCalificacionesViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }

        public List<ActivityHeaderViewModel> Activities { get; set; } = new List<ActivityHeaderViewModel>();
        public List<StudentRowViewModel> Students { get; set; } = new List<StudentRowViewModel>();

        // Estadísticas para Chart.js
        public decimal CourseAverage { get; set; }
        public int ApprovedCount { get; set; }
        public int DisapprovedCount { get; set; }
        public int[] ScoreDistribution { get; set; } // Rango: [0-59, 60-69, 70-79, 80-89, 90-100]
    }

    public class ActivityHeaderViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
    }

    public class StudentRowViewModel
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public Dictionary<Guid, string> Grades { get; set; } = new Dictionary<Guid, string>(); // TareaId -> Nota ("-" si no entregado, o puntuación)
        public decimal AverageScore { get; set; }
    }
}
