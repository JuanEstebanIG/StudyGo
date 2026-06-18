using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Models;
using StudyGo.Services;
using StudyGo.ViewModels.Calificaciones;

namespace StudyGo.Controllers
{
    public class CalificacionesController : Controller
    {
        private readonly IAcademicService _academicService;

        public CalificacionesController(IAcademicService academicService)
        {
            _academicService = academicService;
        }

        private string GetCurrentRole()
        {
            return Request.Cookies["StudyGo_Role"] ?? "Estudiante";
        }

        private Guid GetCurrentUserId()
        {
            return GetCurrentRole() == "Docente" ? AcademicService.DocenteId : AcademicService.Estudiante1Id;
        }

        // GET: /Calificaciones/Estudiante?cursoId={cursoId}
        public async Task<IActionResult> Estudiante(Guid cursoId)
        {
            var course = await _academicService.GetCourseDetailAsync(cursoId);
            if (course == null) return NotFound();

            var studentId = GetCurrentUserId();
            var student = course.Enrollments.FirstOrDefault(e => e.StudentId == studentId)?.Student;
            var grades = await _academicService.GetStudentGradesAsync(cursoId, studentId);

            var vm = new EstudianteCalificacionesViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name,
                StudentName = student?.DisplayName ?? "Estudiante",
                TotalTasksCount = course.Activities.Count(a => a is ProgrammingTask),
                CompletedTasksCount = grades.Count(g => g.Submission.Status == StudyGo.Enums.SubmissionStatus.Calificado || g.Submission.Status == StudyGo.Enums.SubmissionStatus.Enviado),
                AverageScore = grades.Any() ? grades.Average(g => g.FinalScore) : 0.0m,
                Grades = course.Activities.Select(a =>
                {
                    var isTask = a is ProgrammingTask;
                    var grade = grades.FirstOrDefault(g => g.Submission.ProgrammingTaskId == a.Id);
                    return new EstudianteCalificacionItemViewModel
                    {
                        ActivityId = a.Id,
                        ActivityTitle = a.Title,
                        ActivityType = isTask ? "Tarea de Programación" : "Quiz",
                        Score = grade?.FinalScore,
                        Status = grade != null ? "Calificada" : (isTask ? "Sin Entregar" : "Autocalificado"),
                        GradedAt = grade?.GradedAt,
                        Feedback = grade != null ? "Tu código cumple con todos los casos de prueba establecidos." : ""
                    };
                }).ToList()
            };

            return View(vm);
        }

        // GET: /Calificaciones/Docente?cursoId={cursoId}
        public async Task<IActionResult> Docente(Guid cursoId)
        {
            if (GetCurrentRole() != "Docente") return Forbid();

            var course = await _academicService.GetCourseDetailAsync(cursoId);
            if (course == null) return NotFound();

            var gradebook = await _academicService.GetCourseGradebookAsync(cursoId);
            var activities = course.Activities.ToList();
            var enrollments = course.Enrollments.ToList();

            var studentsList = new List<StudentRowViewModel>();
            foreach (var enroll in enrollments)
            {
                var row = new StudentRowViewModel
                {
                    StudentId = enroll.StudentId,
                    StudentName = enroll.Student?.DisplayName ?? "Estudiante",
                    StudentEmail = enroll.Student?.Email ?? "",
                    Grades = new Dictionary<Guid, string>()
                };

                decimal totalScore = 0;
                int gradedCount = 0;

                foreach (var act in activities)
                {
                    var sub = gradebook.FirstOrDefault(s => s.ProgrammingTaskId == act.Id && s.StudentId == enroll.StudentId);
                    if (sub != null)
                    {
                        if (sub.Status == StudyGo.Enums.SubmissionStatus.Calificado && sub.Grade != null)
                        {
                            row.Grades[act.Id] = sub.Grade.FinalScore.ToString("0.#");
                            totalScore += sub.Grade.FinalScore;
                            gradedCount++;
                        }
                        else
                        {
                            row.Grades[act.Id] = "Entregado";
                        }
                    }
                    else
                    {
                        row.Grades[act.Id] = "-";
                    }
                }

                row.AverageScore = gradedCount > 0 ? (totalScore / gradedCount) : 0.0m;
                studentsList.Add(row);
            }

            // Estadísticas agregadas
            var finalGrades = gradebook.Where(s => s.Status == StudyGo.Enums.SubmissionStatus.Calificado && s.Grade != null).Select(s => s.Grade.FinalScore).ToList();
            decimal average = finalGrades.Any() ? finalGrades.Average() : 0.0m;

            int approved = studentsList.Count(s => s.AverageScore >= 60.0m);
            int disapproved = studentsList.Count - approved;

            // Distribución de puntuaciones: [0-59, 60-69, 70-79, 80-89, 90-100]
            int[] distribution = new int[5];
            foreach (var student in studentsList)
            {
                if (student.AverageScore < 60) distribution[0]++;
                else if (student.AverageScore < 70) distribution[1]++;
                else if (student.AverageScore < 80) distribution[2]++;
                else if (student.AverageScore < 90) distribution[3]++;
                else distribution[4]++;
            }

            var vm = new DocenteCalificacionesViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name,
                Activities = activities.Select(a => new ActivityHeaderViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Type = a is ProgrammingTask ? "Tarea" : "Quiz"
                }).ToList(),
                Students = studentsList,
                CourseAverage = average,
                ApprovedCount = approved,
                DisapprovedCount = disapproved,
                ScoreDistribution = distribution
            };

            return View(vm);
        }
    }
}
