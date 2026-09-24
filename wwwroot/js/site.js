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
