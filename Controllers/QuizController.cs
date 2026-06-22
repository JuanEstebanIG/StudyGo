using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    /// <summary>
    /// Controlador administrativo para la gestión de Quizzes (Docente/Administrador).
    /// Maneja el CRUD completo de evaluaciones y las estadísticas de intentos.
    /// </summary>
    [Authorize(Roles = "Docente,Administrador")]
    public class QuizController : Controller
    {
        private readonly AppDbContext _context;

        public QuizController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde los Claims.
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim);
        }

        /// <summary>
        /// Indica si el usuario autenticado tiene el rol de Administrador.
        /// </summary>
        private bool IsAdmin() => User.IsInRole("Administrador");

        // URL: Quiz/Index
        /// <summary>
        /// Dashboard unificado de Quizzes.
        /// - Administrador: ve todos los quizzes de todos los cursos.
        /// - Docente: ve únicamente los quizzes de sus propios cursos.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // Consulta base con todas las relaciones necesarias
            var query = _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts)
                .AsQueryable();

            // El docente solo ve los quizzes de sus cursos
            if (!IsAdmin())
            {
                var teacherCourseIds = await _context.Courses
                    .Where(c => c.TeacherId == userId)
                    .Select(c => c.Id)
                    .ToListAsync();

                query = query.Where(q => teacherCourseIds.Contains(q.CourseId));
            }

            var quizzes = await query
                .OrderByDescending(q => q.DueDate)
                .ToListAsync();

            // Construir el ViewModel del dashboard
            var viewModel = new QuizDashboardViewModel
            {
                TotalQuizzes = quizzes.Count,
                ActiveQuizzes = quizzes.Count(q => q.State == ActivityState.Publicado),
                PendingAttempts = quizzes.Sum(q => q.QuizAttempts.Count),
                CourseGroups = quizzes
                    .GroupBy(q => new { q.CourseId, q.Course.Name })
                    .Select(g => new CourseQuizGroup
                    {
                        CourseId = g.Key.CourseId,
                        CourseName = g.Key.Name,
                        Quizzes = g.Select(q => new QuizSummaryItem
                        {
                            Id = q.Id,
                            Title = q.Title,
                            StateName = q.State.ToString(),
                            TimeLimitMinutes = q.TimeLimitMinutes,
                            DueDate = q.DueDate,
                            OpenDate = q.OpenDate,
                            MaxAttempts = q.MaxAttempts,
                            TotalAttempts = q.QuizAttempts.Count,
                            QuestionCount = q.Questions.Count
                        }).ToList()
                    }).ToList()
            };

            return View(viewModel);
        }

        // URL: Quiz/Create
        /// <summary>
        /// Formulario de creación de un nuevo Quiz.
        /// - Administrador: puede seleccionar cualquier curso de la plataforma.
        /// - Docente: solo ve sus propios cursos en el selector.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();

            var courses = await GetAvailableCoursesAsync(userId);

            var viewModel = new QuizViewModel
            {
                AvailableCourses = courses,
                State = ActivityState.Borrador,
                TimeLimitMinutes = 30,
                MaxAttempts = 1,
                Questions = new List<QuizQuestionViewModel>()
            };

            return View(viewModel);
        }

        // URL: Quiz/Create (POST)
        /// <summary>
        /// Procesa la creación de un nuevo Quiz con sus preguntas y opciones.
        /// - Administrador: puede crear en cualquier curso.
        /// - Docente: solo puede crear en sus propios cursos.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizViewModel model)
        {
            var userId = GetCurrentUserId();

            // Verificar permiso sobre el curso seleccionado
            var courseQuery = _context.Courses.Where(c => c.Id == model.CourseId);
            if (!IsAdmin())
            {
                courseQuery = courseQuery.Where(c => c.TeacherId == userId);
            }

            var course = await courseQuery.FirstOrDefaultAsync();
            if (course == null)
            {
                ModelState.AddModelError("CourseId", "No tienes permiso para crear quizzes en este curso.");
            }

            // Recargar cursos para el select en caso de error
            model.AvailableCourses = await GetAvailableCoursesAsync(userId);

            // Validar que tenga al menos una pregunta
            if (model.Questions == null || model.Questions.Count == 0)
            {
                ModelState.AddModelError("Questions", "Debe agregar al menos una pregunta al quiz.");
            }
            else
            {
                // Validar cada pregunta
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var question = model.Questions[i];
                    if (string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        ModelState.AddModelError($"Questions[{i}].QuestionText", $"La pregunta {i + 1} no tiene texto.");
                    }
                    if (question.Options == null || question.Options.Count < 2)
                    {
                        ModelState.AddModelError($"Questions[{i}].Options", $"La pregunta {i + 1} debe tener al menos 2 opciones.");
                    }
                    else if (!question.Options.Any(o => o.IsCorrect))
                    {
                        ModelState.AddModelError($"Questions[{i}].Options", $"La pregunta {i + 1} debe tener al menos una opción marcada como correcta.");
                    }
                }
            }

            // Remover validaciones de ModelState para propiedades que no vienen del form
            ModelState.Remove("CourseName");
            ModelState.Remove("AvailableCourses");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Crear la entidad Quiz
            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = model.CourseId,
                Title = model.Title,
                Description = model.Description,
                State = model.State,
                DueDate = model.DueDate,
                SelectionMode = model.SelectionMode,
                TimeLimitMinutes = model.TimeLimitMinutes,
                OpenDate = model.OpenDate,
                MaxAttempts = model.MaxAttempts,
                Questions = new List<QuizQuestion>()
            };

            // Mapear preguntas y opciones
            for (int i = 0; i < model.Questions.Count; i++)
            {
                var qvm = model.Questions[i];
                var question = new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    QuizId = quiz.Id,
                    QuestionText = qvm.QuestionText,
                    QuestionType = qvm.QuestionType,
                    Order = i,
                    Options = new List<QuizOption>()
                };

                if (qvm.Options != null)
                {
                    foreach (var ovm in qvm.Options)
                    {
                        question.Options.Add(new QuizOption
                        {
                            Id = Guid.NewGuid(),
                            QuizQuestionId = question.Id,
                            OptionText = ovm.OptionText,
                            IsCorrect = ovm.IsCorrect
                        });
                    }
                }

                quiz.Questions.Add(question);
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Quiz creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // URL: Quiz/Edit/{id}
        /// <summary>
        /// Formulario de edición de un Quiz existente.
        /// - Administrador: puede editar cualquier quiz de la plataforma.
        /// - Docente: solo puede editar quizzes de sus propios cursos.
        /// </summary>
        /// 
        /// 
        /// 
        
