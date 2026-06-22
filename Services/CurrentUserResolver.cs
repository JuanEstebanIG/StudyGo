// ============================================================================
// StudyGo · Services/CurrentUserResolver.cs
// ----------------------------------------------------------------------------
// Resuelve el usuario actual desde los claims. Como la AUTENTICACIÓN todavía
// NO está configurada (es de Micky: ASP.NET Identity + OAuth, §12.1), en
// entorno de DESARROLLO se usa un fallback: el primer usuario de la BD, para
// que el chat sea navegable. En producción, sin sesión → devuelve null.
//
// TODO (Micky): al cablear Identity/OAuth, este resolver empezará a leer el
// claim NameIdentifier automáticamente y el fallback dejará de usarse.
// ============================================================================
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;

namespace StudyGo.Services
{
    public record CurrentUser(Guid Id, string DisplayName, bool IsTeacher);

    public interface ICurrentUserResolver
    {
        Task<CurrentUser?> ResolveAsync(ClaimsPrincipal? principal);
    }

    public class CurrentUserResolver : ICurrentUserResolver
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CurrentUserResolver(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<CurrentUser?> ResolveAsync(ClaimsPrincipal? principal)
        {
            // 1) Camino real (cuando exista auth): claim NameIdentifier.
            var idClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var id))
            {
                var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
                if (u is not null)
                {
                    var teacher = principal!.IsInRole("Docente") || principal.IsInRole("Administrador");
                    return new CurrentUser(u.Id, u.DisplayName, teacher);
                }
            }

            // 2) Fallback SOLO en desarrollo: primer usuario de la BD.
            if (_env.IsDevelopment())
            {
                var demo = await _db.Users.OrderBy(x => x.Email).FirstOrDefaultAsync();
                if (demo is not null)
                    return new CurrentUser(demo.Id, demo.DisplayName, IsTeacher: false);
            }

            return null;
        }
    }
}
