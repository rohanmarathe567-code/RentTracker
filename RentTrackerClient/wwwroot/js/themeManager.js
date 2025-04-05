window.themeManager = {
    currentTheme: 'default',

    // Initialize theme
    initialize: function() {
        console.log('Theme Manager: Initializing...');
        const savedTheme = localStorage.getItem('theme');
        if (savedTheme) {
            console.log('Theme Manager: Found saved theme:', savedTheme);
            this.setTheme(savedTheme);
        } else {
            console.log('Theme Manager: No saved theme found, using default');
        }
    },

    // Set theme
    setTheme: function(themeName) {
        console.log('Theme Manager: Setting theme to:', themeName);
        
        try {
            this.currentTheme = themeName;
            
            if (themeName === 'default') {
                document.documentElement.removeAttribute('data-theme');
                console.log('Theme Manager: Removed data-theme attribute');
            } else {
                document.documentElement.setAttribute('data-theme', themeName);
                console.log('Theme Manager: Set data-theme attribute to:', themeName);
            }
            
            localStorage.setItem('theme', themeName);
            console.log('Theme Manager: Saved theme to localStorage');
            
            return true;
        } catch (error) {
            console.error('Theme Manager: Error setting theme:', error);
            return false;
        }
    },

    // Get current theme
    getCurrentTheme: function() {
        return this.currentTheme;
    }
};

// Initialize when the document is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('Theme Manager: DOM loaded, initializing...');
    window.themeManager.initialize();
});

// Make Blazor initialization method available
window.initializeThemeManager = function() {
    console.log('Theme Manager: Blazor called initialization...');
    window.themeManager.initialize();
};

// Log that the script has loaded
console.log('Theme Manager: Script loaded');