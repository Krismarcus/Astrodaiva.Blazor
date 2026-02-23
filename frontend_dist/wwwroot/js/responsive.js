window.responsive = {
    register: function (dotNetRef) {
        function report() {
            const isMobile = window.innerWidth <= 900;
            dotNetRef.invokeMethodAsync('SetIsMobile', isMobile);
        }
        report();
        window.addEventListener('resize', report);
    }
};
