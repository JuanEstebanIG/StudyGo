using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels.Tareas
{
    public class TareaDetalleViewModel
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int MemoryLimitMb { get; set; }
        public string RubricTitle { get; set; }

        public Guid SubmissionId { get; set; }
        public string SubmissionStatus { get; set; } // "SinEmpezar", "EnProgreso", "Enviado", "Calificado"
        public string CurrentCode { get; set; }
        public decimal? FinalScore { get; set; }

        public List<VersionItemViewModel> Versions { get; set; } = new List<VersionItemViewModel>();
    }

    public class VersionItemViewModel
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public DateTime SavedAt { get; set; }
        public string Status { get; set; } // "En progreso" u "Oficial" (cuando se entregó)
    }

    public class TareaRevisionViewModel
    {
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string RubricTitle { get; set; }

        // Estudiante bajo revisión
        public Guid? SelectedStudentId { get; set; }
        public string SelectedStudentName { get; set; }
        public Guid? SelectedSubmissionId { get; set; }
        public string SelectedSubmissionStatus { get; set; }
        public string SelectedCode { get; set; }
        public decimal? FinalScore { get; set; }
        public string Feedback { get; set; }

        public List<StudentSubmissionItemViewModel> Submissions { get; set; } = new List<StudentSubmissionItemViewModel>();
        public List<VersionItemViewModel> Versions { get; set; } = new List<VersionItemViewModel>();
    }

    public class StudentSubmissionItemViewModel
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string Status { get; set; }
        public decimal? Score { get; set; }
        public DateTime? LastUpdate { get; set; }
    }

    public class EjecutarCodigoRequest
    {
        public string Code { get; set; }
        public string Language { get; set; }
        public Guid? TaskId { get; set; }
    }

    public class EjecutarCodigoResponse
    {
        public string Status { get; set; } // "EnCola", "Ejecutando", "Completado", "Error", "Timeout", "Pendiente"
        public string Stdout { get; set; }
        public string Stderr { get; set; }
        public int ExecutionTimeMs { get; set; }
        public string Message { get; set; }
    }

    public class EntregarTareaRequest
    {
        public string Code { get; set; }
    }
}
