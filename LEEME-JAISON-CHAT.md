# StudyGo · Módulo Comunicación (Jaison) — Entrega 1: **Chat**

Esta entrega trae **la feature Chat completa** (frontend + backend funcional + SignalR + DI) y una **fundación compartida provisional** para que tu parte compile y se vea mientras Micky sube la suya.

---

## ✅ Qué incluye

**Tu módulo (Jaison · Comunicación):**
- `Controllers/ChatController.cs` — `Index`, `Messages` (JSON), `Send` (respaldo HTTP).
- `Hubs/ChatHub.cs` — tiempo real (un grupo por chat): `JoinChat`, `SendMessage`, `Typing`.
- `Services/IChatService.cs` + `ChatService.cs` — caso de uso sobre EF Core (con privacidad §8).
- `Services/CurrentUserResolver.cs` — resuelve el usuario actual (con *fallback* de desarrollo).
- `Services/IMessageCipher.cs` — cifrado de mensajes (impl. provisional **NoOp**).
- `ViewModels/Chat/ChatPageViewModels.cs` — ViewModels de presentación.
- `Views/Chat/Index.cshtml` — chat de dos paneles (lista + hilo).
- `wwwroot/js/chat.js` — SignalR, envío (Enter/Shift+Enter), "escribiendo…", búsqueda y cambio de conversación sin recargar.

**Fundación PROVISIONAL (dueño real: Micky — reemplazar cuando llegue):**
- `wwwroot/css/tailwind.css` — tokens del sistema de diseño en **Tailwind v4** (`@theme`) + clases base.
- `Views/Shared/_Layout.cshtml` — app shell (sidebar + topbar con campana).
- `wwwroot/js/site.js` — `showToast`, `openModal`/`closeModal`, sidebar móvil.
- `Views/Shared/Components/_PageHeader|_Avatar|_Badge|_EmptyState.cshtml` + `ViewModels/Components/SharedComponentModels.cs`.

**Modificados:** `Program.cs` (SignalR + DI + hub + antiforgery), `Views/_ViewImports.cshtml` (usings).

---

## ▶️ Cómo correrlo

1) **Compilar el CSS (Tailwind v4 CLI).** Desde la raíz del proyecto:
```powershell
.\tailwindcss.exe -i ./wwwroot/css/tailwind.css -o ./wwwroot/css/site.css --watch
```
(Esto genera `wwwroot/css/site.css`, que es lo que carga el `_Layout`.)

2) **Levantar la app** (otra terminal):
```powershell
dotnet run
```

3) Abre **`/Chat`** (o haz clic en *Comunicación → Chat* en el sidebar).

---

## ℹ️ No necesitas migración

Las entidades del chat (`Chat`, `ChatParticipant`, `ChatMessage`) **ya existen** en `Models/` y en las migraciones del repo. **No toqué el dominio**, así que **no hay que crear migraciones** por esta entrega.

> Para *ver datos*, la BD necesita al menos **un usuario**, **un chat** y sus **participantes** (`Users`, `Chats`, `ChatParticipants`). Si están vacíos, verás el estado vacío "Aún no tienes conversaciones" (es lo esperado).

---

## ⚠️ Cosas provisionales / a saber

- **Autenticación:** todavía no existe (es de Micky). Mientras tanto, `CurrentUserResolver` usa un **fallback de desarrollo**: toma el *primer usuario de la BD* como "usuario actual". Al cablear Identity/OAuth, empezará a leer el claim automáticamente y el fallback deja de usarse.
- **Cifrado:** `NoOpMessageCipher` guarda el texto tal cual (passthrough). Sustituir por el cifrado real de Infraestructura.
- **"No leídos":** el badge sale en 0 porque el modelo `ChatParticipant` no guarda *última lectura*. Para soportarlo habría que añadir un campo `LastReadAt` → **cambio de dominio (migración)**, decisión del dueño del dominio.
- **Nombre de grupo:** `Chat` no tiene campo de nombre, así que los grupos se titulan con los nombres de los participantes.
- **Librerías por CDN (provisional):** Font Awesome, Toastify y SignalR se cargan por CDN en el `_Layout`. TODO de Micky: *vendorizarlas* en `wwwroot/lib`.

---

## 🔜 Siguientes features (cuando me des el OK)

Orden recomendado, reutilizan esta misma plomería:
1. **Notificaciones** — dropdown de la campana (ya hay hueco en el `_Layout`) + `NotificationHub` + push/toast en vivo.
2. **Calendario** — FullCalendar + endpoint JSON.
3. **Feed de actividad** — timeline de solo lectura.

> El SVG del icono es opcional: si me lo pasas, lo pongo en el logo del sidebar/topbar y como favicon; por ahora uso el cuadro degradado con la "S".
