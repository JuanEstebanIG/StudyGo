/* ============================================================================
   StudyGo · notifications.js — campana de notificaciones (Jaison · §12.2)
   - Carga el dropdown al abrir la campana (fetch /Notification/Dropdown)
   - Conecta al NotificationHub y pinta pushes en tiempo real + toast
   - Marca como leído al hacer clic / "Marcar todo como leído"
   ========================================================================== */
(function () {
  "use strict";

  const root       = document.querySelector("[data-notif-root]");
  const toggleBtn  = document.querySelector("[data-notif-toggle]");
  const panel      = document.querySelector("[data-notif-panel]");
  const dot        = document.querySelector("[data-notif-dot]");

  if (!root || !toggleBtn || !panel) return;

  /* ----------------------------- helpers --------------------------------- */
  function antiForgery() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : "";
  }

  function setDot(count) {
    if (count > 0) dot?.classList.remove("hidden");
    else           dot?.classList.add("hidden");
  }

  function iconFor(type) {
    const map = {
      NuevoMensaje:      "fa-regular fa-comment-dots",
      TareaEntregada:    "fa-solid fa-code",
      NuevoQuiz:         "fa-solid fa-list-check",
      CalificacionLista: "fa-solid fa-star",
      NuevoCurso:        "fa-solid fa-layer-group",
      Recordatorio:      "fa-regular fa-clock",
    };
    return map[type] || "fa-regular fa-bell";
  }

  function colorFor(type) {
    const map = {
      NuevoMensaje:      "text-brand-blue",
      TareaEntregada:    "text-brand-mint",
      NuevoQuiz:         "text-cyan-400",
      CalificacionLista: "text-yellow-400",
      NuevoCurso:        "text-brand-purple",
      Recordatorio:      "text-orange-400",
    };
    return map[type] || "text-dark-muted";
  }

  function timeAgo(isoStr) {
    const diff = (Date.now() - new Date(isoStr).getTime()) / 1000;
    if (diff < 60)        return "ahora";
    if (diff < 3600)      return `hace ${Math.floor(diff / 60)} min`;
    if (diff < 86400)     return `hace ${Math.floor(diff / 3600)} h`;
    if (diff < 604800)    return `hace ${Math.floor(diff / 86400)} d`;
    return new Date(isoStr).toLocaleDateString("es", { day: "2-digit", month: "2-digit", year: "numeric" });
  }

  function esc(s) {
    const d = document.createElement("div");
    d.textContent = s ?? "";
    return d.innerHTML;
  }

  /* ------------------------- render del dropdown ------------------------- */
  function renderDropdown(data) {
    const { items, unreadCount, hasMore } = data;

    let html = `
      <div class="flex items-center justify-between px-4 py-3 border-b border-dark-border">
        <p class="text-sm font-semibold text-white">Notificaciones</p>
        ${unreadCount > 0
          ? `<button class="text-xs text-brand-blue hover:underline" data-mark-all>Marcar todo como leído</button>`
          : `<span class="text-xs text-dark-muted">Todo leído</span>`
        }
      </div>`;

    if (items.length === 0) {
      html += `<p class="px-4 py-8 text-center text-sm text-dark-muted">Sin notificaciones por ahora.</p>`;
    } else {
      html += `<div class="max-h-80 overflow-y-auto divide-y divide-dark-border/50">`;
      for (const item of items) {
        const unreadBar = !item.isRead ? "border-l-2 border-brand-blue" : "";
        const bg        = !item.isRead ? "bg-brand-blue/5" : "";
        html += `
          <div class="flex items-start gap-3 px-4 py-3 hover:bg-dark-elev transition cursor-pointer ${bg} ${unreadBar}"
               data-notif-item data-notif-id="${item.id}" data-notif-link="${esc(item.link || "")}">
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-dark-elev border border-dark-border mt-0.5">
              <i class="${iconFor(item.type)} ${colorFor(item.type)} text-sm"></i>
            </div>
            <div class="min-w-0 flex-1">
              <p class="text-sm text-gray-100 leading-snug">${esc(item.message)}</p>
              <p class="text-[11px] text-dark-muted mt-1">${timeAgo(item.createdAt)}</p>
            </div>
            ${!item.isRead ? `<span class="mt-2 h-2 w-2 rounded-full bg-brand-blue shrink-0"></span>` : ""}
          </div>`;
      }
      html += `</div>`;
    }

    if (hasMore) {
      html += `<div class="border-t border-dark-border px-4 py-3 text-center">
        <a href="/Notification" class="text-xs text-brand-blue hover:underline">Ver todas las notificaciones</a>
      </div>`;
    }

    panel.innerHTML = html;
    setDot(unreadCount);
  }

  /* ------------------------- cargar dropdown ----------------------------- */
  async function loadDropdown() {
    panel.innerHTML = `<p class="px-4 py-8 text-center text-sm text-dark-muted">
      <i class="fa-solid fa-circle-notch fa-spin mr-2"></i>Cargando…</p>`;
    try {
      const res  = await fetch("/Notification/Dropdown", { headers: { Accept: "application/json" } });
      const data = await res.json();
      renderDropdown(data);
    } catch {
      panel.innerHTML = `<p class="px-4 py-8 text-center text-sm text-red-400">Error al cargar notificaciones.</p>`;
    }
  }

  /* ------------------------- toggle del panel ---------------------------- */
  let open = false;

  toggleBtn.addEventListener("click", function (e) {
    e.stopPropagation();
    open = !open;
    if (open) {
      panel.classList.remove("hidden");
      loadDropdown();
    } else {
      panel.classList.add("hidden");
    }
  });

  document.addEventListener("click", function (e) {
    if (open && !root.contains(e.target)) {
      open = false;
      panel.classList.add("hidden");
    }
  });

  /* ------------------------- acciones del panel -------------------------- */
  panel.addEventListener("click", async function (e) {
    // Marcar todo como leído
    const markAll = e.target.closest("[data-mark-all]");
    if (markAll) {
      await fetch("/Notification/MarkAllRead", {
        method: "POST",
        headers: { "RequestVerificationToken": antiForgery() },
      });
      setDot(0);
      await loadDropdown();
      return;
    }

    // Clic en un item
    const item = e.target.closest("[data-notif-item]");
    if (!item) return;

    const id   = item.dataset.notifId;
    const link = item.dataset.notifLink;

    // Marcar como leído
    await fetch(`/Notification/MarkRead/${id}`, {
      method: "POST",
      headers: { "RequestVerificationToken": antiForgery() },
    });

    // Navegar si tiene link
    if (link) window.location.href = link;
    else await loadDropdown();
  });

  /* ------------------------- SignalR push -------------------------------- */
  if (typeof signalR !== "undefined") {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/notifications")
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveNotification", function (item) {
      // Punto rojo en la campana
      dot?.classList.remove("hidden");

      // Toast
      if (window.showToast) {
        window.showToast(item.message || "Nueva notificación", "info");
      }

      // Si el panel está abierto, recargar
      if (open) loadDropdown();
    });

    connection.start().catch(() => {});
  }

  /* ------------------------- carga inicial del dot ----------------------- */
  fetch("/Notification/UnreadCount", { headers: { Accept: "application/json" } })
    .then(r => r.json())
    .then(d => setDot(d.count))
    .catch(() => {});

})();
