using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudyGo.Models;

namespace StudyGo.Services
{
    public interface IAcademicService
    {
        // Cursos
        Task<IEnumerable<Course>> GetCoursesForUserAsync(Guid userId, string role);
        Task<Course> GetCourseDetailAsync(Guid courseId);
        Task<bool> CreateCourseAsync(Course course, Guid teacherId);
        Task<bool> UpdateCourseAsync(Course course);
        Task<bool> DeleteCourseAsync(Guid courseId);
        Task<IEnumerable<Enrollment>> GetCourseMembersAsync(Guid courseId);
        Task<IEnumerable<Course>> GetAllCoursesAsync();
        Task<bool> EnrollAsync(Guid courseId, Guid studentId);
        Task<bool> UnenrollAsync(Guid courseId, Guid studentId);
        Task<bool> IsEnrolledAsync(Guid courseId, Guid studentId);

        // Google Drive
        Task<bool> IsDriveConnectedAsync(Guid userId);
        Task<bool> ConnectDriveAsync(Guid userId);
        Task<bool> DisconnectDriveAsync(Guid userId);
        Task<IEnumerable<DriveFile>> GetCourseDriveFilesAsync(Guid courseId);
        Task<bool> AttachDriveFileAsync(Guid courseId, Guid userId, string fileName, string url);
        Task<bool> RemoveDriveFileAsync(Guid fileId);

        // Tareas y Entregas
        Task<ProgrammingTask> GetTaskDetailAsync(Guid taskId);
        Task<bool> CreateTaskAsync(ProgrammingTask task);
        Task<bool> UpdateTaskAsync(ProgrammingTask task);
        Task<bool> DeleteTaskAsync(Guid taskId);
        Task<Submission> GetOrCreateSubmissionAsync(Guid taskId, Guid studentId);
        Task<IEnumerable<SubmissionVersion>> GetSubmissionVersionsAsync(Guid submissionId);
        Task<SubmissionVersion> GetSubmissionVersionAsync(Guid versionId);
        Task<SubmissionVersion> SaveSubmissionVersionAsync(Guid submissionId, string code);
        Task<bool> SubmitTaskAsync(Guid submissionId);
        Task<IEnumerable<Submission>> GetTaskSubmissionsAsync(Guid taskId);
        Task<bool> GradeSubmissionAsync(Guid submissionId, decimal score, string feedback);

        // Calificaciones
        Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid courseId, Guid studentId);
        Task<IEnumerable<Submission>> GetCourseGradebookAsync(Guid courseId);

        // Usuarios
        Task EnsureUserRegisteredAsync(Guid userId, string displayName, string email);
    }
}