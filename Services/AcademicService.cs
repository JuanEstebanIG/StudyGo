using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudyGo.Enums;
using StudyGo.Models;

namespace StudyGo.Services
{
    public class AcademicService : IAcademicService
    {
        private static readonly List<Course> _courses = new List<Course>();
        private static readonly List<Enrollment> _enrollments = new List<Enrollment>();
        private static readonly List<DriveFile> _driveFiles = new List<DriveFile>();
        private static readonly List<ProgrammingTask> _tasks = new List<ProgrammingTask>();
        private static readonly List<Submission> _submissions = new List<Submission>();
        private static readonly List<SubmissionVersion> _versions = new List<SubmissionVersion>();
        private static readonly List<Grade> _grades = new List<Grade>();
        private static readonly List<User> _users = new List<User>();
        private static readonly ConcurrentDictionary<Guid, bool> _driveConnected = new ConcurrentDictionary<Guid, bool>();

        // GUIDs de ejemplo para datos de demostración vacíos
        public static readonly Guid DocenteEjemploId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid Curso1Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid Curso2Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid Tarea1Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid Tarea2Id = Guid.Parse("77777777-7777-7777-7777-777777777777");

        static AcademicService()
        {
            InitializeData();
        }

        private static void InitializeData()
        {
            // Docente de demostración (sin contraseña real — solo para ejemplos)
            var docenteEjemplo = new User
            {
                Id = DocenteEjemploId,
                DisplayName = "Dr. Armando Paredes",
                Email = "armando.paredes@studygo.edu"
            };
            _users.Add(docenteEjemplo);

            // Cursos de demostración vacíos — sin inscripciones predefinidas
            var c1 = new Course
            {
                Id = Curso1Id,
                Name = "Estructuras de Datos Avanzadas",
                Code = "CS-302",
                TeacherId = DocenteEjemploId,
                Teacher = docenteEjemplo
            };
            var c2 = new Course
            {
                Id = Curso2Id,
                Name = "Diseño de Algoritmos Complejos",
                Code = "CS-401",
                TeacherId = DocenteEjemploId,
                Teacher = docenteEjemplo
            };
            _courses.AddRange(new[] { c1, c2 });

            // Rúbrica para las tareas de demostración
            var rubrica1 = new Rubric { Id = Guid.NewGuid() };

            // Tareas de programación de demostración
            var t1 = new ProgrammingTask
            {
                Id = Tarea1Id,
                CourseId = Curso1Id,
                Course = c1,
                Title = "Suma de dos números en C#",
                Description = "### Enunciado\nEscribe un programa en C# que lea dos enteros de la entrada estándar (consola) separados por un espacio y muestre la suma de ambos.\n\n### Formato de Entrada\nUna línea conteniendo dos números enteros, por ejemplo `5 7`.\n\n### Formato de Salida\nUn único número entero con el resultado de la suma, por ejemplo `12`.\n\n### Restricciones\n- Límite de tiempo: **10 segundos**.\n- Límite de memoria: **256 MB**.\n- Sin acceso a internet.",
                State = ActivityState.Publicado,
                Language = "C#",
                TimeLimitSeconds = 10,
                MemoryLimitMb = 256,
                Rubric = rubrica1
            };

            var t2 = new ProgrammingTask
            {
                Id = Tarea2Id,
                CourseId = Curso1Id,
                Course = c1,
                Title = "Inversión de Cadena (String Reverse)",
                Description = "### Enunciado\nEscribe un programa en C# que tome una cadena de texto y la imprima de forma invertida.\n\n### Formato de Entrada\nUna cadena de texto, por ejemplo: `hola`.\n\n### Formato de Salida\nLa cadena de texto invertida: `aloh`.",
                State = ActivityState.Publicado,
                Language = "C#",
                TimeLimitSeconds = 10,
                MemoryLimitMb = 256,
                Rubric = rubrica1
            };

            _tasks.AddRange(new[] { t1, t2 });
            c1.Activities.Add(t1);
            c1.Activities.Add(t2);

            // NO se crean inscripciones, submissions, grades ni archivos Drive por defecto.
            // Cada usuario real gestiona sus propios datos al usar la aplicación.
        }

