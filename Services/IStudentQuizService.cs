using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudyGo.ViewModels;
using StudyGo.Models;   

namespace StudyGo.Services
{
    public interface IStudentQuizService
    {
        Task<List<StudentQuizCard>> GetActiveQuizzesAsync(Guid studentId);
        Task<List<StudentAttemptHistory>> GetAttemptHistoryAsync(Guid studentId);
        Task<(bool CanTake, string ErrorMessage)> CanStudentTakeQuizAsync(Guid studentId, Guid quizId);
        Task<QuizAttempt> CreateAttemptAsync(Guid studentId, Guid quizId);
        Task<TakeQuizViewModel> GetQuizForTakingAsync(Guid quizId, Guid attemptId, DateTime startedAt);
        Task<(decimal Score, bool TimeExceeded)> SubmitAttemptAsync(Guid attemptId, Guid quizId, Dictionary<string, List<string>> answers);
        Task<QuizResultViewModel> GetResultAsync(Guid attemptId, Guid studentId);
    }
}