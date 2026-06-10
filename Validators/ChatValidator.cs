using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class ChatValidator : AbstractValidator<ChatViewModel>
    {
        public ChatValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("El tipo de chat no es válido.");
        }
    }
}