// ===== FUNÇÕES GLOBAIS DE LOADING =====
window.showLoading = function (message = 'Processando...') {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        const text = overlay.querySelector('.loading-text');
        if (text) text.textContent = message;
        overlay.classList.remove('d-none');
    }
};

window.hideLoading = function () {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('d-none');
    }
};

// ===== FUNÇÃO GLOBAL DE TOAST =====
window.showToast = function (message, type = 'info', duration = 5000) {
    const container = document.querySelector('.toast-container');
    if (!container) return;

    const toastId = 'toast-' + Date.now();
    const bgClass = {
        'success': 'bg-success',
        'error': 'bg-danger',
        'warning': 'bg-warning',
        'info': 'bg-info'
    }[type] || 'bg-info';

    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', toastHTML);

    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, { delay: duration });
    toast.show();

    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
};

// ===== SISTEMA DE TEMA DARK/LIGHT =====
class ThemeManager {
    constructor() {
        this.init();
    }

    init() {
        const savedTheme = localStorage.getItem('theme') || 'light';
        this.setTheme(savedTheme);

        document.querySelectorAll('[data-theme]').forEach(button => {
            button.addEventListener('click', (e) => {
                const theme = e.currentTarget.getAttribute('data-theme');
                this.setTheme(theme);
            });
        });
    }

    setTheme(theme) {
        const html = document.documentElement;
        const themeIcon = document.getElementById('themeIcon');

        html.removeAttribute('data-bs-theme');

        if (theme === 'dark') {
            html.setAttribute('data-bs-theme', 'dark');
            if (themeIcon) themeIcon.className = 'bi bi-moon-stars-fill';
        } else if (theme === 'auto') {
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            html.setAttribute('data-bs-theme', prefersDark ? 'dark' : 'light');
            if (themeIcon) themeIcon.className = 'bi bi-circle-half';
        } else {
            html.setAttribute('data-bs-theme', 'light');
            if (themeIcon) themeIcon.className = 'bi bi-sun-fill';
        }

        localStorage.setItem('theme', theme);
        this.updateThemeIndicators(theme);
    }

    updateThemeIndicators(activeTheme) {
        document.querySelectorAll('[data-theme]').forEach(button => {
            const theme = button.getAttribute('data-theme');
            if (theme === activeTheme) {
                button.classList.add('active');
            } else {
                button.classList.remove('active');
            }
        });
    }
}

// ===== GERENCIADOR DE SIDEBAR =====
class SidebarManager {
    constructor() {
        this.sidebar = document.getElementById('sidebar');
        this.content = document.getElementById('content');
        this.init();
    }

    init() {
        const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (isCollapsed && this.sidebar) {
            this.collapse();
        }

        const toggleButtons = document.querySelectorAll('#sidebarCollapse, #sidebarCollapseTop');
        toggleButtons.forEach(button => {
            button.addEventListener('click', () => this.toggle());
        });

        this.handleResponsive();
        window.addEventListener('resize', () => this.handleResponsive());
        this.setActiveLink();
    }

    toggle() {
        if (this.sidebar.classList.contains('collapsed')) {
            this.expand();
        } else {
            this.collapse();
        }
    }

    collapse() {
        if (this.sidebar) {
            this.sidebar.classList.add('collapsed');
            if (this.content) this.content.classList.add('expanded');
            localStorage.setItem('sidebarCollapsed', 'true');
        }
    }

    expand() {
        if (this.sidebar) {
            this.sidebar.classList.remove('collapsed');
            if (this.content) this.content.classList.remove('expanded');
            localStorage.setItem('sidebarCollapsed', 'false');
        }
    }

    handleResponsive() {
        if (window.innerWidth <= 991) {
            if (this.sidebar) this.sidebar.classList.add('collapsed');
        }
    }

