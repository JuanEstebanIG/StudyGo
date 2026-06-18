using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class InstitutionValidator : AbstractValidator<InstitutionViewModel>
    {
        public InstitutionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de la institución es obligatorio.")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres.")
                .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");
        }
    }
}