window.yearTimeline = {
    closeOpenMenus: function (rootSelector) {
        var root = document.querySelector(rootSelector);
        if (!root) {
            return;
        }

        root.querySelectorAll("details[open]").forEach(function (details) {
            details.removeAttribute("open");
        });
    },

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
        var visibleTrackWidth = Math.max(0, scroller.clientWidth - labelColumn);
        var visibleTrackCenter = labelColumn + (visibleTrackWidth / 2);
        var scrollLeft = Math.max(0, todayPosition - visibleTrackCenter);

        scroller.scrollTo({ left: scrollLeft, behavior: "smooth" });
    }
};
