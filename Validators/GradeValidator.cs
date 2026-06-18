using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class GradeValidator : AbstractValidator<GradeViewModel>
    {
        public GradeValidator()
        {
            RuleFor(x => x.SubmissionId)
                .NotEmpty().WithMessage("La entrega es obligatoria.");

            RuleFor(x => x.FinalScore)
                .InclusiveBetween(0, 100).WithMessage("La nota final debe estar entre 0 y 100.");

            RuleFor(x => x.GradedAt)
                .NotEmpty().WithMessage("La fecha de calificación es obligatoria.")
                .LessThanOrEqualTo(System.DateTime.UtcNow).WithMessage("La fecha de calificación no puede ser futura.");
        }
    }
}