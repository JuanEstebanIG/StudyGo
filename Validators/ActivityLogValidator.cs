using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class ActivityLogValidator : AbstractValidator<ActivityLogViewModel>
    {
        public ActivityLogValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El usuario es obligatorio.");

            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("La acción es obligatoria.")
                .MaximumLength(500).WithMessage("La acción no puede superar los 500 caracteres.");

            RuleFor(x => x.Timestamp)
                .NotEmpty().WithMessage("La fecha es obligatoria.")
                .LessThanOrEqualTo(System.DateTime.UtcNow).WithMessage("La fecha no puede ser futura.");
        }
    }
}