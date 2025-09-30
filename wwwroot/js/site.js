// ===== FUNÇÕES UTILITÁRIAS GLOBAIS =====
window.showLoading = (message = 'Processando...') => {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        const text = overlay.querySelector('.loading-text');
        if (text) text.textContent = message;
        overlay.classList.remove('d-none');
    }
};

window.hideLoading = () => {
    document.getElementById('loadingOverlay')?.classList.add('d-none');
};

window.showToast = (message, type = 'info', duration = 5000) => {
    const container = document.querySelector('.toast-container');
    if (!container) return;

    const icons = {
        success: 'bi-check-circle-fill',
        error: 'bi-x-circle-fill',
        warning: 'bi-exclamation-triangle-fill',
        info: 'bi-info-circle-fill'
    };

    const bgClasses = {
        success: 'bg-success',
        error: 'bg-danger',
        warning: 'bg-warning',
        info: 'bg-info'
    };

    const toastId = `toast-${Date.now()}`;
    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClasses[type]} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi ${icons[type]} me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;

    container.insertAdjacentHTML('beforeend', toastHTML);
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, { delay: duration });
    toast.show();
    toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
};

window.formatCurrency = (value) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

window.formatDate = (date) =>
    new Intl.DateTimeFormat('pt-BR').format(new Date(date));

window.formatDateTime = (date) =>
    new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(date));

window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        showToast('Copiado!', 'success', 2000);
    } catch {
        showToast('Erro ao copiar', 'error', 2000);
    }
};

window.debounce = (func, wait) => {
    let timeout;
    return (...args) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => func(...args), wait);
    };
};

// ===== GERENCIADOR DE TEMA =====
class ThemeManager {
    constructor() {
        this.init();
    }

    init() {
        const savedTheme = localStorage.getItem('theme') || 'light';
        this.setTheme(savedTheme);
        this.bindEvents();
    }

    bindEvents() {
        document.querySelectorAll('[data-theme]').forEach(btn => {
            btn.addEventListener('click', () => {
                const theme = btn.getAttribute('data-theme');
                this.setTheme(theme);
                showToast(this.getThemeMessage(theme), 'info', 2000);
            });
        });

        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
            if (localStorage.getItem('theme') === 'auto') {
                this.setTheme('auto');
            }
        });
    }

    setTheme(theme) {
        const html = document.documentElement;
        const themeIcon = document.getElementById('themeIcon');

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
        this.updateIndicators(theme);
    }

    updateIndicators(activeTheme) {
        document.querySelectorAll('[data-theme]').forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-theme') === activeTheme);
        });
    }

    getThemeMessage(theme) {
        const messages = {
            light: 'Tema claro ativado',
            dark: 'Tema escuro ativado',
            auto: 'Tema automático ativado'
        };
        return messages[theme];
    }
}

// ===== GERENCIADOR DE SIDEBAR MOBILE CORRIGIDO =====
class SidebarManager {
    constructor() {
        this.sidebar = document.getElementById('sidebar');
        this.content = document.getElementById('content');
        this.backdrop = null;
        this.init();
    }

