using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.ViewModels;

namespace StudyGo.Services
{
    public interface IQuizService
    {
        Task<IEnumerable<Quiz>> GetQuizzesForUserAsync(Guid userId, bool isAdmin);
        Task<Quiz> GetQuizByIdAsync(Guid id);
        Task<bool> CanUserManageQuizAsync(Guid quizId, Guid userId, bool isAdmin);
        Task<Quiz> CreateQuizAsync(QuizViewModel model);
        Task UpdateQuizAsync(Guid id, QuizViewModel model);
        Task DeleteQuizAsync(Guid id);
        Task GenerateMissingZeroAttemptsAsync(Guid quizId);
        Task<(List<QuizAttemptViewModel> Attempts, Dictionary<string, object> Metadata)> GetQuizStatsAsync(Guid quizId);
        Task<List<SelectListItem>> GetAvailableCoursesAsync(Guid userId, bool isAdmin);
    }
}