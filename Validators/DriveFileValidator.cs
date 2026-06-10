using FluentValidation;
using StudyGo.ViewModels;

namespace StudyGo.Validators
{
    public class DriveFileValidator : AbstractValidator<DriveFileViewModel>
    {
        public DriveFileValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty().WithMessage("El propietario es obligatorio.");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("El curso es obligatorio.");

            RuleFor(x => x.DriveFileId)
                .NotEmpty().WithMessage("El ID del archivo en Drive es obligatorio.")
                .MaximumLength(300).WithMessage("El DriveFileId no puede superar los 300 caracteres.");

            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("La URL es obligatoria.")
                .MaximumLength(2000).WithMessage("La URL no puede superar los 2000 caracteres.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("La URL no tiene un formato válido.");
        }
    }
}