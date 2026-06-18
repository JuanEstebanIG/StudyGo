using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class ProgrammingTaskValidator : AbstractValidator<ProgrammingTaskViewModel>
    {
        public ProgrammingTaskValidator()
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

            RuleFor(x => x.Language)
                .NotEmpty().WithMessage("El lenguaje de programación es obligatorio.")
                .MaximumLength(50).WithMessage("El lenguaje no puede superar los 50 caracteres.");

            RuleFor(x => x.TimeLimitSeconds)
                .GreaterThan(0).WithMessage("El límite de tiempo debe ser mayor a 0.")
                .LessThanOrEqualTo(300).WithMessage("El límite de tiempo no puede superar los 300 segundos.");

            RuleFor(x => x.MemoryLimitMb)
                .GreaterThanOrEqualTo(16).WithMessage("El límite de memoria debe ser al menos 16 MB.")
                .LessThanOrEqualTo(4096).WithMessage("El límite de memoria no puede superar los 4096 MB.");
        }
    }
}