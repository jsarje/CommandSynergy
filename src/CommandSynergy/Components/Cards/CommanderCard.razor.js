document.addEventListener("click", event => {
    const toggleButton = event.target.closest("[data-face-toggle-target]");
    if (!toggleButton) {
        return;
    }

    const targetId = toggleButton.getAttribute("data-face-toggle-target");
    if (!targetId) {
        return;
    }

    const cardSurface = document.getElementById(targetId);
    if (!cardSurface) {
        return;
    }

    cardSurface.classList.toggle("is-flipped");
});