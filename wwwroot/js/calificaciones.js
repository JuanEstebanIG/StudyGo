// wwwroot/js/calificaciones.js - Dashboards de Calificaciones y Chart.js (Steven)

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initDistributionChart();
        initGradebookSearch();
    });

    // 1. Inicialización de Chart.js (Estadísticas Docente)
    function initDistributionChart() {
        const canvas = document.getElementById("distribution-chart");
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        const distributionData = window.StudyGo_ScoreDistribution || [0, 0, 0, 0, 0];

        // Definir gradiente para las barras
        const gradient = ctx.createLinearGradient(0, 0, 0, 200);
        gradient.addColorStop(0, "rgba(139, 92, 246, 0.7)"); // brand-purple
        gradient.addColorStop(1, "rgba(94, 106, 210, 0.2)");  // brand-blue

        const gradientHover = ctx.createLinearGradient(0, 0, 0, 200);
        gradientHover.addColorStop(0, "rgba(139, 92, 246, 0.95)");
        gradientHover.addColorStop(1, "rgba(94, 106, 210, 0.5)");

        new Chart(ctx, {
            type: "bar",
            data: {
                labels: ["0-59 (Reprobado)", "60-69 (C)", "70-79 (B)", "80-89 (A)", "90-100 (Sobresaliente)"],
                datasets: [{
                    label: "Estudiantes",
                    data: distributionData,
                    backgroundColor: gradient,
                    borderColor: "rgba(94, 106, 210, 0.5)",
                    borderWidth: 1.5,
                    borderRadius: 8,
                    hoverBackgroundColor: gradientHover,
                    hoverBorderColor: "#5e6ad2"
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: "rgba(15, 15, 19, 0.85)",
                        titleFont: {
                            family: "Inter",
                            size: 11,
                            weight: "bold"
                        },
                        bodyFont: {
                            family: "Inter",
                            size: 11
                        },
                        borderColor: "rgba(255, 255, 255, 0.08)",
                        borderWidth: 1,
                        padding: 10,
                        cornerRadius: 8,
                        displayColors: false,
                        callbacks: {
                            label: function (context) {
                                return `Estudiantes: ${context.parsed.y}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: "#8a8f98",
                            font: {
                                family: "Inter",
                                size: 10
                            }
                        }
                    },
                    y: {
                        grid: {
                            color: "rgba(255, 255, 255, 0.04)"
                        },
                        ticks: {
                            color: "#8a8f98",
                            font: {
                                family: "Inter",
                                size: 10
                            },
                            stepSize: 1,
                            precision: 0
                        }
                    }
                }
            }
        });
    }

    // 2. Búsqueda y Filtrado en Tiempo Real (Boletín de Notas)
    function initGradebookSearch() {
        const searchInput = document.getElementById("gradebook-search");
        const studentRows = document.querySelectorAll(".gradebook-student-row");

        if (!searchInput) return;

        searchInput.addEventListener("input", function () {
            const query = this.value.trim().toLowerCase();

            studentRows.forEach(row => {
                const name = row.getAttribute("data-student-name") || "";
                if (name.includes(query)) {
                    row.style.display = "";
                } else {
                    row.style.display = "none";
                }
            });
        });
    }

})();
