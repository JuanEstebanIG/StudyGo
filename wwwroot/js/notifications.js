/* ============================================================================
   StudyGo · notifications.js — campana de notificaciones (Jaison · §12.2)
   - Carga el dropdown al abrir la campana (fetch /Notification/Dropdown)
   - Conecta al NotificationHub y pinta pushes en tiempo real + toast
   - Marca como leído al hacer clic / "Marcar todo como leído"
   ========================================================================== */
// wwwroot/js/notifications.js
document.addEventListener('DOMContentLoaded', () => {
    const notifToggle = document.querySelector('[data-notif-toggle]');
    const notifPanel = document.querySelector('[data-notif-panel]');
    const notifDot = document.querySelector('[data-notif-dot]');

    // 1. Toggle del Dropdown de Campana
    if (notifToggle && notifPanel) {
        notifToggle.addEventListener('click', async (e) => {
            e.stopPropagation();
            const isHidden = notifPanel.classList.contains('hidden');

            if (isHidden) {
                notifPanel.classList.remove('hidden');
                notifPanel.innerHTML = '<div class="p-4 text-center text-xs text-dark-muted"><i class="fa-solid fa-circle-notch fa-spin"></i> Cargando...</div>';

                try {
                    const response = await fetch('/Notification/GetLatest');
                    const result = await response.json();
                    renderDropdown(result.data, notifPanel);
                    updateBadge(result.unreadCount);
                } catch (err) {
                    notifPanel.innerHTML = '<div class="p-4 text-xs text-red-400">Error al cargar notificaciones.</div>';
                }
            } else {
                notifPanel.classList.add('hidden');
            }
        });

        document.addEventListener('click', (e) => {
            if (!notifPanel.contains(e.target) && e.target !== notifToggle) {
                notifPanel.classList.add('hidden');
            }
        });
    }

    function renderDropdown(data, container) {
        if (!data || data.length === 0) {
            container.innerHTML = '<div class="p-6 text-center text-sm text-dark-muted">Estás al día</div>';
            return;
        }

        let html = '<div class="p-3 border-b border-dark-border eyebrow">Recientes</div><ul class="max-h-64 overflow-y-auto">';
        data.forEach(n => {
            html += `
                <li class="border-b border-dark-border/50 hover:bg-dark-elev transition">
                    <a href="${n.link}" class="block p-3">
                        <div class="text-sm text-gray-100">${n.message}</div>
                        <div class="text-xs text-dark-muted mt-1">${n.timeRel}</div>
                    </a>
                </li>`;
        });
        html += '</ul><a href="/Notification" class="block p-3 text-center text-xs text-brand-blue hover:bg-white/5 transition rounded-b-2xl">Ver todas</a>';
        container.innerHTML = html;
    }

    function updateBadge(count) {
        if (!notifDot) return;
        if (count > 0) {
            notifDot.classList.remove('hidden');
            notifDot.classList.add('flex');
            notifDot.textContent = count > 9 ? '+9' : count;
        } else {
            notifDot.classList.add('hidden');
            notifDot.classList.remove('flex');
        }
    }

    // 2. Conexión SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notifications")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveNotification", (notification) => {
        // notification debe tener: type, message, unreadCount
        if (typeof showToast === "function") {
            showToast(notification.message, notification.type);
        }
        updateBadge(notification.unreadCount);
    });

    connection.start().catch(err => console.error("Error conectando a SignalR: ", err.toString()));
});
