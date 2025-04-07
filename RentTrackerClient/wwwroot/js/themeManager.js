window.themeManager = {
    currentTheme: 'light',
    availableThemes: ['light', 'dark'],

    // Initialize theme
    initialize: function() {
        console.log('Theme Manager: Initializing...');
        const savedTheme = localStorage.getItem('theme');
        if (savedTheme && this.availableThemes.includes(savedTheme)) {
            console.log('Theme Manager: Found saved theme:', savedTheme);
            this.setTheme(savedTheme);
        } else {
            console.log('Theme Manager: No saved theme found, using system preference');
            this.setThemeFromSystemPreference();
        }
    },

    // Set theme from system preference
    setThemeFromSystemPreference: function() {
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        this.setTheme(prefersDark ? 'dark' : 'light');
    },

    // Set theme
    setTheme: function(themeName) {
        console.log('Theme Manager: Setting theme to:', themeName);
        
        try {
            if (!this.availableThemes.includes(themeName)) {
                console.error('Theme Manager: Invalid theme name:', themeName);
                return false;
            }

            this.currentTheme = themeName;
            document.documentElement.setAttribute('data-bs-theme', themeName);
            localStorage.setItem('theme', themeName);
            
            return true;
        } catch (error) {
            console.error('Theme Manager: Error setting theme:', error);
            return false;
        }
    },

    // Get current theme
    getCurrentTheme: function() {
        return this.currentTheme;
    },

    // Get available themes
    getAvailableThemes: function() {
        return this.availableThemes;
    }
};

// Initialize when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('Theme Manager: DOM loaded, initializing...');
    window.themeManager.initialize();
});

// Watch for system theme changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    if (!localStorage.getItem('theme')) {
        window.themeManager.setTheme(e.matches ? 'dark' : 'light');
    }
});

// Make Blazor initialization method available
window.initializeThemeManager = function() {
    console.log('Theme Manager: Blazor called initialization...');
    window.themeManager.initialize();
};

// Log that the script has loaded
console.log('Theme Manager: Script loaded');