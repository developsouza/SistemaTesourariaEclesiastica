/* ========================================
   ESCALA MANUAL - DRAG AND DROP LOGIC
   ======================================== */

(function () {
    'use strict';

    console.log('[EscalaManual] Iniciando script');

    // Estado global
    let porteiroDragging = null;

    // Inicialização
    function init() {
        console.log('[EscalaManual] Configurando eventos de drag and drop');

        setupDragAndDrop();
        setupRemoverButtons();
        setupSalvarButton();
        setupLimparButton();
        atualizarContadores();
        verificarCompletude();

        console.log('[EscalaManual] Script inicializado com sucesso');
    }

    // ===== DRAG AND DROP =====
    function setupDragAndDrop() {
        // Configurar itens de porteiros (draggable)
        const porteirosItems = document.querySelectorAll('.porteiro-item');
        porteirosItems.forEach(item => {
            item.addEventListener('dragstart', handleDragStart);
            item.addEventListener('dragend', handleDragEnd);
        });

        // Configurar zonas de drop
        const dropZones = document.querySelectorAll('.porteiros-drop-zone');
        dropZones.forEach(zone => {
            zone.addEventListener('dragover', handleDragOver);
            zone.addEventListener('dragleave', handleDragLeave);
            zone.addEventListener('drop', handleDrop);
        });

        console.log(`[EscalaManual] ${porteirosItems.length} porteiros e ${dropZones.length} zonas de drop configuradas`);
    }

    function handleDragStart(e) {
        const item = e.target.closest('.porteiro-item');
        if (!item) return;

        porteiroDragging = {
            id: item.getAttribute('data-porteiro-id'),
            nome: item.getAttribute('data-porteiro-nome')
        };

        item.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'copy';
        e.dataTransfer.setData('text/plain', JSON.stringify(porteiroDragging));

        console.log('[EscalaManual] Iniciando drag:', porteiroDragging);
    }

    function handleDragEnd(e) {
        const item = e.target.closest('.porteiro-item');
        if (item) {
            item.classList.remove('dragging');
        }
        porteiroDragging = null;
    }

    function handleDragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';

        const dropZone = e.currentTarget;
        const diaCard = dropZone.closest('.dia-card');

        if (!diaCard) return;

        // Verificar se já atingiu o limite
        const qtdNecessaria = parseInt(diaCard.getAttribute('data-qtd-necessaria'));
        const porteirosAtuais = dropZone.querySelectorAll('.porteiro-atribuido').length;

        if (porteirosAtuais >= qtdNecessaria) {
            dropZone.classList.add('limite-atingido');
            dropZone.classList.remove('drag-over');
            e.dataTransfer.dropEffect = 'none';
        } else {
            dropZone.classList.add('drag-over');
            dropZone.classList.remove('limite-atingido');
        }
    }

    function handleDragLeave(e) {
        const dropZone = e.currentTarget;
        dropZone.classList.remove('drag-over', 'limite-atingido');
    }

    function handleDrop(e) {
        e.preventDefault();

        const dropZone = e.currentTarget;
        dropZone.classList.remove('drag-over', 'limite-atingido');

        const diaCard = dropZone.closest('.dia-card');
        if (!diaCard) return;

        // Verificar limite
        const qtdNecessaria = parseInt(diaCard.getAttribute('data-qtd-necessaria'));
        const porteirosAtuais = dropZone.querySelectorAll('.porteiro-atribuido').length;

        if (porteirosAtuais >= qtdNecessaria) {
            mostrarAlerta('Este dia já tem o número máximo de porteiros necessários.', 'warning');
            return;
        }

        // Obter dados do porteiro
        let porteiroData;
        try {
            porteiroData = JSON.parse(e.dataTransfer.getData('text/plain'));
        } catch (err) {
            console.error('[EscalaManual] Erro ao parsear dados:', err);
            return;
        }

        if (!porteiroData || !porteiroData.id) {
            console.error('[EscalaManual] Dados do porteiro inválidos');
            return;
        }

        // Verificar se o porteiro já está atribuído neste dia
        const jaAtribuido = Array.from(dropZone.querySelectorAll('.porteiro-atribuido'))
            .some(el => el.getAttribute('data-porteiro-id') === porteiroData.id);

        if (jaAtribuido) {
            mostrarAlerta('Este porteiro já está atribuído a este dia.', 'warning');
            return;
        }

        // Adicionar porteiro ao dia
        adicionarPorteiroAoDia(dropZone, porteiroData, porteirosAtuais + 1);

        console.log(`[EscalaManual] Porteiro ${porteiroData.nome} adicionado ao dia`);

        // Atualizar UI
        atualizarContadores();
        verificarCompletude();
    }

    // ===== ADICIONAR PORTEIRO =====
    function adicionarPorteiroAoDia(dropZone, porteiroData, posicao) {
        const porteiroHtml = `
            <div class="porteiro-atribuido mb-2 p-2 bg-success bg-opacity-10 border border-success rounded position-relative"
                 data-porteiro-id="${porteiroData.id}"
                 data-porteiro-nome="${porteiroData.nome}"
                 data-posicao="${posicao}">
                <div class="d-flex align-items-center">
                    <span class="badge bg-success me-2">${posicao}</span>
                    <div class="flex-grow-1">
                        <div class="fw-semibold text-success">${porteiroData.nome}</div>
                    </div>
                    <button type="button" class="btn btn-sm btn-danger btn-remover-porteiro" 
                            title="Remover porteiro">
                        <i class="bi bi-x-lg"></i>
                    </button>
                </div>
            </div>
        `;

        dropZone.insertAdjacentHTML('beforeend', porteiroHtml);

        // Configurar botão de remover
        const novoPorteiro = dropZone.lastElementChild;
        const btnRemover = novoPorteiro.querySelector('.btn-remover-porteiro');
        btnRemover.addEventListener('click', handleRemoverPorteiro);
    }

    // ===== REMOVER PORTEIRO =====
    function setupRemoverButtons() {
        const buttons = document.querySelectorAll('.btn-remover-porteiro');
        buttons.forEach(btn => {
            btn.addEventListener('click', handleRemoverPorteiro);
        });
    }

    function handleRemoverPorteiro(e) {
        const button = e.currentTarget;
        const porteiroAtribuido = button.closest('.porteiro-atribuido');
        const dropZone = button.closest('.porteiros-drop-zone');

        if (!porteiroAtribuido || !dropZone) return;

        const nomePorteiro = porteiroAtribuido.getAttribute('data-porteiro-nome');

        // Remover com animação
        porteiroAtribuido.style.opacity = '0';
        porteiroAtribuido.style.transform = 'scale(0.8)';

        setTimeout(() => {
            porteiroAtribuido.remove();

            // Reordenar posições
            reordenarPosicoes(dropZone);

            // Atualizar UI
            atualizarContadores();
            verificarCompletude();

            console.log(`[EscalaManual] Porteiro ${nomePorteiro} removido`);
        }, 200);
    }

    function reordenarPosicoes(dropZone) {
        const porteiros = dropZone.querySelectorAll('.porteiro-atribuido');
        porteiros.forEach((porteiro, index) => {
            const posicao = index + 1;
            porteiro.setAttribute('data-posicao', posicao);
            const badge = porteiro.querySelector('.badge');
            if (badge) {
                badge.textContent = posicao;
            }
        });
    }

    // ===== ATUALIZAR CONTADORES =====
    function atualizarContadores() {
        const diaCards = document.querySelectorAll('.dia-card');
        diaCards.forEach(card => {
            const dropZone = card.querySelector('.porteiros-drop-zone');
            if (!dropZone) return;

            const porteirosAtuais = dropZone.querySelectorAll('.porteiro-atribuido').length;
            const contadorSpan = card.querySelector('.contador-porteiros');

            if (contadorSpan) {
                contadorSpan.textContent = porteirosAtuais;
            }
        });
    }

    // ===== VERIFICAR COMPLETUDE =====
    function verificarCompletude() {
        const diaCards = document.querySelectorAll('.dia-card');
        diaCards.forEach(card => {
            const dropZone = card.querySelector('.porteiros-drop-zone');
            if (!dropZone) return;

            const qtdNecessaria = parseInt(card.getAttribute('data-qtd-necessaria'));
            const porteirosAtuais = dropZone.querySelectorAll('.porteiro-atribuido').length;

            card.classList.remove('completo', 'incompleto', 'precisa-atencao');

            if (porteirosAtuais === qtdNecessaria) {
                card.classList.add('completo');
            } else if (porteirosAtuais > 0) {
                card.classList.add('incompleto', 'precisa-atencao');
            }
        });
    }

    // ===== SALVAR ESCALA =====
    function setupSalvarButton() {
        const btnSalvar = document.getElementById('btnSalvarEscala');
        if (!btnSalvar) return;

        btnSalvar.addEventListener('click', handleSalvarEscala);
    }

    // ===== LIMPAR ESCALA =====
    function setupLimparButton() {
        const btnLimpar = document.getElementById('btnLimparEscala');
        if (!btnLimpar) return;

        btnLimpar.addEventListener('click', handleLimparEscala);
    }

    function handleLimparEscala() {
        console.log('[EscalaManual] Iniciando limpeza da escala');

        // Contar porteiros atribuídos
        const porteirosAtribuidos = document.querySelectorAll('.porteiro-atribuido');
        const totalAtribuidos = porteirosAtribuidos.length;

        if (totalAtribuidos === 0) {
            mostrarAlerta('Não há porteiros atribuídos para remover.', 'info');
            return;
        }

        // Confirmar ação
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: 'Limpar Escala?',
                text: `Isso removerá todos os ${totalAtribuidos} porteiro(s) atribuído(s). Esta ação não pode ser desfeita.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#ffc107',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Sim, limpar tudo!',
                cancelButtonText: 'Cancelar',
                reverseButtons: true
            }).then((result) => {
                if (result.isConfirmed) {
                    executarLimpeza(porteirosAtribuidos);
                    Swal.fire({
                        icon: 'success',
                        title: 'Escala Limpa!',
                        text: `${totalAtribuidos} porteiro(s) removido(s) com sucesso.`,
                        timer: 2000,
                        showConfirmButton: false
                    });
                }
            });
        } else {
            // Fallback para confirm nativo
            const confirmar = confirm(`Deseja remover todos os ${totalAtribuidos} porteiro(s) atribuído(s)?\n\nEsta ação não pode ser desfeita.`);
            if (confirmar) {
                executarLimpeza(porteirosAtribuidos);
                alert(`${totalAtribuidos} porteiro(s) removido(s) com sucesso.`);
            }
        }
    }

    function executarLimpeza(porteirosAtribuidos) {
        console.log(`[EscalaManual] Removendo ${porteirosAtribuidos.length} porteiros atribuídos`);

        // Animar e remover cada porteiro
        porteirosAtribuidos.forEach((porteiro, index) => {
            setTimeout(() => {
                porteiro.style.opacity = '0';
                porteiro.style.transform = 'scale(0.8) translateX(-20px)';

                setTimeout(() => {
                    porteiro.remove();
                }, 200);
            }, index * 50); // Animação em cascata
        });

        // Aguardar todas as animações e atualizar UI
        setTimeout(() => {
            atualizarContadores();
            verificarCompletude();
            console.log('[EscalaManual] Limpeza concluída com sucesso');
        }, (porteirosAtribuidos.length * 50) + 300);
    }

    async function handleSalvarEscala() {
        console.log('[EscalaManual] Iniciando salvamento da escala');

        // Validar se todos os dias estão completos
        const diaCards = document.querySelectorAll('.dia-card');
        const escalas = [];
        let temErros = false;

        for (const card of diaCards) {
            const dropZone = card.querySelector('.porteiros-drop-zone');
            if (!dropZone) continue;

            const data = card.getAttribute('data-dia-date');
            const horarioStr = card.getAttribute('data-dia-horario');
            const tipoCulto = parseInt(card.getAttribute('data-dia-tipo'));
            const qtdNecessaria = parseInt(card.getAttribute('data-qtd-necessaria'));

            const porteiros = dropZone.querySelectorAll('.porteiro-atribuido');

            // Pular dias sem porteiros atribuídos
            if (porteiros.length === 0) {
                continue;
            }

            // Validar quantidade mínima
            if (porteiros.length < qtdNecessaria) {
                card.classList.add('validation-error');
                temErros = true;
                continue;
            }

            card.classList.remove('validation-error');
            card.classList.add('validation-success');

            const porteiroIds = Array.from(porteiros).map(p => parseInt(p.getAttribute('data-porteiro-id')));

            escalas.push({
                Data: data,
                Horario: horarioStr || null,
                TipoCulto: tipoCulto,
                PorteiroId: porteiroIds[0] || null,
                Porteiro2Id: porteiroIds[1] || null,
                Observacao: null
            });
        }

        if (temErros) {
            mostrarAlerta('Alguns dias não têm a quantidade necessária de porteiros. Por favor, complete a escala antes de salvar.', 'danger');
            return;
        }

        if (escalas.length === 0) {
            mostrarAlerta('Nenhum porteiro foi atribuído. Atribua porteiros aos dias antes de salvar.', 'warning');
            return;
        }

        // Preparar payload
        const payload = {
            DataInicio: dataInicio,
            DataFim: dataFim,
            ResponsavelId: parseInt(responsavelId),
            Escalas: escalas
        };

        console.log('[EscalaManual] Payload:', payload);

        // Enviar para o servidor
        try {
            mostrarLoading(true);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch('/EscalasPorteiros/SalvarEscalaManual', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            mostrarLoading(false);

            if (result.success) {
                mostrarAlerta(result.message, 'success');

                setTimeout(() => {
                    if (result.redirectUrl) {
                        window.location.href = result.redirectUrl;
                    }
                }, 1500);
            } else {
                mostrarAlerta(result.message || 'Erro ao salvar escala. Tente novamente.', 'danger');
            }
        } catch (error) {
            console.error('[EscalaManual] Erro ao salvar:', error);
            mostrarLoading(false);
            mostrarAlerta('Erro ao comunicar com o servidor. Verifique sua conexão e tente novamente.', 'danger');
        }
    }

    // ===== UTILITÁRIOS =====
    function mostrarAlerta(mensagem, tipo = 'info') {
        // Usar SweetAlert2 se disponível
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: tipo === 'danger' ? 'error' : tipo,
                title: tipo === 'success' ? 'Sucesso!' : tipo === 'warning' ? 'Atenção!' : 'Ops!',
                text: mensagem,
                confirmButtonText: 'OK'
            });
        } else {
            // Fallback para alert nativo
            alert(mensagem);
        }
    }

    function mostrarLoading(show) {
        let overlay = document.querySelector('.loading-overlay');

        if (show) {
            if (!overlay) {
                overlay = document.createElement('div');
                overlay.className = 'loading-overlay';
                overlay.innerHTML = `
                    <div class="spinner-border text-light" role="status">
                        <span class="visually-hidden">Carregando...</span>
                    </div>
                `;
                document.body.appendChild(overlay);
            }
            overlay.style.display = 'flex';
        } else {
            if (overlay) {
                overlay.style.display = 'none';
            }
        }
    }

    // ===== INICIALIZAÇÃO =====
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
