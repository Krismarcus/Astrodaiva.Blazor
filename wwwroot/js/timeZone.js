window.astroTimeZone = {
    read() {
        const detected = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Vilnius';
        let selected = 'auto';
        try { selected = localStorage.getItem('astrodaiva.timeZone') || 'auto'; } catch { }
        const zones = Intl.supportedValuesOf ? Intl.supportedValuesOf('timeZone') : [detected, 'Europe/Vilnius', 'UTC'];
        return { detected, selected, zones };
    },
    save(value) { try { localStorage.setItem('astrodaiva.timeZone', value); } catch { } }
};
