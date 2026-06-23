using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        // Inyectamos el contexto para interactuar de forma segura con UserRoles y Roles
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Dashboard/Index (Punto de entrada post-login)
        public async Task<IActionResult> Index()
        {
            // Extraemos el email del usuario autenticado desde los claims de Google
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Consultamos al usuario con sus respectivos roles de forma síncrona/directa a la BD
            var dbUser = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (dbUser == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var roles = dbUser.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Flag de control: Mantener en true para forzar la vista de Onboarding/Bienvenida
            bool isNewUser = true;

            if (isNewUser)
            {
                return RedirectToAction("Welcome");
            }

            // Si no es nuevo, lo redirigimos usando la data real de la BD
            if (roles.Contains("Administrador")) return RedirectToAction("Index", "Users");
            if (roles.Contains("Docente")) return RedirectToAction("TeacherPanel");

            return RedirectToAction("StudentPanel");
        }

        // GET: /Dashboard/Welcome
        public async Task<IActionResult> Welcome()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Forzamos la lectura directa desde la base de datos para pintar el rol real
            var dbUser = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            // Extraemos el primer nombre de rol disponible, de lo contrario asignamos Estudiante
            string primaryRole = dbUser?.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Estudiante";
            string nameClaim = dbUser?.DisplayName ?? (User.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario");

            ViewData["UserRole"] = primaryRole;
            ViewData["UserName"] = nameClaim;

            return View();
        }

        /// <summary>
        /// Punto de entrada del botón para redirigir dinámicamente según el rol consultado en BD.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GoToDashboard()
        {
            // 1. Validar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Extraer el email desde los claims de la sesión
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // 3. Consultar los roles reales y actualizados directamente en la Base de Datos
            var dbUser = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (dbUser == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // 4. Capturar el rol primario
            string userRole = dbUser.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault();

            // 5. Redirección condicional estricta basada en la BD
            switch (userRole)
            {
                case "Administrador":
                    return RedirectToAction("Index", "Users"); // Va al UsersController

                case "Docente":
                    return RedirectToAction("Index", "Home"); 

                case "Estudiante":
                    return RedirectToAction("Index", "Home"); 

                default:
                    return RedirectToAction("Welcome");
            }
        }
    }
}