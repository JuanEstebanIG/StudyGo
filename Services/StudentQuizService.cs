using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace StudyGo.Services
{
    public class StudentQuizService : IStudentQuizService
    {
        private readonly AppDbContext _context;

        public StudentQuizService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentQuizCard>> GetActiveQuizzesAsync(Guid studentId)
        {
            var enrolledCourseIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
                .Select(e => e.CourseId)
                .ToListAsync();

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts.Where(a => a.StudentId == studentId))
                .Where(q => enrolledCourseIds.Contains(q.CourseId) && q.State == ActivityState.Publicado)
                .OrderByDescending(q => q.DueDate)
                .ToListAsync();

            return quizzes.Select(q => new StudentQuizCard
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
        }

        public async Task<List<StudentAttemptHistory>> GetAttemptHistoryAsync(Guid studentId)
        {
            var attempts = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Course)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            return attempts.Select(a => new StudentAttemptHistory
            {
                AttemptId = a.Id,
                QuizId = a.QuizId,
                QuizTitle = a.Quiz.Title,
                CourseName = a.Quiz.Course.Name,
                Score = a.Score,
                SubmittedAt = a.SubmittedAt,
                StartedAt = a.StartedAt
            }).ToList();
        }

        public async Task<(bool CanTake, string ErrorMessage)> CanStudentTakeQuizAsync(Guid studentId, Guid quizId)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return (false, "Quiz no encontrado.");

            var isEnrolled = await _context.Enrollments
                .AsNoTracking()
                .AnyAsync(e => e.StudentId == studentId &&
                               e.CourseId == quiz.CourseId &&
                               e.Status == EnrollmentStatus.Active);

            if (!isEnrolled)
                return (false, "No estás inscrito en este curso.");

            if (quiz.State != ActivityState.Publicado)
                return (false, "Este quiz no está disponible actualmente.");

            if (quiz.OpenDate.HasValue && quiz.OpenDate.Value > DateTime.Now)
                return (false, "Este quiz aún no está abierto.");

            if (quiz.DueDate.HasValue && quiz.DueDate.Value < DateTime.Now)
                return (false, "Este quiz ya ha cerrado.");

            var previousAttempts = await _context.QuizAttempts
                .AsNoTracking()
                .CountAsync(a => a.QuizId == quizId && a.StudentId == studentId);

            if (previousAttempts >= quiz.MaxAttempts)
                return (false, "Ya has agotado todos tus intentos para este quiz.");

            return (true, null);
        }

        public async Task<QuizAttempt> CreateAttemptAsync(Guid studentId, Guid quizId)
        {
            var startedAt = DateTime.Now;

            var attempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                QuizId = quizId,
                StudentId = studentId,
                StartedAt = startedAt,
                SubmittedAt = startedAt,
                Score = 0,
                AnswersJson = "{}"
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return attempt;
        }

        public async Task<TakeQuizViewModel> GetQuizForTakingAsync(Guid quizId, Guid attemptId, DateTime startedAt)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Course)
                .Include(q => q.Questions.OrderBy(x => x.Order))
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                throw new InvalidOperationException("Quiz no encontrado.");

            return new TakeQuizViewModel
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
        }

        public async Task<(decimal Score, bool TimeExceeded)> SubmitAttemptAsync(
            Guid attemptId,
            Guid quizId,
            Dictionary<string, List<string>> answers)
        {
            var attempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.QuizId == quizId);

            if (attempt == null)
                throw new InvalidOperationException("Intento no encontrado.");

            bool alreadySubmitted = !string.IsNullOrEmpty(attempt.AnswersJson) && attempt.AnswersJson != "{}";

            if (alreadySubmitted)
                throw new InvalidOperationException("Este intento ya fue enviado.");

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                throw new InvalidOperationException("Quiz no encontrado.");

            var now = DateTime.Now;
            var elapsed = now - attempt.StartedAt;
            var maxAllowed = TimeSpan.FromMinutes(quiz.TimeLimitMinutes + 1);
            bool timeExceeded = elapsed > maxAllowed;

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

                if (correctOptionIds.SetEquals(selectedOptionIds))
                    correctAnswers++;
            }

            decimal score = totalQuestions > 0
                ? Math.Round((decimal)correctAnswers / totalQuestions * 100, 2)
                : 0;

            attempt.SubmittedAt = now;
            attempt.Score = score;
            attempt.AnswersJson = JsonSerializer.Serialize(answers ?? new Dictionary<string, List<string>>());

            await _context.SaveChangesAsync();

            return (score, timeExceeded);
        }

        public async Task<QuizResultViewModel> GetResultAsync(Guid attemptId, Guid studentId)
        {
            var attempt = await _context.QuizAttempts
                .AsNoTracking()
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Course)
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null)
                throw new InvalidOperationException("Resultado no encontrado.");

            var answers = !string.IsNullOrEmpty(attempt.AnswersJson)
                ? JsonSerializer.Deserialize<Dictionary<string, List<string>>>(attempt.AnswersJson)
                : new Dictionary<string, List<string>>();

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

            return new QuizResultViewModel
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
        }
    }
}