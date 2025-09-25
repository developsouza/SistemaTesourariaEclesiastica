// ===== SISTEMA DE TEMA DARK/LIGHT =====
class ThemeManager {
    constructor() {
        this.init();
    }

    init() {
        // Carrega o tema salvo ou usa o padrão
        const savedTheme = localStorage.getItem('theme') || 'light';
        this.setTheme(savedTheme);
        
        // Adiciona event listeners para os botões de tema
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
        
        // Remove classes de tema existentes
        html.removeAttribute('data-bs-theme');
        
        // Aplica o novo tema
        if (theme === 'dark') {
            html.setAttribute('data-bs-theme', 'dark');
            if (themeIcon) themeIcon.className = 'bi bi-moon-stars-fill';
        } else if (theme === 'auto') {
            // Detecta preferência do sistema
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            html.setAttribute('data-bs-theme', prefersDark ? 'dark' : 'light');
            if (themeIcon) themeIcon.className = 'bi bi-circle-half';
        } else {
            html.setAttribute('data-bs-theme', 'light');
            if (themeIcon) themeIcon.className = 'bi bi-sun-fill';
        }
        
        // Salva a preferência
        localStorage.setItem('theme', theme);
        
        // Atualiza indicadores visuais
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
        // Carrega o estado salvo da sidebar
        const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (isCollapsed) {
            this.collapse();
        }

        // Event listeners para botões de toggle
        const toggleButtons = document.querySelectorAll('#sidebarCollapse, #sidebarCollapseTop');
        toggleButtons.forEach(button => {
            button.addEventListener('click', () => this.toggle());
        });

        // Gerencia responsividade
        this.handleResponsive();
        window.addEventListener('resize', () => this.handleResponsive());

        // Gerencia links ativos
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
        this.sidebar.classList.add('collapsed');
        localStorage.setItem('sidebarCollapsed', 'true');
        
        // Fecha todos os submenus quando colapsa
        const collapses = document.querySelectorAll('.sidebar .collapse.show');
        collapses.forEach(collapse => {
            const bsCollapse = bootstrap.Collapse.getInstance(collapse);
            if (bsCollapse) {
                bsCollapse.hide();
            }
        });
    }

    expand() {
        this.sidebar.classList.remove('collapsed');
        localStorage.setItem('sidebarCollapsed', 'false');
    }

    handleResponsive() {
        const isMobile = window.innerWidth < 992;
        
        if (isMobile) {
            this.sidebar.classList.remove('collapsed');
            this.sidebar.classList.remove('show');
        } else {
            this.sidebar.classList.add('show');
        }
    }

    setActiveLink() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.sidebar .nav-link');
        
        navLinks.forEach(link => {
            link.classList.remove('active');
            
            const href = link.getAttribute('href');
            if (href && currentPath.includes(href)) {
                link.classList.add('active');
                
                // Expande o submenu pai se necessário
                const parentCollapse = link.closest('.collapse');
                if (parentCollapse) {
                    const bsCollapse = new bootstrap.Collapse(parentCollapse, { show: true });
                }
            }
        });
    }
}

// ===== UTILITÁRIOS GERAIS =====
class Utils {
    static formatCurrency(value) {
        return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency: 'BRL'
        }).format(value);
    }

    static formatDate(date) {
        return new Intl.DateTimeFormat('pt-BR').format(new Date(date));
    }

    static formatDateTime(date) {
        return new Intl.DateTimeFormat('pt-BR', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        }).format(new Date(date));
    }

    static showToast(message, type = 'info') {
        // Cria um toast Bootstrap
        const toastContainer = document.getElementById('toast-container') || this.createToastContainer();
        
        const toastId = 'toast-' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: 5000
        });
        
        toast.show();
        
        // Remove o elemento após ser ocultado
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    static createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1055';
        document.body.appendChild(container);
        return container;
    }

    static confirmDelete(message = 'Tem certeza que deseja excluir este item?') {
        return confirm(message);
    }

    static showLoading(element) {
        const originalContent = element.innerHTML;
        element.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Carregando...';
        element.disabled = true;
        
        return () => {
            element.innerHTML = originalContent;
            element.disabled = false;
        };
    }
}

// ===== VALIDAÇÕES DE FORMULÁRIO =====
class FormValidator {
    static validateCPF(cpf) {
        cpf = cpf.replace(/[^\d]+/g, '');
        
        if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) {
            return false;
        }
        
        let sum = 0;
        for (let i = 0; i < 9; i++) {
            sum += parseInt(cpf.charAt(i)) * (10 - i);
        }
        
        let remainder = (sum * 10) % 11;
        if (remainder === 10 || remainder === 11) remainder = 0;
        if (remainder !== parseInt(cpf.charAt(9))) return false;
        
        sum = 0;
        for (let i = 0; i < 10; i++) {
            sum += parseInt(cpf.charAt(i)) * (11 - i);
        }
        
