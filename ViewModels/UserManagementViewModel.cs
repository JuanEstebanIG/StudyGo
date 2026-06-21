using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class UserListViewModel
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class UserActionViewModel
    {
        public Guid? Id { get; set; } // Nullable si es una invitación nueva

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar al menos un rol")]
        public List<string> SelectedRoles { get; set; } = new();

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "La institución es obligatoria")]
        public Guid InstitutionId { get; set; }
    }

    public class UserIndexViewModel
    {
        public List<UserListViewModel> Users { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedRoleFilter { get; set; } = string.Empty;
        public string SelectedStatusFilter { get; set; } = string.Empty;
        public UserActionViewModel NewUser { get; set; } = new();
    }
}