using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class QuizAttemptValidator : AbstractValidator<QuizAttemptViewModel>
    {
        public QuizAttemptValidator()
        {
            RuleFor(x => x.QuizId)
                .NotEmpty().WithMessage("El quiz es obligatorio.");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("El estudiante es obligatorio.");

            RuleFor(x => x.Score)
                .InclusiveBetween(0, 100).WithMessage("El puntaje debe estar entre 0 y 100.");

            RuleFor(x => x.SubmittedAt)
                .NotEmpty().WithMessage("La fecha de envío es obligatoria.")
                .LessThanOrEqualTo(System.DateTime.UtcNow).WithMessage("La fecha de envío no puede ser futura.");
        }
    }
}