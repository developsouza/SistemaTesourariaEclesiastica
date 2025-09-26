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

    const iconClass = {
        'success': 'bi-check-circle-fill',
        'error': 'bi-x-circle-fill',
        'warning': 'bi-exclamation-triangle-fill',
        'info': 'bi-info-circle-fill'
    }[type] || 'bi-info-circle-fill';

    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi ${iconClass} me-2"></i>
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
                this.showThemeChangeToast(theme);
            });
        });

        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            const savedTheme = localStorage.getItem('theme');
            if (savedTheme === 'auto') {
                this.setTheme('auto');
            }
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

    showThemeChangeToast(theme) {
        const messages = {
            'light': 'Tema claro ativado',
            'dark': 'Tema escuro ativado',
            'auto': 'Tema automático ativado'
        };
        showToast(messages[theme] || 'Tema alterado', 'info', 2000);
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

        if (window.innerWidth <= 991) {
            const navLinks = document.querySelectorAll('.sidebar .nav-link');
            navLinks.forEach(link => {
                link.addEventListener('click', () => {
                    if (this.sidebar.classList.contains('show')) {
                        this.sidebar.classList.remove('show');
                    }
                });
            });
        }

        this.handleResponsive();
        window.addEventListener('resize', () => this.handleResponsive());
        this.setActiveLink();
    }

    toggle() {
        if (window.innerWidth <= 991) {
            this.sidebar.classList.toggle('show');
        } else {
            if (this.sidebar.classList.contains('collapsed')) {
                this.expand();
            } else {
                this.collapse();
            }
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

                const isOpen = submenu.classList.contains('show');

                document.querySelectorAll('.submenu.show').forEach(sub => {
                    if (sub !== submenu) {
                        sub.classList.remove('show');
                        const parentArrow = sub.parentElement.querySelector('.submenu-arrow');
                        if (parentArrow) {
                            parentArrow.style.transform = 'rotate(0deg)';
                        }
                    }
                });

                if (isOpen) {
                    submenu.classList.remove('show');
                    if (arrow) {
                        arrow.style.transform = 'rotate(0deg)';
                    }
                } else {
                    setTimeout(() => {
                        submenu.classList.add('show');
                        if (arrow) {
                            arrow.style.transform = 'rotate(90deg)';
                        }
                    }, 50);
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

// ===== VALIDAÇÕES EM TEMPO REAL (CONSOLIDADO) =====
class ValidationManager {
    constructor() {
        this.init();
    }

    init() {
        this.initCPFValidation();
        this.initCNPJValidation();
        this.initMoneyValidation();
        this.initPhoneValidation();
        this.initCEPValidation();
        this.initDateValidation();
    }

    initCPFValidation() {
        const cpfInputs = document.querySelectorAll('input[data-val-cpf]');
        cpfInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                const cpf = e.target.value.replace(/\D/g, '');

                if (cpf.length <= 11) {
                    e.target.value = cpf.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
                }

                if (cpf.length === 11) {
                    const isValid = this.validateCPF(cpf);
                    this.toggleValidationFeedback(e.target, isValid, 'CPF inválido');
                } else if (cpf.length > 0) {
                    this.toggleValidationFeedback(e.target, false, 'CPF deve ter 11 dígitos');
                } else {
                    this.clearValidationFeedback(e.target);
                }
            });
        });
    }

    initCNPJValidation() {
        const cnpjInputs = document.querySelectorAll('input[data-val-cnpj]');
        cnpjInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                const cnpj = e.target.value.replace(/\D/g, '');

                if (cnpj.length <= 14) {
                    e.target.value = cnpj.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
                }

                if (cnpj.length === 14) {
                    const isValid = this.validateCNPJ(cnpj);
                    this.toggleValidationFeedback(e.target, isValid, 'CNPJ inválido');
                } else if (cnpj.length > 0) {
                    this.toggleValidationFeedback(e.target, false, 'CNPJ deve ter 14 dígitos');
                } else {
                    this.clearValidationFeedback(e.target);
                }
            });
        });
    }

    initMoneyValidation() {
        const moneyInputs = document.querySelectorAll('input[data-val-money]');
        moneyInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');

                if (value.length > 0) {
                    value = (parseInt(value) / 100).toFixed(2);
                    e.target.value = 'R$ ' + value.replace('.', ',').replace(/\B(?=(\d{3})+(?!\d))/g, '.');

                    const numericValue = parseFloat(value);
                    const min = parseFloat(e.target.getAttribute('data-val-money-min') || '0.01');
                    const max = parseFloat(e.target.getAttribute('data-val-money-max') || '999999.99');

                    const isValid = numericValue >= min && numericValue <= max;
                    this.toggleValidationFeedback(e.target, isValid,
                        `Valor deve estar entre R$ ${min.toFixed(2)} e R$ ${max.toFixed(2)}`);
                } else {
                    this.clearValidationFeedback(e.target);
                }
            });
        });
    }

    initPhoneValidation() {
        const phoneInputs = document.querySelectorAll('input[data-val-phone]');
        phoneInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                let phone = e.target.value.replace(/\D/g, '');

                if (phone.length <= 11) {
                    if (phone.length <= 10) {
                        e.target.value = phone.replace(/(\d{2})(\d{4})(\d{4})/, '($1) $2-$3');
                    } else {
                        e.target.value = phone.replace(/(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
                    }
                }

                if (phone.length >= 10 && phone.length <= 11) {
                    this.toggleValidationFeedback(e.target, true, '');
                } else if (phone.length > 0) {
                    this.toggleValidationFeedback(e.target, false, 'Telefone deve ter 10 ou 11 dígitos');
                } else {
                    this.clearValidationFeedback(e.target);
                }
            });
        });
    }

    initCEPValidation() {
        const cepInputs = document.querySelectorAll('input[data-val-cep]');
        cepInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                let cep = e.target.value.replace(/\D/g, '');

                if (cep.length <= 8) {
                    e.target.value = cep.replace(/(\d{5})(\d{3})/, '$1-$2');
                }

                if (cep.length === 8) {
                    this.toggleValidationFeedback(e.target, true, '');
                    this.searchCEP(cep);
                } else if (cep.length > 0) {
                    this.toggleValidationFeedback(e.target, false, 'CEP deve ter 8 dígitos');
                } else {
                    this.clearValidationFeedback(e.target);
                }
            });
        });
    }

    initDateValidation() {
        const dateInputs = document.querySelectorAll('input[type="date"][data-val-date]');
        dateInputs.forEach(input => {
            input.addEventListener('change', (e) => {
                const inputDate = new Date(e.target.value);
                const today = new Date();

                const allowFuture = e.target.getAttribute('data-val-date-future') === 'true';
                const allowPast = e.target.getAttribute('data-val-date-past') === 'true';
                const maxPastDays = parseInt(e.target.getAttribute('data-val-date-max-past') || '3650');
                const maxFutureDays = parseInt(e.target.getAttribute('data-val-date-max-future') || '365');

                let isValid = true;
                let message = '';

                if (inputDate > today && !allowFuture) {
                    isValid = false;
                    message = 'Data não pode ser futura';
                } else if (inputDate < today && !allowPast) {
                    isValid = false;
                    message = 'Data não pode ser passada';
                } else if (inputDate < today) {
                    const daysDiff = Math.floor((today - inputDate) / (1000 * 60 * 60 * 24));
                    if (daysDiff > maxPastDays) {
                        isValid = false;
                        message = `Data não pode ser mais de ${maxPastDays} dias no passado`;
                    }
                } else if (inputDate > today) {
                    const daysDiff = Math.floor((inputDate - today) / (1000 * 60 * 60 * 24));
                    if (daysDiff > maxFutureDays) {
                        isValid = false;
                        message = `Data não pode ser mais de ${maxFutureDays} dias no futuro`;
                    }
                }

                this.toggleValidationFeedback(e.target, isValid, message);
            });
        });
    }

    validateCPF(cpf) {
        if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) {
            return false;
        }

        let sum = 0;
        for (let i = 0; i < 9; i++) {
            sum += parseInt(cpf.charAt(i)) * (10 - i);
        }
        let remainder = sum % 11;
        let digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (parseInt(cpf.charAt(9)) !== digit1) {
            return false;
        }

        sum = 0;
        for (let i = 0; i < 10; i++) {
            sum += parseInt(cpf.charAt(i)) * (11 - i);
        }
        remainder = sum % 11;
        let digit2 = remainder < 2 ? 0 : 11 - remainder;

        return parseInt(cpf.charAt(10)) === digit2;
    }

    validateCNPJ(cnpj) {
        if (cnpj.length !== 14 || /^(\d)\1{13}$/.test(cnpj)) {
            return false;
        }

        const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        let sum = 0;
        for (let i = 0; i < 12; i++) {
            sum += parseInt(cnpj.charAt(i)) * weights1[i];
        }
        let remainder = sum % 11;
        let digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (parseInt(cnpj.charAt(12)) !== digit1) {
            return false;
        }

        sum = 0;
        for (let i = 0; i < 13; i++) {
            sum += parseInt(cnpj.charAt(i)) * weights2[i];
        }
        remainder = sum % 11;
        let digit2 = remainder < 2 ? 0 : 11 - remainder;

        return parseInt(cnpj.charAt(13)) === digit2;
    }

    async searchCEP(cep) {
        try {
            showLoading('Buscando CEP...');
            const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
            const data = await response.json();

            if (!data.erro) {
                const logradouroField = document.querySelector('input[name*="Logradouro"], input[name*="Endereco"]');
                const bairroField = document.querySelector('input[name*="Bairro"]');
                const cidadeField = document.querySelector('input[name*="Cidade"]');
                const ufField = document.querySelector('input[name*="UF"], select[name*="UF"]');

                if (logradouroField && data.logradouro) logradouroField.value = data.logradouro;
                if (bairroField && data.bairro) bairroField.value = data.bairro;
                if (cidadeField && data.localidade) cidadeField.value = data.localidade;
                if (ufField && data.uf) ufField.value = data.uf;

                showToast('CEP encontrado com sucesso!', 'success', 2000);
            } else {
                showToast('CEP não encontrado', 'error', 3000);
            }
        } catch (error) {
            console.error('Erro ao buscar CEP:', error);
            showToast('Erro ao buscar CEP', 'error', 3000);
        } finally {
            hideLoading();
        }
    }

    toggleValidationFeedback(input, isValid, message) {
        const feedbackElement = input.parentElement.querySelector('.invalid-feedback') ||
            input.parentElement.querySelector('.valid-feedback');

        if (isValid) {
            input.classList.remove('is-invalid');
            input.classList.add('is-valid');
            if (feedbackElement) {
                feedbackElement.style.display = 'none';
            }
        } else {
            input.classList.remove('is-valid');
            input.classList.add('is-invalid');

            if (feedbackElement) {
                feedbackElement.textContent = message;
                feedbackElement.className = 'invalid-feedback';
                feedbackElement.style.display = 'block';
            } else {
                const newFeedback = document.createElement('div');
                newFeedback.className = 'invalid-feedback';
                newFeedback.textContent = message;
                newFeedback.style.display = 'block';
                input.parentElement.appendChild(newFeedback);
            }
        }
    }

    clearValidationFeedback(input) {
        input.classList.remove('is-valid', 'is-invalid');
        const feedbackElement = input.parentElement.querySelector('.invalid-feedback') ||
            input.parentElement.querySelector('.valid-feedback');
        if (feedbackElement) {
            feedbackElement.style.display = 'none';
        }
    }
}

