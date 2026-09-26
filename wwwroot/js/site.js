document.addEventListener("DOMContentLoaded", () => {
    const checkIn = document.getElementById("CheckIn");
    const checkOut = document.getElementById("CheckOut");

    if (checkIn && checkOut) {
        checkIn.addEventListener("change", () => {
            const date = new Date(checkIn.value);
            if (!Number.isNaN(date.getTime())) {
                date.setDate(date.getDate() + 1);
                const next = date.toISOString().split("T")[0];
                if (!checkOut.value || checkOut.value <= checkIn.value) {
                    checkOut.value = next;
                }
                checkOut.min = next;
            }
        });
    }
});


// Auto-dismiss save/result notifications without affecting existing page behavior.
document.addEventListener("DOMContentLoaded", () => {
    const toast = document.getElementById("uplivaToast");
    if (toast) {
        window.setTimeout(() => toast.remove(), 5500);
    }
});

// Clear a server-side field error as soon as the user edits that field.
// The server still re-validates on submit; this only stops a corrected field from staying red.
(() => {
    const clearFieldError = (event) => {
        const el = event.target;
        if (!el?.classList?.contains("input-validation-error")) return;
        el.classList.remove("input-validation-error");
        el.classList.add("input-validation-valid");
        const message = el.closest(".field")?.querySelector(".field-validation-error");
        if (message) {
            message.textContent = "";
            message.classList.remove("field-validation-error");
            message.classList.add("field-validation-valid");
        }
    };
    document.addEventListener("input", clearFieldError);
    document.addEventListener("change", clearFieldError);
})();
