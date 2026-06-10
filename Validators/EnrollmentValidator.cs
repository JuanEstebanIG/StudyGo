using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class EnrollmentValidator : AbstractValidator<EnrollmentViewModel>
    {
        public EnrollmentValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("El estudiante es obligatorio.");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("El curso es obligatorio.");

            RuleFor(x => x.EnrolledBy)
                .NotEmpty().WithMessage("El usuario que inscribe es obligatorio.");

            RuleFor(x => x.EnrolledAt)
                .NotEmpty().WithMessage("La fecha de inscripción es obligatoria.")
                .LessThanOrEqualTo(System.DateTime.UtcNow).WithMessage("La fecha de inscripción no puede ser futura.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("El estado de inscripción no es válido.");
        }
    }
}