using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StudyGo.Models;

namespace StudyGo.ViewModels.Cursos
{
    public class CursosListViewModel
    {
        public Guid UserId { get; set; }
        public string Role { get; set; }
        public List<CursoItemViewModel> Cursos { get; set; } = new List<CursoItemViewModel>();

        // Métricas reales calculadas desde los datos del servicio
        public int TotalPendingTasks { get; set; }
        public int TotalGradedTasks { get; set; }
        public decimal? AverageGrade { get; set; }
        public int TotalPendingGrading { get; set; } // Para docentes/admin
    }

    public class CursoItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string TeacherName { get; set; }
        public string TeacherEmail { get; set; }
        public int StudentCount { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsEnrolled { get; set; } // Para vista Explorar
    }

    public class CursoDetalleViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string TeacherEmail { get; set; }
        public string Role { get; set; }
        public string ActiveTab { get; set; }
        public bool IsDriveConnected { get; set; }
        public bool IsEnrolled { get; set; }

        public List<ActivityItemViewModel> Activities { get; set; } = new List<ActivityItemViewModel>();
        public List<DriveFileItemViewModel> Materials { get; set; } = new List<DriveFileItemViewModel>();
        public List<MemberItemViewModel> Members { get; set; } = new List<MemberItemViewModel>();
    }

    public class ActivityItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public string StudentSubmissionStatus { get; set; }
        public decimal? Grade { get; set; }
        public string Language { get; set; }
    }

    public class DriveFileItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string OwnerName { get; set; }
    }

    public class MemberItemViewModel
    {
        public Guid StudentId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public DateTime EnrolledAt { get; set; }
    }

    public class CursoCrearEditarViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El nombre del curso es obligatorio.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres.")]
        [Display(Name = "Nombre del curso")]
        public string Name { get; set; }

        [Required(ErrorMessage = "La materia o código es obligatorio.")]
        [StringLength(50, ErrorMessage = "El código no debe superar los 50 caracteres.")]
        [Display(Name = "Código / Materia")]
        public string Code { get; set; }
    }

    public class ExplorarCursosViewModel
    {
        public Guid UserId { get; set; }
        public string Role { get; set; }
        public List<CursoItemViewModel> Cursos { get; set; } = new List<CursoItemViewModel>();
    }
}
