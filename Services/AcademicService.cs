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

        // Guids estáticos para consistencia
        public static readonly Guid Estudiante1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Estudiante2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DocenteId = Guid.Parse("33333333-3333-3333-3333-333333333333");
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
            // Crear usuarios
            var docente = new User { Id = DocenteId, DisplayName = "Dr. Armando Paredes", Email = "armando.paredes@studygo.edu" };
            var est1 = new User { Id = Estudiante1Id, DisplayName = "Steven Florez", Email = "steven.florez@studygo.edu" };
            var est2 = new User { Id = Estudiante2Id, DisplayName = "Maria Gomez", Email = "maria.gomez@studygo.edu" };

            _users.AddRange(new[] { docente, est1, est2 });

            // Crear cursos
            var c1 = new Course
            {
                Id = Curso1Id,
                Name = "Estructuras de Datos Avanzadas",
                TeacherId = DocenteId,
                Teacher = docente
            };
            var c2 = new Course
            {
                Id = Curso2Id,
                Name = "Diseño de Algoritmos Complejos",
                TeacherId = DocenteId,
                Teacher = docente
            };

            _courses.AddRange(new[] { c1, c2 });

            // Crear inscripciones (miembros)
            _enrollments.Add(new Enrollment { Id = Guid.NewGuid(), StudentId = Estudiante1Id, Student = est1, CourseId = Curso1Id, Course = c1, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.Now.AddDays(-10) });
            _enrollments.Add(new Enrollment { Id = Guid.NewGuid(), StudentId = Estudiante2Id, Student = est2, CourseId = Curso1Id, Course = c1, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.Now.AddDays(-10) });
            _enrollments.Add(new Enrollment { Id = Guid.NewGuid(), StudentId = Estudiante1Id, Student = est1, CourseId = Curso2Id, Course = c2, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.Now.AddDays(-9) });

            // Cargar Google Drive conectado por defecto para Steven
            _driveConnected[Estudiante1Id] = true;

            // Archivos de Drive
            _driveFiles.Add(new DriveFile { Id = Guid.NewGuid(), CourseId = Curso1Id, OwnerId = Estudiante1Id, Owner = est1, DriveFileId = "drive_123_abc", Url = "https://drive.google.com/file/d/123_abc" });
            _driveFiles.Add(new DriveFile { Id = Guid.NewGuid(), CourseId = Curso1Id, OwnerId = DocenteId, Owner = docente, DriveFileId = "drive_syllabus", Url = "https://drive.google.com/file/d/syllabus_pdf" });

            // Crear Rúbrica para las tareas
            var rubrica1 = new Rubric
            {
                Id = Guid.NewGuid()
            };

            // Crear Tareas de Programación
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

            // Crear entregas mockeadas iniciales
            var s1 = new Submission
            {
                Id = Guid.NewGuid(),
                ProgrammingTaskId = Tarea1Id,
                ProgrammingTask = t1,
                StudentId = Estudiante1Id,
                Student = est1,
                Status = SubmissionStatus.Calificado
            };

            var s1_v1 = new SubmissionVersion
            {
                Id = Guid.NewGuid(),
                SubmissionId = s1.Id,
                Submission = s1,
                VersionNumber = 1,
                Code = "using System;\n\nclass Program {\n    static void Main() {\n        string[] input = Console.ReadLine().Split();\n        int a = int.Parse(input[0]);\n        int b = int.Parse(input[1]);\n        Console.WriteLine(a - b); // Error de lógica inicial\n    }\n}",
                SavedAt = DateTime.Now.AddDays(-2)
            };

            var s1_v2 = new SubmissionVersion
            {
                Id = Guid.NewGuid(),
                SubmissionId = s1.Id,
                Submission = s1,
                VersionNumber = 2,
                Code = "using System;\n\nclass Program {\n    static void Main() {\n        string[] input = Console.ReadLine().Split();\n        int a = int.Parse(input[0]);\n        int b = int.Parse(input[1]);\n        Console.WriteLine(a + b); // Corregido!\n    }\n}",
                SavedAt = DateTime.Now.AddDays(-1)
            };

            s1.Versions.Add(s1_v1);
            s1.Versions.Add(s1_v2);
            _submissions.Add(s1);
            _versions.AddRange(new[] { s1_v1, s1_v2 });

            // Grade para s1
            var grade1 = new Grade
            {
                Id = Guid.NewGuid(),
                SubmissionId = s1.Id,
                Submission = s1,
                FinalScore = 95.0m,
                GradedAt = DateTime.Now.AddDays(-1)
            };
            s1.Grade = grade1;
            _grades.Add(grade1);
        }

        public async Task<IEnumerable<Course>> GetCoursesForUserAsync(Guid userId, string role)
        {
            await Task.Delay(50); // Simular latencia
            if (role == "Docente")
            {
                return _courses.Where(c => c.TeacherId == userId);
            }
            else
            {
                var courseIds = _enrollments.Where(e => e.StudentId == userId).Select(e => e.CourseId).ToList();
                return _courses.Where(c => courseIds.Contains(c.Id));
            }
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

        public async Task<bool> CreateCourseAsync(Course course)
        {
            await Task.Delay(50);
            course.Id = Guid.NewGuid();
            course.TeacherId = DocenteId;
            course.Teacher = _users.FirstOrDefault(u => u.Id == DocenteId);
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
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Enrollment>> GetCourseMembersAsync(Guid courseId)
        {
            await Task.Delay(50);
            return _enrollments.Where(e => e.CourseId == courseId).ToList();
        }

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
            // Usamos DriveFileId o URL para simular el nombre en nuestras pantallas
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

        public async Task<ProgrammingTask> GetTaskDetailAsync(Guid taskId)
        {
            await Task.Delay(50);
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public async Task<Submission> GetOrCreateSubmissionAsync(Guid taskId, Guid studentId)
        {
            await Task.Delay(50);
            var submission = _submissions.FirstOrDefault(s => s.ProgrammingTaskId == taskId && s.StudentId == studentId);
            if (submission == null)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                submission = new Submission
                {
                    Id = Guid.NewGuid(),
                    ProgrammingTaskId = taskId,
                    ProgrammingTask = task,
                    StudentId = studentId,
                    Student = _users.FirstOrDefault(u => u.Id == studentId),
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

                // Crear o actualizar calificación
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

        public async Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid courseId, Guid studentId)
        {
            await Task.Delay(50);
            // Obtener todas las entregas del estudiante en ese curso y sus calificaciones
            return _grades.Where(g => g.Submission.StudentId == studentId && g.Submission.ProgrammingTask.CourseId == courseId).ToList();
        }

        public async Task<IEnumerable<Submission>> GetCourseGradebookAsync(Guid courseId)
        {
            await Task.Delay(50);
            return _submissions.Where(s => s.ProgrammingTask.CourseId == courseId).ToList();
        }
    }
}
