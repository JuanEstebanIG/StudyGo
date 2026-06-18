using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AspNet.Security.OAuth.GitHub;
using FluentValidation;
using StudyGo.Data;
using StudyGo.Validators;
using StudyGo.ViewModels;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// CARGA DE VARIABLES DE ENTORNO: Lee el archivo .env e inyecta las llaves en el sistema
DotNetEnv.Env.Load();

// Configurar los proveedores de configuración para que reconozcan las variables del entorno del sistema
builder.Configuration.AddEnvironmentVariables();

// ------------------------------------------------------------------
// CONFIGURACIÓN DEL SISTEMA DE AUTENTICACIÓN UNIFICADO (COOKIES + OAUTH)
// ------------------------------------------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // Rutas de redirección del flujo de identidad
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";

    // Seguridad y ciclo de vida de la sesión compartida
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Google ClientId no configurado en el entorno.");
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google ClientSecret no configurado en el entorno.");
})
.AddGitHub(githubOptions =>
{
    githubOptions.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]
        ?? throw new InvalidOperationException("GitHub ClientId no configurado en el entorno.");
    githubOptions.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]
        ?? throw new InvalidOperationException("GitHub ClientSecret no configurado en el entorno.");
    githubOptions.Scope.Add("user:email");
});

// CONTEXTO DE BASE DE DATOS
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// REGISTRO MANUAL DE VALIDANDORES (Evita errores de ensamblados externos)
builder.Services.AddScoped<IValidator<UserViewModel>, UserValidator>();

// CONTROLADORES Y VISTAS
builder.Services.AddControllersWithViews();

var app = builder.Build();

// CONFIGURACIÓN DEL PIPELINE HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// El orden de la seguridad es estricto en el pipeline de .NET
app.UseAuthentication();
app.UseAuthorization();

// Optimización de recursos estáticos (Tailwind, JS, etc.)
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();