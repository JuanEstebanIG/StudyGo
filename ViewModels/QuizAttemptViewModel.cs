using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class QuizAttemptViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El quiz es obligatorio.")]
        [Display(Name = "Quiz")]
        public Guid QuizId { get; set; }

        [Required(ErrorMessage = "El estudiante es obligatorio.")]
        [Display(Name = "Estudiante")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "El puntaje es obligatorio.")]
        [Range(0, 100, ErrorMessage = "El puntaje debe estar entre 0 y 100.")]
        [Display(Name = "Puntaje")]
        public decimal Score { get; set; }

        [Required(ErrorMessage = "La fecha de envío es obligatoria.")]
        [Display(Name = "Enviado el")]
        public DateTime SubmittedAt { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [Display(Name = "Iniciado el")]
        public DateTime StartedAt { get; set; }

        [Display(Name = "Respuestas")]
        public string AnswersJson { get; set; }

        /// <summary>
        /// Tiempo utilizado por el estudiante (calculado).
        /// </summary>
        public string TimeSpentDisplay
        {
            get
            {
                var diff = SubmittedAt - StartedAt;
                return $"{(int)diff.TotalMinutes}:{diff.Seconds:D2}";
            }
        }
    }
}