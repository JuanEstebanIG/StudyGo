document.addEventListener("DOMContentLoaded", () => {
    const chatContainer = document.querySelector('[data-chat-container]');
    if (!chatContainer) return;

    let activeChatId = chatContainer.getAttribute('data-active-chat-id');
    const currentUserId = document.querySelector('[data-current-user-id]')?.getAttribute('data-current-user-id')?.toLowerCase().trim();

    // Nodos Principales del DOM
    const messagesArea = document.querySelector('[data-chat-messages]');

    if (messagesArea) {
        messagesArea.addEventListener('click', (e) => {
            // Busca si el clic provino de un botón de borrar o del ícono dentro de él
            const deleteBtn = e.target.closest('[data-delete-btn]');
            if (deleteBtn) {
                const id = deleteBtn.getAttribute('data-delete-btn');
                handleDeleteMessage(id);
            }
        });
    }

    const chatForm = document.querySelector('[data-chat-form]');
    const chatInput = document.querySelector('[data-chat-input]');
    const typingIndicator = document.querySelector('[data-typing-indicator]');
    const threadTitle = document.querySelector('[data-thread-title]');
    const connectionAlert = document.getElementById('connection-alert');
    const connectionAlertText = document.getElementById('connection-alert-text');
    const connectionAlertIcon = document.getElementById('connection-alert-icon');

    // Nodos del Modal de Búsqueda
    const newChatModal = document.getElementById('newChatModal');
    const openModalBtn = document.getElementById('openNewChatModalBtn');
    const closeModalBtn = document.getElementById('closeModalBtn');
    const cancelModalBtn = document.getElementById('cancelModalBtn');
    const emailSearchInput = document.getElementById('emailSearchInput');
    const searchResultsContainer = document.getElementById('searchResultsContainer');
    const searchErrorMsg = document.getElementById('searchErrorMsg');
    const searchSpinner = document.getElementById('searchSpinner');

    let isTyping = false;
    let typingTimeout;
    let searchTimeout;

    // --- 1. GESTIÓN DE ALERTAS DE CONEXIÓN (Estilo WhatsApp) ---
    const showConnectionAlert = (text, isError = false) => {
        if (!connectionAlert) return;
        connectionAlertText.textContent = text;
        connectionAlert.classList.remove('hidden');

        if (isError) {
            connectionAlertIcon.className = "fa-solid fa-circle-exclamation text-red-400";
        } else {
            connectionAlertIcon.className = "animate-spin h-3.5 w-3.5 border-2 border-brand-blue border-t-transparent rounded-full";
        }
    };

    const hideConnectionAlert = () => {
        if (connectionAlert) connectionAlert.classList.add('hidden');
    };

    // --- 2. CONFIGURACIÓN SIGNALR ---
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    connection.onreconnecting(() => {
        showConnectionAlert("Conectando al servidor... Pérdida de conexión transitoria.", false);
    });

    connection.onreconnected(() => {
        hideConnectionAlert();
        if (activeChatId) connection.invoke("JoinChat", activeChatId);
    });

    connection.onclose(() => {
        showConnectionAlert("Desconectado de la red. Revisa tu conexión a internet.", true);
    });

    //Eliminacion de un mensaje en vivo

    connection.on("MessageDeleted", (messageId) => {
        const msgNode = document.querySelector(`[data-msg-id="${messageId}"]`);
        if (msgNode) {
            const bubble = msgNode.querySelector('.rounded-2xl');
            if (bubble) {
                bubble.textContent = "🚫 Este mensaje fue eliminado...";
                bubble.classList.add('italic', 'opacity-80');
            }
            // Desaparecemos el botón de basura si existía
            const deleteBtn = msgNode.querySelector('[data-delete-btn]');
            if (deleteBtn) deleteBtn.remove();
        }
    });

    // Recepción de mensaje en vivo
    connection.on("ReceiveMessage", (msg) => {
        if (msg.chatId !== activeChatId) {
            // Actualizar contador en sidebar si llega en otro hilo
            updateSidebarPreview(msg.chatId, msg.content, msg.sentAt, true);
            return;
        }

        // CONTROL DE DAÑOS: Extrae el ID del emisor sin importar si viene en camelCase o PascalCase
        const senderIdRaw = msg.senderId || msg.SenderId;

        // Compara convirtiendo explícitamente a minúsculas para evitar desfases de GUIDs
        const isOwn = String(senderIdRaw).toLowerCase().trim() === String(currentUserId).toLowerCase().trim();

        // Extrae la fecha correctamente
        const rawDate = msg.sentAt || msg.SentAt;
        const time = new Date(rawDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        const finalContent = msg.content || msg.Content;
        const finalSenderName = msg.senderName || msg.SenderName;

        appendMessage(msg.id || msg.Id, isOwn ? "Tú" : finalSenderName, time, finalContent, isOwn);
        updateSidebarPreview(msg.chatId, finalContent, rawDate, false);
        scrollToBottom();
    });

    connection.on("UserTyping", (senderName) => {
        if (!typingIndicator) return;
        typingIndicator.textContent = `${senderName} está escribiendo...`;
        typingIndicator.classList.remove('opacity-0');

        clearTimeout(typingTimeout);
        typingTimeout = setTimeout(() => typingIndicator.classList.add('opacity-0'), 2500);
    });

    connection.start().then(() => {
        hideConnectionAlert(); // Oculto si la conexión inicial es exitosa
        if (activeChatId && activeChatId !== "null" && activeChatId !== "") {
            connection.invoke("JoinChat", activeChatId).catch(console.error);
        }
    }).catch(() => {
        showConnectionAlert("Fallo en la conexión inicial con el servidor.", true);
    });

    // --- 3. INPUT Y ENVÍO DE MENSAJES ---
    const scrollToBottom = () => {
        if (messagesArea) messagesArea.scrollTop = messagesArea.scrollHeight;
    };
    scrollToBottom();

    if (chatInput && chatForm) {
        chatInput.addEventListener('input', () => {
            chatInput.style.height = 'auto';
            chatInput.style.height = (chatInput.scrollHeight) + 'px';

            if (!isTyping && activeChatId) {
                isTyping = true;
                if (connection.state === signalR.HubConnectionState.Connected) {
                    connection.invoke("Typing", activeChatId).catch(() => { });
                }
            }
        });

        chatInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                chatForm.dispatchEvent(new Event('submit'));
            }
        });

        chatForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const content = chatInput.value.trim();
            if (!content || !activeChatId) return;

            chatInput.value = '';
            chatInput.style.height = 'auto';
            isTyping = false;

            if (connection.state === signalR.HubConnectionState.Connected) {
                try {
                    await connection.invoke("SendMessage", activeChatId, content);
                } catch (err) {
                    sendViaHttpFallback(content);
                }
            } else {
                sendViaHttpFallback(content);
            }
        });
    }

    async function sendViaHttpFallback(content) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        try {
            const response = await fetch('/Chat/Send', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ ChatId: activeChatId, Content: content })
            });
            if (!response.ok) throw new Error();
            const msg = await response.json();
            const time = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            appendMessage(msg.id, "Tú", time, msg.content, true);
            updateSidebarPreview(activeChatId, msg.content, msg.sentAt, false);
            scrollToBottom();
        } catch (e) {
            showToast("Error al enviar el mensaje", "error");
        }
    }

    // --- 4. NAVEGACIÓN ENTRE CHATS EXISTENTES ---
    const initChatLinks = () => {
        document.querySelectorAll('[data-chat-link]').forEach(link => {
            link.addEventListener('click', async (e) => {
                e.preventDefault();
                const newChatId = link.getAttribute('data-chat-link');
                if (newChatId === activeChatId) return;

                switchActiveSidebarChat(newChatId);
                await loadChatThread(newChatId);
            });
        });
    };
    initChatLinks();

    const switchActiveSidebarChat = (id) => {
        document.querySelectorAll('[data-chat-link]').forEach(l => {
            l.className = l.className.replace('bg-dark-elev border-brand-blue', 'border-transparent hover:bg-white/5');
        });
        const activeLink = document.querySelector(`[data-chat-link="${id}"]`);
        if (activeLink) {
            activeLink.className = activeLink.className.replace('border-transparent hover:bg-white/5', 'bg-dark-elev border-l-2 border-brand-blue');
            // Limpiar badge visual de no leídos
            const unreadBadge = activeLink.querySelector('.data-unread');
            if (unreadBadge) unreadBadge.remove();
        }
    };

    const loadChatThread = async (id) => {
        try {
            const res = await fetch(`/Chat/Messages/${id}`);
            if (!res.ok) throw new Error();
            const data = await res.json();

            // 1. Limpiamos el DOM por completo inmediatamente
            messagesArea.innerHTML = '';

            // 2. Si estábamos en otro chat, abandonamos ese canal de SignalR primero
            if (activeChatId && connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke("LeaveChat", activeChatId);
            }

            // 3. Cambiamos las referencias del estado local de la SPA
            activeChatId = id;
            chatContainer.setAttribute('data-active-chat-id', id);
            history.pushState(null, '', `/Chat/Index/${id}`);

            // 4. Mostramos los contenedores de la interfaz
            document.querySelector('[data-empty-thread]')?.classList.add('hidden');
            const activeThread = document.querySelector('[data-active-thread]');
            activeThread.classList.remove('hidden');
            activeThread.classList.add('flex');

            // 5. Actualizamos los textos de los encabezados y barras laterales
            threadTitle.textContent = data.title;
            const sidebarChatLink = document.querySelector(`[data-chat-link="${id}"] .data-title`);
            if (sidebarChatLink) {
                sidebarChatLink.textContent = data.title;
            }

            // =================================================================
            // CORRECCIÓN CRÍTICA: Mapeo tolerante a PascalCase y camelCase
            // =================================================================
            data.messages.forEach(m => {
                // Extraer el contenido
                const finalContent = m.content || m.Content;

                // Extraer el indicador de si es mensaje propio (Verifica ambas variantes)
                const isOwnMessage = m.isOwn !== undefined ? m.isOwn : m.IsOwn;

                // Extraer el nombre del remitente
                const finalSenderName = m.senderName || m.SenderName;

                // Extraer la fecha correctamente
                const rawDate = m.sentAt || m.SentAt;
                const time = new Date(rawDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

                // NUEVO: Extraer si el mensaje está borrado lógico
                const isDeleted = m.isDeleted !== undefined ? m.isDeleted : m.IsDeleted;

                // Inyectamos al DOM con las variables normalizadas
                appendMessage(
                    m.id || m.Id,
                    isOwnMessage ? "Tú" : finalSenderName,
                    time,
                    finalContent,
                    isOwnMessage,
                    isDeleted // <--- Pasamos el estado al renderizador
                );
            });
            // =================================================================

            // 6. Ahora que todo el historial ya está pintado, nos unimos al SignalR de forma segura
            if (connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke("JoinChat", activeChatId);
            }

            scrollToBottom();
        } catch (e) {
            showToast("Error al cargar la conversación", "error");
        }
    };

    // --- 5. MODAL DE BUSCADOR POR CORREO (Estilo WhatsApp) ---
    if (openModalBtn) {
        openModalBtn.addEventListener('click', () => {
            newChatModal.classList.remove('hidden');
            newChatModal.classList.add('flex');
            setTimeout(() => newChatModal.querySelector('[data-modal-content]').classList.remove('scale-95'), 50);
            emailSearchInput.value = '';
            emailSearchInput.focus();
            searchResultsContainer.classList.add('hidden');
            searchErrorMsg.classList.add('hidden');
        });
    }

    const closeNewChatModal = () => {
        newChatModal.querySelector('[data-modal-content]').classList.add('scale-95');
        setTimeout(() => {
            newChatModal.classList.remove('flex');
            newChatModal.classList.add('hidden');
        }, 200);
    };

    if (closeModalBtn) closeModalBtn.addEventListener('click', closeNewChatModal);
    if (cancelModalBtn) cancelModalBtn.addEventListener('click', closeNewChatModal);

    // Búsqueda dinámica con Autocompletado
    if (emailSearchInput) {
        emailSearchInput.addEventListener('input', () => {
            clearTimeout(searchTimeout);
            const query = emailSearchInput.value.trim();

            if (query.length < 3) {
                searchResultsContainer.classList.add('hidden');
                searchErrorMsg.classList.add('hidden');
                return;
            }

            searchSpinner.classList.remove('hidden');

            searchTimeout = setTimeout(async () => {
                try {
                    // Endpoint SPA creado en el controlador para resolver coincidencias
                    const res = await fetch(`/Chat/SearchUsers?emailQuery=${encodeURIComponent(query)}`);
                    const users = await res.json();

                    searchSpinner.classList.add('hidden');
                    searchResultsContainer.innerHTML = '';

                    if (users.length === 0) {
                        searchResultsContainer.classList.add('hidden');
                        // Si ingresó una estructura de correo completa y no hay resultados, activamos error
                        if (query.includes('@')) searchErrorMsg.classList.remove('hidden');
                        return;
                    }

                    searchErrorMsg.classList.add('hidden');
                    searchResultsContainer.classList.remove('hidden');

                    users.forEach(u => {
                        const row = document.createElement('button');
                        row.type = 'button';
                        row.className = "w-full text-left p-3 flex items-center gap-3 hover:bg-white/5 transition-colors text-xs text-gray-200 border-b border-dark-border/30 last:border-0";
                        row.innerHTML = `
                            <div class="w-6 h-6 rounded-full bg-brand-blue/20 text-brand-blue flex items-center justify-center font-bold text-[10px]">
                                ${u.displayName.substring(0, 2).toUpperCase()}
                            </div>
                            <div class="flex-1 min-w-0">
                                <p class="font-medium truncate">${u.displayName}</p>
                                <p class="text-[10px] text-dark-muted truncate">${u.email}</p>
                            </div>
                            <i class="fa-solid fa-chevron-right text-[10px] text-dark-muted px-1"></i>
                        `;

                        row.addEventListener('click', () => handleSelectTargetUser(u.id));
                        searchResultsContainer.appendChild(row);
                    });

                } catch {
                    searchSpinner.classList.add('hidden');
                }
            }, 400);
        });
    }

    // Al seleccionar una coincidencia en el buscador
    const handleSelectTargetUser = async (targetUserId) => {
        closeNewChatModal();
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch('/Chat/StartPrivateChat', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ targetUserId: targetUserId })
            });

            if (!response.ok) throw new Error();
            const data = await response.json();

            // Si es un chat totalmente nuevo, lo inyectamos de inmediato al sidebar
            if (!document.querySelector(`[data-sidebar-chat-id="${data.chatId}"]`)) {
                // Pasamos data.title (que tiene el nombre real del JSON) y el texto descriptivo en caliente
                injectNewChatToSidebar(data.chatId, data.title, "Conversación nueva (sin mensajes)");
            }

            switchActiveSidebarChat(data.chatId);
            await loadChatThread(data.chatId);

        } catch {
            showToast("No se pudo iniciar el chat con el usuario", "error");
        }
    };

    // --- 6. HELPERS DE RENDERIZADO ---
    function appendMessage(id, senderName, time, content, isOwn, isDeleted = false) {
        if (document.querySelector(`[data-msg-id="${id}"]`)) return;

        const justifyPosition = isOwn ? "justify-end" : "justify-start";
        const marginAlign = isOwn ? "ml-auto rounded-tr-sm" : "bg-dark-elev border border-dark-border mr-auto rounded-tl-sm";

        let displayContent = escapeHtml(content);
        let extraClasses = "";
        let customStyle = isOwn ? 'style="background-color: #e2e8f0 !important; color: #1e293b !important; border: 1px solid #121214 !important;"' : '';

        // Condición si el mensaje viene borrado desde la carga inicial
        if (isDeleted || displayContent.includes("🚫 Este mensaje fue eliminado...")) {
            displayContent = "🚫 Este mensaje fue eliminado...";
            extraClasses = "italic opacity-80";
            // Tono más apagado si es tu propio mensaje borrado
            customStyle = isOwn ? 'style="background-color: #cbd5e1 !important; color: #475569 !important; border: 1px solid #94a3b8 !important;"' : '';
        }

        // Dibujamos el botón de basura solo si es propio y no está borrado
        // Usamos 'group' en el contenedor principal para que el botón aparezca al hacer hover
        const deleteBtnHtml = (isOwn && !isDeleted && !displayContent.includes("🚫"))
            ? `<button data-delete-btn="${id}" class="text-xs text-rose-500 opacity-0 group-hover:opacity-100 transition-opacity mr-2 hover:text-rose-400" title="Eliminar para todos"><i class="fa-solid fa-trash"></i></button>`
            : '';

        const html = `
    <div class="flex w-full opacity-0 translate-y-2 transition-all duration-300 ${justifyPosition} group" data-msg-id="${id}" data-new-msg>
        <div class="flex flex-col max-w-[75%]">
            <div class="flex ${justifyPosition} items-baseline gap-2 mb-1">
                ${deleteBtnHtml}
                <span class="text-[10px] font-medium text-dark-muted">${escapeHtml(senderName)}</span>
                <span class="font-mono text-[9px] text-dark-muted/60">${time}</span>
            </div>
            <div class="${marginAlign} rounded-2xl px-4 py-2.5 text-sm shadow-sm whitespace-pre-wrap text-left ${extraClasses}" ${customStyle}>${displayContent}</div>
        </div>
    </div>`;

        messagesArea.insertAdjacentHTML('beforeend', html);

        requestAnimationFrame(() => {
            const newMsg = messagesArea.querySelector(`[data-msg-id="${id}"]`);
            if (newMsg) {
                newMsg.classList.remove('opacity-0', 'translate-y-2');

            }
        });
    }

    function updateSidebarPreview(chatId, content, sentAt, incrementUnread) {
        const item = document.querySelector(`[data-sidebar-chat-id="${chatId}"]`);
        if (!item) return;

        const preview = item.querySelector('.data-preview');
        const time = item.querySelector('.data-time');

        if (preview) preview.textContent = content;
        if (time && sentAt) {
            time.textContent = new Date(sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }

        if (incrementUnread) {
            let badge = item.querySelector('.data-unread');
            if (badge) {
                badge.textContent = parseInt(badge.textContent) + 1;
            } else {
                const container = item.querySelector('.data-preview').parentElement;
                container.insertAdjacentHTML('beforeend', `<span class="flex items-center justify-center w-4 h-4 rounded-full bg-brand-blue text-[9px] font-bold text-white data-unread">1</span>`);
            }
        }

        // Reordenar sidebar para poner el chat con actividad de primero
        const list = document.querySelector('[data-conversation-list]');
        if (list && item.parentElement === list) {
            list.insertBefore(item, list.firstChild);
        }
    }

    function injectNewChatToSidebar(chatId, title, previewText) {
        const emptyState = document.querySelector('[data-conversations-empty]');
        if (emptyState) emptyState.remove();

        const list = document.querySelector('[data-conversation-list]');
        list.classList.remove('hidden');

        const html = `
            <li data-sidebar-chat-id="${chatId}">
                <a href="/Chat/Index/${chatId}" data-chat-link="${chatId}" class="flex items-center gap-3 p-4 border-b border-dark-border/50 border-transparent hover:bg-white/5 transition-colors">
                    <div class="w-7 h-7 rounded-full bg-brand-blue/15 text-brand-blue flex items-center justify-center font-bold text-[11px] border border-brand-blue/10">
                        ${title.substring(0, 2).toUpperCase()}
                    </div>
                    <div class="flex-1 min-w-0">
                        <div class="flex justify-between items-center mb-1">
                            <h3 class="text-sm font-medium text-gray-100 truncate data-title">${escapeHtml(title)}</h3>
                            <span class="font-mono text-[10px] text-dark-muted data-time"></span>
                        </div>
                        <div class="flex justify-between items-center">
                            <p class="text-xs text-dark-muted truncate data-preview">${escapeHtml(previewText)}</p>
                        </div>
                    </div>
                </a>
            </li>`;

        list.insertAdjacentHTML('afterbegin', html);
        initChatLinks(); // Reengancha eventos click para la navegación SPA
    }

    function escapeHtml(unsafe) {
        return (unsafe || "").toString().replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    }

    const showDeleteConfirmation = () => {
        return new Promise((resolve) => {
            const modal = document.getElementById('deleteConfirmModal');
            const btnConfirm = document.getElementById('confirmDeleteBtn');
            const btnCancel = document.getElementById('cancelDeleteBtn');

            // Mostramos el modal cambiando las clases de Tailwind
            modal.classList.remove('hidden');
            modal.classList.add('flex');

            // Función interna para limpiar la interfaz y resolver la promesa
            const cleanup = (result) => {
                modal.classList.add('hidden');
                modal.classList.remove('flex');

                // Removemos los event listeners para evitar que se acumulen en futuros clics
                btnConfirm.removeEventListener('click', onConfirm);
                btnCancel.removeEventListener('click', onCancel);

                resolve(result);
            };

            const onConfirm = () => cleanup(true);
            const onCancel = () => cleanup(false);

            btnConfirm.addEventListener('click', onConfirm);
            btnCancel.addEventListener('click', onCancel);
        });
    };

    async function handleDeleteMessage(messageId) {
        // 1. Usamos nuestra nueva Promesa que espera a que el usuario interactúe con el modal
        const isConfirmed = await showDeleteConfirmation();

        // Si le dio a "Cancelar", detenemos la ejecución aquí
        if (!isConfirmed) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        try {
            // 2. Ejecutar borrado seguro en el Backend HTTP
            const response = await fetch('/Chat/DeleteMessage', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ messageId: messageId })
            });

            if (!response.ok) throw new Error();

            // 3. Si el borrado en BD fue exitoso, emitir por SignalR para actualizar la pantalla de los demás
            if (connection.state === signalR.HubConnectionState.Connected && activeChatId) {
                await connection.invoke("DeleteMessage", activeChatId, messageId);
            }

            // 4. Reflejar el cambio inmediatamente en tu propia pantalla
            const msgNode = document.querySelector(`[data-msg-id="${messageId}"]`);
            if (msgNode) {
                const bubble = msgNode.querySelector('.rounded-2xl');
                if (bubble) {
                    bubble.textContent = "🚫 Este mensaje fue eliminado...";
                    bubble.className = "ml-auto rounded-tr-sm rounded-2xl px-4 py-2.5 text-sm shadow-sm whitespace-pre-wrap text-left italic opacity-80";
                    bubble.style.backgroundColor = "#cbd5e1";
                    bubble.style.color = "#475569";
                    bubble.style.borderColor = "#94a3b8";
                }
                const deleteBtn = msgNode.querySelector('[data-delete-btn]');
                if (deleteBtn) deleteBtn.remove();
            }

            // 5. Opcional: Actualizar el preview en la barra lateral si era el último mensaje
            updateSidebarPreview(activeChatId, "🚫 Este mensaje fue eliminado...", null, false);

        } catch (e) {
            // Puedes cambiar esto por un showToast("error...") si tienes notificaciones UI
            alert("Error al eliminar el mensaje. Verifica tu conexión.");
        }
    }
});