(function () {
    const config = window.chatPageConfig;
    if (!config) {
        return;
    }

    const statusElement = document.getElementById("chatConnectionStatus");
    const generalMessages = document.getElementById("generalMessages");
    const privateMessages = document.getElementById("privateMessages");
    const currentUserId = config.currentUserId;
    const selectedUserId = config.selectedUserId;
    const forms = Array.from(document.querySelectorAll(".chat-send-form"));

    if (typeof signalR === "undefined") {
        setStatus("Не вдалося завантажити клієнт SignalR.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(config.chatHubUrl)
        .withAutomaticReconnect()
        .build();

    connection.on("ReceivePublicMessage", function (message) {
        appendMessage(generalMessages, message);
    });

    connection.on("ReceivePrivateMessage", function (message) {
        if (!privateMessages || !selectedUserId) {
            return;
        }

        const isCurrentConversation =
            (message.senderId === currentUserId && message.recipientId === selectedUserId) ||
            (message.senderId === selectedUserId && message.recipientId === currentUserId);

        if (isCurrentConversation) {
            appendMessage(privateMessages, message);
        }
    });

    connection.onreconnecting(function () {
        setStatus("Відновлення з'єднання…");
    });

    connection.onreconnected(function () {
        setStatus("З'єднання відновлено.");
    });

    connection.onclose(function () {
        setStatus("З'єднання втрачено. Повторна спроба…");
    });

    startConnection();
    forms.forEach(bindForm);

    async function startConnection() {
        try {
            setStatus("Підключення до чату…");
            await connection.start();
            setStatus("Чат підключено.");
        } catch (error) {
            console.error(error);
            setStatus("Не вдалося підключитися. Нова спроба за 5 секунд…");
            window.setTimeout(startConnection, 5000);
        }
    }

    function bindForm(form) {
        form.addEventListener("submit", async function (event) {
            event.preventDefault();

            const submitButton = form.querySelector("button[type='submit']");
            const status = form.querySelector("[data-form-status]");
            const formData = new FormData(form);

            if (submitButton) {
                submitButton.disabled = true;
            }

            setFormStatus(status, "");

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData,
                    credentials: "same-origin"
                });

                const result = await response.json().catch(function () { return {}; });

                if (!response.ok) {
                    throw new Error(result.error || "Не вдалося надіслати повідомлення.");
                }

                const message = result.message;
                if (message && connection.state !== signalR.HubConnectionState.Connected) {
                    routeMessage(message);
                }

                const textArea = form.querySelector("textarea");
                const fileInput = form.querySelector("input[type='file']");
                if (textArea) {
                    textArea.value = "";
                }
                if (fileInput) {
                    fileInput.value = "";
                }

                setFormStatus(status, "Повідомлення надіслано.", "success");
            } catch (error) {
                console.error(error);
                setFormStatus(status, error.message || "Сталася помилка.", "error");
            } finally {
                if (submitButton) {
                    submitButton.disabled = false;
                }
            }
        });
    }

    function routeMessage(message) {
        if (message.isPrivate) {
            if (!privateMessages || !selectedUserId) {
                return;
            }

            const isCurrentConversation =
                (message.senderId === currentUserId && message.recipientId === selectedUserId) ||
                (message.senderId === selectedUserId && message.recipientId === currentUserId);

            if (isCurrentConversation) {
                appendMessage(privateMessages, message);
            }

            return;
        }

        appendMessage(generalMessages, message);
    }

    function appendMessage(container, message) {
        if (!container || !message || !message.id) {
            return;
        }

        if (container.querySelector(`[data-message-id="${message.id}"]`)) {
            return;
        }

        removeEmptyState(container);
        container.appendChild(buildMessageElement(message));
        container.scrollTop = container.scrollHeight;
    }

    function removeEmptyState(container) {
        const placeholder = container.querySelector(".chat-empty-state");
        if (placeholder) {
            placeholder.remove();
        }
    }

    function buildMessageElement(message) {
        const article = document.createElement("article");
        article.className = "chat-message";
        article.dataset.messageId = message.id;
        article.dataset.senderId = message.senderId || "";
        article.dataset.recipientId = message.recipientId || "";

        if (message.senderId === currentUserId) {
            article.classList.add("chat-message--own");
        }

        const meta = document.createElement("div");
        meta.className = "chat-message__meta";

        const author = document.createElement("span");
        author.className = "chat-message__author";
        author.textContent = message.senderName || "User";
        meta.appendChild(author);

        const scope = document.createElement("span");
        scope.className = "chat-message__scope";
        scope.textContent = message.isPrivate
            ? `Приватно для ${message.recipientName || "користувача"}`
            : "Загальний чат";
        meta.appendChild(scope);

        const time = document.createElement("time");
        time.textContent = message.sentAtDisplay || "";
        if (message.sentAt) {
            time.dateTime = message.sentAt;
        }
        meta.appendChild(time);

        article.appendChild(meta);

        if (message.text) {
            const text = document.createElement("div");
            text.className = "chat-message__text";
            text.textContent = message.text;
            article.appendChild(text);
        }

        if (message.attachmentUrl && message.attachmentFileName) {
            const attachment = document.createElement("div");
            attachment.className = "chat-message__attachment";

            if ((message.attachmentContentType || "").startsWith("image/")) {
                const previewLink = document.createElement("a");
                previewLink.href = message.attachmentUrl;
                previewLink.target = "_blank";
                previewLink.rel = "noopener noreferrer";

                const image = document.createElement("img");
                image.className = "chat-message__image";
                image.src = message.attachmentUrl;
                image.alt = message.attachmentFileName;

                previewLink.appendChild(image);
                attachment.appendChild(previewLink);
            }

            const downloadLink = document.createElement("a");
            downloadLink.className = "chat-message__attachment-link";
            downloadLink.href = message.attachmentDownloadUrl || message.attachmentUrl;
            downloadLink.target = "_blank";
            downloadLink.rel = "noopener noreferrer";
            downloadLink.textContent = message.attachmentFileName;
            attachment.appendChild(downloadLink);

            if (message.attachmentSizeDisplay) {
                const size = document.createElement("span");
                size.className = "chat-message__attachment-size";
                size.textContent = message.attachmentSizeDisplay;
                attachment.appendChild(size);
            }

            article.appendChild(attachment);
        }

        return article;
    }

    function setStatus(text) {
        if (statusElement) {
            statusElement.textContent = text;
        }
    }

    function setFormStatus(element, text, kind) {
        if (!element) {
            return;
        }

        element.textContent = text;
        element.classList.remove("is-error", "is-success");

        if (kind === "error") {
            element.classList.add("is-error");
        }

        if (kind === "success") {
            element.classList.add("is-success");
        }
    }
})();
