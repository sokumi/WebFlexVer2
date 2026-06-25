import $ from "jquery";

import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "@tabler/core/dist/css/tabler.min.css";
import "tabulator-tables/dist/css/tabulator_bootstrap5.min.css";

import { createIcons, icons } from "lucide";

import "../css/webflex-theme.css";
import "../css/webflex-layout.css";
import "../css/webflex-components.css";

import { notify } from "./framework/notify";

(window as any).$ = $;
(window as any).jQuery = $;
(window as any).notify = notify;

function initWebFlexIcons(): void {
    createIcons({
        icons
    });
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

function bindActiveMenu(): void {
    const currentPath = window.location.pathname.toLowerCase();

    document.querySelectorAll<HTMLElement>("[data-menu-path]").forEach(menu => {
        const menuPath = (menu.getAttribute("data-menu-path") ?? "").toLowerCase();

        if (menuPath === "/") {
            if (currentPath === "/" || currentPath === "/home" || currentPath === "/home/index") {
                menu.classList.add("active");
            }

            return;
        }

        if (currentPath.startsWith(menuPath)) {
            menu.classList.add("active");
        }
    });
}

function bindSidebarToggle(): void {
    const button = document.getElementById("btnSidebarToggle");
    const sidebar = document.querySelector<HTMLElement>(".wf-sidebar");

    if (button == null || sidebar == null) {
        return;
    }

    button.addEventListener("click", () => {
        if (window.innerWidth <= 991) {
            sidebar.classList.toggle("is-open");
            return;
        }

        sidebar.classList.toggle("is-collapsed");
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

document.addEventListener("DOMContentLoaded", () => {
    initWebFlexTheme();
    bindActiveMenu();
    bindSidebarToggle();
    bindThemeToggle();
    bindSearchPanelToggle();
    initHeaderClock();
    initWebFlexIcons();
});

console.log("WebFlex app loaded.");