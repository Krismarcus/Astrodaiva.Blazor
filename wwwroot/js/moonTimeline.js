window.moonTimeline = (() => {
    let observer = null;
    let frame = null;
    const observed = new Set();

    function measure() {
        document.querySelectorAll('.md-timeline--triple').forEach(timeline => {
            const middle = timeline.querySelector('.md-marker--mid .md-marker-label');
            const last = timeline.querySelector('.md-marker--final .md-marker-label');
            if (!middle || !last) return;

            const a = middle.getBoundingClientRect();
            const b = last.getBoundingClientRect();
            // Compare horizontal bounds even when the labels are already stacked.
            // Keep a small gap so touching labels are separated as well.
            const overlap = a.width > 0 && b.width > 0 && a.left < b.right + 6 && b.left < a.right + 6;
            timeline.classList.toggle('md-timeline--stacked', overlap);
        });
    }

    function schedule() {
        if (frame === null) {
            frame = requestAnimationFrame(() => {
                frame = null;
                measure();
            });
        }
    }

    function refresh() {
        if (!observer) {
            observer = new ResizeObserver(schedule);
            window.addEventListener('resize', schedule, { passive: true });
            document.fonts?.ready.then(() => { if (observer) schedule(); });
        }

        const current = new Set(document.querySelectorAll('.md-timeline--triple .md-line-wrap, .md-timeline--triple .md-marker-label'));
        observed.forEach(element => {
            if (!current.has(element)) {
                observer.unobserve(element);
                observed.delete(element);
            }
        });
        current.forEach(element => {
            if (!observed.has(element)) {
                observed.add(element);
                observer.observe(element);
            }
        });
        measure();
    }

    function dispose() {
        observer?.disconnect();
        observer = null;
        observed.clear();
        window.removeEventListener('resize', schedule);
        if (frame !== null) cancelAnimationFrame(frame);
        frame = null;
    }

    return { refresh, dispose };
})();
