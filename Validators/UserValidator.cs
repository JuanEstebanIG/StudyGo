using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class UserValidator : AbstractValidator<UserViewModel>
    {
        public UserValidator()
        {
            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("La institución es obligatoria.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
                .MaximumLength(256).WithMessage("El correo no puede superar los 256 caracteres.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .MinimumLength(2).WithMessage("El nombre de usuario debe tener al menos 2 caracteres.")
                .MaximumLength(100).WithMessage("El nombre de usuario no puede superar los 100 caracteres.");
        }
    }
}