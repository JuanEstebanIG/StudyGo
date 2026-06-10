using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class SubmissionValidator : AbstractValidator<SubmissionViewModel>
    {
        public SubmissionValidator()
        {
            RuleFor(x => x.ProgrammingTaskId)
                .NotEmpty().WithMessage("La tarea de programación es obligatoria.");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("El estudiante es obligatorio.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("El estado de la entrega no es válido.");
        }
    }
}