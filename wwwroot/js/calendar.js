document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        themeSystem: 'standard',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        buttonText: {
            today: 'Hoy',
            month: 'Mes',
            week: 'Semana',
            day: 'Día'
        },
        locale: 'es',
        firstDay: 1,
        events: '/Calendar/GetEvents',
        eventClick: function (info) {
            // Reutiliza el sistema de modales existente del layout general (Micky)
            const eventData = info.event.extendedProps;
            const msg = `Detalles: ${info.event.title}\nTipo: ${eventData.type}\nCurso: ${eventData.course}`;

            // En un caso real abriríamos un modal usando openModal('eventoDetalle')
            // Por simplicidad, usamos Toastify temporalmente
            if (typeof showToast === "function") {
                showToast(msg, "info");
            }
        }
    });

    calendar.render();
});