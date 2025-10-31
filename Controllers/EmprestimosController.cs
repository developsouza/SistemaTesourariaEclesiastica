using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Controllers
{
    // ==================== ADICIONADA RESTRIÇÃO DE ACESSO ====================
    [Authorize(Roles = Roles.AdminOuTesoureiroGeral)]
    // ========================================================================
    public class EmprestimosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmprestimosController> _logger;

        public EmprestimosController(
            ApplicationDbContext context,
            ILogger<EmprestimosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Emprestimos
        public async Task<IActionResult> Index(string filtro = "todos")
        {
            try
            {
                var saldoFundo = await CalcularSaldoFundo();
                var detalhamento = await ObterDetalhamentoSaldoFundo();

                ViewBag.SaldoFundo = saldoFundo;
                ViewBag.FiltroAtual = filtro;
                ViewBag.Detalhamento = detalhamento;

                _logger.LogInformation($"Saldo do Fundo calculado: {saldoFundo:C}");

                var query = _context.Emprestimos
                    .Include(e => e.Devolucoes)
                    .AsQueryable();

                switch (filtro.ToLower())
                {
                    case "ativos":
                        query = query.Where(e => e.Status == StatusEmprestimo.Ativo);
                        break;
                    case "quitados":
                        query = query.Where(e => e.Status == StatusEmprestimo.Quitado);
                        break;
                }

                var emprestimos = await query
                    .OrderByDescending(e => e.DataEmprestimo)
                    .ToListAsync();

                return View(emprestimos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar empréstimos");
                TempData["Erro"] = $"Erro ao carregar dados: {ex.Message}";
                return View(new List<Emprestimo>());
            }
        }

        // GET: Emprestimos/Create
        public async Task<IActionResult> Create()
        {
            var saldoFundo = await CalcularSaldoFundo();
            var detalhamento = await ObterDetalhamentoSaldoFundo();

            ViewBag.SaldoFundo = saldoFundo;
            ViewBag.Detalhamento = detalhamento;

            if (saldoFundo <= 0)
            {
                TempData["Aviso"] = "O saldo do fundo está zerado. Verifique se há fechamentos aprovados com rateios para o fundo.";
            }

            return View();
        }

        // POST: Emprestimos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Emprestimo emprestimo)
        {
            var saldoFundo = await CalcularSaldoFundo();

            if (emprestimo.ValorTotal > saldoFundo)
            {
                ModelState.AddModelError("ValorTotal",
                    $"O valor solicitado (R$ {emprestimo.ValorTotal:N2}) excede o saldo disponível no fundo (R$ {saldoFundo:N2})");
                ViewBag.SaldoFundo = saldoFundo;
                var detalhamento = await ObterDetalhamentoSaldoFundo();
                ViewBag.Detalhamento = detalhamento;
                TempData["Erro"] = "Saldo insuficiente. Verifique o detalhamento do fundo abaixo.";
                return View(emprestimo);
            }

            if (ModelState.IsValid)
            {
                emprestimo.Status = StatusEmprestimo.Ativo;
                emprestimo.DataEmprestimo = DateTime.Now;

                _context.Add(emprestimo);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Empréstimo registrado: {emprestimo.ValorTotal:C}");
                TempData["Sucesso"] = "Empréstimo registrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SaldoFundo = saldoFundo;
            return View(emprestimo);
        }

        // GET: Emprestimos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emprestimo = await _context.Emprestimos
                .Include(e => e.Devolucoes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            return View(emprestimo);
        }

        // POST: Emprestimos/RegistrarDevolucao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarDevolucao(int emprestimoId, decimal valorDevolvido, DateTime dataDevolucao, string observacoes)
        {
            var emprestimo = await _context.Emprestimos
                .Include(e => e.Devolucoes)
                .FirstOrDefaultAsync(e => e.Id == emprestimoId);

            if (emprestimo == null)
            {
                return Json(new { sucesso = false, mensagem = "Empréstimo não encontrado" });
            }

            if (emprestimo.Status != StatusEmprestimo.Ativo)
            {
                return Json(new { sucesso = false, mensagem = "Este empréstimo não está ativo" });
            }

            var saldoDevedor = emprestimo.SaldoDevedor;
            if (valorDevolvido > saldoDevedor)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = $"O valor da devolução (R$ {valorDevolvido:N2}) não pode ser maior que o saldo devedor (R$ {saldoDevedor:N2})"
                });
            }

            var devolucao = new DevolucaoEmprestimo
            {
                EmprestimoId = emprestimoId,
                ValorDevolvido = valorDevolvido,
                DataDevolucao = dataDevolucao,
                Observacoes = observacoes
            };

            _context.DevolucaoEmprestimos.Add(devolucao);

            var novoSaldoDevedor = saldoDevedor - valorDevolvido;
            if (novoSaldoDevedor == 0)
            {
                emprestimo.Status = StatusEmprestimo.Quitado;
                emprestimo.DataQuitacao = dataDevolucao;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Devolução registrada: {valorDevolvido:C} para empréstimo #{emprestimoId}");

            return Json(new
            {
                sucesso = true,
                mensagem = "Devolução registrada com sucesso!",
                novoSaldo = novoSaldoDevedor,
                status = emprestimo.Status.ToString()
            });
        }

        // Calcular Saldo do Fundo
        private async Task<decimal> CalcularSaldoFundo()
        {
            try
            {
                _logger.LogInformation("=== INICIANDO CÁLCULO DO SALDO DO FUNDO ===");

                // ✅ PASSO 1: Identificar o Centro de Custo FUNDO
                var centroCustoFundo = await _context.CentrosCusto
                    .FirstOrDefaultAsync(c => c.Ativo &&
              (c.Nome.ToUpper().Contains("FUNDO") ||
      c.Nome.ToUpper().Contains("REPASSE") ||
          c.Nome.ToUpper().Contains("DÍZIMO") ||
                   c.Nome.ToUpper().Contains("DIZIMO")));
                if (centroCustoFundo == null)
                {
                    _logger.LogWarning("❌ Centro de Custo FUNDO não encontrado!");
                    _logger.LogWarning("   Nenhum centro de custo ativo contém: FUNDO, REPASSE ou DÍZIMO");
                    return 0;
                }

                _logger.LogInformation($"✓ Centro de Custo FUNDO identificado: '{centroCustoFundo.Nome}' (ID: {centroCustoFundo.Id})");

                // ✅ PASSO 2: Buscar TODAS as regras de rateio que têm o FUNDO como destino
                var regrasParaFundo = await _context.RegrasRateio
                    .Where(r => r.CentroCustoDestinoId == centroCustoFundo.Id && r.Ativo)
                    .ToListAsync();

                _logger.LogInformation($"✓ Regras de Rateio para o FUNDO encontradas: {regrasParaFundo.Count}");
                foreach (var regra in regrasParaFundo)
                {
                    _logger.LogInformation($"   - Regra ID {regra.Id}: '{regra.Nome}' ({regra.Percentual:F2}%)");
                }

                if (!regrasParaFundo.Any())
                {
                    _logger.LogWarning("⚠ ATENÇÃO: Nenhuma regra de rateio ativa aponta para o FUNDO!");
                    _logger.LogWarning("   Crie uma regra: SEDE → FUNDO (Ex: 20%)");
                    return 0;
                }

                // ✅ PASSO 3: Buscar itens de rateio de fechamentos APROVADOS
                var itensRateio = await _context.ItensRateioFechamento
                 .Include(i => i.FechamentoPeriodo)
                          .Include(i => i.RegraRateio)
                          .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                 .ToListAsync();

                var quantidadeFechamentos = itensRateio.Select(i => i.FechamentoPeriodoId).Distinct().Count();
                var totalEntradasFundo = itensRateio.Sum(i => i.ValorRateio);

                _logger.LogInformation($"✓ Fechamentos APROVADOS processados: {quantidadeFechamentos}");
                _logger.LogInformation($"✓ Total de rateios para o FUNDO: {totalEntradasFundo:C}");

                if (itensRateio.Any())
                {
                    _logger.LogInformation("Detalhamento dos rateios:");
                    foreach (var item in itensRateio.OrderByDescending(i => i.FechamentoPeriodo.DataAprovacao))
                    {
                        _logger.LogInformation($"   - Fechamento #{item.FechamentoPeriodoId} " +
                        $"({item.FechamentoPeriodo.DataAprovacao:dd/MM/yyyy}): {item.ValorRateio:C} " +
                    $"({item.Percentual:F2}% de {item.ValorBase:C})");
                    }
                }

                // ✅ PASSO 4: Calcular total de empréstimos ativos
                var emprestimosAtivos = await _context.Emprestimos
               .Include(e => e.Devolucoes)
                  .Where(e => e.Status == StatusEmprestimo.Ativo)
                        .ToListAsync();

                var totalEmprestimos = emprestimosAtivos.Sum(e => e.ValorTotal);
                var totalDevolvido = emprestimosAtivos.Sum(e => e.ValorDevolvido);
                var saldoDevedorAtivos = totalEmprestimos - totalDevolvido;

                _logger.LogInformation($"✓ Empréstimos ativos: {emprestimosAtivos.Count}");
                _logger.LogInformation($"   - Total emprestado: {totalEmprestimos:C}");
                _logger.LogInformation($"   - Total devolvido: {totalDevolvido:C}");
                _logger.LogInformation($"   - Saldo devedor: {saldoDevedorAtivos:C}");

                // ✅ PASSO 5: Calcular saldo disponível
                var saldoDisponivel = totalEntradasFundo - saldoDevedorAtivos;

                _logger.LogInformation("=== RESUMO DO CÁLCULO ===");
                _logger.LogInformation($"Total em Rateios (Entradas no Fundo): {totalEntradasFundo:C}");
                _logger.LogInformation($"Empréstimos Ativos (Saldo Devedor):    - {saldoDevedorAtivos:C}");
                _logger.LogInformation($"────────────────────────────────────────");
                _logger.LogInformation($"SALDO DISPONÍVEL PARA EMPRÉSTIMOS:     {saldoDisponivel:C}");
                _logger.LogInformation("=== FIM DO CÁLCULO ===");

                return saldoDisponivel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao calcular saldo do fundo");
                return 0;
            }
        }

        // Obter Detalhamento do Saldo do Fundo
        private async Task<object> ObterDetalhamentoSaldoFundo()
        {
            try
            {
                // Identificar o Centro de Custo FUNDO
                var centroCustoFundo = await _context.CentrosCusto
                  .FirstOrDefaultAsync(c => c.Ativo &&
                 (c.Nome.ToUpper().Contains("FUNDO") ||
                       c.Nome.ToUpper().Contains("REPASSE") ||
                              c.Nome.ToUpper().Contains("DÍZIMO") ||
                              c.Nome.ToUpper().Contains("DIZIMO")));

                if (centroCustoFundo == null)
                {
                    return new
                    {
                        centroCustoFundoEncontrado = false,
                        nomeCentroCustoFundo = "NÃO ENCONTRADO",
                        totalEntradasFundo = 0m,
                        quantidadeFechamentosAprovados = 0,
                        quantidadeEmprestimosAtivos = 0,
                        totalEmprestimosAtivos = 0m,
                        saldoDevedorAtivos = 0m,
                        devolucoesEmprestimosAtivos = 0m,
                        quantidadeEmprestimosQuitados = 0,
                        totalQuitado = 0m,
                        saldoDisponivel = 0m,
                        percentualComprometido = 0m,
                        mensagemErro = "Centro de Custo FUNDO não encontrado. Crie um centro com nome contendo: FUNDO, REPASSE ou DÍZIMO"
                    };
                }

                // Buscar rateios para o fundo
                var itensRateio = await _context.ItensRateioFechamento
                  .Include(i => i.FechamentoPeriodo)
         .Include(i => i.RegraRateio)
           .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
              i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
               .ToListAsync();

                var totalEntradasFundo = itensRateio.Sum(i => i.ValorRateio);
                var quantidadeFechamentosAprovados = itensRateio
               .Select(i => i.FechamentoPeriodoId)
                .Distinct()
     .Count();

                // Empréstimos ativos
                var emprestimosAtivos = await _context.Emprestimos
                    .Include(e => e.Devolucoes)
                    .Where(e => e.Status == StatusEmprestimo.Ativo)
                 .ToListAsync();

                var totalEmprestimosAtivos = emprestimosAtivos.Sum(e => e.ValorTotal);
                var devolucoesEmprestimosAtivos = emprestimosAtivos.Sum(e => e.ValorDevolvido);
                var saldoDevedorAtivos = totalEmprestimosAtivos - devolucoesEmprestimosAtivos;

                // Empréstimos quitados
                var emprestimosQuitados = await _context.Emprestimos
                                .Where(e => e.Status == StatusEmprestimo.Quitado)
                            .ToListAsync();

                var quantidadeEmprestimosQuitados = emprestimosQuitados.Count;
                var totalQuitado = emprestimosQuitados.Sum(e => e.ValorTotal);

                var saldoDisponivel = totalEntradasFundo - saldoDevedorAtivos;
                var percentualComprometido = totalEntradasFundo > 0
                  ? Math.Round((saldoDevedorAtivos / totalEntradasFundo) * 100, 2)
                    : 0;

                return new
                {
                    centroCustoFundoEncontrado = true,
                    nomeCentroCustoFundo = centroCustoFundo.Nome,
                    totalEntradasFundo,
                    quantidadeFechamentosAprovados,
                    quantidadeEmprestimosAtivos = emprestimosAtivos.Count,
                    totalEmprestimosAtivos,
                    saldoDevedorAtivos,
                    devolucoesEmprestimosAtivos,
                    quantidadeEmprestimosQuitados,
                    totalQuitado,
                    saldoDisponivel,
                    percentualComprometido,
                    mensagemErro = (string?)null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter detalhamento do saldo do fundo");
                return new { erro = ex.Message };
            }
        }

        // GET: Emprestimos/ObterSaldoFundo (para AJAX)
        [HttpGet]
        public async Task<IActionResult> ObterSaldoFundo()
        {
            try
            {
                var saldo = await CalcularSaldoFundo();
                var detalhamento = await ObterDetalhamentoSaldoFundo();

                return Json(new
                {
                    saldo,
                    detalhamento
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saldo do fundo via AJAX");
                return Json(new { erro = ex.Message });
            }
        }

        // GET: Detalhamento Completo do Fundo
        [HttpGet]
        public async Task<IActionResult> DetalhamentoFundo()
        {
            var detalhamento = await ObterDetalhamentoSaldoFundo();

            // ✅ CORREÇÃO: Buscar o Centro de Custo FUNDO primeiro
            var centroCustoFundo = await _context.CentrosCusto
           .FirstOrDefaultAsync(c => c.Ativo &&
         (c.Nome.ToUpper().Contains("FUNDO") ||
     c.Nome.ToUpper().Contains("REPASSE") ||
 c.Nome.ToUpper().Contains("DÍZIMO") ||
c.Nome.ToUpper().Contains("DIZIMO")));

 // ✅ CORREÇÃO: Filtrar APENAS rateios para o FUNDO de empréstimos
       var historico = new List<ItemRateioFechamento>();

    if (centroCustoFundo != null)
     {
       historico = await _context.ItensRateioFechamento
   .Include(i => i.FechamentoPeriodo)
   .ThenInclude(f => f.CentroCusto)
          .Include(i => i.RegraRateio)
      .ThenInclude(r => r.CentroCustoDestino)
             .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
     i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id) // ✅ FILTRO ESSENCIAL
      .OrderByDescending(i => i.FechamentoPeriodo.DataAprovacao)
        .Take(20)
          .ToListAsync();
    }

       ViewBag.Detalhamento = detalhamento;
        ViewBag.Historico = historico;

       return View();
        }

        // ✅ NOVO: Diagnóstico Completo do Sistema de Rateios e Fundo
        [HttpGet]
        public async Task<IActionResult> DiagnosticoRateios()
        {
            var diagnostico = new System.Text.StringBuilder();
            diagnostico.AppendLine("=== DIAGNÓSTICO COMPLETO DO SISTEMA DE RATEIOS E FUNDO ===");
            diagnostico.AppendLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            diagnostico.AppendLine("");

            try
            {
                // 1. Verificar Centro de Custo FUNDO
                diagnostico.AppendLine("1. CENTRO DE CUSTO FUNDO");
                var centroCustoFundo = await _context.CentrosCusto
                     .FirstOrDefaultAsync(c => c.Ativo &&
              (c.Nome.ToUpper().Contains("FUNDO") ||
            c.Nome.ToUpper().Contains("REPASSE") ||
          c.Nome.ToUpper().Contains("DÍZIMO") ||
                 c.Nome.ToUpper().Contains("DIZIMO")));

                if (centroCustoFundo != null)
                {
                    diagnostico.AppendLine($"   ✓ Encontrado: '{centroCustoFundo.Nome}' (ID: {centroCustoFundo.Id})");
                    diagnostico.AppendLine($"   ✓ Tipo: {centroCustoFundo.Tipo}");
                    diagnostico.AppendLine($"   ✓ Ativo: {centroCustoFundo.Ativo}");
                }
                else
                {
                    diagnostico.AppendLine("   ❌ NÃO ENCONTRADO!");
                    diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar um Centro de Custo com nome contendo: FUNDO, REPASSE ou DÍZIMO");
                }
                diagnostico.AppendLine("");

                // 2. Verificar Centro de Custo SEDE
                diagnostico.AppendLine("2. CENTRO DE CUSTO SEDE");
                var centroCustoSede = await _context.CentrosCusto
              .FirstOrDefaultAsync(c => c.Ativo &&
                    (c.Nome.ToUpper().Contains("SEDE") ||
            c.Nome.ToUpper().Contains("GERAL") ||
           c.Nome.ToUpper().Contains("PRINCIPAL") ||
                          c.Nome.ToUpper().Contains("CENTRAL")));

                if (centroCustoSede != null)
                {
                    diagnostico.AppendLine($"✓ Encontrado: '{centroCustoSede.Nome}' (ID: {centroCustoSede.Id})");
                    diagnostico.AppendLine($"   ✓ Tipo: {centroCustoSede.Tipo}");
                    diagnostico.AppendLine($"   ✓ Ativo: {centroCustoSede.Ativo}");
                }
                else
                {
                    diagnostico.AppendLine("   ❌ NÃO ENCONTRADO!");
                    diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar um Centro de Custo com nome contendo: SEDE ou GERAL");
                }
                diagnostico.AppendLine("");

                // 3. Verificar Regras de Rateio
                diagnostico.AppendLine("3. REGRAS DE RATEIO PARA O FUNDO");
                if (centroCustoFundo != null)
                {
                    var regrasParaFundo = await _context.RegrasRateio
                          .Include(r => r.CentroCustoOrigem)
                             .Include(r => r.CentroCustoDestino)
                         .Where(r => r.CentroCustoDestinoId == centroCustoFundo.Id)
                           .ToListAsync();

                    if (regrasParaFundo.Any())
                    {
                        diagnostico.AppendLine($"   ✓ Total de regras encontradas: {regrasParaFundo.Count}");
                        foreach (var regra in regrasParaFundo)
                        {
                            var status = regra.Ativo ? "ATIVA" : "INATIVA";
                            diagnostico.AppendLine($"   - [{status}] '{regra.Nome}':");
                            diagnostico.AppendLine($"     Origem: {regra.CentroCustoOrigem.Nome} (ID: {regra.CentroCustoOrigemId})");
                            diagnostico.AppendLine($"     Destino: {regra.CentroCustoDestino.Nome} (ID: {regra.CentroCustoDestinoId})");
                            diagnostico.AppendLine($"  Percentual: {regra.Percentual:F2}%");
                        }
                    }
                    else
                    {
                        diagnostico.AppendLine("   ❌ NENHUMA REGRA ENCONTRADA!");
                        diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar uma Regra de Rateio:");
                        if (centroCustoSede != null)
                        {
                            diagnostico.AppendLine($" - Origem: {centroCustoSede.Nome}");
                        }
                        else
                        {
                            diagnostico.AppendLine($"     - Origem: (SEDE)");
                        }
                        diagnostico.AppendLine($"     - Destino: {centroCustoFundo.Nome}");
                        diagnostico.AppendLine($"     - Percentual: 20% (exemplo)");
                    }
                }
                else
                {
                    diagnostico.AppendLine("   ⚠ Não é possível verificar regras sem o Centro de Custo FUNDO");
                }
                diagnostico.AppendLine("");

                // 4. Verificar Fechamentos Aprovados
                diagnostico.AppendLine("4. FECHAMENTOS APROVADOS");
                var fechamentosAprovados = await _context.FechamentosPeriodo
                .Include(f => f.CentroCusto)
             .Include(f => f.ItensRateio)
                 .Where(f => f.Status == StatusFechamentoPeriodo.Aprovado)
                  .OrderByDescending(f => f.DataAprovacao)
                  .ToListAsync();

                diagnostico.AppendLine($"   ✓ Total de fechamentos aprovados: {fechamentosAprovados.Count}");

                if (fechamentosAprovados.Any())
                {
                    var comRateios = fechamentosAprovados.Count(f => f.ItensRateio.Any());
                    var semRateios = fechamentosAprovados.Count(f => !f.ItensRateio.Any());

                    diagnostico.AppendLine($"   ✓ Com rateios: {comRateios}");
                    diagnostico.AppendLine($"   ⚠ Sem rateios: {semRateios}");

                    diagnostico.AppendLine("");
                    diagnostico.AppendLine("   Últimos 5 fechamentos aprovados:");
                    foreach (var fechamento in fechamentosAprovados.Take(5))
                    {
                        var totalRateios = fechamento.ItensRateio.Sum(i => i.ValorRateio);
                        var status = fechamento.ItensRateio.Any() ? "✓ TEM RATEIOS" : "⚠ SEM RATEIOS";
                        diagnostico.AppendLine($"   - [{status}] {fechamento.CentroCusto.Nome} - {fechamento.DataInicio:dd/MM/yyyy}:");
                        diagnostico.AppendLine($"     Receitas: {fechamento.TotalEntradas:C}");
                        diagnostico.AppendLine($"     Rateios: {totalRateios:C} ({fechamento.ItensRateio.Count} item(ns))");
                    }
                }
                else
                {
                    diagnostico.AppendLine("   ⚠ Nenhum fechamento aprovado encontrado");
                    diagnostico.AppendLine("   AÇÃO NECESSÁRIA: Criar e aprovar fechamentos para gerar rateios");
                }
                diagnostico.AppendLine("");

                // 5. Verificar Rateios Aplicados ao FUNDO
                if (centroCustoFundo != null)
                {
                    diagnostico.AppendLine("5. RATEIOS APLICADOS AO FUNDO");
                    var itensRateioFundo = await _context.ItensRateioFechamento
                          .Include(i => i.FechamentoPeriodo)
                     .Include(i => i.RegraRateio)
                       .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                     i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                              .OrderByDescending(i => i.FechamentoPeriodo.DataAprovacao)
                         .ToListAsync();

                    var totalRateios = itensRateioFundo.Sum(i => i.ValorRateio);

                    diagnostico.AppendLine($"   ✓ Total de rateios para o fundo: {totalRateios:C}");
                    diagnostico.AppendLine($"   ✓ Quantidade de itens: {itensRateioFundo.Count}");

                    if (itensRateioFundo.Any())
                    {
                        diagnostico.AppendLine("");
                        diagnostico.AppendLine("   Últimos 5 rateios:");
                        foreach (var item in itensRateioFundo.Take(5))
                        {
                            diagnostico.AppendLine($"   - Fechamento #{item.FechamentoPeriodoId} ({item.FechamentoPeriodo.DataAprovacao:dd/MM/yyyy}):");
                            diagnostico.AppendLine($"     Valor Base: {item.ValorBase:C}");
                            diagnostico.AppendLine($"     Percentual: {item.Percentual:F2}%");
                            diagnostico.AppendLine($"     Valor Rateado: {item.ValorRateio:C}");
                        }
                    }
                    else
                    {
                        diagnostico.AppendLine("   ❌ NENHUM RATEIO ENCONTRADO!");
                        diagnostico.AppendLine("   POSSÍVEIS CAUSAS:");
                        diagnostico.AppendLine("     1. Não há fechamentos aprovados");
                        diagnostico.AppendLine("     2. Os fechamentos não têm regras de rateio configuradas");
                        diagnostico.AppendLine("     3. As regras de rateio não apontam para o FUNDO");
                    }
                }
                else
                {
                    diagnostico.AppendLine("5. RATEIOS APLICADOS AO FUNDO");
                    diagnostico.AppendLine("   ⚠ Não é possível verificar sem o Centro de Custo FUNDO");
                }
                diagnostico.AppendLine("");

                // 6. Verificar Empréstimos
                diagnostico.AppendLine("6. EMPRÉSTIMOS");
                var emprestimosAtivos = await _context.Emprestimos
                         .Include(e => e.Devolucoes)
                 .Where(e => e.Status == StatusEmprestimo.Ativo)
                .ToListAsync();

                var totalEmprestado = emprestimosAtivos.Sum(e => e.ValorTotal);
                var totalDevolvido = emprestimosAtivos.Sum(e => e.ValorDevolvido);
                var saldoDevedor = totalEmprestado - totalDevolvido;

                diagnostico.AppendLine($"   ✓ Empréstimos ativos: {emprestimosAtivos.Count}");
                diagnostico.AppendLine($"   ✓ Total emprestado: {totalEmprestado:C}");
                diagnostico.AppendLine($"   ✓ Total devolvido: {totalDevolvido:C}");
                diagnostico.AppendLine($"   ✓ Saldo devedor: {saldoDevedor:C}");
                diagnostico.AppendLine("");

                // 7. Cálculo Final
                diagnostico.AppendLine("7. CÁLCULO DO SALDO DISPONÍVEL");
                if (centroCustoFundo != null)
                {
                    var totalRateiosFundo = await _context.ItensRateioFechamento
                .Include(i => i.FechamentoPeriodo)
                        .Include(i => i.RegraRateio)
                      .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                 i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                          .SumAsync(i => i.ValorRateio);

                    var saldoDisponivel = totalRateiosFundo - saldoDevedor;

                    diagnostico.AppendLine($"   Total de Rateios para o Fundo:    {totalRateiosFundo:C}");
                    diagnostico.AppendLine($"   Saldo Devedor de Empréstimos:    - {saldoDevedor:C}");
                    diagnostico.AppendLine($"   ────────────────────────────────────");
                    diagnostico.AppendLine($"   SALDO DISPONÍVEL:      {saldoDisponivel:C}");

                    if (saldoDisponivel <= 0)
                    {
                        diagnostico.AppendLine("");
                        diagnostico.AppendLine("   ⚠ SALDO ZERADO OU NEGATIVO!");
                        if (totalRateiosFundo <= 0)
                        {
                            diagnostico.AppendLine("   CAUSA: Não há rateios contabilizados no fundo");
                            diagnostico.AppendLine("   SOLUÇÃO:");
                            diagnostico.AppendLine("     1. Verifique se há fechamentos aprovados");
                            diagnostico.AppendLine("     2. Verifique se as regras de rateio estão ativas");
                            diagnostico.AppendLine("     3. Verifique se os fechamentos são da SEDE/GERAL");
                        }
                        else if (saldoDevedor >= totalRateiosFundo)
                        {
                            diagnostico.AppendLine("   CAUSA: Empréstimos consomem todo o saldo do fundo");
                            diagnostico.AppendLine("   SOLUÇÃO: Aguardar devoluções ou novos rateios");
                        }
                    }
                }
                else
                {
                    diagnostico.AppendLine("   ⚠ Não é possível calcular sem o Centro de Custo FUNDO");
                }
                diagnostico.AppendLine("");

                // 8. Recomendações
                diagnostico.AppendLine("8. RECOMENDAÇÕES");
                var recomendacoes = new List<string>();

                if (centroCustoFundo == null)
                {
                    recomendacoes.Add("❌ CRÍTICO: Criar Centro de Custo FUNDO");
                }

                if (centroCustoSede == null)
                {
                    recomendacoes.Add("❌ CRÍTICO: Criar Centro de Custo SEDE");
                }

                if (centroCustoFundo != null)
                {
                    var temRegras = await _context.RegrasRateio
                             .AnyAsync(r => r.CentroCustoDestinoId == centroCustoFundo.Id && r.Ativo);

                    if (!temRegras)
                    {
                        recomendacoes.Add("❌ CRÍTICO: Criar Regra de Rateio SEDE → FUNDO");
                    }
                }

                if (!fechamentosAprovados.Any())
                {
                    recomendacoes.Add("⚠ Criar e aprovar fechamentos da SEDE");
                }

                if (fechamentosAprovados.Any() && !fechamentosAprovados.Any(f => f.ItensRateio.Any()))
                {
                    recomendacoes.Add("⚠ Os fechamentos não estão gerando rateios - verificar configuração");
                }

                if (recomendacoes.Any())
                {
                    foreach (var rec in recomendacoes)
                    {
                        diagnostico.AppendLine($"   {rec}");
                    }
                }
                else
                {
                    diagnostico.AppendLine("   ✓ Sistema configurado corretamente!");
                }

                ViewBag.Diagnostico = diagnostico.ToString();
            }
            catch (Exception ex)
            {
                diagnostico.AppendLine("");
                diagnostico.AppendLine($"❌ ERRO no diagnóstico: {ex.Message}");
                _logger.LogError(ex, "Erro no diagnóstico de rateios");
                ViewBag.Diagnostico = diagnostico.ToString();
            }

            return View();
        }

        // POST: Emprestimos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var emprestimo = await _context.Emprestimos
         .Include(e => e.Devolucoes)
             .FirstOrDefaultAsync(e => e.Id == id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            if (emprestimo.Devolucoes.Any())
            {
                TempData["Erro"] = "Não é possível excluir um empréstimo que já possui devoluções registradas.";
                return RedirectToAction(nameof(Index));
            }

            emprestimo.Status = StatusEmprestimo.Cancelado;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Empréstimo #{id} cancelado");
            TempData["Sucesso"] = "Empréstimo cancelado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}