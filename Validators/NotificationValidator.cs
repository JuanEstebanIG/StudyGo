using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class NotificationValidator : AbstractValidator<NotificationViewModel>
    {
        public NotificationValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El usuario es obligatorio.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("El tipo de notificación es obligatorio.")
                .MaximumLength(100).WithMessage("El tipo no puede superar los 100 caracteres.");

            RuleFor(x => x.CreatedAt)
                .NotEmpty().WithMessage("La fecha de creación es obligatoria.");
        }
    }
}