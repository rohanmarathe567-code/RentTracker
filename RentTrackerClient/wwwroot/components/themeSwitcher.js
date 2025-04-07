class ThemeSwitcher extends HTMLElement {
    constructor() {
        super();
        this.render();
        this.setupEventListeners();
    }

    render() {
        const themes = window.themeManager.getAvailableThemes();
        const currentTheme = window.themeManager.getCurrentTheme();
        
        this.innerHTML = `
            <div class="theme-switcher d-flex align-items-center">
                <div class="dropdown">
                    <button class="btn btn-outline-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                        <i class="bi ${currentTheme === 'dark' ? 'bi-moon-stars' : 'bi-sun'}"></i>
                        ${currentTheme.charAt(0).toUpperCase() + currentTheme.slice(1)}
                    </button>
                    <ul class="dropdown-menu">
                        ${themes.map(theme => `
                            <li>
                                <button class="dropdown-item ${theme === currentTheme ? 'active' : ''}" data-theme="${theme}">
                                    <i class="bi ${theme === 'dark' ? 'bi-moon-stars' : 'bi-sun'} me-2"></i>
                                    ${theme.charAt(0).toUpperCase() + theme.slice(1)}
                                </button>
                            </li>
                        `).join('')}
                    </ul>
                </div>
            </div>
        `;
    }

    setupEventListeners() {
        const dropdownItems = this.querySelectorAll('.dropdown-item');
        dropdownItems.forEach(item => {
            item.addEventListener('click', (e) => {
                const theme = e.currentTarget.dataset.theme;
                window.themeManager.setTheme(theme);
                this.render(); // Re-render to update the button icon
            });
        });
    }
}

// Register the custom element
customElements.define('theme-switcher', ThemeSwitcher);