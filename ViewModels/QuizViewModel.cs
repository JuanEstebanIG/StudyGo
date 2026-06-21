using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyGo.Enums;

namespace StudyGo.ViewModels
{
    public class QuizViewModel : ActivityViewModel
    {
        [Required(ErrorMessage = "El modo de selección es obligatorio.")]
        [Display(Name = "Modo de selección")]
        public SelectionMode SelectionMode { get; set; }

        [Required(ErrorMessage = "El tiempo máximo es obligatorio.")]
        [Range(1, 300, ErrorMessage = "El tiempo debe estar entre 1 y 300 minutos.")]
        [Display(Name = "Tiempo máximo (minutos)")]
        public int TimeLimitMinutes { get; set; } = 30;

        [Display(Name = "Fecha límite")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Fecha de apertura")]
        [DataType(DataType.DateTime)]
        public DateTime? OpenDate { get; set; }

        [Required(ErrorMessage = "El número de intentos es obligatorio.")]
        [Range(1, 10, ErrorMessage = "Los intentos deben estar entre 1 y 10.")]
        [Display(Name = "Intentos permitidos")]
        public int MaxAttempts { get; set; } = 1;

        /// <summary>
        /// Lista de preguntas con sus opciones para el constructor dinámico.
        /// </summary>
        public List<QuizQuestionViewModel> Questions { get; set; } = new();

        /// <summary>
        /// Cursos disponibles para asignar el quiz (poblado por el controlador).
        /// </summary>
        public List<SelectListItem> AvailableCourses { get; set; } = new();

        /// <summary>
        /// Nombre del curso (solo lectura, para vistas de listado).
        /// </summary>
        public string CourseName { get; set; }
    }
}