        remainder = (sum * 10) % 11;
        if (remainder === 10 || remainder === 11) remainder = 0;
        
        return remainder === parseInt(cpf.charAt(10));
    }

    static validateCNPJ(cnpj) {
        cnpj = cnpj.replace(/[^\d]+/g, '');
        
        if (cnpj.length !== 14) return false;
        
        if (/^(\d)\1{13}$/.test(cnpj)) return false;
        
        let length = cnpj.length - 2;
        let numbers = cnpj.substring(0, length);
        let digits = cnpj.substring(length);
        let sum = 0;
        let pos = length - 7;
        
        for (let i = length; i >= 1; i--) {
            sum += numbers.charAt(length - i) * pos--;
            if (pos < 2) pos = 9;
        }
        
        let result = sum % 11 < 2 ? 0 : 11 - sum % 11;
        if (result !== parseInt(digits.charAt(0))) return false;
        
        length = length + 1;
        numbers = cnpj.substring(0, length);
        sum = 0;
        pos = length - 7;
        
        for (let i = length; i >= 1; i--) {
            sum += numbers.charAt(length - i) * pos--;
            if (pos < 2) pos = 9;
        }
        
        result = sum % 11 < 2 ? 0 : 11 - sum % 11;
        
        return result === parseInt(digits.charAt(1));
    }

    static formatCPF(value) {
        return value
            .replace(/\D/g, '')
            .replace(/(\d{3})(\d)/, '$1.$2')
            .replace(/(\d{3})(\d)/, '$1.$2')
            .replace(/(\d{3})(\d{1,2})/, '$1-$2')
            .replace(/(-\d{2})\d+?$/, '$1');
    }

    static formatCNPJ(value) {
        return value
            .replace(/\D/g, '')
            .replace(/(\d{2})(\d)/, '$1.$2')
            .replace(/(\d{3})(\d)/, '$1.$2')
            .replace(/(\d{3})(\d)/, '$1/$2')
            .replace(/(\d{4})(\d{1,2})/, '$1-$2')
            .replace(/(-\d{2})\d+?$/, '$1');
    }

    static formatCurrency(value) {
        value = value.replace(/\D/g, '');
        value = (parseInt(value) / 100).toFixed(2);
        return value.replace('.', ',').replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1.');
    }
}

// ===== MÁSCARAS DE INPUT =====
class InputMasks {
    static init() {
        // Máscara para CPF
        document.querySelectorAll('[data-mask="cpf"]').forEach(input => {
            input.addEventListener('input', (e) => {
                e.target.value = FormValidator.formatCPF(e.target.value);
            });
        });

        // Máscara para CNPJ
        document.querySelectorAll('[data-mask="cnpj"]').forEach(input => {
            input.addEventListener('input', (e) => {
                e.target.value = FormValidator.formatCNPJ(e.target.value);
            });
        });

        // Máscara para moeda
        document.querySelectorAll('[data-mask="currency"]').forEach(input => {
            input.addEventListener('input', (e) => {
                e.target.value = FormValidator.formatCurrency(e.target.value);
            });
        });

        // Máscara para telefone
        document.querySelectorAll('[data-mask="phone"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length <= 10) {
                    value = value.replace(/(\d{2})(\d)/, '($1) $2');
                    value = value.replace(/(\d{4})(\d)/, '$1-$2');
                } else {
                    value = value.replace(/(\d{2})(\d)/, '($1) $2');
                    value = value.replace(/(\d{5})(\d)/, '$1-$2');
                }
                e.target.value = value;
            });
        });
    }
}

// ===== INICIALIZAÇÃO =====
document.addEventListener('DOMContentLoaded', function() {
    // Inicializa os gerenciadores
    new ThemeManager();
    new SidebarManager();
    InputMasks.init();
    
    // Inicializa tooltips do Bootstrap
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Inicializa popovers do Bootstrap
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
    
    // Adiciona animação de fade-in aos elementos
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    document.querySelectorAll('.card, .stat-card').forEach(el => {
        observer.observe(el);
    });
    
    // Gerencia cliques em links de confirmação
    document.querySelectorAll('[data-confirm]').forEach(link => {
        link.addEventListener('click', function(e) {
            const message = this.getAttribute('data-confirm');
            if (!Utils.confirmDelete(message)) {
                e.preventDefault();
            }
        });
    });
    
    // Auto-hide alerts após 5 segundos
    document.querySelectorAll('.alert:not(.alert-permanent)').forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});

// ===== FUNÇÕES GLOBAIS =====
window.showToast = Utils.showToast;
window.formatCurrency = Utils.formatCurrency;
window.formatDate = Utils.formatDate;
window.formatDateTime = Utils.formatDateTime;
window.confirmDelete = Utils.confirmDelete;
window.showLoading = Utils.showLoading;