        // ────────────────────────────────────────────────────────────────
        // CURSOS
        // ────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Course>> GetCoursesForUserAsync(Guid userId, string role)
        {
            await Task.Delay(50);
            if (role == "Administrador")
            {
                return _courses.ToList();
            }
            else if (role == "Docente")
            {
                return _courses.Where(c => c.TeacherId == userId).ToList();
            }
            else
            {
                var courseIds = _enrollments
                    .Where(e => e.StudentId == userId && e.Status == EnrollmentStatus.Active)
                    .Select(e => e.CourseId)
                    .ToList();
                return _courses.Where(c => courseIds.Contains(c.Id)).ToList();
            }
        }

        public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            await Task.Delay(50);
            return _courses.ToList();
        }

        public async Task<Course> GetCourseDetailAsync(Guid courseId)
        {
            await Task.Delay(50);
            var course = _courses.FirstOrDefault(c => c.Id == courseId);
            if (course != null)
            {
                course.Activities = _tasks.Where(t => t.CourseId == courseId).Cast<Activity>().ToList();
                course.DriveFiles = _driveFiles.Where(d => d.CourseId == courseId).ToList();
                course.Enrollments = _enrollments.Where(e => e.CourseId == courseId).ToList();
            }
            return course;
        }

        public async Task<bool> CreateCourseAsync(Course course, Guid teacherId)
        {
            await Task.Delay(50);
            course.Id = Guid.NewGuid();
            course.TeacherId = teacherId;
            course.Teacher = _users.FirstOrDefault(u => u.Id == teacherId);
            _courses.Add(course);
            return true;
        }

