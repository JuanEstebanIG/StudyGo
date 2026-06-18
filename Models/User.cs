using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid InstitutionId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }

        // PROPIEDAD ADICIONADA: Necesaria para validar la autenticación en el Login
        public string Password { get; set; } = string.Empty;

        public Institution Institution { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public ICollection<DriveFile> DriveFiles { get; set; } = new List<DriveFile>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }
}