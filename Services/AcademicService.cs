using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;

namespace StudyGo.Services
{
    public class AcademicService : IAcademicService
    {
        private readonly AppDbContext _context;
        private static readonly ConcurrentDictionary<Guid, bool> _driveConnected = new();

        public AcademicService(AppDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────────
        // CURSOS
        // ────────────────────────────────────────────────────────────────

         public async Task<IEnumerable<Course>> GetCoursesForUserAsync(Guid userId, string role)
        {
            if (role == "Administrador")
            {
                return await _context.Courses
                    .Include(c => c.Teacher)
                    .Include(c => c.Enrollments)   // <-- AGREGAR
                    .ToListAsync();
            }
            else if (role == "Docente")
            {
                return await _context.Courses
                    .Where(c => c.TeacherId == userId)
                    .Include(c => c.Teacher)
                    .Include(c => c.Enrollments)   // <-- AGREGAR
                    .ToListAsync();
            }
            else
            {
                var courseIds = await _context.Enrollments
                    .Where(e => e.StudentId == userId && e.Status == EnrollmentStatus.Active)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                return await _context.Courses
                    .Where(c => courseIds.Contains(c.Id))
                    .Include(c => c.Teacher)
                    .Include(c => c.Enrollments)   // <-- AGREGAR
                    .ToListAsync();
            }
        }

      public async Task<IEnumerable<Course>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)       // <-- AGREGAR
                .ToListAsync();
        }

