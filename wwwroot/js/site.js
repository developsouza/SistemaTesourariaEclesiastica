// ===== FUNÇÕES GLOBAIS =====

// Loading Overlay
window.showLoading = (text = 'Processando...') => {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        const loadingText = overlay.querySelector('.loading-text');
        if (loadingText) loadingText.textContent = text;
        overlay.classList.remove('d-none');
    }
};

window.hideLoading = () => {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.classList.add('d-none');
    }
};

// Toast Notifications
window.showToast = (message, type = 'info', duration = 3000) => {
    const container = document.querySelector('.toast-container');
    if (!container) return;

    const iconMap = {
        success: 'bi-check-circle',
        error: 'bi-x-circle',
        warning: 'bi-exclamation-triangle',
        info: 'bi-info-circle'
    };

    const bgMap = {
        success: 'bg-success',
        error: 'bg-danger',
        warning: 'bg-warning',
        info: 'bg-info'
    };

    const toastId = `toast-${Date.now()}`;
    const toast = document.createElement('div');
    toast.className = 'toast align-items-center text-white border-0';
    toast.classList.add(bgMap[type] || 'bg-info');
    toast.id = toastId;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body d-flex align-items-center">
                <i class="bi ${iconMap[type] || 'bi-info-circle'} me-2 fs-5"></i>
                <span>${message}</span>
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    container.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast, { delay: duration });
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => toast.remove());
};

// Confirm Dialog
window.confirmAction = async (title, message, type = 'warning') => {
    return Swal.fire({
        title: title,
        text: message,
        icon: type,
        showCancelButton: true,
        confirmButtonColor: '#2563eb',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Sim, confirmar!',
        cancelButtonText: 'Cancelar',
        reverseButtons: true
    });
};

// Copiar para Clipboard
window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        showToast('Copiado para área de transferência', 'success', 2000);
    } catch {
        showToast('Erro ao copiar', 'error', 2000);
    }
};

// Debounce
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

// ===== GERENCIADOR DE SIDEBAR =====
class SidebarManager {
    constructor() {
        this.sidebar = document.getElementById('sidebar');
        this.content = document.getElementById('content');
        this.backdrop = null;
        this.tempExpandTimeout = null;
        this.autoCollapseTimeout = null;
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

        // Gerenciar cliques em submenus quando recolhido
        this.setupCollapsedSubmenuBehavior();
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
        this.sidebar.classList.remove('temp-expanded');
        this.content?.classList.add('expanded');
        localStorage.setItem('sidebarCollapsed', 'true');

        // Fechar todos os submenus
        this.closeAllSubmenus();
    }

