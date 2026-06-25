import type { notify } from "../framework/notify";

declare global {
    interface Window {
        viewModel: unknown;
        notify: typeof notify;
    }
}

export { };