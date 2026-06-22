/* ============================================================================
   StudyGo · site.js — helpers globales
   STAND-IN PROVISIONAL (dueño: MICKY)
   ========================================================================== */
(function () {
  "use strict";

  /* --- Toasts ------------------------------------------------------------ */
  const TOAST_COLORS = { success: "#4ade80", error: "#f87171", warn: "#facc15", info: "#5e6ad2" };

  window.showToast = function (message, type = "info") {
    if (typeof Toastify === "undefined") return;
    Toastify({
      text: message, duration: 4000, gravity: "top", position: "right", close: true, stopOnFocus: true,
      style: {
        background: "#0f0f13", border: "1px solid rgba(255,255,255,0.08)", color: "#f3f4f6",
        borderLeft: `3px solid ${TOAST_COLORS[type] || TOAST_COLORS.info}`,
        borderRadius: "12px", boxShadow: "0 10px 40px -10px rgba(0,0,0,0.6)",
      },
    }).showToast();
  };

  /* --- Modales ------------------------------------------------------------ */
  window.openModal = function (id) {
    const m = document.getElementById(id);
    if (!m) return;
    m.classList.remove("hidden"); m.classList.add("flex");
    const content = m.querySelector("[data-modal-content]");
    requestAnimationFrame(() => { m.classList.remove("opacity-0"); if (content) content.classList.remove("scale-95"); });
    document.body.style.overflow = "hidden";
  };
  window.closeModal = function (id) {
    const m = document.getElementById(id);
    if (!m) return;
    const content = m.querySelector("[data-modal-content]");
    m.classList.add("opacity-0");
    if (content) content.classList.add("scale-95");
    setTimeout(() => { m.classList.add("hidden"); m.classList.remove("flex"); document.body.style.overflow = ""; }, 300);
  };
  document.addEventListener("click", function (e) {
    const opener = e.target.closest("[data-modal-open]");
    if (opener) { window.openModal(opener.getAttribute("data-modal-open")); return; }
    const closer = e.target.closest("[data-modal-close]");
    if (closer) { const wrap = closer.closest("[data-modal]"); if (wrap && wrap.id) window.closeModal(wrap.id); }
  });
  document.addEventListener("click", function (e) {
    const overlay = e.target.closest("[data-modal]");
    if (overlay && e.target === overlay && overlay.id) window.closeModal(overlay.id);
  });
  document.addEventListener("keydown", function (e) {
    if (e.key !== "Escape") return;
    document.querySelectorAll("[data-modal].flex").forEach((m) => { if (m.id) window.closeModal(m.id); });
  });

  /* --- Sidebar estilo YouTube -------------------------------------------- */
  const sidebar  = document.getElementById("app-sidebar");
  const main     = document.getElementById("app-main");
  const backdrop = document.getElementById("sidebar-backdrop");
  const toggle   = document.getElementById("sidebar-toggle");

  if (!sidebar || !toggle) return;

  let expanded = window.innerWidth >= 1024;

  function applyState() {
    const isDesktop = window.innerWidth >= 1024;
    if (isDesktop) {
      if (expanded) {
        sidebar.style.transform = "translateX(0)";
        main.style.marginLeft = "16rem"; // 256px = w-64
      } else {
        sidebar.style.transform = "translateX(-100%)";
        main.style.marginLeft = "0";
      }
      if (backdrop) backdrop.classList.add("hidden");
    } else {
      main.style.marginLeft = "0";
      if (expanded) {
        sidebar.style.transform = "translateX(0)";
        if (backdrop) backdrop.classList.remove("hidden");
      } else {
        sidebar.style.transform = "translateX(-100%)";
        if (backdrop) backdrop.classList.add("hidden");
      }
    }
  }

  toggle.addEventListener("click", function () {
    expanded = !expanded;
    applyState();
  });

  if (backdrop) {
    backdrop.addEventListener("click", function () {
      expanded = false;
      applyState();
    });
  }

  window.addEventListener("resize", function () {
    if (window.innerWidth >= 1024) expanded = true;
    applyState();
  });

  applyState();

})();
