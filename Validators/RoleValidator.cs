using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class RoleValidator : AbstractValidator<RoleViewModel>
    {
        public RoleValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres.")
                .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
        }
    }
}