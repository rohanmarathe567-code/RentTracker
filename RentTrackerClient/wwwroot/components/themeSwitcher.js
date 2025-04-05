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
            <div class="theme-switcher">
                <select class="form-select" aria-label="Theme Selector">
                    ${themes.map(theme => `
                        <option value="${theme}" ${theme === currentTheme ? 'selected' : ''}>
                            ${theme.charAt(0).toUpperCase() + theme.slice(1)}
                        </option>
                    `).join('')}
                </select>
            </div>
        `;

        // Add styles
        const style = document.createElement('style');
        style.textContent = `
            .theme-switcher {
                margin: 10px;
                min-width: 150px;
            }
            
            .theme-switcher .form-select {
                background-color: var(--background-secondary);
                color: var(--text-primary);
                border: 1px solid var(--border-color);
                border-radius: 4px;
                padding: 8px;
            }
            
            .theme-switcher .form-select:focus {
                border-color: var(--text-secondary);
                box-shadow: 0 0 0 0.2rem rgba(137, 180, 250, 0.25);
            }
            
            .theme-switcher .form-select option {
                background-color: var(--background-secondary);
                color: var(--text-primary);
            }
        `;
        this.appendChild(style);
    }

    setupEventListeners() {
        const select = this.querySelector('select');
        select.addEventListener('change', (e) => {
            window.themeManager.setTheme(e.target.value);
        });
    }
}

// Register the custom element
customElements.define('theme-switcher', ThemeSwitcher);