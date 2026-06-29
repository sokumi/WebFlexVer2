import { dispatchLayoutChanged } from "../framework/common";

export type WebFlexPopupOptions = {
    selector: string | HTMLElement;
    openClass?: string;
    bodyOpenClass?: string;
    closeSelector?: string;
    widthPercent?: number;
    heightPercent?: number;
    closeOnEscape?: boolean;
    onOpen?: () => void;
    onClose?: () => void;
};

export class WebFlexPopup {
    readonly element: HTMLElement;
    readonly openClass: string;
    readonly bodyOpenClass: string;
    readonly closeSelector: string;
    readonly closeOnEscape: boolean;

    widthPercent?: number;
    heightPercent?: number;

    onOpen?: () => void;
    onClose?: () => void;

    constructor(options: WebFlexPopupOptions) {
        this.element = this.resolveElement(options.selector);
        this.openClass = options.openClass ?? "is-open";
        this.bodyOpenClass = options.bodyOpenClass ?? "wf-popup-open";
        this.closeSelector = options.closeSelector ?? "[data-popup-close]";
        this.closeOnEscape = options.closeOnEscape ?? true;
        this.widthPercent = options.widthPercent;
        this.heightPercent = options.heightPercent;
        this.onOpen = options.onOpen;
        this.onClose = options.onClose;

        this.bindEvents();
    }

    open(options: { widthPercent?: number; heightPercent?: number } = {}): void {
        this.applySize(
            options.widthPercent ?? this.widthPercent,
            options.heightPercent ?? this.heightPercent
        );

        this.element.classList.add(this.openClass);
        this.element.setAttribute("aria-hidden", "false");
        document.body.classList.add(this.bodyOpenClass);

        this.onOpen?.();
        dispatchLayoutChanged();
    }

    close(): void {
        this.element.classList.remove(this.openClass);
        this.element.setAttribute("aria-hidden", "true");
        document.body.classList.remove(this.bodyOpenClass);

        this.onClose?.();
        dispatchLayoutChanged();
    }

    toggle(): void {
        if (this.isOpen()) {
            this.close();
        } else {
            this.open();
        }
    }

    isOpen(): boolean {
        return this.element.classList.contains(this.openClass);
    }

    applySize(widthPercent?: number, heightPercent?: number): void {
        this.setPercentProperty("--wf-popup-width", "--wf-tag-popup-width", widthPercent, "vw");
        this.setPercentProperty("--wf-popup-height", "--wf-tag-popup-height", heightPercent, "vh");
    }

    private setPercentProperty(primary: string, legacy: string, value: number | undefined, unit: string): void {
        if (value == null || Number.isNaN(value)) {
            this.element.style.removeProperty(primary);
            this.element.style.removeProperty(legacy);
            return;
        }

        const normalized = Math.min(100, Math.max(10, value));
        this.element.style.setProperty(primary, `${normalized}${unit}`);
        this.element.style.setProperty(legacy, `${normalized}${unit}`);
    }

    private bindEvents(): void {
        this.element.addEventListener("click", event => {
            const target = event.target as HTMLElement | null;

            if (target?.closest(this.closeSelector) != null) {
                this.close();
            }
        });

        if (this.closeOnEscape) {
            document.addEventListener("keydown", event => {
                if (event.key === "Escape" && this.isOpen()) {
                    this.close();
                }
            });
        }
    }

    private resolveElement(selector: string | HTMLElement): HTMLElement {
        if (typeof selector !== "string") {
            return selector;
        }

        const element = document.querySelector<HTMLElement>(selector);

        if (element == null) {
            throw new Error(`WebFlexPopup element not found. selector=${selector}`);
        }

        return element;
    }
}