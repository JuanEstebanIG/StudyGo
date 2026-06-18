using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Models; // Ajusta según el namespace de tus entidades base
using StudyGo.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    // Bloqueo estricto a nivel de controlador: Solo usuarios con el Claim de Administrador entran aquí
    [Authorize(Roles = "Administrador")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Users
        public async Task<IActionResult> Index(string searchTerm, string roleFilter, string statusFilter)
        {
            // Query base trayendo las relaciones canónicas de UserRole y Role
            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            // 1. Aplicar Filtro de Búsqueda por Nombre o Correo
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.DisplayName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            // 2. Aplicar Filtro por Rol
            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleFilter));
            }

            // 3. Aplicar Filtro por Estado (Mapeado lógicamente por el campo Password o una bandera si la tienes)
            // Para este ejemplo, asumiremos una propiedad lógica 'IsActive' o la evaluaremos por la estructura
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool targetStatus = statusFilter == "Active";
                // query = query.Where(u => u.IsActive == targetStatus); // Descomentar si tu entidad ya tiene IsActive
            }

            // Mapeo eficiente hacia el ViewModel de listado corporativo
            var userList = await query.Select(u => new UserListViewModel
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                IsActive = true, // Mapeo temporal, cámbialo por tu propiedad de entidad real
                LastActivity = DateTime.UtcNow.AddHours(-2) // Simulación para el requerimiento de la tabla
            }).ToListAsync();

            var model = new UserIndexViewModel
            {
                Users = userList,
                SearchTerm = searchTerm,
                SelectedRoleFilter = roleFilter,
                SelectedStatusFilter = statusFilter,
                // Pre-cargamos la institución del administrador actual para cumplir la FK restrictiva
                NewUser = new UserActionViewModel
                {
                    InstitutionId = Guid.Parse("00000000-0000-0000-0000-000000000001")
                }
            };

            return View(model);
        }

        // GET: /Users/GetUserDetails/{id}
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // Retornamos los datos mínimos necesarios en formato JSON
            return Json(new
            {
                id = user.Id,
                displayName = user.DisplayName,
                email = user.Email,
                // Tomamos el primer rol asignado si existe
                roleId = user.UserRoles.FirstOrDefault()?.RoleId.ToString() ?? ""
            });
        }

        // POST: /Users/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(Guid userId, Guid newRoleId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            // 1. Remover las relaciones de rol anteriores de la tabla intermedia
            if (user.UserRoles.Any())
            {
                _context.UserRoles.RemoveRange(user.UserRoles);
            }

            // 2. Insertar la nueva relación canónica
            var newUserRole = new UserRole
            {
                UserId = userId,
                RoleId = newRoleId
            };
            _context.UserRoles.Add(newUserRole);

            // 3. Persistir cambios en la base de datos
            await _context.SaveChangesAsync();

            // Redireccionamos a la lista refrescando la estructura limpia
            return RedirectToAction(nameof(Index));
        }


    }
}