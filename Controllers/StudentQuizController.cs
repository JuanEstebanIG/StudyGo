using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Services;
using StudyGo.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    [Authorize(Roles = "Estudiante")]
    public class StudentQuizController : Controller
    {
        private readonly IStudentQuizService _studentQuizService;

        public StudentQuizController(IStudentQuizService studentQuizService)
        {
            _studentQuizService = studentQuizService;
        }

        private Guid GetCurrentStudentId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var studentId = GetCurrentStudentId();

            var activeQuizzes = await _studentQuizService.GetActiveQuizzesAsync(studentId);
            var attemptHistory = await _studentQuizService.GetAttemptHistoryAsync(studentId);

            var viewModel = new StudentQuizViewModel
            {
                ActiveQuizzes = activeQuizzes,
                AttemptHistory = attemptHistory
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Take(Guid quizId)
        {
            var studentId = GetCurrentStudentId();

            var (canTake, errorMessage) = await _studentQuizService.CanStudentTakeQuizAsync(studentId, quizId);

            if (!canTake)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction(nameof(Index));
            }

            var attempt = await _studentQuizService.CreateAttemptAsync(studentId, quizId);

            var viewModel = await _studentQuizService.GetQuizForTakingAsync(
                quizId, attempt.Id, attempt.StartedAt);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            Guid quizId,
            Guid attemptId,
            Dictionary<string, List<string>> answers)
        {
            try
            {
                var (score, timeExceeded) = await _studentQuizService.SubmitAttemptAsync(attemptId, quizId, answers);

                if (timeExceeded)
                    TempData["WarningMessage"] = "Se excedió el tiempo límite. El envío fue aceptado pero se registró la demora.";

                return RedirectToAction(nameof(Result), new { attemptId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Result(Guid attemptId)
        {
            var studentId = GetCurrentStudentId();

            try
            {
                var viewModel = await _studentQuizService.GetResultAsync(attemptId, studentId);
                return View(viewModel);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }
    }
}