        public async Task<bool> UpdateCourseAsync(Course course)
        {
            await Task.Delay(50);
            var existing = _courses.FirstOrDefault(c => c.Id == course.Id);
            if (existing != null)
            {
                existing.Name = course.Name;
                existing.Code = course.Code;
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteCourseAsync(Guid courseId)
        {
            await Task.Delay(50);
            var course = _courses.FirstOrDefault(c => c.Id == courseId);
            if (course != null)
            {
                _courses.Remove(course);
                _enrollments.RemoveAll(e => e.CourseId == courseId);
                _tasks.RemoveAll(t => t.CourseId == courseId);
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Enrollment>> GetCourseMembersAsync(Guid courseId)
        {
            await Task.Delay(50);
            return _enrollments.Where(e => e.CourseId == courseId).ToList();
        }

        // ────────────────────────────────────────────────────────────────
        // INSCRIPCIONES
        // ────────────────────────────────────────────────────────────────

        public async Task<bool> IsEnrolledAsync(Guid courseId, Guid studentId)
        {
            await Task.Delay(20);
            return _enrollments.Any(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.Status == EnrollmentStatus.Active);
        }

        public async Task<bool> EnrollAsync(Guid courseId, Guid studentId)
        {
            await Task.Delay(50);

            // Verificar que el curso existe
            var course = _courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null) return false;

            // No inscribir dos veces
            if (_enrollments.Any(e => e.CourseId == courseId && e.StudentId == studentId && e.Status == EnrollmentStatus.Active))
                return false;

            // Buscar o crear referencia al usuario
            var student = _users.FirstOrDefault(u => u.Id == studentId);

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = student,
                CourseId = courseId,
                Course = course,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.Now
            };

            _enrollments.Add(enrollment);
            course.Enrollments = _enrollments.Where(e => e.CourseId == courseId).ToList();
            return true;
        }

        public async Task<bool> UnenrollAsync(Guid courseId, Guid studentId)
        {
            await Task.Delay(50);
            var enrollment = _enrollments.FirstOrDefault(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.Status == EnrollmentStatus.Active);

            if (enrollment == null) return false;

            _enrollments.Remove(enrollment);

            // Actualizar referencia en el curso
            var course = _courses.FirstOrDefault(c => c.Id == courseId);
            if (course != null)
                course.Enrollments = _enrollments.Where(e => e.CourseId == courseId).ToList();

            return true;
        }

        // ────────────────────────────────────────────────────────────────
        // GOOGLE DRIVE
        // ────────────────────────────────────────────────────────────────

        public async Task<bool> IsDriveConnectedAsync(Guid userId)
        {
            await Task.Delay(20);
            return _driveConnected.TryGetValue(userId, out var connected) && connected;
        }

        public async Task<bool> ConnectDriveAsync(Guid userId)
        {
            await Task.Delay(100);
            _driveConnected[userId] = true;
            return true;
        }

        public async Task<bool> DisconnectDriveAsync(Guid userId)
        {
            await Task.Delay(100);
            _driveConnected[userId] = false;
            return true;
        }

        public async Task<IEnumerable<DriveFile>> GetCourseDriveFilesAsync(Guid courseId)
        {
            await Task.Delay(50);
            return _driveFiles.Where(d => d.CourseId == courseId).ToList();
        }

        public async Task<bool> AttachDriveFileAsync(Guid courseId, Guid userId, string fileName, string url)
        {
            await Task.Delay(50);
            var driveFile = new DriveFile
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                OwnerId = userId,
                Owner = _users.FirstOrDefault(u => u.Id == userId),
                DriveFileId = "drive_" + Guid.NewGuid().ToString().Substring(0, 8),
                Url = url
            };
            _driveFiles.Add(driveFile);
            return true;
        }

        public async Task<bool> RemoveDriveFileAsync(Guid fileId)
        {
            await Task.Delay(50);
            var file = _driveFiles.FirstOrDefault(d => d.Id == fileId);
            if (file != null)
            {
                _driveFiles.Remove(file);
                return true;
            }
            return false;
        }

        // ────────────────────────────────────────────────────────────────
        // TAREAS Y ENTREGAS
        // ────────────────────────────────────────────────────────────────

        public async Task<ProgrammingTask> GetTaskDetailAsync(Guid taskId)
        {
            await Task.Delay(50);
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public async Task<bool> CreateTaskAsync(ProgrammingTask task)
        {
            await Task.Delay(50);
            task.Id = Guid.NewGuid();
            var course = _courses.FirstOrDefault(c => c.Id == task.CourseId);
            if (course == null) return false;
            task.Course = course;
            _tasks.Add(task);
            course.Activities.Add(task);
            return true;
        }

        public async Task<bool> UpdateTaskAsync(ProgrammingTask task)
        {
            await Task.Delay(50);
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing == null) return false;
            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Language = task.Language;
            existing.TimeLimitSeconds = task.TimeLimitSeconds;
            existing.MemoryLimitMb = task.MemoryLimitMb;
            existing.State = task.State;
            return true;
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            await Task.Delay(50);
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return false;
            var course = _courses.FirstOrDefault(c => c.Id == task.CourseId);
            course?.Activities.Remove(task);
            _tasks.Remove(task);
            _submissions.RemoveAll(s => s.ProgrammingTaskId == taskId);
            return true;
        }

        public async Task<Submission> GetOrCreateSubmissionAsync(Guid taskId, Guid studentId)
        {
            await Task.Delay(50);
            var submission = _submissions.FirstOrDefault(s => s.ProgrammingTaskId == taskId && s.StudentId == studentId);
            if (submission == null)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                var student = _users.FirstOrDefault(u => u.Id == studentId);
                submission = new Submission
                {
                    Id = Guid.NewGuid(),
                    ProgrammingTaskId = taskId,
                    ProgrammingTask = task,
                    StudentId = studentId,
                    Student = student,
                    Status = SubmissionStatus.EnProgreso
                };
                _submissions.Add(submission);
            }
            return submission;
        }

