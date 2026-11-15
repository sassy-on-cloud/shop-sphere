document.addEventListener("DOMContentLoaded", function () {
    const countdown = document.querySelector("[data-countdown]");

    if (!countdown) {
        return;
    }

    let remainingSeconds = 3 * 60 * 60 + 42 * 60 + 18;
    const hours = countdown.querySelector("[data-hours]");
    const minutes = countdown.querySelector("[data-minutes]");
    const seconds = countdown.querySelector("[data-seconds]");

    function renderCountdown() {
        const remainingHours = Math.floor(remainingSeconds / 3600);
        const remainingMinutes = Math.floor((remainingSeconds % 3600) / 60);
        const remaining = remainingSeconds % 60;

        hours.textContent = String(remainingHours).padStart(2, "0");
        minutes.textContent = String(remainingMinutes).padStart(2, "0");
        seconds.textContent = String(remaining).padStart(2, "0");

        if (remainingSeconds > 0) {
            remainingSeconds -= 1;
        }
    }

    renderCountdown();
    window.setInterval(renderCountdown, 1000);
});
