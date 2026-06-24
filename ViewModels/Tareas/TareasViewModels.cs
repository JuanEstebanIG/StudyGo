using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StudyGo.ViewModels; // Para RubricCriteriaViewModel

namespace StudyGo.ViewModels.Tareas
{
    // ── ViewModel para el listado general de tareas (Index) ─────────────────
    public class TareasIndexViewModel
    {
        public string Role { get; set; } = string.Empty;
        public List<TareaListItemViewModel> Tareas { get; set; } = new();
    }

    public class TareaListItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string Language { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        // Para Estudiante
        public string SubmissionStatus { get; set; } = string.Empty;
        public decimal? Grade { get; set; }

        // Para Docente / Admin
        public int TotalSubmissions { get; set; }
        public int PendingGrading { get; set; }
        public int Graded { get; set; }
    }

    // ── ViewModel de detalle de tarea (editor de código para Estudiante / vista previa para Docente/Admin) ─────
    public class TareaDetalleViewModel
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int MemoryLimitMb { get; set; }
        public string RubricTitle { get; set; } = string.Empty;

        /// <summary>Rol del usuario actual: Estudiante, Docente o Administrador.</summary>
        public string Role { get; set; } = "Estudiante";

        public Guid SubmissionId { get; set; }
        public string SubmissionStatus { get; set; } = string.Empty;
        public string CurrentCode { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }

        public List<VersionItemViewModel> Versions { get; set; } = new List<VersionItemViewModel>();
    }

    public class VersionItemViewModel
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public DateTime SavedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ── ViewModel de revisión de entregas (Docente) ──────────────────────────
    public class TareaRevisionViewModel
    {
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string RubricTitle { get; set; } = string.Empty;

        public Guid? SelectedStudentId { get; set; }
        public string SelectedStudentName { get; set; } = string.Empty;
        public Guid? SelectedSubmissionId { get; set; }
        public string SelectedSubmissionStatus { get; set; } = string.Empty;
        public string SelectedCode { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public string Feedback { get; set; } = string.Empty;

        public List<StudentSubmissionItemViewModel> Submissions { get; set; } = new List<StudentSubmissionItemViewModel>();
        public List<VersionItemViewModel> Versions { get; set; } = new List<VersionItemViewModel>();
        // Rúbrica
        public List<RubricCriteriaViewModel> RubricCriteria { get; set; } = new List<RubricCriteriaViewModel>();
        public List<CriterionEvaluationInputModel> CriterionEvaluations { get; set; } = new List<CriterionEvaluationInputModel>();
    }

    public class StudentSubmissionItemViewModel
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? Score { get; set; }
        public DateTime? LastUpdate { get; set; }
    }

    // ── Peticiones API del sandbox ────────────────────────────────────────────
    public class EjecutarCodigoRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public Guid? TaskId { get; set; }
    }

    public class EjecutarCodigoResponse
    {
        public string Status { get; set; } = string.Empty;
        public string? Stdout { get; set; }
        public string? Stderr { get; set; }
        public int ExecutionTimeMs { get; set; }
        public string? Message { get; set; }
    }

    public class EntregarTareaRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    // ── ViewModel para crear/editar tareas (Docente / Admin) ─────────────────
    public class TareaCrearEditarViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "El enunciado es obligatorio.")]
        [Display(Name = "Enunciado / Descripción")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "El lenguaje es obligatorio.")]
        [Display(Name = "Lenguaje")]
        public string Language { get; set; } = "C#";

        [Required]
        [Range(1, 300, ErrorMessage = "Entre 1 y 300 segundos.")]
        [Display(Name = "Límite de tiempo (segundos)")]
        public int TimeLimitSeconds { get; set; } = 10;

        [Required]
        [Range(16, 1024, ErrorMessage = "Entre 16 y 1024 MB.")]
        [Display(Name = "Límite de memoria (MB)")]
        public int MemoryLimitMb { get; set; } = 256;

        [Required(ErrorMessage = "El curso es obligatorio.")]
        [Display(Name = "Curso")]
        public Guid CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        // Para poblar el selector de cursos disponibles
        public List<(Guid Id, string Name)> AvailableCourses { get; set; } = new();
        // Rúbrica
        public List<RubricCriteriaInputModel> RubricCriteria { get; set; } = new List<RubricCriteriaInputModel>();
        public bool HasSubmissions { get; set; }
    }
}
