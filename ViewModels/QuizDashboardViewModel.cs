using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels
{
    /// <summary>
    /// ViewModel para el Dashboard de gestión de quizzes del Docente.
    /// Contiene métricas de resumen y la lista de quizzes agrupados por curso.
    /// </summary>
    public class QuizDashboardViewModel
    {
        // Métricas rápidas
        public int TotalQuizzes { get; set; }
        public int ActiveQuizzes { get; set; }
        public int PendingAttempts { get; set; }

        // Quizzes agrupados por curso
        public List<CourseQuizGroup> CourseGroups { get; set; } = new();
    }

    /// <summary>
    /// Agrupación de quizzes por curso para la tabla de gestión.
    /// </summary>
    public class CourseQuizGroup
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public List<QuizSummaryItem> Quizzes { get; set; } = new();
    }

    /// <summary>
    /// Resumen de un quiz individual para la tabla de gestión.
    /// </summary>
    public class QuizSummaryItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string StateName { get; set; }
        public int TimeLimitMinutes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public int MaxAttempts { get; set; }
        public int TotalAttempts { get; set; }
        public int QuestionCount { get; set; }
    }
}