// ===== GERENCIADOR DE FORMULÁRIOS =====
class FormManager {
    constructor() {
        this.init();
    }

    init() {
        this.preventDoubleSubmit();
        this.trackFormChanges();
        this.initializeTooltips();
        this.initializePopovers();
    }

    preventDoubleSubmit() {
        document.addEventListener('submit', (e) => {
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
    }

    trackFormChanges() {
        const forms = document.querySelectorAll('form[data-warn-unsaved]');
        forms.forEach(form => {
            const inputs = form.querySelectorAll('input, textarea, select');
            inputs.forEach(input => {
                input.dataset.originalValue = input.value;
            });
        });

        window.addEventListener('beforeunload', (e) => {
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
    }

    initializeTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
    }

    initializePopovers() {
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(popoverTriggerEl => new bootstrap.Popover(popoverTriggerEl));
    }
}

// ===== GERENCIADOR DE TABELAS =====
class TableManager {
    constructor() {
        this.init();
    }

    init() {
        this.initSearchInTable();
        this.initTableSort();
    }

    initSearchInTable() {
        const searchInputs = document.querySelectorAll('[data-table-search]');
        searchInputs.forEach(input => {
            const tableId = input.getAttribute('data-table-search');
            const table = document.getElementById(tableId);

            if (table) {
                input.addEventListener('input', (e) => {
                    const searchTerm = e.target.value.toLowerCase();
                    const rows = table.querySelectorAll('tbody tr');

                    rows.forEach(row => {
                        const text = row.textContent.toLowerCase();
                        row.style.display = text.includes(searchTerm) ? '' : 'none';
                    });
                });
            }
        });
    }

    initTableSort() {
        const sortableHeaders = document.querySelectorAll('th[data-sortable]');
        sortableHeaders.forEach(header => {
            header.style.cursor = 'pointer';
            header.innerHTML += ' <i class="bi bi-arrow-down-up ms-1"></i>';

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

                    if (newDirection === 'asc') {
                        return aValue.localeCompare(bValue, 'pt-BR', { numeric: true });
                    } else {
                        return bValue.localeCompare(aValue, 'pt-BR', { numeric: true });
                    }
                });

                rows.forEach(row => tbody.appendChild(row));
                header.setAttribute('data-sort-direction', newDirection);

                const icon = header.querySelector('i');
                icon.className = newDirection === 'asc' ? 'bi bi-arrow-up ms-1' : 'bi bi-arrow-down ms-1';
            });
        });
    }
}

