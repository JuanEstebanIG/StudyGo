using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Hubs;       // Jaison · Comunicación
using StudyGo.Services;   // Jaison · Comunicación

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)); // Cambia .UseSqlServer si usas otro motor (PostgreSQL, SQLite, etc.)

// Add services to the container.
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

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
