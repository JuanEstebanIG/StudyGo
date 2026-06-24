document.addEventListener("DOMContentLoaded", () => {
    const chatContainer = document.querySelector('[data-chat-container]');
    if (!chatContainer) return;

    let activeChatId = chatContainer.getAttribute('data-active-chat-id');
    const currentUserId = chatContainer.getAttribute('data-current-user-id');

    // Nodos del DOM
    const messagesArea = document.querySelector('[data-chat-messages]');
    const chatForm = document.querySelector('[data-chat-form]');
    const chatInput = document.querySelector('[data-chat-input]');
    const typingIndicator = document.querySelector('[data-typing-indicator]');
    const statusBadgeContainer = document.querySelector('[data-connection-status]');
    const threadTitle = document.querySelector('[data-thread-title]');

    let isTyping = false;
    let typingTimeout;

    // --- 1. CONFIGURACIÓN SIGNALR ---
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    const updateStatus = (text, type) => {
        if (statusBadgeContainer) {
            statusBadgeContainer.innerHTML = `<span class="badge badge-${type}">${text}</span>`;
        }
    };

    connection.onreconnecting(() => updateStatus("Reconectando...", "warn"));
    connection.onreconnected(() => {
        updateStatus("Conectado", "success");
        if (activeChatId) connection.invoke("JoinChat", activeChatId);
    });
    connection.onclose(() => updateStatus("Desconectado", "danger"));

    // Recepción de mensaje en vivo
    connection.on("ReceiveMessage", (msg) => {
        if (msg.chatId !== activeChatId) return; // Ignorar si es de otro chat

        const isOwn = msg.senderId === currentUserId;
        const time = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        appendMessage(msg.id, isOwn ? "Tú" : msg.senderName, time, msg.content, isOwn);
        scrollToBottom();
    });

    // Indicador de escritura
    connection.on("UserTyping", (senderName) => {
        if (!typingIndicator) return;
        typingIndicator.textContent = `${senderName} está escribiendo...`;
        typingIndicator.classList.remove('opacity-0');

        clearTimeout(typingTimeout);
        typingTimeout = setTimeout(() => typingIndicator.classList.add('opacity-0'), 2500);
    });

    // Iniciar conexión y unirse al grupo inicial
    connection.start().then(() => {
        updateStatus("Conectado", "success");
        if (activeChatId && activeChatId !== "null" && activeChatId !== "") {
            connection.invoke("JoinChat", activeChatId).catch(console.error);
        }
    }).catch(err => console.error("Error SignalR: ", err));


    // --- 2. LÓGICA DE UI E INPUT ---
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

            // Envío vía SignalR (Primario)
            if (connection.state === signalR.HubConnectionState.Connected) {
                try {
                    await connection.invoke("SendMessage", activeChatId, content);
                } catch (err) {
                    showToast("No se pudo enviar el mensaje por SignalR", "error");
                    sendViaHttpFallback(content);
                }
            } else {
                // Respaldo HTTP si el socket cayó
                sendViaHttpFallback(content);
            }
        });
    }

    // --- 3. RESPALDO HTTP ---
    async function sendViaHttpFallback(content) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        try {
            const response = await fetch('/Chat/Send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ ChatId: activeChatId, Content: content, __RequestVerificationToken: token })
            });
            if (!response.ok) throw new Error("Error HTTP");
            const msg = await response.json();
            // El fallback HTTP inserta manual, SignalR lo haría por ReceiveMessage
            const time = new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            appendMessage(msg.id, "Tú", time, msg.content, true);
            scrollToBottom();
        } catch (e) {
            showToast("Error crítico al enviar", "error");
        }
    }

    // --- 4. NAVEGACIÓN SPA ENTRE CHATS ---
    const chatLinks = document.querySelectorAll('[data-chat-link]');
    chatLinks.forEach(link => {
        link.addEventListener('click', async (e) => {
            e.preventDefault();
            const newChatId = link.getAttribute('data-chat-link');
            if (newChatId === activeChatId) return;

            // Marcar activo en sidebar
            chatLinks.forEach(l => l.parentElement.querySelector('a').className = l.parentElement.querySelector('a').className.replace('bg-dark-elev border-brand-blue', 'border-transparent hover:bg-white/5'));
            link.className = link.className.replace('border-transparent hover:bg-white/5', 'bg-dark-elev border-l-2 border-brand-blue');

            try {
                const res = await fetch(`/Chat/Messages/${newChatId}`);
                if (!res.ok) throw new Error("No se pudo cargar el chat");
                const data = await res.json();

                // Cambiar grupos en SignalR
                if (activeChatId && connection.state === signalR.HubConnectionState.Connected) {
                    await connection.invoke("LeaveChat", activeChatId);
                }
                activeChatId = newChatId;
                chatContainer.setAttribute('data-active-chat-id', newChatId);
                history.pushState(null, '', `/Chat/Index/${newChatId}`); // Actualiza URL

                if (connection.state === signalR.HubConnectionState.Connected) {
                    await connection.invoke("JoinChat", activeChatId);
                }

                // Renderizar vista
                document.querySelector('[data-empty-thread]')?.classList.add('hidden');
                document.querySelector('[data-active-thread]')?.classList.remove('hidden');
                document.querySelector('[data-active-thread]').classList.add('flex');

                threadTitle.textContent = data.title;
                messagesArea.innerHTML = '';

                data.messages.forEach(m => {
                    const time = new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
                    appendMessage(m.id, m.isOwn ? "Tú" : m.senderName, time, m.content, m.isOwn);
                });

                scrollToBottom();

            } catch (e) {
                showToast("Error al abrir la conversación", "error");
            }
        });
    });

    // Helper de renderizado
    function appendMessage(id, senderName, time, content, isOwn) {
        // Evitar duplicados si SignalR y HTTP chocan
        if (document.querySelector(`[data-msg-id="${id}"]`)) return;

        const align = isOwn ? "items-end" : "items-start";
        const bg = isOwn ? "bg-brand-blue/15 border-brand-blue/20 rounded-tr-sm" : "bg-dark-elev border-dark-border rounded-tl-sm";

        const html = `
            <div class="flex flex-col ${align} opacity-0 translate-y-2 transition-all duration-300" data-msg-id="${id}" data-new-msg>
                <div class="flex items-baseline gap-2 mb-1">
                    <span class="text-[10px] font-medium text-dark-muted">${escapeHtml(senderName)}</span>
                    <span class="font-mono text-[9px] text-dark-muted/60">${time}</span>
                </div>
                <div class="${bg} border text-gray-100 rounded-2xl px-4 py-2.5 max-w-[75%] text-sm shadow-sm">
                    ${escapeHtml(content)}
                </div>
            </div>`;

        messagesArea.insertAdjacentHTML('beforeend', html);
        requestAnimationFrame(() => {
            const newMsg = messagesArea.querySelector('[data-new-msg]:last-child');
            if (newMsg) newMsg.classList.remove('opacity-0', 'translate-y-2');
        });
    }

    function escapeHtml(unsafe) {
        return (unsafe || "").toString().replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    }
});