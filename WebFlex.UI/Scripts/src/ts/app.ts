import $ from "jquery";

import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "@tabler/core/dist/css/tabler.min.css";
import "tabulator-tables/dist/css/tabulator_bootstrap5.min.css";

import { createIcons, icons } from "lucide";

type WebFlexLucideWindow = Window & {
    lucide?: {
        createIcons: () => void;
    };
};

import "../css/webflex-theme.css";
import "../css/webflex-layout.css";
import "../css/webflex-components.css";

import { notify } from "./framework/notify";

(window as any).$ = $;
(window as any).jQuery = $;
(window as any).notify = notify;

type ApiResponse<T> = {
    success?: boolean;
    message?: string;
    data?: T;
};

type LayoutMenuItem = {
    id: string;
    parentId?: string | null;
    menuCode: string;
    menuName: string;
    url: string;
    icon: string;
    sortOrder: number;
    children: LayoutMenuItem[];
};

function initWebFlexIcons(): void {
    createIcons({
        icons
    });

    removeLucideMarkerFromSvg();
}

function removeLucideMarkerFromSvg(): void {
    document.querySelectorAll<SVGElement>("svg[data-lucide]").forEach(svg => {
        svg.removeAttribute("data-lucide");
    });
}

function exposeWebFlexIcons(): void {
    const target = window as WebFlexLucideWindow;

    target.lucide = {
        createIcons: () => {
            initWebFlexIcons();
        }
    };
}

function initWebFlexTheme(): void {
    const savedTheme = localStorage.getItem("webflex-theme");
    const theme = savedTheme === "dark" || savedTheme === "light" ? savedTheme : "light";

    document.documentElement.setAttribute("data-bs-theme", theme);
    document.documentElement.setAttribute("data-webflex-theme", theme);
}

function bindThemeToggle(): void {
    document.addEventListener("click", event => {
        const target = event.target as HTMLElement | null;
        const button = target?.closest("[data-webflex-theme-toggle]");

        if (!button) {
            return;
        }

        const currentTheme = document.documentElement.getAttribute("data-webflex-theme") ?? "light";
        const nextTheme = currentTheme === "dark" ? "light" : "dark";

        document.documentElement.setAttribute("data-bs-theme", nextTheme);
        document.documentElement.setAttribute("data-webflex-theme", nextTheme);
        localStorage.setItem("webflex-theme", nextTheme);

        initWebFlexIcons();
    });
}