        public async Task<Course> GetCourseDetailAsync(Guid courseId)
        {
            return await _context.Courses
                .Include(c => c.Activities)
                .Include(c => c.DriveFiles)
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<bool> CreateCourseAsync(Course course, Guid teacherId)
        {
            var teacher = await _context.Users.FindAsync(teacherId);

            course.Id = Guid.NewGuid();
            course.TeacherId = teacherId;
            course.InstitutionId = teacher?.InstitutionId
                ?? Guid.Parse("00000000-0000-0000-0000-000000000001");

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCourseAsync(Course course)
        {
            var existing = await _context.Courses.FindAsync(course.Id);
            if (existing == null) return false;

            existing.Name = course.Name;
            existing.Code = course.Code;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(Guid courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return false;

            // 1. Borrar submissions de las tareas de programación (cascade de Grades, Versions, Evaluations)
            var taskIds = course.Activities.OfType<ProgrammingTask>().Select(a => a.Id).ToList();
            if (taskIds.Any())
            {
                await _context.Submissions
                    .Where(s => taskIds.Contains(s.ProgrammingTaskId))
                    .ExecuteDeleteAsync();
            }

            // 2. Borrar intentos de los quizzes
            var quizIds = course.Activities.OfType<Quiz>().Select(a => a.Id).ToList();
            if (quizIds.Any())
            {
                await _context.QuizAttempts
                    .Where(qa => quizIds.Contains(qa.QuizId))
                    .ExecuteDeleteAsync();
            }

            // 3. Ahora sí borrar el curso (cascade automático de Activities, Enrollments, DriveFiles, CalendarEvents)
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<Enrollment>> GetCourseMembersAsync(Guid courseId)
        {
            return await _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.Student)
                .ToListAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // INSCRIPCIONES
        // ────────────────────────────────────────────────────────────────

        public async Task<bool> IsEnrolledAsync(Guid courseId, Guid studentId)
        {
            return await _context.Enrollments.AnyAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.Status == EnrollmentStatus.Active);
        }

        public async Task<bool> EnrollAsync(Guid courseId, Guid studentId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return false;

            if (await _context.Enrollments.AnyAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.Status == EnrollmentStatus.Active))
                return false;

            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.Now
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnenrollAsync(Guid courseId, Guid studentId)
        {
            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e =>
                e.CourseId == courseId &&
                e.StudentId == studentId &&
                e.Status == EnrollmentStatus.Active);

            if (enrollment == null) return false;

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
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
            return await _context.DriveFiles
                .Where(d => d.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<bool> AttachDriveFileAsync(Guid courseId, Guid userId, string fileName, string url)
        {
            var driveFile = new DriveFile
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                OwnerId = userId,
                DriveFileId = "drive_" + Guid.NewGuid().ToString().Substring(0, 8),
                Url = url
            };

            _context.DriveFiles.Add(driveFile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveDriveFileAsync(Guid fileId)
        {
            var file = await _context.DriveFiles.FindAsync(fileId);
            if (file == null) return false;

            _context.DriveFiles.Remove(file);
            await _context.SaveChangesAsync();
            return true;
        }

        // ────────────────────────────────────────────────────────────────
        // TAREAS Y ENTREGAS
        // ────────────────────────────────────────────────────────────────

        public async Task<ProgrammingTask> GetTaskDetailAsync(Guid taskId)
        {
            return await _context.ProgrammingTasks
                .AsNoTracking()
                .Include(t => t.Course)
                .Include(t => t.Rubric)
                .ThenInclude(r => r.Criteria)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<bool> CreateTaskAsync(ProgrammingTask task)
        {
            task.Id = Guid.NewGuid();
            _context.ProgrammingTasks.Add(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTaskAsync(ProgrammingTask task)
        {
            var rows = await _context.ProgrammingTasks
                .Where(t => t.Id == task.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Title, task.Title)
                    .SetProperty(t => t.Description, task.Description)
                    .SetProperty(t => t.Language, task.Language)
                    .SetProperty(t => t.TimeLimitSeconds, task.TimeLimitSeconds)
                    .SetProperty(t => t.MemoryLimitMb, task.MemoryLimitMb)
                    .SetProperty(t => t.State, task.State)
                    .SetProperty(t => t.CourseId, task.CourseId));

            return rows > 0;
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            var task = await _context.ProgrammingTasks.FindAsync(taskId);
            if (task == null) return false;

            var submissions = await _context.Submissions
                .Where(s => s.ProgrammingTaskId == taskId)
                .ToListAsync();

            _context.Submissions.RemoveRange(submissions);
            _context.ProgrammingTasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Submission> GetOrCreateSubmissionAsync(Guid taskId, Guid studentId)
        {
            var submission = await _context.Submissions
                .Include(s => s.Grade)  // <-- AGREGAR ESTA LÍNEA
                .FirstOrDefaultAsync(s => s.ProgrammingTaskId == taskId && s.StudentId == studentId);

            if (submission == null)
            {
                submission = new Submission
                {
                    Id = Guid.NewGuid(),
                    ProgrammingTaskId = taskId,
                    StudentId = studentId,
                    Status = SubmissionStatus.EnProgreso
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();
            }

            return submission;
        }

        public async Task<IEnumerable<SubmissionVersion>> GetSubmissionVersionsAsync(Guid submissionId)
        {
            return await _context.SubmissionVersions
                .Where(v => v.SubmissionId == submissionId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<SubmissionVersion> GetSubmissionVersionAsync(Guid versionId)
        {
            return await _context.SubmissionVersions.FindAsync(versionId);
        }

        public async Task<SubmissionVersion> SaveSubmissionVersionAsync(Guid submissionId, string code)
        {
            var currentVersions = await _context.SubmissionVersions
                .Where(v => v.SubmissionId == submissionId)
                .ToListAsync();

            int nextVersionNum = currentVersions.Count > 0
                ? currentVersions.Max(v => v.VersionNumber) + 1
                : 1;

            var newVersion = new SubmissionVersion
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                VersionNumber = nextVersionNum,
                Code = code,
                SavedAt = DateTime.Now
            };

            _context.SubmissionVersions.Add(newVersion);
            await _context.SaveChangesAsync();
            return newVersion;
        }

        public async Task<bool> SubmitTaskAsync(Guid submissionId)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission == null) return false;

            submission.Status = SubmissionStatus.Enviado;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Submission>> GetTaskSubmissionsAsync(Guid taskId)
        {
            return await _context.Submissions
                .Where(s => s.ProgrammingTaskId == taskId)
                .Include(s => s.Student)
                .Include(s => s.Versions)
                .Include(s => s.Grade)
                .ToListAsync();
        }

        public async Task<bool> GradeSubmissionAsync(Guid submissionId, decimal score, string feedback)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission == null) return false;

            submission.Status = SubmissionStatus.Calificado;

            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.SubmissionId == submissionId);

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
                    FinalScore = score,
                    GradedAt = DateTime.Now
                };
                _context.Grades.Add(newGrade);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ────────────────────────────────────────────────────────────────
        // CALIFICACIONES
        // ────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid courseId, Guid studentId)
        {
            return await _context.Grades
                .Include(g => g.Submission)
                .Where(g => g.Submission.StudentId == studentId &&
                            g.Submission.ProgrammingTask.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetCourseGradebookAsync(Guid courseId)
        {
            return await _context.Submissions
                .Where(s => s.ProgrammingTask.CourseId == courseId)
                .Include(s => s.Student)
                .Include(s => s.Grade)
                .ToListAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // USUARIOS
        // ────────────────────────────────────────────────────────────────

        public async Task EnsureUserRegisteredAsync(Guid userId, string displayName, string email)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == userId))
            {
                _context.Users.Add(new User
                {
                    Id = userId,
                    DisplayName = displayName,
                    Email = email,
                    Password = "OAuth_External_Account",
                    InstitutionId = Guid.Parse("00000000-0000-0000-0000-000000000001")
                });

                await _context.SaveChangesAsync();
            }
        }
    }
}