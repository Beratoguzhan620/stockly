(function () {
    "use strict";

    // ---------- Toasts ----------
    const TOAST_DURATION_MS = 5000;

    function dismissToast(toast) {
        toast.style.transition = "opacity .2s ease";
        toast.style.opacity = "0";
        setTimeout(() => toast.remove(), 200);
    }

    function mountToast(toast) {
        const stack = document.getElementById("toastStack");
        if (!stack) return;

        stack.appendChild(toast);
        toast.hidden = false;
        toast.querySelector("[data-toast-close]")?.addEventListener("click", () => dismissToast(toast));
        setTimeout(() => dismissToast(toast), TOAST_DURATION_MS);
    }

    /** Shows a toast from script, e.g. Stockly.toast("Kopyalandı"). */
    function showToast(message, type = "success") {
        const toast = document.createElement("div");
        const isError = type === "error";
        toast.className = `app-toast ${isError ? "app-toast-error" : "app-toast-success"}`;
        toast.setAttribute("role", "status");
        toast.innerHTML = `<i class="bi ${isError ? "bi-exclamation-circle" : "bi-check-circle"}"></i><span></span>` +
            `<button type="button" class="btn-close" aria-label="Kapat" data-toast-close></button>`;
        toast.querySelector("span").textContent = message;
        mountToast(toast);
    }

    // ---------- Confirm dialog ----------
    let pendingAction = null;

    function openConfirm(trigger, onAccept) {
        const modalElement = document.getElementById("confirmModal");
        if (!modalElement || !window.bootstrap) {
            if (window.confirm(trigger.dataset.confirm)) onAccept();
            return;
        }

        modalElement.querySelector("#confirmModalTitle").textContent = trigger.dataset.confirmTitle || "Emin misiniz?";
        modalElement.querySelector("[data-confirm-message]").textContent = trigger.dataset.confirm;

        const accept = modalElement.querySelector("[data-confirm-accept]");
        accept.textContent = trigger.dataset.confirmOk || "Onayla";
        accept.className = `btn ${trigger.dataset.confirmVariant === "primary" ? "btn-primary" : "btn-danger"}`;

        pendingAction = onAccept;
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    document.addEventListener("click", (event) => {
        const accept = event.target.closest("[data-confirm-accept]");
        if (accept && pendingAction) {
            const action = pendingAction;
            pendingAction = null;
            bootstrap.Modal.getInstance(document.getElementById("confirmModal"))?.hide();
            action();
            return;
        }

        const link = event.target.closest("a[data-confirm]");
        if (link) {
            event.preventDefault();
            openConfirm(link, () => { window.location.href = link.href; });
        }
    });

    // The confirmation text can sit on the form or on the clicked submit button.
    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.dataset.confirmed === "true") { delete form.dataset.confirmed; return; }

        const submitter = event.submitter;
        const source = submitter?.dataset.confirm ? submitter : form.dataset.confirm ? form : null;
        if (!source) return;

        event.preventDefault();
        openConfirm(source, () => {
            form.dataset.confirmed = "true";
            submitter ? form.requestSubmit(submitter) : form.requestSubmit();
        });
    });

    // ---------- Password visibility ----------
    document.addEventListener("click", (event) => {
        const toggle = event.target.closest("[data-password-toggle]");
        if (!toggle) return;

        const input = document.getElementById(toggle.dataset.passwordToggle);
        if (!input) return;

        const reveal = input.type === "password";
        input.type = reveal ? "text" : "password";
        toggle.setAttribute("aria-label", reveal ? "Şifreyi gizle" : "Şifreyi göster");
        toggle.querySelector("i").className = reveal ? "bi bi-eye-slash" : "bi bi-eye";
    });

    // ---------- Bulk selection ----------
    // A table with [data-bulk] gets a "select all" box and a bar that appears once rows are picked.
    function updateBulk(scope) {
        const boxes = [...scope.querySelectorAll("input[data-bulk-item]")];
        const checked = boxes.filter((b) => b.checked);
        const bar = scope.querySelector("[data-bulk-bar]");
        const all = scope.querySelector("input[data-bulk-all]");

        if (bar) {
            bar.classList.toggle("visible", checked.length > 0);
            const count = bar.querySelector("[data-bulk-count]");
            if (count) count.textContent = `${checked.length} kayıt seçildi`;
        }
        if (all) {
            all.checked = checked.length > 0 && checked.length === boxes.length;
            all.indeterminate = checked.length > 0 && checked.length < boxes.length;
        }
    }

    document.addEventListener("change", (event) => {
        const scope = event.target.closest("[data-bulk]");
        if (!scope) return;

        if (event.target.matches("input[data-bulk-all]")) {
            scope.querySelectorAll("input[data-bulk-item]").forEach((b) => { b.checked = event.target.checked; });
        }
        updateBulk(scope);
    });

    // ---------- Loading state ----------
    // Disables the submit button while a POST form is on its way, so it cannot be sent twice.
    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || event.defaultPrevented) return;
        if ((form.method || "get").toLowerCase() !== "post") return;
        if (window.jQuery && jQuery(form).data("validator") && !jQuery(form).valid()) return;

        const button = event.submitter || form.querySelector("button[type=submit]");
        if (!button) return;

        // Keep the clicked button's name/value in the request after it is disabled.
        if (button.name) {
            const hidden = document.createElement("input");
            hidden.type = "hidden";
            hidden.name = button.name;
            hidden.value = button.value;
            form.appendChild(hidden);
        }

        setTimeout(() => {
            button.classList.add("is-loading");
            button.setAttribute("aria-busy", "true");
            button.disabled = true;
        }, 0);
    });

    window.addEventListener("pageshow", () => {
        document.querySelectorAll(".btn.is-loading").forEach((button) => {
            button.classList.remove("is-loading");
            button.removeAttribute("aria-busy");
            button.disabled = false;
        });
    });

    // ---------- Field errors ----------
    // Re-check a field that shows an error as soon as its value changes (typing, pasting or autofill).
    // Otherwise the message only disappears when the field loses focus, which happens on mousedown on the
    // submit button: the button then moves up and the click can miss it.
    document.addEventListener("input", (event) => {
        const field = event.target;
        if (!(field instanceof HTMLElement) || !field.classList.contains("input-validation-error")) return;
        if (!window.jQuery || !field.form || !jQuery(field.form).data("validator")) return;
        jQuery(field).valid();
    });

    // ---------- Boot ----------
    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("[data-toast]").forEach(mountToast);
    });

    window.Stockly = { toast: showToast };
})();