// ===== INICIALIZAÇÃO GLOBAL =====
document.addEventListener('DOMContentLoaded', function () {
    // Inicializar gerenciadores
    new ThemeManager();
    new FormManager();
    new TableManager();
    new ValidationManager();

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

    // Configuração global para requisições AJAX (se jQuery estiver disponível)
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

    // Animação suave nos elementos
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

    document.querySelectorAll('.card, .alert').forEach(el => {
        observer.observe(el);
    });
});

// ===== TRATAMENTO GLOBAL DE ERROS =====
window.addEventListener('error', (e) => {
    console.error('Erro JavaScript:', e.error);
    hideLoading();
});

// ===== FUNÇÕES UTILITÁRIAS GLOBAIS =====
window.formatCurrency = function (value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
};

window.formatDate = function (date) {
    return new Intl.DateTimeFormat('pt-BR').format(new Date(date));
};

window.formatDateTime = function (date) {
    return new Intl.DateTimeFormat('pt-BR', {
        dateStyle: 'short',
        timeStyle: 'short'
    }).format(new Date(date));
};

window.copyToClipboard = function (text) {
    navigator.clipboard.writeText(text).then(() => {
        showToast('Copiado para área de transferência', 'success', 2000);
    }).catch(err => {
        console.error('Erro ao copiar:', err);
        showToast('Erro ao copiar', 'error', 2000);
    });
};

// ===== DEBOUNCE UTILITY =====
window.debounce = function (func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
};

console.log('✨ Sistema carregado com sucesso!');