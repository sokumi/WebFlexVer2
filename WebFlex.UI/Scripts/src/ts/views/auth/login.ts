export default class Page {
    public init(): void {
        this.focusUserId();
        this.bindSubmitGuard();
    }

    private focusUserId(): void {
        const userId = document.getElementById("txtUserId") as HTMLInputElement | null;

        if (userId == null) {
            return;
        }

        userId.focus();
    }

    private bindSubmitGuard(): void {
        const form = document.getElementById("frmLogin") as HTMLFormElement | null;
        const userId = document.getElementById("txtUserId") as HTMLInputElement | null;
        const password = document.getElementById("txtPassword") as HTMLInputElement | null;
        const submitButton = document.getElementById("btnLogin") as HTMLButtonElement | null;

        if (form == null || userId == null || password == null) {
            return;
        }

        form.addEventListener("submit", event => {
            const userIdValue = userId.value.trim();
            const passwordValue = password.value.trim();

            if (userIdValue.length === 0) {
                event.preventDefault();
                this.showError("아이디를 입력해 주세요.");
                userId.focus();
                return;
            }

            if (passwordValue.length === 0) {
                event.preventDefault();
                this.showError("비밀번호를 입력해 주세요.");
                password.focus();
                return;
            }

            if (submitButton != null) {
                submitButton.disabled = true;
                submitButton.innerText = "로그인 중...";
            }
        });
    }

    private showError(message: string): void {
        const errorBox = document.getElementById("loginErrorBox");

        if (errorBox == null) {
            alert(message);
            return;
        }

        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
    }
}