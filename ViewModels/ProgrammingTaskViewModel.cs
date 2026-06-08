using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class ProgrammingTaskViewModel : ActivityViewModel
    {
        [Required(ErrorMessage = "El lenguaje de programación es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El lenguaje debe tener entre 1 y 50 caracteres.")]
        [Display(Name = "Lenguaje")]
        public string Language { get; set; }

        [Required(ErrorMessage = "El límite de tiempo es obligatorio.")]
        [Range(1, 300, ErrorMessage = "El límite de tiempo debe estar entre 1 y 300 segundos.")]
        [Display(Name = "Límite de tiempo (segundos)")]
        public int TimeLimitSeconds { get; set; }

        [Required(ErrorMessage = "El límite de memoria es obligatorio.")]
        [Range(16, 4096, ErrorMessage = "El límite de memoria debe estar entre 16 y 4096 MB.")]
        [Display(Name = "Límite de memoria (MB)")]
        public int MemoryLimitMb { get; set; }
    }
}