// URL: Quiz/Edit/{id}
/// <summary>
/// Formulario de edición de un Quiz existente.
/// - Administrador: puede editar cualquier quiz de la plataforma.
/// - Docente: solo puede editar quizzes de sus propios cursos.
/// </summary>
[HttpGet]
public async Task<IActionResult> Edit(Guid id)
{
    var userId = GetCurrentUserId();

    var quiz = await _context.Quizzes
        .Include(q => q.Course)
        .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(q => q.Id == id);

    if (quiz == null) return NotFound();

    if (!IsAdmin() && quiz.Course.TeacherId != userId)
        return Forbid();

    var courses = await GetAvailableCoursesAsync(userId);

    var viewModel = new QuizViewModel
    {
        Id = quiz.Id,
        CourseId = quiz.CourseId,
        Title = quiz.Title,
        Description = quiz.Description,
        State = quiz.State,
        DueDate = quiz.DueDate,
        SelectionMode = quiz.SelectionMode,
        TimeLimitMinutes = quiz.TimeLimitMinutes,
        OpenDate = quiz.OpenDate,
        MaxAttempts = quiz.MaxAttempts,
        AvailableCourses = courses,
        Questions = quiz.Questions
            .OrderBy(q => q.Order)
            .Select(q => new QuizQuestionViewModel
            {
                Id = q.Id,                  // ← Conservar ID para que el POST sepa que existe
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Order = q.Order,
                Options = q.Options.Select(o => new QuizOptionViewModel
                {
                    Id = o.Id,              // ← Conservar ID para que el POST sepa que existe
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
    };

    return View(viewModel);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(Guid id, QuizViewModel model)
{
    var userId = GetCurrentUserId();

    var quiz = await _context.Quizzes
        .Include(q => q.Course)
        .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(q => q.Id == id);

    if (quiz == null) return NotFound();
    if (!IsAdmin() && quiz.Course.TeacherId != userId) return Forbid();

    model.AvailableCourses = await GetAvailableCoursesAsync(userId);

    // ─── Validaciones ───
    if (model.Questions == null || model.Questions.Count == 0)
    {
        ModelState.AddModelError("Questions", "Debe agregar al menos una pregunta al quiz.");
    }
    else
    {
        for (int i = 0; i < model.Questions.Count; i++)
        {
            var q = model.Questions[i];
            if (string.IsNullOrWhiteSpace(q.QuestionText))
                ModelState.AddModelError($"Questions[{i}].QuestionText", $"La pregunta {i + 1} no tiene texto.");
            if (q.Options == null || q.Options.Count < 2)
                ModelState.AddModelError($"Questions[{i}].Options", $"La pregunta {i + 1} debe tener al menos 2 opciones.");
            else if (!q.Options.Any(o => o.IsCorrect))
                ModelState.AddModelError($"Questions[{i}].Options", $"La pregunta {i + 1} debe tener al menos una opción correcta.");
        }
    }

    ModelState.Remove("CourseName");
    ModelState.Remove("AvailableCourses");

    if (!ModelState.IsValid)
    {
        model.Id = id;
        return View(model);
    }

    // ─── Actualizar datos del quiz ───
    quiz.Title = model.Title;
    quiz.Description = model.Description;
    quiz.CourseId = model.CourseId;
    quiz.State = model.State;
    quiz.DueDate = model.DueDate;
    quiz.SelectionMode = model.SelectionMode;
    quiz.TimeLimitMinutes = model.TimeLimitMinutes;
    quiz.OpenDate = model.OpenDate;
    quiz.MaxAttempts = model.MaxAttempts;

    // 1) Eliminar todas las opciones existentes primero
    var existingOptions = quiz.Questions.SelectMany(q => q.Options).ToList();
    _context.QuizOptions.RemoveRange(existingOptions);

    // 2) Eliminar todas las preguntas existentes
    var existingQuestions = quiz.Questions.ToList();
    _context.QuizQuestions.RemoveRange(existingQuestions);

    // 3) Recrear TODO desde el modelo (como si fuera un quiz nuevo)
    for (int i = 0; i < model.Questions.Count; i++)
    {
        var qvm = model.Questions[i];
        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            QuestionText = qvm.QuestionText,
            QuestionType = qvm.QuestionType,
            Order = i,
            Options = new List<QuizOption>()
        };

        if (qvm.Options != null)
        {
            foreach (var ovm in qvm.Options)
            {
                question.Options.Add(new QuizOption
                {
                    Id = Guid.NewGuid(),
                    OptionText = ovm.OptionText,
                    IsCorrect = ovm.IsCorrect
                });
            }
        }

        _context.QuizQuestions.Add(question);
    }

    // 4) Guardar todo en una sola transacción atómica.
    //    Si falla, EF Core hace rollback automático: no se borró nada en la BD.
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "Quiz actualizado exitosamente.";
    return RedirectToAction(nameof(Index));
}
 
        // URL: Quiz/Delete/{id} (POST)
        /// <summary>
        /// Elimina un Quiz y todas sus dependencias (preguntas, opciones, intentos).
        /// - Administrador: puede eliminar cualquier quiz de la plataforma.
        /// - Docente: solo puede eliminar quizzes de sus propios cursos.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            if (!IsAdmin() && quiz.Course.TeacherId != userId) return Forbid();

            // Eliminar intentos asociados primero (Restrict FK)
            _context.QuizAttempts.RemoveRange(quiz.QuizAttempts);
            // Las preguntas y opciones se eliminan en cascada
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Quiz eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // URL: Quiz/Stats/{id}
        /// <summary>
        /// Muestra estadísticas de intentos de un quiz específico.
        /// - Administrador: puede ver estadísticas de cualquier quiz.
        /// - Docente: solo puede ver estadísticas de quizzes de sus cursos.
        /// </summary>
[HttpGet]
public async Task<IActionResult> Stats(Guid id)
{
    var userId = GetCurrentUserId();

    // Cargar quiz con todo lo necesario
    var quiz = await _context.Quizzes
        .Include(q => q.Course)
            .ThenInclude(c => c.Enrollments)   // ← Necesario para saber quién está matriculado
        .Include(q => q.Questions)
        .Include(q => q.QuizAttempts)
            .ThenInclude(a => a.Student)
        .FirstOrDefaultAsync(q => q.Id == id);

    if (quiz == null) return NotFound();
    if (!IsAdmin() && quiz.Course.TeacherId != userId) return Forbid();

    // ─── GENERACIÓN AUTOMÁTICA DE CEROS ───
    // Solo si el quiz ya venció y tiene fecha límite definida
    if (quiz.DueDate.HasValue && quiz.DueDate.Value < DateTime.Now)
    {
        // IDs de estudiantes que YA presentaron el quiz
        var attemptedStudentIds = quiz.QuizAttempts
            .Select(a => a.StudentId)
            .ToHashSet();

        // Estudiantes matriculados en el curso que NO tienen intento
        // NOTA: Si tu EnrollmentStatus tiene un estado "Activo", agrega: && e.Status == EnrollmentStatus.Active
        var missingStudentIds = quiz.Course.Enrollments
            .Where(e => !attemptedStudentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        if (missingStudentIds.Any())
        {
            var defaultDate = quiz.DueDate.Value;

            foreach (var studentId in missingStudentIds)
            {
                _context.QuizAttempts.Add(new QuizAttempt
                {
                    Id = Guid.NewGuid(),
                    QuizId = quiz.Id,
                    StudentId = studentId,
                    Score = 0,
                    StartedAt = defaultDate,
                    SubmittedAt = defaultDate,
                    AnswersJson = "{}"  // JSON vacío = no respondió nada
                });
            }

            await _context.SaveChangesAsync();
        }
    }

    // ─── Recargar TODOS los intentos (incluyendo los recién creados) ───
    var allAttempts = await _context.QuizAttempts
        .AsNoTracking()
        .Where(a => a.QuizId == id)
        .Include(a => a.Student)
        .OrderByDescending(a => a.SubmittedAt)
        .ToListAsync();

    // ViewModel para la vista
    var attempts = allAttempts.Select(a => new QuizAttemptViewModel
    {
        Id = a.Id,
        QuizId = a.QuizId,
        StudentId = a.StudentId,
        Score = a.Score,
        SubmittedAt = a.SubmittedAt,
        StartedAt = a.StartedAt,
        AnswersJson = a.AnswersJson
    }).ToList();

    ViewBag.QuizTitle = quiz.Title;
    ViewBag.CourseName = quiz.Course.Name;
    ViewBag.TotalQuestions = quiz.Questions.Count;
    ViewBag.TimeLimitMinutes = quiz.TimeLimitMinutes;

    // Diccionario de nombres para la vista
    ViewBag.StudentNames = allAttempts.ToDictionary(
        a => a.Id,
        a => a.Student?.DisplayName ?? "Desconocido"
    );

    ViewBag.AverageScore = attempts.Any() ? attempts.Average(a => a.Score) : 0;

    return View(attempts);
}

        // ──────────────────────────────────────────────
        // Métodos privados de ayuda
        // ──────────────────────────────────────────────

        /// <summary>
        /// Devuelve la lista de cursos disponibles para el selector del formulario.
        /// - Administrador: todos los cursos de la plataforma.
        /// - Docente: únicamente sus propios cursos.
        /// </summary>
        private async Task<List<SelectListItem>> GetAvailableCoursesAsync(Guid userId)
        {
            var query = _context.Courses.AsQueryable();

            if (!IsAdmin())
            {
                query = query.Where(c => c.TeacherId == userId);
            }

            return await query
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }
    }
}