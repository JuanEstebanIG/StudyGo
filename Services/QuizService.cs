using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.ViewModels;

namespace StudyGo.Services
{
    public class QuizService : IQuizService
    {
        private readonly AppDbContext _context;

        public QuizService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Quiz>> GetQuizzesForUserAsync(Guid userId, bool isAdmin)
        {
            var query = _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts)
                .AsQueryable();

            if (!isAdmin)
            {
                var teacherCourseIds = await _context.Courses
                    .Where(c => c.TeacherId == userId)
                    .Select(c => c.Id)
                    .ToListAsync();

                query = query.Where(q => teacherCourseIds.Contains(q.CourseId));
            }

            return await query.OrderByDescending(q => q.DueDate).ToListAsync();
        }

        public async Task<Quiz> GetQuizByIdAsync(Guid id)
        {
            return await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> CanUserManageQuizAsync(Guid quizId, Guid userId, bool isAdmin)
        {
            if (isAdmin) return true;
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);
            return quiz?.Course.TeacherId == userId;
        }

        public async Task<Quiz> CreateQuizAsync(QuizViewModel model)
        {
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
            return quiz;
        }

        public async Task UpdateQuizAsync(Guid id, QuizViewModel model)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) throw new InvalidOperationException("Quiz no encontrado.");

            // Actualizar datos del quiz
            quiz.Title = model.Title;
            quiz.Description = model.Description;
            quiz.CourseId = model.CourseId;
            quiz.State = model.State;
            quiz.DueDate = model.DueDate;
            quiz.SelectionMode = model.SelectionMode;
            quiz.TimeLimitMinutes = model.TimeLimitMinutes;
            quiz.OpenDate = model.OpenDate;
            quiz.MaxAttempts = model.MaxAttempts;

            // Estrategia borrar y recrear preguntas/opciones
            var existingOptions = quiz.Questions.SelectMany(q => q.Options).ToList();
            _context.QuizOptions.RemoveRange(existingOptions);

            var existingQuestions = quiz.Questions.ToList();
            _context.QuizQuestions.RemoveRange(existingQuestions);

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

            await _context.SaveChangesAsync();
        }

        public async Task DeleteQuizAsync(Guid id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.QuizAttempts)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) throw new InvalidOperationException("Quiz no encontrado.");

            _context.QuizAttempts.RemoveRange(quiz.QuizAttempts);
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task GenerateMissingZeroAttemptsAsync(Guid quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                    .ThenInclude(c => c.Enrollments)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null || !quiz.DueDate.HasValue || quiz.DueDate.Value >= DateTime.Now)
                return;

            var attemptedStudentIds = quiz.QuizAttempts
                .Select(a => a.StudentId)
                .ToHashSet();

            var missingStudentIds = quiz.Course.Enrollments
                .Where(e => !attemptedStudentIds.Contains(e.StudentId))
                .Select(e => e.StudentId)
                .Distinct()
                .ToList();

            if (!missingStudentIds.Any()) return;

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
                    AnswersJson = "{}"
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<(List<QuizAttemptViewModel> Attempts, Dictionary<string, object> Metadata)> GetQuizStatsAsync(Guid quizId)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) throw new InvalidOperationException("Quiz no encontrado.");

            var allAttempts = await _context.QuizAttempts
                .AsNoTracking()
                .Where(a => a.QuizId == quizId)
                .Include(a => a.Student)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

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

            var metadata = new Dictionary<string, object>
            {
                ["QuizTitle"] = quiz.Title,
                ["CourseName"] = quiz.Course.Name,
                ["TotalQuestions"] = quiz.Questions.Count,
                ["TimeLimitMinutes"] = quiz.TimeLimitMinutes,
                ["StudentNames"] = allAttempts.ToDictionary(
                    a => a.Id,
                    a => a.Student?.DisplayName ?? "Desconocido"),
                ["AverageScore"] = attempts.Any() ? attempts.Average(a => a.Score) : 0
            };

            return (attempts, metadata);
        }

        public async Task<List<SelectListItem>> GetAvailableCoursesAsync(Guid userId, bool isAdmin)
        {
            var query = _context.Courses.AsQueryable();

            if (!isAdmin)
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