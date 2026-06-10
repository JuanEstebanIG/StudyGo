using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class RubricValidator : AbstractValidator<RubricViewModel>
    {
        public RubricValidator()
        {
            RuleFor(x => x.ProgrammingTaskId)
                .NotEmpty().WithMessage("La tarea de programación es obligatoria.");
        }
    }
}