    init() {
        if (!this.sidebar) return;

        // Criar backdrop
        this.createBackdrop();

        // Restaurar estado no desktop
        if (window.innerWidth > 991) {
            const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
            if (isCollapsed) this.collapse();
        }

        // Eventos de toggle
        document.querySelectorAll('#sidebarCollapse, #sidebarCollapseTop').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.toggle();
            });
        });

        // Fechar sidebar ao clicar em links no mobile
        document.querySelectorAll('.sidebar .nav-link').forEach(link => {
            link.addEventListener('click', () => {
                if (window.innerWidth <= 991 && this.sidebar.classList.contains('show')) {
                    this.closeMobile();
                }
            });
        });

        // Responsividade
        this.handleResponsive();
        window.addEventListener('resize', () => this.handleResponsive());

        // Marcar link ativo
        this.setActiveLink();
    }

    createBackdrop() {
        this.backdrop = document.createElement('div');
        this.backdrop.className = 'sidebar-backdrop';
        document.body.appendChild(this.backdrop);

        // Fechar ao clicar no backdrop
        this.backdrop.addEventListener('click', () => {
            if (window.innerWidth <= 991) {
                this.closeMobile();
            }
        });
    }

    toggle() {
        if (window.innerWidth <= 991) {
            // Mobile: toggle show/hide
            if (this.sidebar.classList.contains('show')) {
                this.closeMobile();
            } else {
                this.openMobile();
            }
        } else {
            // Desktop: toggle collapse
            if (this.sidebar.classList.contains('collapsed')) {
                this.expand();
            } else {
                this.collapse();
            }
        }
    }

    openMobile() {
        this.sidebar.classList.add('show');
        this.backdrop.classList.add('show');
        document.body.style.overflow = 'hidden';
    }

    closeMobile() {
        this.sidebar.classList.remove('show');
        this.backdrop.classList.remove('show');
        document.body.style.overflow = '';
    }

    collapse() {
        this.sidebar.classList.add('collapsed');
        this.content?.classList.add('expanded');
        localStorage.setItem('sidebarCollapsed', 'true');
    }

    expand() {
        this.sidebar.classList.remove('collapsed');
        this.content?.classList.remove('expanded');
        localStorage.setItem('sidebarCollapsed', 'false');
    }

    handleResponsive() {
        if (window.innerWidth <= 991) {
            // Mobile: sempre fechado por padrão
            this.closeMobile();
        } else {
            // Desktop: remover classes mobile
            this.sidebar.classList.remove('show');
            this.backdrop.classList.remove('show');
            document.body.style.overflow = '';
        }
    }

    setActiveLink() {
        const currentPath = window.location.pathname.toLowerCase();
        document.querySelectorAll('.nav-menu .nav-link').forEach(link => {
            const href = link.getAttribute('href')?.toLowerCase();
            if (href && currentPath.includes(href) && href !== '/') {
                link.classList.add('active');

                // Abrir submenu se o link estiver dentro dele
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
        document.querySelectorAll('.has-submenu > .nav-link').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const submenu = document.getElementById(item.getAttribute('href').substring(1));
                const arrow = item.querySelector('.submenu-arrow');

                if (submenu) {
                    const isShowing = submenu.classList.contains('show');

                    // Fechar outros submenus
                    document.querySelectorAll('.submenu.show').forEach(other => {
                        if (other !== submenu) {
                            other.classList.remove('show');
                            const otherArrow = other.closest('.has-submenu')?.querySelector('.submenu-arrow');
                            if (otherArrow) otherArrow.style.transform = 'rotate(0deg)';
                        }
                    });

                    // Toggle atual
                    submenu.classList.toggle('show');
                    if (arrow) {
                        arrow.style.transform = isShowing ? 'rotate(0deg)' : 'rotate(90deg)';
                    }
                }
            });
        });
    }
}

// ===== GERENCIADOR DE FORMULÁRIOS =====
class FormManager {
    constructor() {
        this.init();
    }

    init() {
        this.setupFormSubmit();
        this.setupCPFMask();
        this.setupPhoneMask();
        this.setupCEPMask();
        this.setupMoneyMask();
    }

    setupFormSubmit() {
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', () => showLoading('Enviando...'));
        });
    }

    setupCPFMask() {
        document.querySelectorAll('input[data-mask="cpf"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{3})(\d{1,2})$/, '$1-$2');
                e.target.value = value;
            });
        });
    }

    setupPhoneMask() {
        document.querySelectorAll('input[data-mask="phone"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length > 10) {
                    value = value.replace(/(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
                } else {
                    value = value.replace(/(\d{2})(\d{4})(\d{4})/, '($1) $2-$3');
                }
                e.target.value = value;
            });
        });
    }

    setupCEPMask() {
        document.querySelectorAll('input[data-mask="cep"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                value = value.replace(/(\d{5})(\d)/, '$1-$2');
                e.target.value = value;
            });

            input.addEventListener('blur', async (e) => {
                const cep = e.target.value.replace(/\D/g, '');
                if (cep.length === 8) {
                    await this.fetchCEP(cep);
                }
            });
        });
    }

    setupMoneyMask() {
        document.querySelectorAll('input[data-mask="money"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                value = (value / 100).toFixed(2).replace('.', ',');
                value = value.replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1.');
                e.target.value = 'R$ ' + value;
            });
        });
    }

    async fetchCEP(cep) {
        try {
            showLoading('Buscando CEP...');
            const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
            const data = await response.json();

            if (!data.erro) {
                document.getElementById('Logradouro')?.setAttribute('value', data.logradouro);
                document.getElementById('Bairro')?.setAttribute('value', data.bairro);
                document.getElementById('Cidade')?.setAttribute('value', data.localidade);
                document.getElementById('Estado')?.setAttribute('value', data.uf);
                showToast('CEP encontrado!', 'success', 2000);
            } else {
                showToast('CEP não encontrado', 'error', 3000);
            }
        } catch {
            showToast('Erro ao buscar CEP', 'error', 3000);
        } finally {
            hideLoading();
        }
    }
}

// ===== GERENCIADOR DE VALIDAÇÃO =====
class ValidationManager {
    constructor() {
        this.init();
    }

