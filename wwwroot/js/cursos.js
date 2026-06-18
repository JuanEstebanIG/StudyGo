// wwwroot/js/cursos.js - Interactividad para Cursos (Steven)

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initTabs();
    });

    // Inicialización del control de tabs
    function initTabs() {
        const tabBtns = document.querySelectorAll("[data-tab-btn]");
        const tabContents = document.querySelectorAll("[data-tab-content]");

        if (tabBtns.length === 0) return;

        tabBtns.forEach(btn => {
            btn.addEventListener("click", function () {
                const targetTab = this.getAttribute("data-tab-btn");

                // Actualizar botones de tab
                tabBtns.forEach(b => {
                    b.classList.remove("text-white", "border-brand-blue");
                    b.classList.add("text-dark-muted", "border-transparent");
                });
                this.classList.remove("text-dark-muted", "border-transparent");
                this.classList.add("text-white", "border-brand-blue");

                // Actualizar contenedores de contenido
                tabContents.forEach(content => {
                    if (content.getAttribute("data-tab-content") === targetTab) {
                        content.classList.remove("hidden");
                    } else {
                        content.classList.add("hidden");
                    }
                });

                // Opcional: Actualizar el query string sin refrescar la página
                const url = new URL(window.location);
                url.searchParams.set("tab", targetTab);
                window.history.replaceState({}, "", url);
            });
        });
    }

    // Helpers Globales expuestos
    window.openDriveSelector = function () {
        if (typeof openModal === "function") {
            openModal("driveSelectorModal");
        } else {
            // Fallback en caso de que site.js no esté cargado
            const m = document.getElementById("driveSelectorModal");
            if (m) {
                m.classList.remove("hidden");
                m.classList.add("flex");
                setTimeout(() => {
                    m.classList.remove("opacity-0");
                    m.querySelector("[data-modal-content]").classList.remove("scale-95");
                }, 50);
            }
        }
    };

    window.selectDriveFile = function (fileName, fileUrl) {
        document.getElementById("attachFileName").value = fileName;
        document.getElementById("attachFileUrl").value = fileUrl;
        
        // Cerrar modal
        if (typeof closeModal === "function") {
            closeModal("driveSelectorModal");
        } else {
            const m = document.getElementById("driveSelectorModal");
            if (m) m.classList.add("hidden");
        }

        if (typeof showToast === "function") {
            showToast("Adjuntando archivo '" + fileName + "'...", "info");
        }
        
        // Simular envío
        setTimeout(() => {
            document.getElementById("attachFileForm").submit();
        }, 800);
    };

    window.openEnrollmentModal = function () {
        if (typeof openModal === "function") {
            openModal("enrollmentModal");
        } else {
            const m = document.getElementById("enrollmentModal");
            if (m) {
                m.classList.remove("hidden");
                m.classList.add("flex");
                setTimeout(() => {
                    m.classList.remove("opacity-0");
                    m.querySelector("[data-modal-content]").classList.remove("scale-95");
                }, 50);
            }
        }
    };

})();
