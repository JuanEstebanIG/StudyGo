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

    // =================================================================
    // NUEVO: Listener global para capturar la eliminación de chats
    // =================================================================
    const sidebarList = document.querySelector('[data-conversation-list]');
    if (sidebarList) {
        sidebarList.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-leave-chat-btn]');
            if (btn) {
                e.preventDefault();
                e.stopPropagation(); // Evita que se abra el chat al presionar borrar
                const chatId = btn.getAttribute('data-leave-chat-btn');
                const chatTitle = btn.getAttribute('data-chat-title');
                handleLeaveChat(chatId, chatTitle);
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

    const selectedUsersContainer = document.getElementById('selectedUsersContainer');
    const startChatActionBtn = document.getElementById('startChatActionBtn');

    // Estado local de usuarios seleccionados
    let selectedUsersForNewChat = [];

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
    // Dentro de connection.on("ReceiveMessage", (msg) => { ... })
    connection.on("ReceiveMessage", (msg) => {
        const finalContent = msg.content || msg.Content;
        const chatIdRaw = msg.chatId || msg.ChatId;

        if (msg.chatId !== activeChatId) {
            updateSidebarPreview(chatIdRaw, finalContent, msg.sentAt || msg.SentAt, true);
            return;
        }

        // NUEVO: Si es un mensaje del sistema de abandono y estamos en un chat privado (1:1), 
        // actualizamos el título del chat a "Conversación vacía" o recalculamos el header
        if (finalContent.includes("📢")) {
            // Si el chat abandonado está abierto, podemos actualizar el título superior
            if (threadTitle && !document.getElementById('groupNameInput')) {
                // Si no es un grupo explícito, cambiamos el header para reflejar que se marchó
                threadTitle.textContent = "Conversación (Usuario retirado)";
            }
        }

        const senderIdRaw = msg.senderId || msg.SenderId;
        const isOwn = String(senderIdRaw).toLowerCase().trim() === String(currentUserId).toLowerCase().trim();
        const rawDate = msg.sentAt || msg.SentAt;
        const time = new Date(rawDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const finalSenderName = msg.senderName || msg.SenderName;

        appendMessage(msg.id || msg.Id, isOwn ? "Tú" : finalSenderName, time, finalContent, isOwn);
        updateSidebarPreview(chatIdRaw, finalContent, rawDate, false);
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
            // CORRECCIÓN: Actualizar el texto del link en la barra lateral por si decía "Conversación"
            const sidebarChatLink = document.querySelector(`[data-chat-link="${id}"] .data-title`);
            if (sidebarChatLink) {
                sidebarChatLink.textContent = data.title;
            }

            // CORRECCIÓN: Sincronizar el atributo del botón de la papelera con el título real recuperado
            const leaveBtn = document.querySelector(`[data-leave-chat-btn="${id}"]`);
            if (leaveBtn) {
                leaveBtn.setAttribute('data-chat-title', data.title);
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
        // 1. Vaciamos el arreglo de seleccionados y actualizamos la vista para borrar los chips
        selectedUsersForNewChat = [];
        if (typeof renderSelectedUsers === 'function') {
            renderSelectedUsers();
        }

        // 2. Limpiamos el texto del input de búsqueda y ocultamos los resultados previos
        if (emailSearchInput) emailSearchInput.value = '';
        if (searchResultsContainer) {
            searchResultsContainer.classList.add('hidden');
            searchResultsContainer.innerHTML = '';
        }

        // 3. CORRECCIÓN: Limpiar el campo del nombre del grupo y OCULTAR su contenedor
        const groupNameInput = document.getElementById('groupNameInput');
        const groupNameContainer = document.getElementById('groupNameContainer');
        if (groupNameInput) groupNameInput.value = '';
        if (groupNameContainer) {
            groupNameContainer.classList.add('hidden');
            groupNameContainer.classList.remove('flex');
        }

        // 4. CORRECCIÓN: Asegurar que el botón de acción principal se oculte por defecto al reiniciar
        if (startChatActionBtn) {
            startChatActionBtn.classList.add('hidden');
            startChatActionBtn.disabled = false;
            startChatActionBtn.textContent = "Iniciar Chat";
        }

        // 5. Tu animación original de cierre intacta
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

                        row.addEventListener('click', () => handleSelectTargetUser(u.id, u.displayName || u.email));
                        searchResultsContainer.appendChild(row);
                    });

                } catch {
                    searchSpinner.classList.add('hidden');
                }
            }, 400);
        });
    }

    // 1. Agrega al usuario al estado y actualiza la UI
    const handleSelectTargetUser = (userId, userName) => {
        // Evitar duplicados
        if (!selectedUsersForNewChat.some(u => u.id === userId)) {
            selectedUsersForNewChat.push({ id: userId, name: userName });
            renderSelectedUsers();
        }

        // Limpiar el buscador
        emailSearchInput.value = '';
        searchResultsContainer.classList.add('hidden');
        searchResultsContainer.innerHTML = '';
        emailSearchInput.focus();
    };

    // 2. Dibuja los "Chips" en el DOM
    // Dibuja los "Chips" en el DOM mostrando a quién agregaste
    const renderSelectedUsers = () => {
        selectedUsersContainer.innerHTML = '';

        selectedUsersForNewChat.forEach(user => {
            const chip = document.createElement('div');
            // Agregamos padding horizontal y alineación para que el texto y la X coexistan bien
            chip.className = "flex items-center gap-1.5 bg-brand-blue/20 text-brand-blue border border-brand-blue/30 px-2.5 py-1 rounded-xl text-[11px] font-medium transition-all duration-150 animate-fade-in";
            chip.innerHTML = `
            <span class="truncate max-w-[150px]" title="${escapeHtml(user.name)}">${escapeHtml(user.name)}</span>
            <button type="button" class="text-brand-blue hover:text-rose-400 transition-colors ml-0.5 flex items-center justify-center w-3 h-3 text-[10px]" data-remove-user="${user.id}">
                <i class="fa-solid fa-xmark"></i>
            </button>
        `;
            selectedUsersContainer.appendChild(chip);

            // Lógica interactiva para el nombre del grupo
            const groupNameContainer = document.getElementById('groupNameContainer');
            const groupNameInput = document.getElementById('groupNameInput');

            if (groupNameContainer) {
                if (selectedUsersForNewChat.length > 1) {
                    // Si es un grupo, habilitamos el campo
                    groupNameContainer.classList.remove('hidden');
                    groupNameContainer.classList.add('flex');
                } else {
                    // Si es conversación individual, lo escondemos y reseteamos
                    groupNameContainer.classList.add('hidden');
                    groupNameContainer.classList.remove('flex');
                    if (groupNameInput) groupNameInput.value = '';
                }
            }
        });

        // Mostrar u ocultar el botón principal dependiendo si hay gente seleccionada
        if (selectedUsersForNewChat.length > 0) {
            startChatActionBtn.classList.remove('hidden');
            startChatActionBtn.textContent = selectedUsersForNewChat.length === 1 ? "Iniciar Chat Privado" : `Crear Grupo (${selectedUsersForNewChat.length})`;
        } else {
            startChatActionBtn.classList.add('hidden');
        }
    };

    // 3. Permite borrar a alguien de la selección
    selectedUsersContainer.addEventListener('click', (e) => {
        const removeBtn = e.target.closest('[data-remove-user]');
        if (removeBtn) {
            const idToRemove = removeBtn.getAttribute('data-remove-user');
            selectedUsersForNewChat = selectedUsersForNewChat.filter(u => u.id !== idToRemove);
            renderSelectedUsers();
        }
    });

    startChatActionBtn.addEventListener('click', async () => {
        if (selectedUsersForNewChat.length === 0) return;

        // Deshabilitar botón para evitar doble clic
        startChatActionBtn.disabled = true;
        startChatActionBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Creando...';

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const isGroup = selectedUsersForNewChat.length > 1;

        try {
            let response;

            if (!isGroup) {
                // Flujo Chat Privado (1 vs 1)
                response = await fetch('/Chat/StartPrivateChat', {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ targetUserId: selectedUsersForNewChat[0].id })
                });
            } else {
                // Flujo Chat Grupal
                const formData = new URLSearchParams();
                selectedUsersForNewChat.forEach(u => formData.append('targetUserIds', u.id));

                // Capturamos el nombre personalizado que ingresó el usuario
                const gName = document.getElementById('groupNameInput')?.value || '';
                formData.append('groupName', gName);

                response = await fetch('/Chat/CreateGroup', {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: formData
                });
            }

            if (!response.ok) throw new Error();
            const data = await response.json();

            // Guardamos el ID del chat creado/recuperado antes de limpiar variables
            const createdChatId = data.chatId;
            const chatTitleForSignalR = data.title;

            // Limpiar el modal y cerrarlo
            selectedUsersForNewChat = [];
            renderSelectedUsers();
            closeNewChatModal();

            // Inyectar en el sidebar si no existe
            if (!document.querySelector(`[data-sidebar-chat-id="${createdChatId}"]`)) {
                injectNewChatToSidebar(createdChatId, chatTitleForSignalR, "Conversación nueva (sin mensajes)");
            }

            switchActiveSidebarChat(createdChatId);

            // 1. Cargamos el hilo (esto limpia la pantalla y une al usuario al grupo de SignalR de forma segura)
            await loadChatThread(createdChatId);

            // 2. NUEVO: Si no es un grupo, disparamos la notificación de reingreso en vivo por SignalR.
            // Se ejecuta justo después de loadChatThread porque aquí ya estamos 100% conectados a la sala.
            if (!isGroup && connection.state === signalR.HubConnectionState.Connected) {
                // Buscamos el nombre del usuario actual del panel inferior izquierdo de StudyGo
                const currentUserName = document.querySelector('.data-title')?.textContent || "Tu compañero";
                const sysReentryMsg = `📢 ${currentUserName} se ha reincorporado a la conversación.`;

                // Invocamos el método para esparcir la alerta en tiempo real a la otra persona
                await connection.invoke("NotifyLeaveChat", String(createdChatId), sysReentryMsg, currentUserId, currentUserName);
            }

        } catch (e) {
            alert("Error al iniciar la conversación.");
        } finally {
            startChatActionBtn.disabled = false;
            startChatActionBtn.textContent = "Iniciar Chat";
        }
    });

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

        // Añadimos la estructura 'group relative' y el botón de la papelera dinámico
        const html = `
            <li data-sidebar-chat-id="${chatId}" class="group relative">
                <button type="button" 
                        data-leave-chat-btn="${chatId}" 
                        data-chat-title="${escapeHtml(title)}"
                        class="absolute right-4 top-1/2 -translate-y-1/2 z-20 hidden group-hover:flex items-center justify-center w-7 h-7 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 hover:bg-rose-500 hover:text-white transition-all duration-200 shadow-lg shadow-rose-500/10" 
                        title="Eliminar conversación">
                    <i class="fa-solid fa-trash text-xs"></i>
                </button>

                <a href="/Chat/Index/${chatId}" data-chat-link="${chatId}" class="flex items-center gap-3 p-4 border-b border-dark-border/50 border-transparent hover:bg-white/5 transition-colors">
                    <div class="w-7 h-7 rounded-full bg-brand-blue/15 text-brand-blue flex items-center justify-center font-bold text-[11px] border border-brand-blue/10">
                        ${title.substring(0, 2).toUpperCase()}
                    </div>
                    <div class="flex-1 min-w-0 pr-4">
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
        initChatLinks(); // Reengancha la navegación SPA
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
        const isConfirmed = await showDeleteConfirmation();
        if (!isConfirmed) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        try {
            const response = await fetch('/Chat/DeleteMessage', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ messageId: messageId })
            });

            if (!response.ok) throw new Error();

            if (connection.state === signalR.HubConnectionState.Connected && activeChatId) {
                await connection.invoke("DeleteMessage", activeChatId, messageId);
            }

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

            updateSidebarPreview(activeChatId, "🚫 Este mensaje fue eliminado...", null, false);

        } catch (e) {
            alert("Error al eliminar el mensaje. Verifica tu conexión.");
        }
    }

    // =================================================================
    // NUEVO: Funciones Desanidadas para Ocultar/Salir de Conversaciones
    // =================================================================
    const showChatLeaveConfirmation = (chatTitle) => {
        return new Promise((resolve) => {
            const modal = document.getElementById('chatLeaveConfirmModal');
            const btnConfirm = document.getElementById('confirmLeaveChatBtn');
            const btnCancel = document.getElementById('cancelLeaveChatBtn');
            const titleLabel = document.getElementById('chatLeaveModalTitle');

            if (titleLabel) titleLabel.textContent = `¿Eliminar "${chatTitle}"?`;

            modal.classList.remove('hidden');
            modal.classList.add('flex');

            const cleanup = (result) => {
                modal.classList.add('hidden');
                modal.classList.remove('flex');
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

    async function handleLeaveChat(chatId, chatTitle) {
        const isConfirmed = await showChatLeaveConfirmation(chatTitle);
        if (!isConfirmed) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        try {
            const response = await fetch('/Chat/RemoveChat', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ chatId: chatId })
            });

            if (!response.ok) throw new Error();
            const data = await response.json();

            if (data.success) {
                // NUEVO: Avisamos a SignalR para que pinte el mensaje del sistema a los demás en tiempo real
                if (connection.state === signalR.HubConnectionState.Connected) {
                    const systemMsg = `📢 ${data.leavingUserName} ha abandonado la conversación.`;
                    await connection.invoke("NotifyLeaveChat", String(chatId), systemMsg, data.userId, data.leavingUserName);
                }

                // 1. Remover visualmente el chat de la barra lateral
                const sidebarItem = document.querySelector(`[data-sidebar-chat-id="${chatId}"]`);
                if (sidebarItem) sidebarItem.remove();

                // 2. Si el chat eliminado era el activo, limpiamos la pantalla principal
                if (activeChatId === chatId) {
                    activeChatId = null;
                    messagesArea.innerHTML = '';
                    document.querySelector('[data-active-thread]')?.classList.add('hidden');
                    document.querySelector('[data-empty-thread]')?.classList.remove('hidden');
                    history.pushState(null, '', '/Chat');
                }
            }
        } catch (e) {
            alert("Error al intentar salir del chat. Verifica tu conexión.");
        }
    }
});