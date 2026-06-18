using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class RubricCriteriaValidator : AbstractValidator<RubricCriteriaViewModel>
    {
        public RubricCriteriaValidator()
        {
            RuleFor(x => x.RubricId)
                .NotEmpty().WithMessage("La rúbrica es obligatoria.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("La descripción del criterio es obligatoria.")
                .MinimumLength(5).WithMessage("La descripción debe tener al menos 5 caracteres.")
                .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.");

            RuleFor(x => x.Weight)
                .GreaterThan(0).WithMessage("El peso debe ser mayor a 0.")
                .LessThanOrEqualTo(100).WithMessage("El peso no puede superar 100.");
        }
    }
}