    expand() {
        this.sidebar.classList.remove('collapsed');
        this.sidebar.classList.remove('temp-expanded');
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
                    if (parent) {
                        parent.classList.add('show');
                    }
                }
            }
        });
    }

    setupCollapsedSubmenuBehavior() {
        // Detectar cliques em itens com submenu quando sidebar está recolhido
        document.querySelectorAll('.has-submenu > .nav-link').forEach(menuLink => {
            menuLink.addEventListener('click', (e) => {
                const parent = menuLink.closest('.has-submenu');

                // Se sidebar está recolhido no desktop
                if (this.sidebar.classList.contains('collapsed') && window.innerWidth > 991) {
                    e.preventDefault();
                    this.temporaryExpand(parent);
                }
            });
        });

        // Fechar sidebar recolhido ao clicar em links de submenu
        document.querySelectorAll('.submenu .nav-link').forEach(submenuLink => {
            submenuLink.addEventListener('click', () => {
                if (this.sidebar.classList.contains('collapsed') &&
                    this.sidebar.classList.contains('temp-expanded') &&
                    window.innerWidth > 991) {
                    // Auto-colapsar após um pequeno delay para permitir navegação
                    setTimeout(() => {
                        this.autoCollapse();
                    }, 150);
                }
            });
        });

        // Fechar expansão temporária ao clicar fora do sidebar
        document.addEventListener('click', (e) => {
            if (this.sidebar.classList.contains('collapsed') &&
                this.sidebar.classList.contains('temp-expanded') &&
                !this.sidebar.contains(e.target) &&
                window.innerWidth > 991) {
                this.autoCollapse();
            }
        });
    }

    temporaryExpand(menuItem) {
        // Limpar timeouts anteriores
        clearTimeout(this.autoCollapseTimeout);

        // Adicionar classe de expansão temporária
        this.sidebar.classList.add('temp-expanded');

        // Fechar todos os submenus
        this.closeAllSubmenus();

        // Abrir o submenu clicado após um pequeno delay para animação
        setTimeout(() => {
            if (menuItem) {
                menuItem.classList.add('show');
                const submenu = menuItem.querySelector('.submenu');
                if (submenu) {
                    submenu.classList.add('show');
                }
            }
        }, 50);

        // Auto-colapsar após 10 segundos se não houver interação
        this.autoCollapseTimeout = setTimeout(() => {
            this.autoCollapse();
        }, 10000);
    }

    autoCollapse() {
        // Fechar submenus
        this.closeAllSubmenus();

        // Remover expansão temporária
        this.sidebar.classList.remove('temp-expanded');

        // Limpar timeout
        clearTimeout(this.autoCollapseTimeout);
    }

    closeAllSubmenus() {
        document.querySelectorAll('.has-submenu.show').forEach(menu => {
            menu.classList.remove('show');
            const submenu = menu.querySelector('.submenu');
            if (submenu) {
                submenu.classList.remove('show');
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
                const sidebar = document.getElementById('sidebar');

                // Se sidebar está recolhido no desktop, não processar aqui
                // (será tratado pelo SidebarManager)
                if (sidebar && sidebar.classList.contains('collapsed') && window.innerWidth > 991) {
                    return;
                }

                e.preventDefault();

                const parent = item.closest('.has-submenu');
                const submenu = parent.querySelector('.submenu');

                if (submenu) {
                    const isShowing = parent.classList.contains('show');

                    // Fechar outros submenus
                    document.querySelectorAll('.has-submenu.show').forEach(other => {
                        if (other !== parent) {
                            other.classList.remove('show');
                            const otherSubmenu = other.querySelector('.submenu');
                            if (otherSubmenu) {
                                otherSubmenu.classList.remove('show');
                            }
                        }
                    });

                    // Toggle atual
                    if (isShowing) {
                        parent.classList.remove('show');
                        submenu.classList.remove('show');
                    } else {
                        parent.classList.add('show');
                        submenu.classList.add('show');
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
            // Não interceptar formulários que já tem seu próprio handler
            if (form.hasAttribute('data-ajax') || form.id === 'formDevolucaoDetails') {
                return;
            }

            form.addEventListener('submit', (e) => {
                if (form.checkValidity()) {
                    showLoading('Enviando...');
                }
            });
        });
    }

    setupCPFMask() {
        document.querySelectorAll('input[data-mask="cpf"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{3})(\d)/, '$1.$2');
                value = value.replace(/(\d{3})(\d{1,2})$/, '$1-$2');
                e.target.value = value.substring(0, 14);
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
                e.target.value = value.substring(0, 15);
            });
        });
    }

    setupCEPMask() {
        document.querySelectorAll('input[data-mask="cep"]').forEach(input => {
            input.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                value = value.replace(/(\d{5})(\d)/, '$1-$2');
                e.target.value = value.substring(0, 9);
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
                document.querySelector('[name="Endereco"]')?.setAttribute('value', data.logradouro);
                document.querySelector('[name="Bairro"]')?.setAttribute('value', data.bairro);
                document.querySelector('[name="Cidade"]')?.setAttribute('value', data.localidade);
                document.querySelector('[name="Estado"]')?.setAttribute('value', data.uf);
                showToast('CEP encontrado!', 'success', 2000);
            } else {
                showToast('CEP não encontrado', 'warning', 2000);
            }
        } catch (error) {
            showToast('Erro ao buscar CEP', 'error', 2000);
            console.error('Erro ao buscar CEP:', error);
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
        this.setupRealTimeValidation();
    }

    setupRealTimeValidation() {
        document.querySelectorAll('.form-control, .form-select').forEach(input => {
            input.addEventListener('blur', () => {
                if (input.hasAttribute('required') && !input.value.trim()) {
                    input.classList.add('is-invalid');
                    input.classList.remove('is-valid');
                } else if (input.value.trim()) {
                    input.classList.remove('is-invalid');
                    input.classList.add('is-valid');
                }
            });

            input.addEventListener('input', () => {
                if (input.classList.contains('is-invalid') && input.value.trim()) {
                    input.classList.remove('is-invalid');
                    input.classList.add('is-valid');
                }
            });
        });
    }
}

// ===== GERENCIADOR DE TABELAS =====
class TableManager {
    constructor() {
        this.init();
    }

    init() {
        this.setupDataTables();
        this.setupSortableTables();
    }

    setupDataTables() {
        if (typeof $.fn.dataTable !== 'undefined') {
            $('.table-datatable').each(function () {
                if (!$.fn.DataTable.isDataTable(this)) {
                    $(this).DataTable({
                        responsive: true,
                        language: {
                            url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/pt-BR.json'
                        },
                        pageLength: 25,
                        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "Todos"]],
                        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                            '<"row"<"col-sm-12"tr>>' +
                            '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
                        drawCallback: function () {
                            hideLoading();
                        }
                    });
                }
            });
        }
    }

    setupSortableTables() {
        document.querySelectorAll('.table-sortable thead th[data-sort]').forEach(header => {
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
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
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
        // Remover configuração global que pode causar conflitos
        // Cada formulário agora gerencia seu próprio token

        // Apenas adicionar header para requests que não tenham token no data
        $.ajaxSetup({
            beforeSend: function (xhr, settings) {
                // Não interferir se já tem o token nos dados ou é cross-domain
                if (settings.crossDomain) {
                    return;
                }

                // Se a request não tem token no data, pegar do meta tag
                // Verificar se data é string antes de usar includes
                const hasTokenInData = settings.data && 
                    typeof settings.data === 'string' && 
                    settings.data.includes('__RequestVerificationToken');
                
                if (!hasTokenInData) {
                    const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
                    if (token) {
                        xhr.setRequestHeader("X-CSRF-Token", token);
                    }
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

    document.querySelectorAll('.card, .stat-card').forEach(el => observer.observe(el));
});

// Tratamento global de erros
window.addEventListener('error', (e) => {
    console.error('Erro:', e.error);
    hideLoading();
});

// Log de sucesso
console.log('[OK] Sistema de Tesouraria carregado com sucesso!');

// Versão: 1.0.2 - Encoding fix