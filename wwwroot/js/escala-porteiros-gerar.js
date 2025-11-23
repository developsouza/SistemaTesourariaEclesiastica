/* Escala Porteiros - Gerar (externo) */
console.log('[EscalaPorteirosGerar] Arquivo externo carregado');

(function(){
  function parseDataString(val){
    if(!val) return null;
    if(typeof val !== 'string') return null;
    const base = val.includes('T') ? val : (val + 'T12:00:00');
    const dt = new Date(base);
    if(isNaN(dt.getTime())){ 
      console.warn('[EscalaPorteirosGerar] Data invalida recebida:', val); 
      return null; 
    }
    return dt;
  }
  
  function init(){
    const btnGerar = document.getElementById('btnGerarEscala');
    const btnSugerir = document.getElementById('btnSugerirDias');
    const btnLimpar = document.getElementById('btnLimparDias');
    
    if(!btnGerar){
      console.error('[EscalaPorteirosGerar] Botoes nao encontrados');
      return;
    }
    
    let diasSelecionados = [];

    function render(){
      const container = $('#listaDiasSelecionados');
      $('#totalDiasSelecionados').text(diasSelecionados.length);
      container.empty();
      
      if(!diasSelecionados.length){
        container.html('<p class="text-muted text-center">Nenhum dia selecionado</p>');
        return;
      }
      
      diasSelecionados.sort((a,b) => new Date(a.Data||a.data) - new Date(b.Data||b.data));
      
      let html = '<div class="row g-2">';
      
      diasSelecionados.forEach(d => {
        const raw = d.Data || d.data; 
        const dataObj = parseDataString(raw); 
        const dataFormatada = dataObj ? dataObj.toLocaleDateString('pt-BR') : '(data invalida)'; 
        const semana = dataObj ? dataObj.toLocaleDateString('pt-BR', {weekday:'short'}) : 'Inv'; 
        
        const tipo = d.TipoCulto ?? d.tipoCulto ?? 1;
        const horario = d.Horario || d.horario || null;
        const horarioFormatado = horario ? horario : '';
        
        html += '<div class="col-12 col-md-6 col-lg-3">' +
        '<div class="card h-100">' +
        '<div class="card-body p-2">' +
        '<div class="d-flex justify-content-between align-items-start mb-2">' +
        '<div><small class="text-muted">' + semana + '</small><br><strong class="d-block">' + dataFormatada + '</strong></div>' +
        '<button type="button" class="btn btn-sm btn-outline-danger" onclick="removerDiaClick(\'' + raw + '\')" title="Remover">' +
        '<i class="bi bi-trash"></i></button>' +
        '</div>' +
        (horarioFormatado ? '<div class="mb-2"><small><i class="bi bi-clock"></i> ' + horarioFormatado + '</small></div>' : '') +
        '<select class="form-select form-select-sm" onchange="atualizarTipoCulto(\'' + raw + '\', this.value)">' +
        '<option value="0" ' + (tipo == 0 ? 'selected' : '') + '>Escola Biblica</option>' +
        '<option value="1" ' + (tipo == 1 ? 'selected' : '') + '>Culto Evangelistico</option>' +
        '<option value="2" ' + (tipo == 2 ? 'selected' : '') + '>Culto da Familia</option>' +
        '<option value="3" ' + (tipo == 3 ? 'selected' : '') + '>Culto de Doutrina</option>' +
        '<option value="4" ' + (tipo == 4 ? 'selected' : '') + '>Culto Especial</option>' +
        '<option value="5" ' + (tipo == 5 ? 'selected' : '') + '>Congresso</option>' +
        '<option value="6" ' + (tipo == 6 ? 'selected' : '') + '>Outro Tipo de Culto</option>' +
        '</select>' +
        '</div></div></div>';
      });
      
      html += '</div>';
      container.html(html);
    }
    
    window.removerDiaClick = (data) => {
      diasSelecionados = diasSelecionados.filter(x => (x.Data || x.data) !== data);
      render();
    };
    
    window.atualizarTipoCulto = (data, tipo) => {
      const dia = diasSelecionados.find(x => (x.Data || x.data) === data);
      if(dia) dia.TipoCulto = parseInt(tipo);
    };

    btnSugerir?.addEventListener('click', () => {
      console.log('[EscalaPorteirosGerar] click sugerir');
      const di = $('#dataInicio').val(); 
      const df = $('#dataFim').val();
      
      if(!di || !df){
        alert('Selecione inicio e fim.');
        return;
      }
      
      const $b = $(btnSugerir); 
      const orig = $b.html(); 
      $b.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i> Carregando...');
      
      $.get('/EscalasPorteiros/SugerirDias', {dataInicio: di, dataFim: df})
       .done(r => {
         console.log('[EscalaPorteirosGerar] resposta sugerir', r); 
         if(r.success){
           diasSelecionados = r.dias.map(x => ({
             Data: x.Data || x.data, 
             TipoCulto: x.TipoCulto ?? x.tipoCulto ?? 1,
             Horario: x.Horario || x.horario || null
           })); 
           console.log('[EscalaPorteirosGerar] Dias sugeridos:', diasSelecionados);
           render();
         } else {
           alert('Erro: ' + r.message);
         }
       })
       .fail(x => {
         console.error('[EscalaPorteirosGerar] falha sugerir', x.status, x.responseText); 
         alert('Erro ao comunicar com o servidor.');
       })
       .always(() => {
         $b.prop('disabled', false).html(orig);
       });
    });

    btnLimpar?.addEventListener('click', () => {
      console.log('[EscalaPorteirosGerar] click limpar'); 
      if(confirm('Limpar todos os dias?')){
        diasSelecionados = [];
        render();
      }
    });

    btnGerar.addEventListener('click', () => {
      console.log('[EscalaPorteirosGerar] click gerar');
      
      if(!diasSelecionados.length){
        alert('Selecione pelo menos um dia.');
        return;
      }
      
      const di = $('#dataInicio').val(); 
      const df = $('#dataFim').val();
      
      const payload = {
        DataInicio: di,
        DataFim: df,
        DiasSelecionados: diasSelecionados.map(d => ({
          Data: d.Data || d.data,
          TipoCulto: d.TipoCulto ?? 1,
          Horario: d.Horario || d.horario || null
        }))
      };
      
      console.log('[EscalaPorteirosGerar] Payload enviado:', payload);
      
      const token = $('input[name="__RequestVerificationToken"]').val();
      const $b = $(btnGerar); 
      const orig = $b.html(); 
      $b.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i> Gerando...');
      
      $.ajax({
        url: '/EscalasPorteiros/GerarEscala',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: {
          'RequestVerificationToken': token,
          'X-Requested-With': 'XMLHttpRequest'
        }
      })
      .done(r => {
        console.log('[EscalaPorteirosGerar] resposta gerar', r); 
        if(r.success){
          alert(r.message); 
          if(r.redirectUrl) window.location.href = r.redirectUrl;
        } else {
          alert('Erro: ' + r.message);
        }
      })
      .fail(x => {
        console.error('[EscalaPorteirosGerar] falha gerar', x.status, x.responseText); 
        let msg = 'Erro ao gerar escala.'; 
        if(x.status === 400) msg = 'Token invalido ou dados incorretos.'; 
        if(x.status === 401) msg = 'Nao autorizado.'; 
        alert(msg);
      })
      .always(() => {
        $b.prop('disabled', false).html(orig);
      });
    });

    const diasJsonEl = document.getElementById('diasIniciaisJson');
    if(diasJsonEl){ 
      try { 
        const arr = JSON.parse(diasJsonEl.textContent); 
        if(Array.isArray(arr)){ 
          diasSelecionados = arr.map(x => ({
            Data: x.Data || x.data, 
            TipoCulto: x.TipoCulto ?? x.tipoCulto ?? 1,
            Horario: x.Horario || x.horario || null
          })); 
          render(); 
          console.log('[EscalaPorteirosGerar] dias iniciais carregados', arr.length);
        } 
      } catch(e){ 
        console.error('[EscalaPorteirosGerar] erro parse dias iniciais', e); 
      } 
    }
    
    console.log('[EscalaPorteirosGerar] init completo');
  }
  
  if(document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
