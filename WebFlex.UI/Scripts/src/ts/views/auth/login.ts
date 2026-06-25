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

        if (form == null || userId == null || password == null) {
            return;
        }

        form.addEventListener("submit", event => {
            const userIdValue = userId.value.trim();
            const passwordValue = password.value.trim();

            if (userIdValue.length === 0) {
                event.preventDefault();
                alert("아이디를 입력해 주세요.");
                userId.focus();
                return;
            }

            if (passwordValue.length === 0) {
                event.preventDefault();
                alert("비밀번호를 입력해 주세요.");
                password.focus();
                return;
            }
        });
    }
}