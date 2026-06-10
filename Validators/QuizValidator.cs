using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class QuizValidator : AbstractValidator<QuizViewModel>
    {
        public QuizValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("El curso es obligatorio.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MinimumLength(3).WithMessage("El título debe tener al menos 3 caracteres.")
                .MaximumLength(200).WithMessage("El título no puede superar los 200 caracteres.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("La descripción es obligatoria.")
                .MinimumLength(10).WithMessage("La descripción debe tener al menos 10 caracteres.")
                .MaximumLength(2000).WithMessage("La descripción no puede superar los 2000 caracteres.");

            RuleFor(x => x.State)
                .IsInEnum().WithMessage("El estado de la actividad no es válido.");

            RuleFor(x => x.SelectionMode)
                .IsInEnum().WithMessage("El modo de selección no es válido.");
        }
    }
}