using Microsoft.EntityFrameworkCore;
using StudyGo.Enums;
using StudyGo.Models;
using System.Reflection.Emit;

namespace StudyGo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Institution> Institutions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ProgrammingTask> ProgrammingTasks { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Rubric> Rubrics { get; set; }
        public DbSet<RubricCriteria> RubricCriterias { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionVersion> SubmissionVersions { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<CriterionEvaluation> CriterionEvaluations { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<DriveFile> DriveFiles { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------------------------------------
            // Institution
            // ------------------------------------------------
            modelBuilder.Entity<Institution>(entity =>
                {
                    entity.ToTable("Institutions");
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedOnAdd();

                    entity.Property(x => x.Name)
                        .IsRequired()
                        .HasMaxLength(200);
                });

            // ------------------------------------------------
            // User
            // ------------------------------------------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(x => x.DisplayName)
                    .IsRequired()
                    .HasMaxLength(100);

                // CONFIGURACIÓN ADICIONADA: Mapeo estricto para el almacenamiento de contraseñas
                entity.Property(x => x.Password)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(x => x.Email)
                    .IsUnique();

                entity.HasOne(x => x.Institution)
                    .WithMany(i => i.Users)
                    .HasForeignKey(x => x.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------
            // Role
            // ------------------------------------------------
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            // ------------------------------------------------
            // UserRole  (Many-to-Many: User <-> Role)
            // ------------------------------------------------
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(x => new { x.UserId, x.RoleId });

                entity.HasOne(x => x.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------
            // Course
            // ------------------------------------------------
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                // Mapeo para la columna Code (opcional, longitud 50)
                entity.Property(x => x.Code)
                    .HasMaxLength(50)
                    .IsRequired(false);

                entity.HasOne(x => x.Institution)
                    .WithMany(i => i.Courses)
                    .HasForeignKey(x => x.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------
            // Enrollment
            // ------------------------------------------------
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.ToTable("Enrollments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.EnrolledAt)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.HasOne(x => x.Student)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.StudentId, x.CourseId })
                    .IsUnique();
            });

            // ------------------------------------------------
            // Activity  (TPH — Table-Per-Hierarchy)
            // ------------------------------------------------
            modelBuilder.Entity<Activity>(entity =>
            {
                entity.ToTable("Activities");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Description)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(x => x.State)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(30);

                // Configuración agregada para garantizar la consistencia en BD
                entity.Property(x => x.DueDate)
                    .IsRequired(false);

                entity.HasDiscriminator<string>("ActivityType")
                    .HasValue<ProgrammingTask>("ProgrammingTask")
                    .HasValue<Quiz>("Quiz");

                entity.HasOne(x => x.Course)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------
            // ProgrammingTask
            // ------------------------------------------------
            modelBuilder.Entity<ProgrammingTask>(entity =>
            {
                entity.Property(x => x.Language)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.TimeLimitSeconds)
                    .IsRequired();

                entity.Property(x => x.MemoryLimitMb)
                    .IsRequired();
            });

            // ------------------------------------------------
            // Quiz
            // ------------------------------------------------
            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.Property(x => x.SelectionMode)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(x => x.TimeLimitMinutes)
                    .IsRequired()
                    .HasDefaultValue(30);

                entity.Property(x => x.MaxAttempts)
                    .IsRequired()
                    .HasDefaultValue(1);
            });

            // ------------------------------------------------
            // QuizQuestion
            // ------------------------------------------------
            modelBuilder.Entity<QuizQuestion>(entity =>
            {
                entity.ToTable("QuizQuestions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.QuestionText)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(x => x.QuestionType)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Unica");

                entity.Property(x => x.Order)
                    .IsRequired();

                entity.HasOne(x => x.Quiz)
                    .WithMany(q => q.Questions)
                    .HasForeignKey(x => x.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.QuizId, x.Order });
            });

            // ------------------------------------------------
            // QuizOption
            // ------------------------------------------------
            modelBuilder.Entity<QuizOption>(entity =>
            {
                entity.ToTable("QuizOptions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.OptionText)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(x => x.IsCorrect)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.HasOne(x => x.QuizQuestion)
                    .WithMany(q => q.Options)
                    .HasForeignKey(x => x.QuizQuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------
            // Rubric  (One-to-One with ProgrammingTask)
            // ------------------------------------------------
            modelBuilder.Entity<Rubric>(entity =>
            {
                entity.ToTable("Rubrics");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.HasOne(x => x.ProgrammingTask)
                    .WithOne(pt => pt.Rubric)
                    .HasForeignKey<Rubric>(x => x.ProgrammingTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------
            // RubricCriteria
            // ------------------------------------------------
            modelBuilder.Entity<RubricCriteria>(entity =>
            {
                entity.ToTable("RubricCriterias");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(x => x.Weight)
                    .IsRequired()
                    .HasPrecision(5, 2);

                entity.HasOne(x => x.Rubric)
                    .WithMany(r => r.Criteria)
                    .HasForeignKey(x => x.RubricId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------
            // Submission
            // ------------------------------------------------
            modelBuilder.Entity<Submission>(entity =>
            {
                entity.ToTable("Submissions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.HasOne(x => x.ProgrammingTask)
                    .WithMany(pt => pt.Submissions)
                    .HasForeignKey(x => x.ProgrammingTaskId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Student)
                    .WithMany(u => u.Submissions)
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.ProgrammingTaskId, x.StudentId });
            });

            // ------------------------------------------------
            // SubmissionVersion
            // ------------------------------------------------
            modelBuilder.Entity<SubmissionVersion>(entity =>
            {
                entity.ToTable("SubmissionVersions");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Code)
                    .IsRequired();

                entity.Property(x => x.VersionNumber)
                    .IsRequired();

                entity.Property(x => x.SavedAt)
                    .IsRequired();

                entity.HasOne(x => x.Submission)
                    .WithMany(s => s.Versions)
                    .HasForeignKey(x => x.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SubmissionId, x.VersionNumber })
                    .IsUnique();
            });

            // ------------------------------------------------
            // Grade  (One-to-One with Submission, optional)
            // ------------------------------------------------
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.ToTable("Grades");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.FinalScore)
                    .IsRequired()
                    .HasPrecision(5, 2);

                entity.Property(x => x.GradedAt)
                    .IsRequired();

                entity.HasOne(x => x.Submission)
                    .WithOne(s => s.Grade)
                    .HasForeignKey<Grade>(x => x.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ------------------------------------------------
            // CriterionEvaluation
            // ------------------------------------------------
            modelBuilder.Entity<CriterionEvaluation>(entity =>
            {
                entity.ToTable("CriterionEvaluations");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Score)
                    .IsRequired()
                    .HasPrecision(5, 2);

                entity.Property(x => x.Comment)
                    .HasMaxLength(1000);

                entity.HasOne(x => x.Grade)
                    .WithMany(g => g.CriterionEvaluations)
                    .HasForeignKey(x => x.GradeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.RubricCriteria)
                    .WithMany(rc => rc.CriterionEvaluations)
                    .HasForeignKey(x => x.RubricCriteriaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------
            // QuizAttempt
            // ------------------------------------------------
            modelBuilder.Entity<QuizAttempt>(entity =>
            {
                entity.ToTable("QuizAttempts");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Score)
                    .IsRequired()
                    .HasPrecision(5, 2);

                entity.Property(x => x.SubmittedAt)
                    .IsRequired();

                entity.Property(x => x.StartedAt)
                    .IsRequired();

                entity.Property(x => x.AnswersJson)
                    .HasColumnType("nvarchar(max)");

                entity.HasOne(x => x.Quiz)
                    .WithMany(q => q.QuizAttempts)
                    .HasForeignKey(x => x.QuizId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Student)
                    .WithMany(u => u.QuizAttempts)
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.QuizId, x.StudentId });
            });

            // ------------------------------------------------
            // Notification
            // ------------------------------------------------
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Type)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.IsRead });
            });

            // ------------------------------------------------
            // Chat
            // ------------------------------------------------
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.ToTable("Chats");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Type)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });

            // ------------------------------------------------
            // ChatParticipant  (Many-to-Many: Chat <-> User)
            // ------------------------------------------------
            modelBuilder.Entity<ChatParticipant>(entity =>
            {
                entity.ToTable("ChatParticipants");
                entity.HasKey(x => new { x.ChatId, x.UserId });

                entity.HasOne(x => x.Chat)
                    .WithMany(c => c.Participants)
                    .HasForeignKey(x => x.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.User)
                    .WithMany(u => u.ChatParticipants)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------
            // ChatMessage
            // ------------------------------------------------
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("ChatMessages");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.EncryptedContent)
                    .IsRequired();

                entity.Property(x => x.SentAt)
                    .IsRequired();

                entity.HasOne(x => x.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(x => x.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Sender)
                    .WithMany(u => u.ChatMessages)
                    .HasForeignKey(x => x.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ChatId);
            });

            // ------------------------------------------------
            // DriveFile
            // ------------------------------------------------
            modelBuilder.Entity<DriveFile>(entity =>
            {
                entity.ToTable("DriveFiles");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.DriveFileId)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(x => x.Url)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(x => x.Owner)
                    .WithMany(u => u.DriveFiles)
                    .HasForeignKey(x => x.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Course)
                    .WithMany(c => c.DriveFiles)
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.DriveFileId)
                    .IsUnique();
            });

            // ------------------------------------------------
            // CalendarEvent
            // ------------------------------------------------
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.ToTable("CalendarEvents");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.StartsAt)
                    .IsRequired();

                entity.Property(x => x.EndsAt)
                    .IsRequired();

                entity.HasOne(x => x.Course)
                    .WithMany(c => c.CalendarEvents)
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.CourseId, x.StartsAt });
            });

            // ------------------------------------------------
            // ActivityLog
            // ------------------------------------------------
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("ActivityLogs");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();

                entity.Property(x => x.Action)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(x => x.Timestamp)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(u => u.ActivityLogs)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.UserId, x.Timestamp });
            });




            //----------------------------------------------------------
            // SEEDERS UNIFICADOS (Institución, Roles, Usuarios y Relaciones)
            //----------------------------------------------------------

            // 1. GUID Fijo de la Institución
            Guid configInstitutionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            modelBuilder.Entity<Institution>().HasData(
                new Institution
                {
                    Id = configInstitutionId,
                    Name = "Institución Educativa StudyGo"
                }
            );

            // 2. GUIDs Fijos para los Roles del Sistema
            Guid adminRoleId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
            Guid docenteRoleId = Guid.Parse("b2222222-2222-2222-2222-222222222222");
            Guid estudianteRoleId = Guid.Parse("c3333333-3333-3333-3333-333333333333");

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = adminRoleId, Name = "Administrador" },
                new Role { Id = docenteRoleId, Name = "Docente" },
                new Role { Id = estudianteRoleId, Name = "Estudiante" }
            );

            // 3. Usuarios con los mismos GUIDs fijos que capturó la migración
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("c20a47c0-8977-4e0a-b612-7f8d7cd4398d"), // Usuario Isaza
                    Email = "isazaj601@gmail.com",
                    Password = "OAuth_External_Account",
                    DisplayName = "Usuario Isaza",
                    InstitutionId = configInstitutionId
                },
                new User
                {
                    Id = Guid.Parse("1fbed7cb-b26a-49c3-b9a5-b1f74111548a"), // Steven Florez
                    Email = "stevenflorez2304@gmail.com",
                    Password = "OAuth_External_Account",
                    DisplayName = "Steven Florez",
                    InstitutionId = configInstitutionId
                },
                new User
                {
                    Id = Guid.Parse("b0cf00ae-dc66-4092-8113-efa1b46959a6"), // Luis Alejandro Londoño
                    Email = "londonovalleluisalejandro@gmail.com",
                    Password = "OAuth_External_Account",
                    DisplayName = "Luis Alejandro Londoño",
                    InstitutionId = configInstitutionId
                }
            );

            // 4. Relación Muchos a Muchos (UserRoles) - Mapeo Primitivo Estricto
            modelBuilder.Entity<UserRole>().HasData(
                // Luis Alejandro Londoño -> Administrador
                new UserRole
                {
                    UserId = Guid.Parse("b0cf00ae-dc66-4092-8113-efa1b46959a6"),
                    RoleId = adminRoleId
                },
                // Steven Florez -> Estudiante
                new UserRole
                {
                    UserId = Guid.Parse("1fbed7cb-b26a-49c3-b9a5-b1f74111548a"),
                    RoleId = estudianteRoleId
                },
                // Usuario Isaza -> Estudiante
                new UserRole
                {
                    UserId = Guid.Parse("c20a47c0-8977-4e0a-b612-7f8d7cd4398d"),
                    RoleId = estudianteRoleId
                }
            );
        }
    }
}