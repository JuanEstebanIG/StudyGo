// wwwroot/js/cursos.js - Interactividad para Cursos (Steven)

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initTabs();
        initSearch();
    });

    // Inicialización de la búsqueda en tiempo real
    function initSearch() {
        const searchInput = document.getElementById("course-search-input");
        if (!searchInput) return;

        searchInput.addEventListener("input", function () {
            const query = this.value.toLowerCase().trim();
            const cards = document.querySelectorAll(".course-card");
            let visibleCount = 0;

            cards.forEach(card => {
                const name = card.getAttribute("data-name") || "";
                const code = card.getAttribute("data-code") || "";
                if (name.includes(query) || code.includes(query)) {
                    card.style.display = "";
                    visibleCount++;
                } else {
                    card.style.display = "none";
                }
            });

            // Mostrar u ocultar mensaje de "no hay resultados"
            const noResults = document.getElementById("no-search-results");
            if (noResults) {
                if (visibleCount === 0 && query !== "") {
                    noResults.classList.remove("hidden");
                    noResults.classList.add("flex");
                } else {
                    noResults.classList.add("hidden");
                    noResults.classList.remove("flex");
                }
            }
        });
    }

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

    window.confirmUnenroll = function (courseId, courseName, actionUrl) {
        const nameEl = document.getElementById("unenroll-course-name");
        if (nameEl) nameEl.textContent = courseName;

        const form = document.getElementById("confirmUnenrollForm");
        if (form) form.action = actionUrl;

        if (typeof openModal === "function") {
            openModal("confirmUnenrollModal");
        } else {
            const m = document.getElementById("confirmUnenrollModal");
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

    window.confirmExpel = function (studentId, studentName) {
        const nameEl = document.getElementById("expel-student-name");
        if (nameEl) nameEl.textContent = studentName;

        const idEl = document.getElementById("expel-student-id");
        if (idEl) idEl.value = studentId;

        if (typeof openModal === "function") {
            openModal("confirmExpelModal");
        } else {
            const m = document.getElementById("confirmExpelModal");
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
