using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class CalendarEventValidator : AbstractValidator<CalendarEventViewModel>
    {
        public CalendarEventValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("El curso es obligatorio.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MinimumLength(3).WithMessage("El título debe tener al menos 3 caracteres.")
                .MaximumLength(200).WithMessage("El título no puede superar los 200 caracteres.");

            RuleFor(x => x.StartsAt)
                .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

            RuleFor(x => x.EndsAt)
                .NotEmpty().WithMessage("La fecha de fin es obligatoria.")
                .GreaterThan(x => x.StartsAt).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");
        }
    }
}