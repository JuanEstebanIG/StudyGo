using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class CourseValidator : AbstractValidator<CourseViewModel>
    {
        public CourseValidator()
        {
            RuleFor(x => x.InstitutionId)
                .NotEmpty().WithMessage("La institución es obligatoria.");

            RuleFor(x => x.TeacherId)
                .NotEmpty().WithMessage("El docente es obligatorio.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del curso es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres.")
                .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");
        }
    }
}