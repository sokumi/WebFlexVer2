import { common } from "./framework/common";
import type { PageModel } from "./framework/page";

declare global {
    interface Window {
        wf: typeof common;
        viewModel: PageModel | null;
    }

    const wf: typeof common;
    const viewModel: PageModel | null;
}

window.wf = common;
window.viewModel = null;