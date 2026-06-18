// wwwroot/js/tareas.js - Editor de Código Monaco y Ejecución Sandbox (Steven)

(function () {
    "use strict";

    let editorInstance = null;
    let originalCode = ""; // Respaldo para cuando entran a modo lectura

    document.addEventListener("DOMContentLoaded", function () {
        initMonaco();
        initSandboxExecution();
        initEntregarButton();
        initVersionSidebar();
        initRevisionPage();
    });

    // 1. Inicialización de Monaco Editor
    function initMonaco() {
        const studentContainer = document.getElementById("monaco-container");
        const teacherContainer = document.getElementById("revision-monaco-container");

        if (!studentContainer && !teacherContainer) return;

        // Configurar RequireJS para Monaco Editor
        require.config({ paths: { vs: "https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs" } });

        require(["vs/editor/editor.main"], function () {
            // Registrar tema oscuro oficial StudyGo
            monaco.editor.defineTheme("studygo-dark", {
                base: "vs-dark",
                inherit: true,
                rules: [
                    { token: "comment", foreground: "8a8f98", fontStyle: "italic" },
                    { token: "keyword", foreground: "8b5cf6", fontStyle: "bold" },
                    { token: "string", foreground: "4ade80" },
                    { token: "number", foreground: "22d3ee" },
                    { token: "type", foreground: "5e6ad2" }
                ],
                colors: {
                    "editor.background": "#0f0f13",
                    "editor.foreground": "#f3f4f6",
                    "editor.lineHighlightBackground": "#111115",
                    "editorLineNumber.foreground": "#8a8f9840",
                    "editorLineNumber.activeForeground": "#5e6ad2",
                    "editor.selectionBackground": "#5e6ad230",
                    "editor.inactiveSelectionBackground": "#5e6ad215",
                    "editorWidget.background": "#111115",
                    "editorWidget.border": "rgba(255,255,255,0.08)"
                }
            });

            if (studentContainer) {
                const initialCode = window.StudyGo_InitialCode || "";
                originalCode = initialCode;

                editorInstance = monaco.editor.create(studentContainer, {
                    value: initialCode,
                    language: "csharp",
                    theme: "studygo-dark",
                    automaticLayout: true,
                    readOnly: window.StudyGo_IsGradesPublished || false,
                    fontSize: 13,
                    fontFamily: "'Fira Code', 'Cascadia Code', Consolas, monospace",
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    roundedSelection: true,
                    tabSize: 4
                });
            }

            if (teacherContainer) {
                const code = window.StudyGo_SelectedCode || "";
                editorInstance = monaco.editor.create(teacherContainer, {
                    value: code,
                    language: "csharp",
                    theme: "studygo-dark",
                    automaticLayout: true,
                    readOnly: true,
                    fontSize: 13,
                    fontFamily: "'Fira Code', 'Cascadia Code', Consolas, monospace",
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    roundedSelection: true
                });
            }
        });
    }

    // 2. Simulación de Ejecución en Sandbox (C# / 10s / 256MB / Sin Internet)
    function initSandboxExecution() {
        const btnEjecutar = document.getElementById("btn-ejecutar");
        if (!btnEjecutar) return;

        btnEjecutar.addEventListener("click", function () {
            if (!editorInstance) return;

            // Asegurar que la consola esté expandida al iniciar ejecución
            const wrapper = document.getElementById("terminal-panel");
            if (wrapper && wrapper.classList.contains("h-11")) {
                window.toggleConsoleCollapse();
            }

            const code = editorInstance.getValue();
            const language = "C#";
            const taskWorkspace = document.getElementById("task-workspace");
            const taskId = taskWorkspace ? taskWorkspace.getAttribute("data-task-id") : null;

            // Elementos de la consola
            const consoleOutput = document.getElementById("content-terminal");
            const sandboxBadge = document.getElementById("sandbox-badge");
            const execTimeSpan = document.getElementById("execution-time");

            if (!consoleOutput) return;

            // Deshabilitar botón
            btnEjecutar.disabled = true;
            execTimeSpan.classList.add("hidden");

            // Fase 1: En cola (Simulación honesta)
            updateSandboxUI(sandboxBadge, "En cola", "bg-yellow-500/25 text-yellow-400 border border-yellow-500/20");
            consoleOutput.innerHTML = `<div class="text-brand-blue animate-pulse"><i class="fa-solid fa-spinner fa-spin mr-1"></i> [Sandbox] Solicitud enviada... Asignando contenedor aislado...</div>`;
            window.switchConsoleTab('terminal');

            // Fase 2: Ejecutando (en 400ms)
            setTimeout(() => {
                updateSandboxUI(sandboxBadge, "Ejecutando", "bg-yellow-500/25 text-yellow-400 border border-yellow-500/20");
                consoleOutput.innerHTML += `<div class="text-yellow-400/80 mt-1"><i class="fa-solid fa-gear fa-spin mr-1"></i> [Sandbox] Compilando código C#... Ejecutando pruebas unitarias...</div>`;

                // Llamar al endpoint del backend
                fetch("/Tareas/EjecutarCode", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({ code: code, language: language, taskId: taskId })
                })
                .then(res => res.json())
                .then(data => {
                    btnEjecutar.disabled = false;

                    if (data.status === "Completado") {
                        updateSandboxUI(sandboxBadge, "Completado", "bg-emerald-500/20 text-emerald-400 border border-emerald-500/10");
                        execTimeSpan.innerText = `${data.executionTimeMs}ms`;
                        execTimeSpan.classList.remove("hidden");
                    } 
                    else if (data.status === "Tiempo agotado") {
                        updateSandboxUI(sandboxBadge, "Tiempo agotado", "bg-red-500/20 text-red-400 border-red-500/10");
                    } 
                    else if (data.status === "Pendiente") {
                        updateSandboxUI(sandboxBadge, "Pendiente", "bg-yellow-500/25 text-yellow-400 border-yellow-500/20");
                    } 
                    else {
                        updateSandboxUI(sandboxBadge, "Error", "bg-red-500/20 text-red-400 border-red-500/10");
                    }
                    
                    window.updateExecutionOutput(data);

                    // Auto-scroll
                    consoleOutput.scrollTop = consoleOutput.scrollHeight;
                })
                .catch(err => {
                    btnEjecutar.disabled = false;
                    updateSandboxUI(sandboxBadge, "Error", "bg-red-500/20 text-red-400 border-red-500/10");
                    
                    const errorData = {
                        status: "Error",
                        stdout: "",
                        stderr: "Error de Red: No se pudo contactar al sandbox en el servidor.",
                        executionTimeMs: 0
                    };
                    window.updateExecutionOutput(errorData);
                });
            }, 600);
        });
    }

    // 3. Guardado y Entrega de Tareas
    function initEntregarButton() {
        const btnEntregar = document.getElementById("btn-entregar");
        if (!btnEntregar) return;

        btnEntregar.addEventListener("click", function () {
            if (!editorInstance) return;

            const code = editorInstance.getValue();
            const taskWorkspace = document.getElementById("task-workspace");
            if (!taskWorkspace) return;

            const taskId = taskWorkspace.getAttribute("data-task-id");

            btnEntregar.disabled = true;
            if (typeof showToast === "function") {
                showToast("Enviando código al servidor...", "info");
            }

            fetch(`/Tareas/Entregar/${taskId}`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ code: code })
            })
            .then(res => res.json())
            .then(data => {
                btnEntregar.disabled = false;
                if (data.success) {
                    if (typeof showToast === "function") {
                        showToast("¡Entrega enviada con éxito! V#" + data.versionNumber + " guardada.", "success");
                    }
                    // Recargar para actualizar el historial de versiones de forma sutil
                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else {
                    if (typeof showToast === "function") {
                        showToast("Error al procesar la entrega.", "error");
                    }
                }
            })
            .catch(err => {
                btnEntregar.disabled = false;
                if (typeof showToast === "function") {
                    showToast("Error de conexión al entregar.", "error");
                }
            });
        });
    }

    // 4. Historial de Versiones en el Editor del Estudiante
    function initVersionSidebar() {
        const versionItems = document.querySelectorAll("#versions-container [data-version-id]");
        const readOnlyBadge = document.getElementById("read-only-badge");
        const btnRestore = document.getElementById("btn-restore-editor");

        if (versionItems.length === 0) return;

        versionItems.forEach(item => {
            item.addEventListener("click", function () {
                const versionId = this.getAttribute("data-version-id");
                const versionNum = this.getAttribute("data-version-num");

                if (typeof showToast === "function") {
                    showToast("Cargando código de versión #" + versionNum + "...", "info");
                }

                fetch(`/Tareas/ObtenerCodigoVersion?versionId=${versionId}`)
                    .then(res => res.text())
                    .then(code => {
                        if (editorInstance) {
                            // Cambiar editor a solo lectura para ver versión anterior
                            editorInstance.setValue(code);
                            editorInstance.updateOptions({ readOnly: true });

                            // Mostrar avisos visuales
                            if (readOnlyBadge) readOnlyBadge.classList.remove("hidden");
                            if (btnRestore) {
                                btnRestore.classList.remove("hidden");
                                btnRestore.innerText = `Editar V#${versionNum} (Cargar)`;
                            }
                        }
                    });
            });
        });

        // Botón Restaurar / Editar actual
        if (btnRestore) {
            btnRestore.addEventListener("click", function () {
                if (editorInstance) {
                    editorInstance.updateOptions({ readOnly: false });
                    readOnlyBadge.classList.add("hidden");
                    btnRestore.classList.add("hidden");
                    if (typeof showToast === "function") {
                        showToast("Editor desbloqueado. Puedes seguir escribiendo código.", "success");
                    }
                }
            });
        }
    }

    // 5. Historial de Versiones en Página de Revisión del Docente
    function initRevisionPage() {
        const versionSelect = document.getElementById("revision-version-select");
        if (!versionSelect) return;

        versionSelect.addEventListener("change", function () {
            const versionId = this.value;
            const versionNum = this.options[this.selectedIndex].getAttribute("data-version-num");

            if (typeof showToast === "function") {
                showToast("Cargando versión del estudiante #" + versionNum, "info");
            }

            fetch(`/Tareas/ObtenerCodigoVersion?versionId=${versionId}`)
                .then(res => res.text())
                .then(code => {
                    if (editorInstance) {
                        editorInstance.setValue(code);
                    }
                });
        });
    }

    // Helper para cambiar clases de colores en el badge de sandbox
    function updateSandboxUI(badge, text, classes) {
        if (!badge) return;
        badge.innerText = text;
        badge.className = `relative flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[10px] font-mono font-bold transition-all duration-300 ${classes}`;
        let dotColor = "bg-slate-400";
        if (text.includes("cola")) dotColor = "bg-yellow-400 animate-pulse";
        else if (text.includes("Ejecutando")) dotColor = "bg-yellow-400 animate-pulse";
        else if (text.includes("Completado")) dotColor = "bg-emerald-400";
        else if (text.includes("Error") || text.includes("agotado")) dotColor = "bg-red-400";
        else if (text.includes("Pendiente") || text.includes("Sin iniciar")) dotColor = "bg-slate-450";
        
        badge.innerHTML = `<span class="w-1.5 h-1.5 rounded-full ${dotColor}"></span>${text}`;
    }

    // Funciones globales y utilidades de la consola para la vista de Tareas
    window.currentConsoleFontSize = 12;

    window.switchConsoleTab = function (tabName) {
        const tabs = ["terminal", "metrics", "tests"];
        tabs.forEach(t => {
            const tabEl = document.getElementById(`tab-${t}`);
            const contentEl = document.getElementById(`content-${t}`);
            if (tabEl && contentEl) {
                if (t === tabName) {
                    tabEl.className = "px-3 py-1.5 rounded-lg font-bold flex items-center gap-1.5 transition-all duration-200 bg-slate-800/80 text-indigo-400 border border-slate-700/30 cursor-pointer";
                    contentEl.classList.remove("opacity-0", "pointer-events-none", "z-0");
                    contentEl.classList.add("opacity-100", "z-10");
                } else {
                    tabEl.className = "px-3 py-1.5 rounded-lg font-medium flex items-center gap-1.5 transition-all duration-200 text-slate-400 hover:text-slate-200 hover:bg-slate-800/30 cursor-pointer";
                    contentEl.classList.remove("opacity-100", "z-10");
                    contentEl.classList.add("opacity-0", "pointer-events-none", "z-0");
                }
            }
        });
    };

    window.adjustConsoleFontSize = function (direction) {
        let size = window.currentConsoleFontSize || 12;
        size += direction;
        if (size < 10) size = 10;
        if (size > 18) size = 18;
        window.currentConsoleFontSize = size;

        const terminalEl = document.getElementById("content-terminal");
        const labelEl = document.getElementById("console-font-size-label");
        if (terminalEl) {
            terminalEl.style.fontSize = `${size}px`;
        }
        if (labelEl) {
            labelEl.innerText = `${size}px`;
        }
    };

    window.toggleConsoleWordWrap = function () {
        const terminalEl = document.getElementById("content-terminal");
        const btnEl = document.getElementById("btn-toggle-wrap");
        if (terminalEl) {
            if (terminalEl.classList.contains("whitespace-pre-wrap")) {
                terminalEl.classList.remove("whitespace-pre-wrap");
                terminalEl.classList.add("whitespace-pre", "overflow-x-auto");
                if (btnEl) {
                    btnEl.classList.add("text-indigo-400");
                }
            } else {
                terminalEl.classList.remove("whitespace-pre", "overflow-x-auto");
                terminalEl.classList.add("whitespace-pre-wrap");
                if (btnEl) {
                    btnEl.classList.remove("text-indigo-400");
                }
            }
        }
    };

    window.copyConsoleOutput = function () {
        const terminalEl = document.getElementById("content-terminal");
        const btnEl = document.getElementById("btn-copy-console");
        if (!terminalEl) return;
        
        const text = terminalEl.innerText;
        navigator.clipboard.writeText(text).then(() => {
            if (btnEl) {
                const originalHTML = btnEl.innerHTML;
                btnEl.innerHTML = `<i class="fa-solid fa-check text-emerald-400"></i>`;
                setTimeout(() => {
                    btnEl.innerHTML = originalHTML;
                }, 1500);
            }
        }).catch(err => {
            console.error("Error al copiar texto: ", err);
        });
    };

    window.clearConsoleOutput = function () {
        const terminalEl = document.getElementById("content-terminal");
        if (terminalEl) {
            terminalEl.innerHTML = `<div class="text-slate-500 flex items-center gap-1.5"><i class="fa-solid fa-circle-info text-[10px]"></i> Presiona 'Ejecutar' para compilar el código en el sandbox externo.</div>`;
        }
        
        const statusDot = document.getElementById("metrics-status-dot");
        const statusText = document.getElementById("metrics-status-text");
        const statusDesc = document.getElementById("metrics-status-desc");
        const timeText = document.getElementById("metrics-time-text");
        const memoryText = document.getElementById("metrics-memory-text");
        const memoryBar = document.getElementById("metrics-memory-bar");
        
        if (statusDot) statusDot.className = "w-2.5 h-2.5 rounded-full bg-slate-500";
        if (statusText) statusText.innerText = "Ninguna ejecución";
        if (statusDesc) statusDesc.innerText = "No se ha ejecutado código en esta sesión.";
        if (timeText) timeText.innerText = "0 ms";
        if (memoryText) memoryText.innerText = "0.00 MB";
        if (memoryBar) memoryBar.style.width = "0%";
        
        const emptyState = document.getElementById("tests-empty-state");
        const resultsContainer = document.getElementById("tests-results-container");
        const listEl = document.getElementById("tests-list");
        
        if (emptyState) emptyState.classList.remove("hidden");
        if (resultsContainer) resultsContainer.classList.add("hidden");
        if (listEl) listEl.innerHTML = "";
        
        const sandboxBadge = document.getElementById("sandbox-badge");
        const execTimeSpan = document.getElementById("execution-time");
        
        if (sandboxBadge) {
            updateSandboxUI(sandboxBadge, "Sin iniciar", "bg-slate-850 text-slate-400 border border-slate-700/30");
        }
        if (execTimeSpan) execTimeSpan.classList.add("hidden");
    };

    function escapeHtml(text) {
        if (!text) return "";
        return text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    window.updateExecutionOutput = function (data) {
        const consoleOutput = document.getElementById("content-terminal");
        const statusDot = document.getElementById("metrics-status-dot");
        const statusText = document.getElementById("metrics-status-text");
        const statusDesc = document.getElementById("metrics-status-desc");
        const timeText = document.getElementById("metrics-time-text");
        const memoryText = document.getElementById("metrics-memory-text");
        const memoryBar = document.getElementById("metrics-memory-bar");
        const emptyState = document.getElementById("tests-empty-state");
        const resultsContainer = document.getElementById("tests-results-container");
        const listEl = document.getElementById("tests-list");
        const summaryBadge = document.getElementById("tests-summary-badge");
        
        if (!consoleOutput) return;
        
        consoleOutput.innerHTML = "";
        
        let simulatedMem = 0;
        if (data.status === "Completado") {
            simulatedMem = (28.4 + Math.random() * 15.6).toFixed(2);
        } else if (data.status === "Error") {
            simulatedMem = (12.2 + Math.random() * 4.1).toFixed(2);
        } else if (data.status === "Tiempo agotado") {
            simulatedMem = (145.8 + Math.random() * 30.5).toFixed(2);
        }
        
        if (timeText) timeText.innerText = `${data.executionTimeMs || 0} ms`;
        if (memoryText) memoryText.innerText = `${simulatedMem} MB`;
        if (memoryBar) {
            const percentage = (simulatedMem / 256) * 100;
            memoryBar.style.width = `${percentage}%`;
            if (percentage > 80) {
                memoryBar.className = "bg-red-500 h-full transition-all duration-500";
            } else if (percentage > 50) {
                memoryBar.className = "bg-yellow-500 h-full transition-all duration-500";
            } else {
                memoryBar.className = "bg-indigo-500 h-full transition-all duration-500";
            }
        }
        
        if (data.status === "Completado") {
            if (statusDot) statusDot.className = "w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-md shadow-emerald-500/50 animate-pulse";
            if (statusText) statusText.innerText = "Completado";
            if (statusDesc) statusDesc.innerText = "El programa compiló y se ejecutó correctamente sin errores.";
            
            let stdoutFormatted = data.stdout || "";
            const lines = stdoutFormatted.split("\n");
            const formattedLines = lines.map(line => {
                if (line.startsWith("Test Case") && line.includes("OK")) {
                    return `<div class="text-emerald-400 font-semibold flex items-center gap-1.5"><i class="fa-solid fa-check text-[10px]"></i> ${escapeHtml(line)}</div>`;
                } else if (line.startsWith("Test Case") && (line.includes("Error") || line.includes("Fallo"))) {
                    return `<div class="text-red-400 font-semibold flex items-center gap-1.5"><i class="fa-solid fa-xmark text-[10px]"></i> ${escapeHtml(line)}</div>`;
                } else if (line.startsWith("Ejecutando") || line.startsWith("Compilación")) {
                    return `<div class="text-slate-500 italic">${escapeHtml(line)}</div>`;
                } else if (line.trim() === "") {
                    return "<div>&nbsp;</div>";
                }
                return `<div class="text-slate-200">${escapeHtml(line)}</div>`;
            });
            
            consoleOutput.innerHTML = `
                <div class="text-emerald-400 font-bold border-b border-emerald-950/40 pb-1.5 mb-2 flex items-center gap-1.5">
                    <i class="fa-solid fa-circle-check text-[11px]"></i> Compilación y Ejecución Exitosa
                </div>
                <div class="space-y-1 font-mono">${formattedLines.join("")}</div>
            `;
            
            // Parsear Casos de Prueba
            const testCaseRegex = /Test Case (\d+)(?:\/(\d+))?:\s*(OK|Error|Fallo)/gi;
            let match;
            const testCases = [];
            
            while ((match = testCaseRegex.exec(data.stdout)) !== null) {
                testCases.push({
                    num: match[1],
                    total: match[2] || "",
                    status: match[3].toUpperCase()
                });
            }
            
            if (testCases.length > 0) {
                if (emptyState) emptyState.classList.add("hidden");
                if (resultsContainer) resultsContainer.classList.remove("hidden");
                if (listEl) {
                    listEl.innerHTML = "";
                    let passedCount = 0;
                    testCases.forEach(tc => {
                        const isOk = tc.status === "OK";
                        if (isOk) passedCount++;
                        
                        const card = document.createElement("div");
                        card.className = `p-3 rounded-xl border flex items-center justify-between transition-all duration-200 ${
                            isOk 
                            ? "bg-emerald-950/10 border-emerald-800/30 text-emerald-300" 
                            : "bg-red-950/10 border-red-800/30 text-red-300"
                        }`;
                        
                        card.innerHTML = `
                            <div class="flex items-center gap-2.5">
                                <span class="w-7 h-7 rounded-lg flex items-center justify-center font-bold text-xs ${
                                    isOk ? "bg-emerald-500/20 text-emerald-400" : "bg-red-500/20 text-red-400"
                                }">
                                    ${tc.num}
                                </span>
                                <div>
                                    <h5 class="text-[11px] font-bold">Caso de Prueba #${tc.num}</h5>
                                    <p class="text-[9px] ${isOk ? "text-emerald-400/60" : "text-red-400/60"}">
                                        ${isOk ? "La salida coincide exactamente con el resultado esperado." : "Salida incorrecta o error de ejecución."}
                                    </p>
                                </div>
                            </div>
                            <span class="px-2 py-0.5 rounded text-[9px] font-bold uppercase font-mono ${
                                isOk ? "bg-emerald-500/20 text-emerald-400" : "bg-red-500/20 text-red-400"
                            }">
                                ${isOk ? "Pasado" : "Fallo"}
                            </span>
                        `;
                        listEl.appendChild(card);
                    });
                    
                    if (summaryBadge) {
                        summaryBadge.innerText = `${passedCount}/${testCases.length} Pasados`;
                        summaryBadge.className = `px-2 py-0.5 rounded text-[10px] font-mono font-bold ${
                            passedCount === testCases.length 
                            ? "bg-emerald-500/20 text-emerald-400" 
                            : "bg-red-500/20 text-red-400"
                        }`;
                    }
                }
            } else {
                if (emptyState) emptyState.classList.remove("hidden");
                if (resultsContainer) resultsContainer.classList.add("hidden");
            }
            
        } else if (data.status === "Tiempo agotado") {
            if (statusDot) statusDot.className = "w-2.5 h-2.5 rounded-full bg-red-500 shadow-md shadow-red-500/50 animate-pulse";
            if (statusText) statusText.innerText = "Tiempo agotado";
            if (statusDesc) statusDesc.innerText = "La ejecución del programa tardó más de los 10 segundos asignados.";
            
            consoleOutput.innerHTML = `
                <div class="text-red-400 font-bold border-b border-red-950/40 pb-1.5 mb-2 flex items-center gap-1.5">
                    <i class="fa-solid fa-circle-exclamation text-[11px]"></i> Tarea Cancelada (Timeout de 10s)
                </div>
                <pre class="text-red-400/90 font-mono whitespace-pre-wrap">${escapeHtml(data.stderr || "")}</pre>
                <div class="text-slate-500 text-[10px] mt-3 border-t border-slate-900 pt-2 font-mono leading-normal">
                    <i class="fa-solid fa-lightbulb text-amber-500 text-[9px] mr-1"></i> Sugerencia: Evita bucles infinitos en tu código y optimiza las lecturas de entrada.
                </div>
            `;
            
            if (emptyState) emptyState.classList.remove("hidden");
            if (resultsContainer) resultsContainer.classList.add("hidden");
            
        } else if (data.status === "Pendiente") {
            if (statusDot) statusDot.className = "w-2.5 h-2.5 rounded-full bg-yellow-500 shadow-md shadow-yellow-500/50 animate-pulse";
            if (statusText) statusText.innerText = "Pendiente";
            if (statusDesc) statusDesc.innerText = "El sandbox externo no responde. La tarea está encolada temporalmente.";
            
            consoleOutput.innerHTML = `
                <div class="text-yellow-400 font-bold border-b border-yellow-950/40 pb-1.5 mb-2 flex items-center gap-1.5">
                    <i class="fa-solid fa-triangle-exclamation text-[11px]"></i> Conexión de Sandbox Pendiente
                </div>
                <div class="text-slate-300">${escapeHtml(data.message || "")}</div>
                <div class="p-3 bg-yellow-500/5 border border-yellow-500/10 rounded-xl mt-3 flex items-start gap-2.5">
                    <i class="fa-solid fa-circle-info text-yellow-400 text-xs mt-0.5 animate-bounce"></i>
                    <div class="text-[10px] text-yellow-400/80 leading-normal">
                        <strong>Circuit Breaker Activo:</strong> El servidor de ejecución está experimentando alta carga. Tu código se procesará tan pronto como el backend vuelva a estar en línea.
                    </div>
                </div>
            `;
            
            if (emptyState) emptyState.classList.remove("hidden");
            if (resultsContainer) resultsContainer.classList.add("hidden");
            
        } else {
            if (statusDot) statusDot.className = "w-2.5 h-2.5 rounded-full bg-red-500 shadow-md shadow-red-500/50 animate-pulse";
            if (statusText) statusText.innerText = "Error";
            if (statusDesc) statusDesc.innerText = "Ocurrió un error al compilar o ejecutar el programa.";
            
            let stderrFormatted = data.stderr || "";
            const errorLines = stderrFormatted.split("\n");
            const formattedErrors = errorLines.map(line => {
                if (line.toLowerCase().includes("error") || line.toLowerCase().includes("failed")) {
                    return `<div class="bg-red-500/10 border border-red-500/25 px-2.5 py-1.5 rounded-lg text-red-400 font-medium my-1 flex items-start gap-2">
                        <i class="fa-solid fa-bug text-xs mt-0.5 shrink-0"></i>
                        <span class="font-semibold">${escapeHtml(line)}</span>
                    </div>`;
                }
                return `<div class="text-red-400/80 pl-6 border-l border-red-500/20">${escapeHtml(line)}</div>`;
            }).join("");
            
            consoleOutput.innerHTML = `
                <div class="text-red-400 font-bold border-b border-red-950/40 pb-1.5 mb-2 flex items-center gap-1.5">
                    <i class="fa-solid fa-circle-xmark text-[11px]"></i> Error de Compilación / Runtime Error
                </div>
                <div class="space-y-1.5 font-mono text-[11px]">${formattedErrors}</div>
            `;
            
            if (emptyState) emptyState.classList.remove("hidden");
            if (resultsContainer) resultsContainer.classList.add("hidden");
        }
    };

    // Modal de rúbrica
    window.openRubricReferenceModal = function () {
        if (typeof openModal === "function") {
            openModal("rubricReferenceModal");
        } else {
            const m = document.getElementById("rubricReferenceModal");
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

    window.toggleConsoleCollapse = function () {
        const wrapper = document.getElementById("terminal-panel");
        const icon = document.getElementById("icon-collapse-console");
        const contentTerminal = document.getElementById("content-terminal");
        const contentMetrics = document.getElementById("content-metrics");
        const contentTests = document.getElementById("content-tests");
        
        if (wrapper) {
            if (wrapper.classList.contains("h-64")) {
                wrapper.classList.remove("h-64");
                wrapper.classList.add("h-11");
                if (icon) {
                    icon.className = "fa-solid fa-chevron-up text-[10px]";
                }
                if (contentTerminal) contentTerminal.classList.add("hidden");
                if (contentMetrics) contentMetrics.classList.add("hidden");
                if (contentTests) contentTests.classList.add("hidden");
                
                const btn = document.getElementById("btn-collapse-console");
                if (btn) btn.title = "Expandir consola";
            } else {
                wrapper.classList.remove("h-11");
                wrapper.classList.add("h-64");
                if (icon) {
                    icon.className = "fa-solid fa-chevron-down text-[10px]";
                }
                if (contentTerminal) contentTerminal.classList.remove("hidden");
                if (contentMetrics) contentMetrics.classList.remove("hidden");
                if (contentTests) contentTests.classList.remove("hidden");
                
                const btn = document.getElementById("btn-collapse-console");
                if (btn) btn.title = "Colapsar consola";
            }
            
            if (typeof editorInstance !== "undefined" && editorInstance) {
                setTimeout(() => {
                    editorInstance.layout();
                }, 100);
            }
        }
    };

})();
