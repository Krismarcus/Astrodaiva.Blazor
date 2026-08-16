window.yearTimeline = {
    scrollToToday: function (selector, percentage) {
        var scroller = document.querySelector(selector);
        if (!scroller) {
            return;
        }

        var content = scroller.querySelector(".timeline-content");
        if (!content) {
            return;
        }

        var rootStyles = getComputedStyle(document.documentElement);
        var labelColumn = parseFloat(rootStyles.getPropertyValue("--year-label-column")) || 96;
        var trackWidth = Math.max(0, content.scrollWidth - labelColumn);
        var clampedPercentage = Math.max(0, Math.min(100, Number(percentage) || 0));
        var todayPosition = labelColumn + (trackWidth * clampedPercentage / 100);
        var scrollLeft = Math.max(0, todayPosition - (scroller.clientWidth / 2));

        scroller.scrollTo({ left: scrollLeft, behavior: "smooth" });
    }
};
