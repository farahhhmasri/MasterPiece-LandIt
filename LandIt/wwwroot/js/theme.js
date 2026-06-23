/**
 * LandIt Theme Toggle
 * Reads saved preference from localStorage, defaults to light.
 * Toggles data-theme on <html> and swaps the icon.
 */
(function () {
    const ROOT = document.documentElement;
    const STORAGE_KEY = 'landit-theme';

    function applyTheme(dark) {
        ROOT.setAttribute('data-theme', dark ? 'dark' : 'light');

        var icon = document.getElementById('themeIcon');
        if (icon) {
            icon.className = dark ? 'ti ti-sun' : 'ti ti-moon';
        }

        try {
            localStorage.setItem(STORAGE_KEY, dark ? 'dark' : 'light');
        } catch (e) { }
    }

    // On load — read saved preference, default to light
    var saved = null;
    try { saved = localStorage.getItem(STORAGE_KEY); } catch (e) { }
    applyTheme(saved === 'dark');

    // Wire up button after DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        var btn = document.getElementById('themeToggle');
        if (!btn) return;

        btn.addEventListener('click', function () {
            var isDark = ROOT.getAttribute('data-theme') === 'dark';
            applyTheme(!isDark);
        });
    });
})();
