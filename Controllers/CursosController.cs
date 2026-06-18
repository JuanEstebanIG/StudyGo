using System;
using System.Collections.Generic;
using System.Linq;
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
            return Request.Cookies["StudyGo_Role"] ?? "Estudiante";
        }

        private Guid GetCurrentUserId()
        {
            return GetCurrentRole() == "Docente" ? AcademicService.DocenteId : AcademicService.Estudiante1Id;
        }

        public IActionResult ToggleRole(string returnUrl)
        {
            var role = GetCurrentRole() == "Estudiante" ? "Docente" : "Estudiante";
            Response.Cookies.Append("StudyGo_Role", role);
            return Redirect(returnUrl ?? "/Cursos");
        }

        // GET: /Cursos
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();
            var courses = await _academicService.GetCoursesForUserAsync(userId, role);

            var vm = new CursosListViewModel
            {
                UserId = userId,
                Role = role,
                Cursos = courses.Select(c => new CursoItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Id == AcademicService.Curso1Id ? "CS-302" : "CS-401", // Mocks de código
                    TeacherName = c.Teacher?.DisplayName ?? "Docente",
                    TeacherEmail = c.Teacher?.Email ?? "",
                    StudentCount = c.Enrollments.Count > 0 ? c.Enrollments.Count : 12,
                    ProgressPercent = role == "Estudiante" ? (c.Id == AcademicService.Curso1Id ? 65 : 10) : 100
                }).ToList()
            };

            return View(vm);
        }

        // GET: /Cursos/Detalle/{id}?tab={tab}
        public async Task<IActionResult> Detalle(Guid id, string tab = "Asignaciones")
        {
            var course = await _academicService.GetCourseDetailAsync(id);
            if (course == null) return NotFound();

            var userId = GetCurrentUserId();
            var role = GetCurrentRole();
            var isDriveConnected = await _academicService.IsDriveConnectedAsync(userId);

            var vm = new CursoDetalleViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Id == AcademicService.Curso1Id ? "CS-302" : "CS-401",
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher?.DisplayName ?? "Docente",
                TeacherEmail = course.Teacher?.Email ?? "",
                Role = role,
                ActiveTab = tab,
                IsDriveConnected = isDriveConnected,
                Activities = course.Activities.Select(a => {
                    var isTask = a is ProgrammingTask;
                    var task = a as ProgrammingTask;
                    var submission = isTask ? _academicService.GetOrCreateSubmissionAsync(a.Id, userId).Result : null;
                    return new ActivityItemViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        Type = isTask ? "ProgrammingTask" : "Quiz",
                        State = a.State.ToString(),
                        Language = isTask ? task?.Language : "-",
                        StudentSubmissionStatus = isTask ? (submission?.Status.ToString() ?? "SinEmpezar") : "Calificada",
                        Grade = (isTask && submission?.Grade != null) ? submission.Grade.FinalScore : (decimal?)null
                    };
                }).ToList(),
                Materials = course.DriveFiles.Select(f => new DriveFileItemViewModel
                {
                    Id = f.Id,
                    Name = f.Url.Split('/').Last(), // Obtiene nombre del final del url
                    Url = f.Url,
                    OwnerName = f.Owner?.DisplayName ?? "Estudiante"
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
            if (GetCurrentRole() != "Docente") return Forbid();
            return View(new CursoCrearEditarViewModel());
        }

        // POST: /Cursos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CursoCrearEditarViewModel vm)
        {
            if (GetCurrentRole() != "Docente") return Forbid();
            if (!ModelState.IsValid) return View(vm);

            var course = new Course
            {
                Name = vm.Name
            };

            await _academicService.CreateCourseAsync(course);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cursos/Editar/{id}
        public async Task<IActionResult> Editar(Guid id)
        {
            if (GetCurrentRole() != "Docente") return Forbid();
            var course = await _academicService.GetCourseDetailAsync(id);
            if (course == null) return NotFound();

            var vm = new CursoCrearEditarViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Code = course.Id == AcademicService.Curso1Id ? "CS-302" : "CS-401"
            };

            return View(vm);
        }

        // POST: /Cursos/Editar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Guid id, CursoCrearEditarViewModel vm)
        {
            if (GetCurrentRole() != "Docente") return Forbid();
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var course = new Course
            {
                Id = vm.Id,
                Name = vm.Name
            };

            var success = await _academicService.UpdateCourseAsync(course);
            if (!success) return NotFound();

            return RedirectToAction(nameof(Detalle), new { id = course.Id });
        }

        // POST: /Cursos/ConectarDrive
        [HttpPost]
        public async Task<IActionResult> ConectarDrive(Guid courseId)
        {
            var userId = GetCurrentUserId();
            await _academicService.ConnectDriveAsync(userId);
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Material" });
        }

        // POST: /Cursos/DesconectarDrive
        [HttpPost]
        public async Task<IActionResult> DesconectarDrive(Guid courseId)
        {
            var userId = GetCurrentUserId();
            await _academicService.DisconnectDriveAsync(userId);
            return RedirectToAction(nameof(Detalle), new { id = courseId, tab = "Material" });
        }

        // POST: /Cursos/AttachFile
        [HttpPost]
        public async Task<IActionResult> AttachFile(Guid courseId, string fileName, string fileUrl)
        {
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
    }
}
