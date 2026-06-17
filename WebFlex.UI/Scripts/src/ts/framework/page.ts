export type PageModel = {
    init: () => void;
};

type PageConstructor = new () => PageModel;

export function runPage(Page: PageConstructor): void {
    document.addEventListener("DOMContentLoaded", () => {
        const page = new Page();

        window.viewModel = page;

        page.init();
    });
}