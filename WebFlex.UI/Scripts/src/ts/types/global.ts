import type { PageModel } from "../framework/page";

declare global {
    interface Window {
        viewModel: PageModel | null;
    }
}

export { };