async function loadLayoutMenu(): Promise<void> {
    const host = document.getElementById("wfLayoutMenu");

    if (host == null) {
        return;
    }

    try {
        const response = await fetch("/layout/menu", {
            method: "GET",
            headers: {
                "Accept": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        const result = await response.json() as ApiResponse<LayoutMenuItem[]>;

        if (result.success === false) {
            throw new Error(result.message ?? "메뉴 조회에 실패했습니다.");
        }

        const menus = result.data ?? [];

        host.innerHTML = createLayoutMenuHtml(menus);

        bindActiveMenu();
        bindMobileMenuLinkClose();
        initWebFlexIcons();
    } catch (error) {
        console.error(error);

        host.innerHTML = `
            <div class="wf-nav-section">
                <div class="wf-nav-title">MENU</div>
                <div class="wf-nav-link">
                    <i data-lucide="circle-alert"></i>
                    <span>메뉴 조회 실패</span>
                </div>
            </div>
        `;

        initWebFlexIcons();
    }
}

function createLayoutMenuHtml(menus: LayoutMenuItem[]): string {
    if (menus.length === 0) {
        return `
            <div class="wf-nav-section">
                <div class="wf-nav-title">MENU</div>
                <div class="wf-nav-link">
                    <i data-lucide="circle-alert"></i>
                    <span>표시할 메뉴가 없습니다.</span>
                </div>
            </div>
        `;
    }

    return menus
        .map(parent => createLayoutMenuSectionHtml(parent))
        .join("");
}

function createLayoutMenuSectionHtml(parent: LayoutMenuItem): string {
    const children = parent.children ?? [];

    if (children.length === 0 && normalizeUrl(parent.url) === "#") {
        return "";
    }

    const hasActive = children.some(child => isActiveMenu(child.url)) || isActiveMenu(parent.url);
    const activeClass = hasActive ? "active" : "";

    const linkHtml = children.length > 0
        ? children.map(child => createLayoutMenuLinkHtml(child)).join("")
        : createLayoutMenuLinkHtml(parent);

    return `
        <div class="wf-nav-section ${activeClass}">
            <div class="wf-nav-title">${escapeHtml(parent.menuName)}</div>
            ${linkHtml}
        </div>
    `;
}

function createLayoutMenuLinkHtml(menu: LayoutMenuItem): string {
    const url = normalizeUrl(menu.url);
    const icon = menu.icon?.trim().length > 0 ? menu.icon.trim() : "circle";
    const activeClass = isActiveMenu(url) ? "active" : "";

    return `
        <a class="wf-nav-link ${activeClass}"
           href="${escapeAttribute(url)}"
           data-menu-path="${escapeAttribute(url)}">
            <i data-lucide="${escapeAttribute(icon)}"></i>
            <span>${escapeHtml(menu.menuName)}</span>
        </a>
    `;
}

function bindActiveMenu(): void {
    const currentPath = normalizeCurrentPath();

    document.querySelectorAll<HTMLElement>("[data-menu-path]").forEach(menu => {
        const menuPath = normalizeUrl(menu.getAttribute("data-menu-path") ?? "");

        menu.classList.remove("active");

        if (menuPath === "/") {
            if (currentPath === "/" || currentPath === "/home" || currentPath === "/home/index") {
                menu.classList.add("active");
            }

            return;
        }

        if (menuPath !== "#" && currentPath === menuPath.toLowerCase()) {
            menu.classList.add("active");
        }
    });

    document.querySelectorAll<HTMLElement>(".wf-nav-section").forEach(section => {
        const hasActiveLink = section.querySelector(".wf-nav-link.active") != null;
        section.classList.toggle("active", hasActiveLink);
    });
}

function bindSidebarToggle(): void {
    const collapseButton = document.getElementById("btnSidebarToggle");
    const mobileButton = document.getElementById("btnMobileMenuToggle");
    const backdrop = document.getElementById("wfSidebarBackdrop");
    const sidebar = document.querySelector<HTMLElement>(".wf-sidebar");

    if (sidebar == null) {
        return;
    }

    const isMobile = (): boolean => window.innerWidth <= 991;

    const setMobileSidebarOpen = (isOpen: boolean): void => {
        sidebar.classList.toggle("is-open", isOpen);
        document.body.classList.toggle("wf-sidebar-open", isOpen);
        mobileButton?.setAttribute("aria-expanded", String(isOpen));

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    };

    collapseButton?.addEventListener("click", () => {
        if (isMobile()) {
            setMobileSidebarOpen(!sidebar.classList.contains("is-open"));
            return;
        }

        sidebar.classList.toggle("is-collapsed");
        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    });

    mobileButton?.addEventListener("click", () => {
        if (isMobile()) {
            setMobileSidebarOpen(!sidebar.classList.contains("is-open"));
            return;
        }

        sidebar.classList.toggle("is-collapsed");

        const isCollapsed = sidebar.classList.contains("is-collapsed");
        mobileButton.setAttribute("aria-expanded", String(!isCollapsed));

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    });

    backdrop?.addEventListener("click", () => {
        setMobileSidebarOpen(false);
    });

    window.addEventListener("resize", () => {
        if (!isMobile()) {
            setMobileSidebarOpen(false);
        }
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && isMobile()) {
            setMobileSidebarOpen(false);
        }
    });

    bindMobileMenuLinkClose();
}

function bindMobileMenuLinkClose(): void {
    const sidebar = document.querySelector<HTMLElement>(".wf-sidebar");
    const mobileButton = document.getElementById("btnMobileMenuToggle");

    if (sidebar == null) {
        return;
    }

    const isMobile = (): boolean => window.innerWidth <= 991;

    document.querySelectorAll<HTMLElement>(".wf-nav-link").forEach(link => {
        if (link.dataset.mobileCloseBound === "true") {
            return;
        }

        link.dataset.mobileCloseBound = "true";

        link.addEventListener("click", () => {
            if (!isMobile()) {
                return;
            }

            sidebar.classList.remove("is-open");
            document.body.classList.remove("wf-sidebar-open");
            mobileButton?.setAttribute("aria-expanded", "false");

            window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
        });
    });
}

function initHeaderClock(): void {
    const clock = document.getElementById("lblHeaderClock");

    if (clock == null) {
        return;
    }

    const updateClock = (): void => {
        const now = new Date();
        const hh = String(now.getHours()).padStart(2, "0");
        const mm = String(now.getMinutes()).padStart(2, "0");
        const ss = String(now.getSeconds()).padStart(2, "0");

        clock.textContent = `${hh}:${mm}:${ss}`;
    };

    updateClock();
    window.setInterval(updateClock, 1000);
}

function bindSearchPanelToggle(): void {
    document.addEventListener("click", event => {
        const target = event.target as HTMLElement | null;
        const button = target?.closest<HTMLElement>("[data-wf-search-toggle]");

        if (button == null) {
            return;
        }

        const panel = button.closest<HTMLElement>(".wf-search-panel");

        if (panel == null) {
            return;
        }

        panel.classList.toggle("is-collapsed");

        const expanded = !panel.classList.contains("is-collapsed");
        button.setAttribute("aria-expanded", String(expanded));

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    });
}

function initGridSearchClearButtons(root: ParentNode = document): void {
    root.querySelectorAll<HTMLElement>(".wf-grid-search").forEach(search => {
        const input = search.querySelector<HTMLInputElement>("input.form-control, input");

        if (input == null) {
            return;
        }

        search.classList.add("has-clear");

        let button = search.querySelector<HTMLButtonElement>(".wf-grid-search-clear");

        if (button == null) {
            button = document.createElement("button");
            button.type = "button";
            button.className = "wf-grid-search-clear";
            button.setAttribute("aria-label", "검색어 지우기");
            button.textContent = "×";
            search.appendChild(button);
        }

        const sync = (): void => {
            search.classList.toggle("has-value", input.value.length > 0);
        };

        if (input.dataset.wfSearchClearBound !== "true") {
            input.dataset.wfSearchClearBound = "true";
            input.addEventListener("input", sync);
            input.addEventListener("change", sync);
        }

        if (button.dataset.wfSearchClearBound !== "true") {
            button.dataset.wfSearchClearBound = "true";
            button.addEventListener("click", () => {
                if (input.value.length === 0) {
                    input.focus();
                    return;
                }

                input.value = "";
                input.dispatchEvent(new Event("input", { bubbles: true }));
                input.dispatchEvent(new Event("change", { bubbles: true }));
                input.focus();
            });
        }

        sync();
    });
}

function observeGridSearchClearButtons(): void {
    const observer = new MutationObserver(mutations => {
        mutations.forEach(mutation => {
            mutation.addedNodes.forEach(node => {
                if (!(node instanceof HTMLElement)) {
                    return;
                }

                if (node.classList.contains("wf-grid-search")) {
                    initGridSearchClearButtons(node.parentElement ?? document);
                    return;
                }

                if (node.querySelector(".wf-grid-search") != null) {
                    initGridSearchClearButtons(node);
                }
            });
        });
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
}

function normalizeCurrentPath(): string {
    let path = window.location.pathname.toLowerCase();

    if (path.length === 0) {
        path = "/";
    }

    if (path !== "/" && path.endsWith("/")) {
        path = path.substring(0, path.length - 1);
    }

    return path;
}

function normalizeUrl(url: string | null | undefined): string {
    if (url == null || url.trim().length === 0) {
        return "#";
    }

    let value = url.trim();

    if (value !== "/" && value.endsWith("/")) {
        value = value.substring(0, value.length - 1);
    }

    return value.toLowerCase();
}

function isActiveMenu(url: string | null | undefined): boolean {
    const currentPath = normalizeCurrentPath();
    const menuUrl = normalizeUrl(url);

    if (menuUrl === "#") {
        return false;
    }

    if (menuUrl === "/") {
        return currentPath === "/" || currentPath === "/home" || currentPath === "/home/index";
    }

    return currentPath === menuUrl;
}

function escapeHtml(value: string | number | null | undefined): string {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/\"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function escapeAttribute(value: string | number | null | undefined): string {
    return escapeHtml(value);
}

document.addEventListener("DOMContentLoaded", () => {
    exposeWebFlexIcons();
    initWebFlexTheme();
    bindSidebarToggle();
    bindThemeToggle();
    bindSearchPanelToggle();
    initGridSearchClearButtons();
    observeGridSearchClearButtons();
    initHeaderClock();
    initWebFlexIcons();

    void loadLayoutMenu();
});

console.log("WebFlex app loaded.");
