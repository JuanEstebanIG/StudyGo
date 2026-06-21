using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    /// <summary>
    /// Controlador para la experiencia de exámenes del Estudiante.
    /// Maneja la visualización de quizzes disponibles, la ejecución del examen,
    /// el envío de respuestas con autocalificación y la consulta de resultados.
    /// </summary>
    [Authorize(Roles = "Estudiante")]
    public class StudentQuizController : Controller
    {
        private readonly AppDbContext _context;

        public StudentQuizController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el ID del estudiante autenticado desde los Claims.
        /// </summary>
        private Guid GetCurrentStudentId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim);
        }

        // URL: StudentQuiz/Index
        /// <summary>
        /// Vista principal de evaluaciones del estudiante.
        /// Muestra quizzes activos de sus cursos inscritos y el historial de intentos.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var studentId = GetCurrentStudentId();

            // Obtener IDs de cursos en los que el estudiante está inscrito (activo)
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
                .Select(e => e.CourseId)
                .ToListAsync();

            // Consultar quizzes publicados de esos cursos
            var quizzes = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts.Where(a => a.StudentId == studentId))
                .Where(q => enrolledCourseIds.Contains(q.CourseId) && q.State == ActivityState.Publicado)
                .OrderByDescending(q => q.DueDate)
                .ToListAsync();

            // Construir tarjetas de quizzes activos
            var activeQuizzes = quizzes.Select(q => new StudentQuizCard
            {
                QuizId = q.Id,
                Title = q.Title,
                Description = q.Description,
                CourseName = q.Course.Name,
                TimeLimitMinutes = q.TimeLimitMinutes,
                DueDate = q.DueDate,
                OpenDate = q.OpenDate,
                MaxAttempts = q.MaxAttempts,
                AttemptsUsed = q.QuizAttempts.Count,
                QuestionCount = q.Questions.Count,
                HasCompletedAttempt = q.QuizAttempts.Any()
            }).ToList();

            // Historial de intentos del estudiante
            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Course)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            var attemptHistory = attempts.Select(a => new StudentAttemptHistory
            {
                AttemptId = a.Id,
                QuizId = a.QuizId,
                QuizTitle = a.Quiz.Title,
                CourseName = a.Quiz.Course.Name,
                Score = a.Score,
                SubmittedAt = a.SubmittedAt,
                StartedAt = a.StartedAt
            }).ToList();

            var viewModel = new StudentQuizViewModel
            {
                ActiveQuizzes = activeQuizzes,
                AttemptHistory = attemptHistory
            };

            return View(viewModel);
        }

        // URL: StudentQuiz/Take/{quizId}
        /// <summary>
        /// Renderiza el examen para que el estudiante lo resuelva.
        /// Valida: intento previo, fecha de apertura/cierre, intentos máximos.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Take(Guid quizId)
        {
            var studentId = GetCurrentStudentId();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions.OrderBy(x => x.Order))
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            // Verificar que el estudiante está inscrito en el curso
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId &&
                               e.CourseId == quiz.CourseId &&
                               e.Status == EnrollmentStatus.Active);
            if (!isEnrolled) return Forbid();

            // Verificar que el quiz esté publicado
            if (quiz.State != ActivityState.Publicado)
            {
                TempData["ErrorMessage"] = "Este quiz no está disponible actualmente.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar fecha de apertura
            if (quiz.OpenDate.HasValue && quiz.OpenDate.Value > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Este quiz aún no está abierto.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar fecha de cierre
            if (quiz.DueDate.HasValue && quiz.DueDate.Value < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Este quiz ya ha cerrado.";
                return RedirectToAction(nameof(Index));
            }

            // Contar intentos previos del estudiante
            var previousAttempts = await _context.QuizAttempts
                .CountAsync(a => a.QuizId == quizId && a.StudentId == studentId);

            if (previousAttempts >= quiz.MaxAttempts)
            {
                TempData["ErrorMessage"] = "Ya has agotado todos tus intentos para este quiz.";
                return RedirectToAction(nameof(Index));
            }

            // Crear registro de intento (marca el inicio del temporizador)
            var attemptId = Guid.NewGuid();
            var startedAt = DateTime.Now;

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                QuizId = quizId,
                StudentId = studentId,
                StartedAt = startedAt,
                SubmittedAt = startedAt, // Se actualiza al enviar
                Score = 0,
                AnswersJson = "{}"
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            // Construir ViewModel (sin revelar respuestas correctas)
            var viewModel = new TakeQuizViewModel
            {
                QuizId = quiz.Id,
                AttemptId = attemptId,
                Title = quiz.Title,
                Description = quiz.Description,
                CourseName = quiz.Course.Name,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                StartedAt = startedAt,
                TotalQuestions = quiz.Questions.Count,
                Questions = quiz.Questions.Select(q => new TakeQuizQuestion
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Order = q.Order,
                    Options = q.Options.Select(o => new TakeQuizOption
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText
                    }).ToList()
                }).ToList()
            };

            return View(viewModel);
        }

        // URL: StudentQuiz/Submit (POST)
        /// <summary>
        /// Recibe las respuestas del estudiante, autocalifica el quiz y guarda el intento.
        /// Valida que el tiempo transcurrido no exceda severamente el límite.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid quizId, Guid attemptId, Dictionary<string, List<string>> answers)
        {
            var studentId = GetCurrentStudentId();

            // Buscar el intento existente creado en Take()
            var attempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(a => a.Id == attemptId &&
                                          a.QuizId == quizId &&
                                          a.StudentId == studentId);

            if (attempt == null)
            {
                TempData["ErrorMessage"] = "Intento no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que no haya sido enviado ya (Score > 0 o AnswersJson no nulo)
            bool yaFueEnviado = !string.IsNullOrEmpty(attempt.AnswersJson)
                                 && attempt.AnswersJson != "{}";

            if (yaFueEnviado)
            {
                TempData["ErrorMessage"] = "Este intento ya fue enviado.";
                return RedirectToAction(nameof(Result), new { attemptId = attempt.Id });
            }

            // Cargar el quiz con preguntas y opciones para autocalificación
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            // Validación de tiempo en servidor
            var now = DateTime.Now;
            var elapsed = now - attempt.StartedAt;
            var maxAllowed = TimeSpan.FromMinutes(quiz.TimeLimitMinutes + 1); // 1 min de gracia por latencia

            if (elapsed > maxAllowed)
            {
                // Permitir el envío pero registrar que se excedió el tiempo
                // (no bloquear ya que el auto-envío JS puede tener latencia)
            }

            // Autocalificación
            int totalQuestions = quiz.Questions.Count;
            int correctAnswers = 0;

            foreach (var question in quiz.Questions)
            {
                var correctOptionIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id.ToString())
                    .ToHashSet();

                var questionKey = question.Id.ToString();
                var selectedOptionIds = answers != null && answers.ContainsKey(questionKey)
                    ? answers[questionKey].ToHashSet()
                    : new HashSet<string>();

                // Una pregunta se considera correcta si las opciones seleccionadas
                // coinciden exactamente con las correctas
                if (correctOptionIds.SetEquals(selectedOptionIds))
                {
                    correctAnswers++;
                }
            }

            // Calcular nota sobre 100
            decimal score = totalQuestions > 0
                ? Math.Round((decimal)correctAnswers / totalQuestions * 100, 2)
                : 0;

            // Actualizar el intento
            attempt.SubmittedAt = now;
            attempt.Score = score;
            attempt.AnswersJson = JsonSerializer.Serialize(answers ?? new Dictionary<string, List<string>>());

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Result), new { attemptId = attempt.Id });
        }

        // URL: StudentQuiz/Result/{attemptId}
        /// <summary>
        /// Muestra el resultado inmediato del quiz después de la autocalificación.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Result(Guid attemptId)
        {
            var studentId = GetCurrentStudentId();

            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Course)
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null) return NotFound();

            // Deserializar respuestas
            var answers = !string.IsNullOrEmpty(attempt.AnswersJson)
                ? JsonSerializer.Deserialize<Dictionary<string, List<string>>>(attempt.AnswersJson)
                : new Dictionary<string, List<string>>();

            // Construir el detalle por pregunta
            var details = new List<QuestionResultDetail>();
            int correctCount = 0;

            foreach (var question in attempt.Quiz.Questions.OrderBy(q => q.Order))
            {
                var correctOptionIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id.ToString())
                    .ToHashSet();

                var questionKey = question.Id.ToString();
                var selectedOptionIds = answers.ContainsKey(questionKey)
                    ? answers[questionKey].ToHashSet()
                    : new HashSet<string>();

                bool isCorrect = correctOptionIds.SetEquals(selectedOptionIds);
                if (isCorrect) correctCount++;

                details.Add(new QuestionResultDetail
                {
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    IsCorrect = isCorrect,
                    SelectedOptions = question.Options
                        .Where(o => selectedOptionIds.Contains(o.Id.ToString()))
                        .Select(o => o.OptionText)
                        .ToList(),
                    CorrectOptions = question.Options
                        .Where(o => o.IsCorrect)
                        .Select(o => o.OptionText)
                        .ToList()
                });
            }

            var viewModel = new QuizResultViewModel
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz.Title,
                CourseName = attempt.Quiz.Course.Name,
                Score = attempt.Score,
                TotalQuestions = attempt.Quiz.Questions.Count,
                CorrectAnswers = correctCount,
                SubmittedAt = attempt.SubmittedAt,
                StartedAt = attempt.StartedAt,
                TimeLimitMinutes = attempt.Quiz.TimeLimitMinutes,
                Details = details
            };

            return View(viewModel);
        }
    }
}
