using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Models;
using StudyGo.Services;
using StudyGo.ViewModels.Cursos;

namespace StudyGo.Controllers
{
    public class CursosController : Controller
    {
        private readonly IAcademicService _academicService;

        public CursosController(IAcademicService academicService)
        {
            _academicService = academicService;
        }

        private string GetCurrentRole()
        {
            if (User.IsInRole("Administrador")) return "Administrador";
            if (User.IsInRole("Docente")) return "Docente";
            return "Estudiante";
        }

        /// <summary>
        /// Obtiene el ID del usuario autenticado real desde los claims.
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(idClaim, out var id)) return id;
            return Guid.Empty;
        }

        /// <summary>
        /// Asegura que el usuario actual esté registrado en la caché del servicio en memoria,
        /// para que las relaciones Enrollment/Submission se puedan resolver.
        /// </summary>
        private void EnsureCurrentUserCached()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return;
            var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
            AcademicService.EnsureUserRegistered(userId, displayName, email);
        }

        // GET: /Cursos
        public async Task<IActionResult> Index()
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();
            var courses = (await _academicService.GetCoursesForUserAsync(userId, role)).ToList();

            // Calcular métricas reales
            int pendingTasks = 0;
            int gradedTasks = 0;
            int pendingGrading = 0;
            decimal totalScore = 0;
            int scoredCount = 0;

            foreach (var course in courses)
            {
                var detail = await _academicService.GetCourseDetailAsync(course.Id);
                if (detail == null) continue;

                if (role == "Estudiante")
                {
                    foreach (var act in detail.Activities.OfType<ProgrammingTask>())
                    {
                        var sub = await _academicService.GetOrCreateSubmissionAsync(act.Id, userId);
                        if (sub?.Status == Enums.SubmissionStatus.Calificado)
                        {
                            gradedTasks++;
                            if (sub.Grade != null)
                            {
                                totalScore += sub.Grade.FinalScore;
                                scoredCount++;
                            }
                        }
                        else
                        {
                            pendingTasks++;
                        }
                    }
                }
                else
                {
                    foreach (var act in detail.Activities.OfType<ProgrammingTask>())
                    {
                        var subs = (await _academicService.GetTaskSubmissionsAsync(act.Id)).ToList();
                        pendingGrading += subs.Count(s => s.Status == Enums.SubmissionStatus.Enviado);
                        gradedTasks += subs.Count(s => s.Status == Enums.SubmissionStatus.Calificado);
                    }
                }
            }

            decimal? averageGrade = scoredCount > 0 ? Math.Round(totalScore / scoredCount, 1) : (decimal?)null;

            var vm = new CursosListViewModel
            {
                UserId = userId,
                Role = role,
                TotalPendingTasks = pendingTasks,
                TotalGradedTasks = gradedTasks,
                TotalPendingGrading = pendingGrading,
                AverageGrade = averageGrade,
                Cursos = courses.Select(c => new CursoItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code ?? "-",
                    TeacherName = c.Teacher?.DisplayName ?? "Docente",
                    TeacherEmail = c.Teacher?.Email ?? "",
                    StudentCount = c.Enrollments?.Count ?? 0,
                    ProgressPercent = 0,
                    IsEnrolled = true
                }).ToList()
            };

            return View(vm);
        }

        // GET: /Cursos/Explorar
        public async Task<IActionResult> Explorar()
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var allCourses = (await _academicService.GetAllCoursesAsync()).ToList();

            var vm = new ExplorarCursosViewModel
            {
                UserId = userId,
                Role = role,
                Cursos = new List<CursoItemViewModel>()
            };

            foreach (var c in allCourses)
            {
                var detail = await _academicService.GetCourseDetailAsync(c.Id);
                var isEnrolled = await _academicService.IsEnrolledAsync(c.Id, userId);

                vm.Cursos.Add(new CursoItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code ?? "-",
                    TeacherName = c.Teacher?.DisplayName ?? "Docente",
                    TeacherEmail = c.Teacher?.Email ?? "",
                    StudentCount = detail?.Enrollments?.Count ?? 0,
                    IsEnrolled = isEnrolled
                });
            }

            return View(vm);
        }

        // POST: /Cursos/Inscribir/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscribir(Guid id)
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _academicService.EnrollAsync(id, userId);
            TempData["SuccessMessage"] = "¡Te has inscrito al curso exitosamente!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cursos/Cancelar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(Guid id)
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _academicService.UnenrollAsync(id, userId);
            TempData["SuccessMessage"] = "Has cancelado tu inscripción al curso.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cursos/ExpulsarStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpulsarStudent(Guid courseId, Guid studentId)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();

            var success = await _academicService.UnenrollAsync(courseId, studentId);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "El estudiante ha sido retirado del curso exitosamente.";
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Miembros" });
        }

        // GET: /Cursos/Detalle/{id}?tab={tab}
        public async Task<IActionResult> Detalle(Guid id, string tab = "Asignaciones")
        {
            EnsureCurrentUserCached();
            var course = await _academicService.GetCourseDetailAsync(id);
            if (course == null) return NotFound();

            var userId = GetCurrentUserId();
            var role = GetCurrentRole();
            var isDriveConnected = await _academicService.IsDriveConnectedAsync(userId);
            var isEnrolled = await _academicService.IsEnrolledAsync(id, userId);

            var vm = new CursoDetalleViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Code ?? "-",
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher?.DisplayName ?? "Docente",
                TeacherEmail = course.Teacher?.Email ?? "",
                Role = role,
                ActiveTab = tab,
                IsDriveConnected = isDriveConnected,
                IsEnrolled = isEnrolled,
                Activities = course.Activities.Select(a => {
                    var isTask = a is ProgrammingTask;
                    var task = a as ProgrammingTask;
                    Models.Submission submission = null;
                    if (isTask && role == "Estudiante")
                    {
                        submission = _academicService.GetOrCreateSubmissionAsync(a.Id, userId).Result;
                    }
                    return new ActivityItemViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        Type = isTask ? "ProgrammingTask" : "Quiz",
                        State = a.State.ToString(),
                        Language = isTask ? task?.Language : "-",
                        StudentSubmissionStatus = isTask && role == "Estudiante"
                            ? (submission?.Status.ToString() ?? "SinEmpezar")
                            : (role != "Estudiante" ? "Vista docente" : "SinEmpezar"),
                        Grade = (isTask && submission?.Grade != null) ? submission.Grade.FinalScore : (decimal?)null
                    };
                }).ToList(),
                Materials = course.DriveFiles.Select(f => new DriveFileItemViewModel
                {
                    Id = f.Id,
                    Name = f.Url.Split('/').Last(),
                    Url = f.Url,
                    OwnerName = f.Owner?.DisplayName ?? "Usuario"
                }).ToList(),
                Members = course.Enrollments.Select(e => new MemberItemViewModel
                {
                    StudentId = e.StudentId,
                    DisplayName = e.Student?.DisplayName ?? "Estudiante",
                    Email = e.Student?.Email ?? "",
                    EnrolledAt = e.EnrolledAt
                }).ToList()
            };

            return View(vm);
        }

        // GET: /Cursos/Crear
        public IActionResult Crear()
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();
            return View(new CursoCrearEditarViewModel());
        }

        // POST: /Cursos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CursoCrearEditarViewModel vm)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();
            if (!ModelState.IsValid) return View(vm);

            EnsureCurrentUserCached();
            var teacherId = GetCurrentUserId();

            var course = new Course
            {
                Name = vm.Name,
                Code = vm.Code
            };

            await _academicService.CreateCourseAsync(course, teacherId);

            return User.IsInRole("Administrador")
                ? RedirectToAction(nameof(Admin))
                : RedirectToAction(nameof(Index));
        }

        // GET: /Cursos/Editar/{id}
        public async Task<IActionResult> Editar(Guid id)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();
            var course = await _academicService.GetCourseDetailAsync(id);
            if (course == null) return NotFound();

            var vm = new CursoCrearEditarViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Code ?? ""
            };

            return View(vm);
        }

        // POST: /Cursos/Editar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Guid id, CursoCrearEditarViewModel vm)
        {
            if (!User.IsInRole("Docente") && !User.IsInRole("Administrador")) return Forbid();
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var course = new Course
            {
                Id = vm.Id,
                Name = vm.Name,
                Code = vm.Code
            };

            var success = await _academicService.UpdateCourseAsync(course);
            if (!success) return NotFound();

            return User.IsInRole("Administrador")
                ? RedirectToAction(nameof(Admin))
                : RedirectToAction(nameof(Detalle), new { id = course.Id });
        }

        // POST: /Cursos/ConectarDrive
        [HttpPost]
        public async Task<IActionResult> ConectarDrive(Guid courseId)
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            await _academicService.ConnectDriveAsync(userId);
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Material" });
        }

        // POST: /Cursos/DesconectarDrive
        [HttpPost]
        public async Task<IActionResult> DesconectarDrive(Guid courseId)
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            await _academicService.DisconnectDriveAsync(userId);
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Material" });
        }

        // POST: /Cursos/AttachFile
        [HttpPost]
        public async Task<IActionResult> AttachFile(Guid courseId, string fileName, string fileUrl)
        {
            EnsureCurrentUserCached();
            var userId = GetCurrentUserId();
            await _academicService.AttachDriveFileAsync(courseId, userId, fileName, fileUrl);
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Material" });
        }

        // POST: /Cursos/RemoveFile
        [HttpPost]
        public async Task<IActionResult> RemoveFile(Guid courseId, Guid fileId)
        {
            await _academicService.RemoveDriveFileAsync(fileId);
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Material" });
        }

        // GET: /Cursos/Admin
        public async Task<IActionResult> Admin()
        {
            if (!User.IsInRole("Administrador"))
                return RedirectToAction(nameof(Index));

            EnsureCurrentUserCached();
            var courses = (await _academicService.GetAllCoursesAsync()).ToList();

            var vm = new CursosListViewModel
            {
                UserId = GetCurrentUserId(),
                Role = "Administrador",
                Cursos = courses.Select(c => new CursoItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code ?? "-",
                    TeacherName = c.Teacher?.DisplayName ?? "Docente",
                    TeacherEmail = c.Teacher?.Email ?? "",
                    StudentCount = c.Enrollments?.Count ?? 0,
                    ProgressPercent = 100
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Cursos/Eliminar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            if (!User.IsInRole("Administrador") && !User.IsInRole("Docente")) return Forbid();

            var success = await _academicService.DeleteCourseAsync(id);
            if (!success) return NotFound();

            return User.IsInRole("Administrador")
                ? RedirectToAction(nameof(Admin))
                : RedirectToAction(nameof(Index));
        }
    }
}
