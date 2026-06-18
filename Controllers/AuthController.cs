using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Models;
using StudyGo.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Auth/ExternalLogin
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            if (provider != "Google" && provider != "GitHub")
            {
                return RedirectToAction("Login");
            }

            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { returnUrl });

            // Forzamos a que el Challenge use el proveedor solicitado
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }


        // GET: /Auth/ExternalLoginCallback
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error del proveedor externo: {remoteError}");
                return View("Login", new LoginViewModel());
            }

            // Leemos el esquema dinámico con el que se autenticó el proveedor externo (Google/GitHub)
            var result = await HttpContext.AuthenticateAsync(User.Identity?.AuthenticationType ?? "Google");

            if (!result.Succeeded)
            {
                result = await HttpContext.AuthenticateAsync();
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Error al recuperar la información del proveedor externo.");
                return View("Login", new LoginViewModel());
            }

            // Extraemos el Email del Claim externo devuelto por los servidores de Google/GitHub
            var emailClaim = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el correo electrónico desde el proveedor externo.");
                return View("Login", new LoginViewModel());
            }

            // Extraemos también el nombre que viene del proveedor externo para usarlo de DisplayName
            var nameClaim = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? emailClaim;

            // Buscamos rigurosamente en la BD de StudyGo incluyendo la relación de roles
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == emailClaim);

            // 🚀 LÓGICA DE AUTO-REGISTRO SI EL USUARIO NO EXISTE
            if (user == null)
            {
                // Usamos los mismos GUIDs fijos definidos en tu AppDbContext
                Guid configInstitutionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                Guid estudianteRoleId = Guid.Parse("c3333333-3333-3333-3333-333333333333");

                // 1. Instanciamos el nuevo usuario con los requerimientos de tu DbContext
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = emailClaim,
                    DisplayName = nameClaim,
                    Password = "OAuth_External_User", // Valor por defecto ya que tu base de datos lo marca como IsRequired()
                    InstitutionId = configInstitutionId,
                    UserRoles = new List<UserRole>()
                };

                // 2. Agregamos el registro a la tabla Users
                _context.Users.Add(user);

                // 3. Creamos la relación explícita en la tabla intermedia UserRoles para asignarle Estudiante
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = estudianteRoleId
                };
                _context.UserRoles.Add(userRole);

                // 4. Impactamos la base de datos de forma asíncrona
                await _context.SaveChangesAsync();

                // 5. Volvemos a cargar el usuario con sus relaciones mapeadas para que el flujo de Claims no falle
                user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);
            }

            // Creamos los nuevos Claims corporativos locales basados en la REALIDAD de nuestra BD
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email)
    };

            // Inyectamos sus roles canónicos (Ahora incluirá "Estudiante" de manera dinámica)
            if (user.UserRoles != null)
            {
                foreach (var ur in user.UserRoles)
                {
                    if (ur.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));
                    }
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Iniciamos la sesión definitiva e institucional de StudyGo
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Redirigimos al Dashboard index para evaluar si va a Welcome o a su panel
            return RedirectToAction("Index", "Dashboard");
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || user.Password != model.Password)
            {
                ModelState.AddModelError(string.Empty, "Credenciales incorrectas. Inténtalo de nuevo.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email)
            };

            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                    }
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }


        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Destruye la sesión de la app limpiando la cookie local sin afectar la cuenta de Google del navegador.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Borra la cookie de autenticación de nuestra aplicación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Te devuelve al Login o a la raíz de la aplicación para que puedas re-autenticarte
            return RedirectToAction("Login", "Auth");
        }
    }
}