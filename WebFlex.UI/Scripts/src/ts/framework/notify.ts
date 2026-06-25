export type NotifyType = "success" | "info" | "warning" | "error";

export type NotifyOptions = {
    type?: NotifyType;
    duration?: number;
};

const defaultDuration = 2500;

function getHost(): HTMLElement {
    let host = document.getElementById("wfToastHost");

    if (host != null) {
        return host;
    }

    host = document.createElement("div");
    host.id = "wfToastHost";
    host.className = "wf-toast-host";
    host.setAttribute("aria-live", "polite");
    host.setAttribute("aria-atomic", "true");

    document.body.appendChild(host);

    return host;
}

function getIcon(type: NotifyType): string {
    if (type === "success") {
        return "✓";
    }

    if (type === "warning") {
        return "!";
    }

    if (type === "error") {
        return "×";
    }

    return "i";
}

function removeToast(toast: HTMLElement): void {
    toast.classList.add("is-hide");

    window.setTimeout(() => {
        toast.remove();
    }, 180);
}

function show(message: string, options: NotifyOptions = {}): void {
    const type = options.type ?? "info";
    const duration = options.duration ?? defaultDuration;
    const host = getHost();

    const toast = document.createElement("div");
    toast.className = `wf-toast ${type}`;

    toast.innerHTML = `
        <span class="wf-toast-icon">${getIcon(type)}</span>
        <span class="wf-toast-message"></span>
        <button class="wf-toast-close" type="button" aria-label="알림 닫기">×</button>
    `;

    const messageElement = toast.querySelector<HTMLElement>(".wf-toast-message");
    const closeButton = toast.querySelector<HTMLButtonElement>(".wf-toast-close");

    if (messageElement != null) {
        messageElement.textContent = message;
    }

    closeButton?.addEventListener("click", () => {
        removeToast(toast);
    });

    host.appendChild(toast);

    if (duration > 0) {
        window.setTimeout(() => {
            removeToast(toast);
        }, duration);
    }
}

export const notify = {
    success(message: string, duration?: number): void {
        show(message, {
            type: "success",
            duration
        });
    },

    info(message: string, duration?: number): void {
        show(message, {
            type: "info",
            duration
        });
    },

    warning(message: string, duration?: number): void {
        show(message, {
            type: "warning",
            duration
        });
    },

    error(message: string, duration?: number): void {
        show(message, {
            type: "error",
            duration
        });
    },

    show
};