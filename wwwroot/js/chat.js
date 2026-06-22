/* ============================================================================
   StudyGo · chat.js — tiempo real del chat (módulo de Jaison · §12.1)
   ----------------------------------------------------------------------------
   - Conexión SignalR a /hubs/chat (Conectado / Reconectando / Desconectado).
   - Enviar: Enter envía, Shift+Enter salto de línea. Respaldo HTTP si el hub cae.
   - "Está escribiendo…", autoscroll, búsqueda en la lista y cambio de
     conversación sin recargar (con history.pushState; el <a> es el fallback).
   Engancha SOLO por atributos data-*; no acopla comportamiento a Tailwind.
   ========================================================================== */
(function () {
  "use strict";

  const root = document.querySelector("[data-chat-root]");
  if (!root) return;

  const state = {
    currentUserId: root.dataset.currentUser || "",
    chatId: root.dataset.activeChat || "",
    isPrivate: root.dataset.activePrivate === "true",
  };

  const $conversations = root.querySelector("[data-conversations]");
  const $search = root.querySelector("[data-search]");

  /* ----------------------------- utilidades ----------------------------- */
  function esc(s) {
    const d = document.createElement("div");
    d.textContent = s == null ? "" : String(s);
    return d.innerHTML;
  }
  function hhmm(iso) {
    const d = new Date(iso);
    return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  }
  function threadPane() { return root.querySelector("[data-thread-pane]"); }
  function messagesBox() { return root.querySelector("[data-messages]"); }
  function scrollToBottom() {
    const box = messagesBox();
    if (box) box.scrollTop = box.scrollHeight;
  }

  function setConn(stateName) {
    const $conn = root.querySelector("[data-conn]");
    if (!$conn) return;
    const $reconnecting = $conn.querySelector("[data-conn-reconnecting]");
    const $disconnected = $conn.querySelector("[data-conn-disconnected]");

    if (stateName === "connected") {
      // Ocultar todo — cuando está bien no molestamos al usuario
      $conn.classList.add("hidden");
      $conn.classList.remove("flex");
    } else if (stateName === "reconnecting") {
      $conn.classList.remove("hidden");
      $conn.classList.add("flex");
      if ($reconnecting) $reconnecting.classList.remove("hidden");
      if ($disconnected) $disconnected.classList.add("hidden");
    } else {
      // disconnected
      $conn.classList.remove("hidden");
      $conn.classList.add("flex");
      if ($reconnecting) $reconnecting.classList.add("hidden");
      if ($disconnected) $disconnected.classList.remove("hidden");
    }
  }

  /* ------------------------- render de un mensaje ------------------------ */
  function messageNode(msg) {
    const own = String(msg.senderId) === String(state.currentUserId);
    const wrap = document.createElement("div");
    wrap.className = "flex " + (own ? "justify-end" : "justify-start items-end gap-2");
    wrap.setAttribute("data-message", "");
    if (msg.id) wrap.setAttribute("data-message-id", msg.id);

    const initials = (msg.senderName || "?").trim().charAt(0).toUpperCase();
    const avatar = `<span class="h-7 w-7 shrink-0 inline-flex items-center justify-center rounded-full bg-brand-blue/20 text-brand-blue text-[10px] font-semibold">${esc(initials)}</span>`;
    const meAvatar = `<span class="h-7 w-7 shrink-0 inline-flex items-center justify-center rounded-full bg-brand-blue text-white text-[10px] font-semibold">Yo</span>`;

    const name = (!own && !state.isPrivate)
      ? `<p class="text-[11px] font-semibold text-brand-blue mb-1 ml-1">${esc(msg.senderName)}</p>` : "";

    const bubbleCls = own
      ? "bg-brand-blue rounded-2xl rounded-br-sm text-white"
      : "bg-dark-card border border-dark-border rounded-2xl rounded-bl-sm text-gray-100";

    const timeCls = own ? "text-white/60" : "text-dark-muted";

    wrap.innerHTML =
      (!own ? avatar : "") +
      `<div class="max-w-[65%]">
         ${name}
         <div class="${bubbleCls} px-4 py-2.5">
           <p class="text-sm whitespace-pre-wrap break-words leading-relaxed">${esc(msg.content)}</p>
           <p class="mt-1 text-right text-[10px] ${timeCls}">${hhmm(msg.sentAt)}</p>
         </div>
       </div>` +
      (own ? meAvatar : "");
    return wrap;
  }

  function appendMessage(msg) {
    const box = messagesBox();
    if (!box) return;
    if (msg.id && box.querySelector(`[data-message-id="${msg.id}"]`)) return; // dedup
    box.appendChild(messageNode(msg));
    scrollToBottom();
  }

  /* ----------------------------- SignalR -------------------------------- */
  let connection = null;
  const hasSignalR = typeof signalR !== "undefined";

  async function joinCurrent() {
    if (connection && state.chatId) {
      try { await connection.invoke("JoinChat", state.chatId); } catch (e) { console.warn(e); }
    }
  }

  if (hasSignalR) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat")
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveMessage", function (msg) {
      if (String(msg.chatId) === String(state.chatId)) appendMessage(msg);
      // (Sin soporte de no-leídos: requiere LastReadAt en el dominio.)
    });

    connection.on("UserTyping", function (name) {
      const t = root.querySelector("[data-typing]");
      if (!t) return;
      t.textContent = `${name} está escribiendo…`;
      t.classList.remove("hidden");
      clearTimeout(t._timer);
      t._timer = setTimeout(() => t.classList.add("hidden"), 2500);
    });

    connection.onreconnecting(() => setConn("reconnecting"));
    connection.onreconnected(async () => { setConn("connected"); await joinCurrent(); });
    connection.onclose(() => setConn("disconnected"));

    connection.start()
      .then(async () => { setConn("connected"); await joinCurrent(); scrollToBottom(); })
      .catch(() => setConn("disconnected"));
  } else {
    setConn("disconnected");
    console.warn("[StudyGo] SignalR no está cargado; el chat usará el respaldo HTTP.");
  }

  scrollToBottom();

  /* --------------------------- enviar mensaje --------------------------- */
  function antiForgery() {
    const el = root.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : "";
  }

  async function sendViaHttp(content) {
    try {
      const res = await fetch("/Chat/Send", {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": antiForgery() },
        body: JSON.stringify({ chatId: state.chatId, content }),
      });
      if (!res.ok) throw new Error("HTTP " + res.status);
      const item = await res.json();
      appendMessage({ ...item, chatId: state.chatId });
    } catch (e) {
      if (window.showToast) window.showToast("No se pudo enviar el mensaje. Reintenta.", "error");
      console.error(e);
    }
  }

  async function send(content) {
    content = (content || "").trim();
    if (!content || !state.chatId) return;

    if (connection && connection.state === "Connected") {
      try {
        await connection.invoke("SendMessage", state.chatId, content); // el eco lo pinta
        return;
      } catch (e) { console.warn("Hub send falló, uso HTTP:", e); }
    }
    await sendViaHttp(content); // respaldo
  }

  function wireSendForm() {
    const form = root.querySelector("[data-send-form]");
    if (!form) return;
    const input = form.querySelector("[data-message-input]");

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      const v = input.value;
      input.value = "";
      input.style.height = "auto";
      send(v);
    });

    input.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        form.requestSubmit();
      }
    });

    // Auto-alto del textarea + aviso de "escribiendo".
    let typingThrottle = 0;
    input.addEventListener("input", function () {
      input.style.height = "auto";
      input.style.height = Math.min(input.scrollHeight, 128) + "px";
      const now = Date.now();
      if (connection && connection.state === "Connected" && now - typingThrottle > 1500) {
        typingThrottle = now;
        connection.invoke("Typing", state.chatId).catch(() => {});
      }
    });
  }
  wireSendForm();

  /* toggle panel izquierdo */
  root.addEventListener("click", function (e) {
    const btn = e.target.closest("[data-toggle-sidebar]");
    if (!btn) return;
    const aside = root.querySelector("aside");
    const grid = root.querySelector(".grid");
    if (!aside || !grid) return;
    const collapsed = aside.classList.contains("hidden");
    if (collapsed) {
      aside.classList.remove("hidden");
      grid.style.gridTemplateColumns = "";
      btn.title = "Ocultar conversaciones";
    } else {
      aside.classList.add("hidden");
      grid.style.gridTemplateColumns = "1fr";
      btn.title = "Mostrar conversaciones";
    }
  });

  /* ---------------- cambiar de conversación sin recargar ---------------- */
  async function openConversation(chatId, anchorEl) {
    if (!chatId || chatId === state.chatId) return;
    try {
      const res = await fetch(`/Chat/Messages/${chatId}`, { headers: { "Accept": "application/json" } });
      if (!res.ok) throw new Error("HTTP " + res.status);
      const data = await res.json();

      if (connection && state.chatId) connection.invoke("LeaveChat", state.chatId).catch(() => {});

      state.chatId = String(data.chatId);
      state.isPrivate = !!data.isPrivate;
      state.currentUserId = String(data.currentUserId);
      root.dataset.activeChat = state.chatId;
      root.dataset.activePrivate = String(state.isPrivate);

      renderThread(data);
      history.pushState({ chatId: state.chatId }, "", `/Chat/Index/${state.chatId}`);

      // Estado activo en la lista.
      root.querySelectorAll("[data-conversation]").forEach((a) => a.classList.remove("bg-dark-elev"));
      if (anchorEl) anchorEl.classList.add("bg-dark-elev");

      await joinCurrent();
    } catch (e) {
      if (window.showToast) window.showToast("No se pudo abrir la conversación.", "error");
      console.error(e);
    }
  }

  function renderThread(data) {
    const pane = threadPane();
    if (!pane) return;
    const sub = data.isPrivate ? "En línea" : `${(data.participants || []).length} participantes`;
    const badge = data.isPrivate
      ? '<span class="badge badge-neutral"><i class="fa-solid fa-lock text-[10px]"></i> Privado</span>'
      : '<span class="badge badge-info"><i class="fa-solid fa-users text-[10px]"></i> Grupo</span>';
    const initials = (data.title || "?").trim().charAt(0).toUpperCase();

    pane.innerHTML =
      `<header class="h-16 px-5 flex items-center justify-between border-b border-dark-border shrink-0 bg-dark-card">
         <div class="flex items-center gap-3 min-w-0">
           <span class="h-9 w-9 shrink-0 inline-flex items-center justify-center rounded-full bg-brand-blue/20 text-brand-blue text-xs font-semibold ring-1 ring-brand-blue/20">${initials}</span>
           <div class="min-w-0">
             <p class="truncate text-sm font-semibold text-white" data-thread-title>${esc(data.title)}</p>
             <p class="truncate text-xs text-dark-muted" data-thread-sub>${esc(sub)}</p>
           </div>
         </div>
         ${badge}
       </header>
       <div class="flex-1 overflow-y-auto px-6 py-5 space-y-2 bg-dark-bg" data-messages></div>
       <div class="px-6 h-6 flex items-center text-xs text-dark-muted hidden" data-typing>
         <i class="fa-solid fa-ellipsis fa-beat text-brand-blue mr-2"></i><span></span>
       </div>
       <div class="border-t border-dark-border bg-dark-card shrink-0">
         <form class="flex items-end gap-3 px-4 py-3" data-send-form>
           <input type="hidden" name="__RequestVerificationToken" value="${antiForgery()}" />
           <textarea rows="1" class="flex-1 bg-dark-elev border border-dark-border rounded-2xl px-4 py-2.5 text-sm text-gray-100 placeholder:text-dark-muted/60 focus:outline-none focus:border-brand-blue/60 focus:ring-2 focus:ring-brand-blue/20 resize-none max-h-32 transition" placeholder="Escribe un mensaje…" data-message-input aria-label="Mensaje"></textarea>
           <button type="submit" class="flex h-10 w-10 items-center justify-center rounded-full bg-brand-blue text-white hover:bg-brand-blue/90 transition shrink-0" data-send aria-label="Enviar"><i class="fa-solid fa-paper-plane text-sm"></i></button>
         </form>
       </div>`;

    (data.messages || []).forEach(appendMessage);
    scrollToBottom();
    wireSendForm();
  }

  if ($conversations) {
    $conversations.addEventListener("click", function (e) {
      const a = e.target.closest("[data-conversation]");
      if (!a) return;
      e.preventDefault(); // usamos fetch; el href queda como fallback sin JS
      openConversation(a.getAttribute("data-chat-id"), a);
    });
  }

  /* ----------------------------- búsqueda ------------------------------- */
  if ($search) {
    $search.addEventListener("input", function () {
      const q = this.value.trim().toLowerCase();
      root.querySelectorAll("[data-conversation]").forEach((a) => {
        const hit = !q || (a.getAttribute("data-search-text") || "").includes(q);
        a.classList.toggle("hidden", !hit);
      });
    });
  }
})();