    init() {
        document.querySelectorAll('input[data-validate="email"]').forEach(input => {
            input.addEventListener('blur', () => this.validateEmail(input));
        });

        document.querySelectorAll('input[data-validate="cpf"]').forEach(input => {
            input.addEventListener('blur', () => this.validateCPF(input));
        });
    }

    validateEmail(input) {
        const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const isValid = regex.test(input.value);
        this.toggleFeedback(input, isValid, 'Email inválido');
        return isValid;
    }

    validateCPF(input) {
        const cpf = input.value.replace(/\D/g, '');
        const isValid = this.isValidCPF(cpf);
        this.toggleFeedback(input, isValid, 'CPF inválido');
        return isValid;
    }

    isValidCPF(cpf) {
        if (cpf.length !== 11 || /^(\d)\1+$/.test(cpf)) return false;

        let sum = 0;
        for (let i = 0; i < 9; i++) sum += parseInt(cpf.charAt(i)) * (10 - i);
        let digit = 11 - (sum % 11);
        if (digit > 9) digit = 0;
        if (digit !== parseInt(cpf.charAt(9))) return false;

        sum = 0;
        for (let i = 0; i < 10; i++) sum += parseInt(cpf.charAt(i)) * (11 - i);
        digit = 11 - (sum % 11);
        if (digit > 9) digit = 0;
        return digit === parseInt(cpf.charAt(10));
    }

    toggleFeedback(input, isValid, message) {
        input.classList.toggle('is-valid', isValid);
        input.classList.toggle('is-invalid', !isValid);

        let feedback = input.parentElement.querySelector('.invalid-feedback, .valid-feedback');
        if (!isValid) {
            if (!feedback) {
                feedback = document.createElement('div');
                feedback.className = 'invalid-feedback';
                input.parentElement.appendChild(feedback);
            }
            feedback.textContent = message;
            feedback.style.display = 'block';
        } else if (feedback) {
            feedback.style.display = 'none';
        }
    }
}

// ===== GERENCIADOR DE TABELAS =====
class TableManager {
    constructor() {
        this.init();
    }

    init() {
        this.setupSorting();
    }

    setupSorting() {
        document.querySelectorAll('table.sortable thead th[data-sort]').forEach(header => {
            header.style.cursor = 'pointer';
            header.addEventListener('click', () => {
                const table = header.closest('table');
                const tbody = table.querySelector('tbody');
                const rows = Array.from(tbody.querySelectorAll('tr'));
                const columnIndex = Array.from(header.parentElement.children).indexOf(header);
                const currentDirection = header.getAttribute('data-sort-direction') || 'asc';
                const newDirection = currentDirection === 'asc' ? 'desc' : 'asc';

                rows.sort((a, b) => {
                    const aValue = a.children[columnIndex].textContent.trim();
                    const bValue = b.children[columnIndex].textContent.trim();
                    return newDirection === 'asc'
                        ? aValue.localeCompare(bValue, 'pt-BR', { numeric: true })
                        : bValue.localeCompare(aValue, 'pt-BR', { numeric: true });
                });

                rows.forEach(row => tbody.appendChild(row));
                header.setAttribute('data-sort-direction', newDirection);

                const icon = header.querySelector('i');
                if (icon) {
                    icon.className = newDirection === 'asc' ? 'bi bi-arrow-up ms-1' : 'bi bi-arrow-down ms-1';
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
        document.querySelectorAll('.alert').forEach(alert => {
            setTimeout(() => {
                const bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            }, 8000);
        });
    }
}

// ===== INICIALIZAÇÃO =====
document.addEventListener('DOMContentLoaded', () => {
    // Inicializar gerenciadores
    new ThemeManager();
    new FormManager();
    new TableManager();
    new ValidationManager();
    new AlertManager();

    if (document.getElementById('sidebar')) {
        new SidebarManager();
        new SubmenuManager();
    }

    // Remover preloader
    const preloader = document.getElementById('preloader');
    if (preloader) {
        setTimeout(() => {
            preloader.classList.add('fade-out');
            setTimeout(() => preloader.remove(), 500);
        }, 1000);
    }

    // Configuração AJAX com CSRF Token
    if (typeof $ !== 'undefined') {
        $.ajaxSetup({
            beforeSend: (xhr, settings) => {
                if (!settings.crossDomain) {
                    const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
                    if (token) xhr.setRequestHeader("X-CSRF-Token", token);
                }
            }
        });
    }

    // Intersection Observer para animações
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });

    document.querySelectorAll('.card, .alert').forEach(el => observer.observe(el));
});

// Tratamento global de erros
window.addEventListener('error', (e) => {
    console.error('Erro:', e.error);
    hideLoading();
});

console.log('✨ Sistema carregado com sucesso!');