using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AspNet.Security.OAuth.GitHub;
using FluentValidation;
using StudyGo.Data;
using StudyGo.Hubs;       // Jaison · Comunicación
using StudyGo.Services;   // Jaison · Comunicación
using StudyGo.Validators;
using StudyGo.ViewModels;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)); // Cambia .UseSqlServer si usas otro motor (PostgreSQL, SQLite, etc.)

builder.Services.AddScoped<IValidator<UserViewModel>, UserValidator>();
builder.Services.AddScoped<StudyGo.Services.IAcademicService, StudyGo.Services.AcademicService>();
builder.Services.AddControllersWithViews();

// ============================================================================
// Módulo Comunicación (Jaison) — registro de servicios + tiempo real
// ============================================================================
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

// Cifrado de mensajes: PROVISIONAL (passthrough). TODO Infraestructura: real.
builder.Services.AddSingleton<IMessageCipher, NoOpMessageCipher>();

// Resolución de usuario actual (con fallback de desarrollo mientras no hay auth).
builder.Services.AddScoped<ICurrentUserResolver, CurrentUserResolver>();

// Caso de uso del chat.
builder.Services.AddScoped<IChatService, ChatService>();

// Caso de uso de notificaciones.
builder.Services.AddScoped<INotificationService, NotificationService>();

// Antiforgery por header (el respaldo HTTP de chat.js envía el token así).
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
// ============================================================================

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ⚠️ El orden de la seguridad es estricto en el pipeline de .NET:
// Primero se autentica (quién eres) y luego se autoriza (a qué tienes permiso)
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Hub de SignalR del chat (Jaison).
app.MapHub<ChatHub>("/hubs/chat");

// Hub de SignalR de notificaciones (Jaison).
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();