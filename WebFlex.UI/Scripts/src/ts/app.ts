import $ from "jquery";

import "bootstrap/dist/js/bootstrap.bundle.min.js";
import "@tabler/core/dist/css/tabler.min.css";
import "tabulator-tables/dist/css/tabulator_bootstrap5.min.css";

import { createIcons, icons } from "lucide";

import "../css/webflex-theme.css";
import "../css/webflex-layout.css";
import "../css/webflex-components.css";

(window as any).$ = $;
(window as any).jQuery = $;

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

document.addEventListener("DOMContentLoaded", () => {
    initWebFlexTheme();
    initWebFlexIcons();
    bindThemeToggle();
});

console.log("WebFlex app loaded.");