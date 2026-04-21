window.activitySlider = (function () {
    let initialized = false;

    function update(selector) {
        const grids = document.querySelectorAll(selector || '.js-activity-grid');
        grids.forEach((grid) => {
            const slider = grid.querySelector('.activity-grid__slider');
            const selected = grid.querySelector('.activity-btn.selected');
            if (!slider || !selected) return;

            const gridRect = grid.getBoundingClientRect();
            const btnRect = selected.getBoundingClientRect();

            slider.style.left = `${btnRect.left - gridRect.left}px`;
            slider.style.top = `${btnRect.top - gridRect.top}px`;
            slider.style.width = `${btnRect.width}px`;
            slider.style.height = `${btnRect.height}px`;
            slider.style.opacity = '1';
        });
    }

    function init(selector) {
        if (initialized) return;
        initialized = true;

        let rafId = null;
        const onResize = () => {
            if (rafId) cancelAnimationFrame(rafId);
            rafId = requestAnimationFrame(() => update(selector));
        };

        window.addEventListener('resize', onResize, { passive: true });
        window.addEventListener('orientationchange', onResize, { passive: true });
        setTimeout(() => update(selector), 0);
    }

    return { init, update };
})();
