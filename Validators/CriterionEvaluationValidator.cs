using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class CriterionEvaluationValidator : AbstractValidator<CriterionEvaluationViewModel>
    {
        public CriterionEvaluationValidator()
        {
            RuleFor(x => x.GradeId)
                .NotEmpty().WithMessage("La calificación es obligatoria.");

            RuleFor(x => x.RubricCriteriaId)
                .NotEmpty().WithMessage("El criterio de rúbrica es obligatorio.");

            RuleFor(x => x.Score)
                .InclusiveBetween(0, 100).WithMessage("El puntaje debe estar entre 0 y 100.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("El comentario no puede superar los 1000 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Comment));
        }
    }
}