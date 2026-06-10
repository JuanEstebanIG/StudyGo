using StudyGo.ViewModels;

using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class SubmissionVersionValidator : AbstractValidator<SubmissionVersionViewModel>
    {
        public SubmissionVersionValidator()
        {
            RuleFor(x => x.SubmissionId)
                .NotEmpty().WithMessage("La entrega es obligatoria.");

            RuleFor(x => x.VersionNumber)
                .GreaterThan(0).WithMessage("El número de versión debe ser mayor a 0.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código no puede estar vacío.");

            RuleFor(x => x.SavedAt)
                .NotEmpty().WithMessage("La fecha de guardado es obligatoria.")
                .LessThanOrEqualTo(System.DateTime.UtcNow).WithMessage("La fecha de guardado no puede ser futura.");
        }
    }
}