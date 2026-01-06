// ============================================
// Dark Mode Theme Toggle
// ============================================

(function() {
    'use strict';

    const THEME_KEY = 'cloudStorage_theme';
    const DARK_THEME = 'dark';
    const LIGHT_THEME = 'light';

    // Initialize theme on page load
    function initTheme() {
        const savedTheme = localStorage.getItem(THEME_KEY) || LIGHT_THEME;
        applyTheme(savedTheme);
        updateToggleIcon(savedTheme);
    }

    // Apply theme to document
    function applyTheme(theme) {
        if (theme === DARK_THEME) {
            document.documentElement.setAttribute('data-theme', 'dark');
            document.body.classList.add('dark-mode');
        } else {
            document.documentElement.removeAttribute('data-theme');
            document.body.classList.remove('dark-mode');
        }
    }

    // Update toggle button icon
    function updateToggleIcon(theme) {
        const icon = document.getElementById('themeIcon');
        if (icon) {
            if (theme === DARK_THEME) {
                icon.className = 'fas fa-sun';
                icon.parentElement.setAttribute('title', 'Switch to Light Mode');
            } else {
                icon.className = 'fas fa-moon';
                icon.parentElement.setAttribute('title', 'Switch to Dark Mode');
            }
        }
    }

    // Toggle theme
    function toggleTheme() {
        const currentTheme = localStorage.getItem(THEME_KEY) || LIGHT_THEME;
        const newTheme = currentTheme === DARK_THEME ? LIGHT_THEME : DARK_THEME;
        
        localStorage.setItem(THEME_KEY, newTheme);
        applyTheme(newTheme);
        updateToggleIcon(newTheme);

        // Add smooth transition effect
        document.body.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        setTimeout(() => {
            document.body.style.transition = '';
        }, 300);
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheme);
    } else {
        initTheme();
    }

    // Attach toggle handler
    document.addEventListener('DOMContentLoaded', function() {
        const toggleButton = document.getElementById('themeToggle');
        if (toggleButton) {
            toggleButton.addEventListener('click', toggleTheme);
        }
    });

    // Expose getCurrentTheme for debugging
    window.getCurrentTheme = function() {
        return localStorage.getItem(THEME_KEY) || LIGHT_THEME;
    };
})();