    setActiveLink() {
        const currentPath = window.location.pathname.toLowerCase();
        const menuLinks = document.querySelectorAll('.nav-menu .nav-link');

        menuLinks.forEach(link => {
            const href = link.getAttribute('href')?.toLowerCase();
            if (href && currentPath.includes(href) && href !== '/') {
                link.classList.add('active');

                const submenu = link.closest('.submenu');
                if (submenu) {
                    submenu.classList.add('show');
                    const parent = submenu.closest('.has-submenu');
                    const arrow = parent?.querySelector('.submenu-arrow');
                    if (arrow) arrow.style.transform = 'rotate(90deg)';
                }
            }
        });
    }
}

// ===== GERENCIADOR DE SUBMENU =====
class SubmenuManager {
    constructor() {
        this.init();
    }

    init() {
        const submenuItems = document.querySelectorAll('.has-submenu > .nav-link');
        submenuItems.forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const parent = item.parentElement;
                const submenu = parent.querySelector('.submenu');
                const arrow = item.querySelector('.submenu-arrow');

                if (submenu.classList.contains('show')) {
                    submenu.classList.remove('show');
                    if (arrow) arrow.style.transform = 'rotate(0deg)';
                } else {
                    document.querySelectorAll('.submenu.show').forEach(sub => {
                        sub.classList.remove('show');
                        const parentArrow = sub.parentElement.querySelector('.submenu-arrow');
                        if (parentArrow) parentArrow.style.transform = 'rotate(0deg)';
                    });

                    submenu.classList.add('show');
                    if (arrow) arrow.style.transform = 'rotate(90deg)';
                }
            });
        });
    }
}

// ===== GERENCIADOR DE ALERTAS =====
class AlertManager {
    constructor() {
        this.init();
    }

    init() {
        const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
        alerts.forEach(alert => {
            setTimeout(() => {
                if (alert.parentElement) {
                    const bsAlert = new bootstrap.Alert(alert);
                    bsAlert.close();
                }
            }, 5000);
        });
    }
}

// ===== INICIALIZAÇÃO GLOBAL =====
document.addEventListener('DOMContentLoaded', function () {
    // Inicializar gerenciadores
    new ThemeManager();

    if (document.getElementById('sidebar')) {
        new SidebarManager();
        new SubmenuManager();
    }

    new AlertManager();

    // Remover preloader se existir
    const preloader = document.getElementById('preloader');
    if (preloader) {
        setTimeout(() => {
            preloader.classList.add('fade-out');
            setTimeout(() => preloader.remove(), 500);
        }, 1000);
    }

    // Configuração global para requisições AJAX
    if (typeof $ !== 'undefined') {
        $.ajaxSetup({
            beforeSend: function (xhr, settings) {
                if (!settings.crossDomain) {
                    const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
                    if (token) {
                        xhr.setRequestHeader("X-CSRF-Token", token);
                    }
                }
            }
        });
    }
});

// ===== PREVENIR ENVIO DUPLO DE FORMULÁRIOS =====
document.addEventListener('submit', function (e) {
    const form = e.target;
    const submitButton = form.querySelector('[type="submit"]');

    if (submitButton && !submitButton.disabled) {
        submitButton.disabled = true;

        if (form.checkValidity()) {
            showLoading('Enviando dados...');
        }

        setTimeout(() => {
            submitButton.disabled = false;
            hideLoading();
        }, 3000);
    }
});

// ===== TRATAMENTO GLOBAL DE ERROS =====
window.addEventListener('error', function (e) {
    console.error('Erro JavaScript:', e.error);
    hideLoading();
});

// ===== PREVENIR NAVEGAÇÃO ACIDENTAL =====
window.addEventListener('beforeunload', function (e) {
    const forms = document.querySelectorAll('form[data-warn-unsaved]');
    for (const form of forms) {
        const inputs = form.querySelectorAll('input, textarea, select');
        for (const input of inputs) {
            if (input.dataset.originalValue !== input.value) {
                e.preventDefault();
                e.returnValue = '';
                return '';
            }
        }
    }
});