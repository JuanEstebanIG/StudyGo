using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class ChatMessageValidator : AbstractValidator<ChatMessageViewModel>
    {
        public ChatMessageValidator()
        {
            RuleFor(x => x.ChatId)
                .NotEmpty().WithMessage("El chat es obligatorio.");

            RuleFor(x => x.SenderId)
                .NotEmpty().WithMessage("El remitente es obligatorio.");

            RuleFor(x => x.EncryptedContent)
                .NotEmpty().WithMessage("El contenido del mensaje no puede estar vacío.")
                .MaximumLength(4000).WithMessage("El contenido no puede superar los 4000 caracteres.");

            RuleFor(x => x.SentAt)
                .NotEmpty().WithMessage("La fecha de envío es obligatoria.")
                .LessThanOrEqualTo(System.DateTime.UtcNow).WithMessage("La fecha de envío no puede ser futura.");
        }
    }
}