        public async Task<IEnumerable<SubmissionVersion>> GetSubmissionVersionsAsync(Guid submissionId)
        {
            await Task.Delay(50);
            return _versions.Where(v => v.SubmissionId == submissionId).OrderByDescending(v => v.VersionNumber).ToList();
        }

        public async Task<SubmissionVersion> GetSubmissionVersionAsync(Guid versionId)
        {
            await Task.Delay(20);
            return _versions.FirstOrDefault(v => v.Id == versionId);
        }

        public async Task<SubmissionVersion> SaveSubmissionVersionAsync(Guid submissionId, string code)
        {
            await Task.Delay(50);
            var submission = _submissions.FirstOrDefault(s => s.Id == submissionId);
            if (submission == null) return null;

            var currentVersions = _versions.Where(v => v.SubmissionId == submissionId).ToList();
            int nextVersionNum = currentVersions.Count > 0 ? currentVersions.Max(v => v.VersionNumber) + 1 : 1;

            var newVersion = new SubmissionVersion
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                Submission = submission,
                VersionNumber = nextVersionNum,
                Code = code,
                SavedAt = DateTime.Now
            };

            _versions.Add(newVersion);
            submission.Versions.Add(newVersion);
            submission.Status = SubmissionStatus.EnProgreso;

            return newVersion;
        }

        public async Task<bool> SubmitTaskAsync(Guid submissionId)
        {
            await Task.Delay(100);
            var submission = _submissions.FirstOrDefault(s => s.Id == submissionId);
            if (submission != null)
            {
                submission.Status = SubmissionStatus.Enviado;
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Submission>> GetTaskSubmissionsAsync(Guid taskId)
        {
            await Task.Delay(50);
            return _submissions.Where(s => s.ProgrammingTaskId == taskId).ToList();
        }

        public async Task<bool> GradeSubmissionAsync(Guid submissionId, decimal score, string feedback)
        {
            await Task.Delay(100);
            var submission = _submissions.FirstOrDefault(s => s.Id == submissionId);
            if (submission != null)
            {
                submission.Status = SubmissionStatus.Calificado;
                var existingGrade = _grades.FirstOrDefault(g => g.SubmissionId == submissionId);
                if (existingGrade != null)
                {
                    existingGrade.FinalScore = score;
                    existingGrade.GradedAt = DateTime.Now;
                }
                else
                {
                    var newGrade = new Grade
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submissionId,
                        Submission = submission,
                        FinalScore = score,
                        GradedAt = DateTime.Now
                    };
                    submission.Grade = newGrade;
                    _grades.Add(newGrade);
                }
                return true;
            }
            return false;
        }

        // ────────────────────────────────────────────────────────────────
        // CALIFICACIONES
        // ────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid courseId, Guid studentId)
        {
            await Task.Delay(50);
            return _grades
                .Where(g => g.Submission.StudentId == studentId && g.Submission.ProgrammingTask.CourseId == courseId)
                .ToList();
        }

        public async Task<IEnumerable<Submission>> GetCourseGradebookAsync(Guid courseId)
        {
            await Task.Delay(50);
            return _submissions.Where(s => s.ProgrammingTask.CourseId == courseId).ToList();
        }

        // ────────────────────────────────────────────────────────────────
        // USUARIOS (registro interno para el servicio en memoria)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registra un usuario en la caché del servicio en memoria para que
        /// las relaciones de Enrollment/Submission puedan resolverse correctamente.
        /// Debe llamarse después del login/registro OAuth.
        /// </summary>
        public static void EnsureUserRegistered(Guid userId, string displayName, string email)
        {
            if (!_users.Any(u => u.Id == userId))
            {
                _users.Add(new User
                {
                    Id = userId,
                    DisplayName = displayName,
                    Email = email
                });
            }
        }
    }
}
