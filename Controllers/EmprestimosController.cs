using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using System.Text;

namespace SistemaTesourariaEclesiastica.Controllers
{
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

            // Adicionar mensagem de aviso se saldo for zero
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

            // Validação: valor não pode ser maior que o saldo disponível
            if (emprestimo.ValorTotal > saldoFundo)
            {
                ModelState.AddModelError("ValorTotal",
                    $"O valor solicitado (R$ {emprestimo.ValorTotal:N2}) excede o saldo disponível no fundo (R$ {saldoFundo:N2})");
                ViewBag.SaldoFundo = saldoFundo;

                // Adicionar detalhamento para debug
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
                novoSaldoDevedor = novoSaldoDevedor,
                percentualDevolvido = emprestimo.PercentualDevolvido,
                status = emprestimo.Status.ToString()
            });
        }

        // MÉTODO MELHORADO: Calcular Saldo do Fundo com Logs
        private async Task<decimal> CalcularSaldoFundo()
        {
            try
            {
                _logger.LogInformation("=== INICIANDO CÁLCULO DO SALDO DO FUNDO ===");

                // PASSO 1: Buscar o Centro de Custo do Fundo
                var centroCustoFundo = await _context.CentrosCusto
                    .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("FUNDO") ||
                                             c.Nome.ToUpper().Contains("REPASSE") ||
                                             c.Nome.ToUpper().Contains("DÍZIMO DOS DÍZIMOS") ||
                                             c.Nome.ToUpper().Contains("DIZIMO DOS DIZIMOS"));

                decimal totalEntradasFundo = 0;

                if (centroCustoFundo != null)
                {
                    _logger.LogInformation($"Centro de Custo FUNDO encontrado: {centroCustoFundo.Nome} (ID: {centroCustoFundo.Id})");

                    // Buscar itens de rateio aprovados destinados ao fundo
                    var itensRateio = await _context.ItensRateioFechamento
                        .Include(i => i.FechamentoPeriodo)
                        .Include(i => i.RegraRateio)
                        .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                                   i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                        .ToListAsync();

                    totalEntradasFundo = itensRateio.Sum(i => i.ValorRateio);

                    _logger.LogInformation($"Itens de rateio aprovados encontrados: {itensRateio.Count}");
                    _logger.LogInformation($"Total de entradas no fundo: {totalEntradasFundo:C}");

                    if (!itensRateio.Any())
                    {
                        _logger.LogWarning("AVISO: Nenhum item de rateio aprovado encontrado!");
                        _logger.LogWarning("Possíveis causas:");
                        _logger.LogWarning("1. Nenhum fechamento foi aprovado");
                        _logger.LogWarning("2. Não há regras de rateio para o fundo");
                        _logger.LogWarning("3. Os fechamentos não geraram rateios");
                    }
                }
                else
                {
                    _logger.LogWarning("AVISO: Centro de Custo FUNDO não encontrado!");
                    _logger.LogWarning("Tentando buscar por regras de rateio...");

                    // Fallback: buscar por regras com nome do fundo
                    var regrasFundo = await _context.RegrasRateio
                        .Where(r => r.Ativo &&
                                   (r.Nome.ToUpper().Contains("FUNDO") ||
                                    r.Nome.ToUpper().Contains("REPASSE") ||
                                    r.Nome.ToUpper().Contains("DÍZIMO") ||
                                    r.Nome.ToUpper().Contains("DIZIMO")))
                        .ToListAsync();

                    if (regrasFundo.Any())
                    {
                        _logger.LogInformation($"Regras de rateio para FUNDO encontradas: {regrasFundo.Count}");
                        var idsRegras = regrasFundo.Select(r => r.Id).ToList();

                        totalEntradasFundo = await _context.ItensRateioFechamento
                            .Include(i => i.FechamentoPeriodo)
                            .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                                       idsRegras.Contains(i.RegraRateioId))
                            .SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

                        _logger.LogInformation($"Total através das regras: {totalEntradasFundo:C}");
                    }
                    else
                    {
                        _logger.LogError("ERRO: Nem Centro de Custo nem Regras de Rateio para FUNDO foram encontrados!");
                        return 0;
                    }
                }

                // PASSO 2: Calcular Total Emprestado (Ativo)
                var totalEmprestimosAtivos = await _context.Emprestimos
                    .Where(e => e.Status == StatusEmprestimo.Ativo)
                    .SumAsync(e => (decimal?)e.ValorTotal) ?? 0;

                _logger.LogInformation($"Total de empréstimos ativos: {totalEmprestimosAtivos:C}");

                // PASSO 3: Calcular Total Devolvido (dos Ativos)
                var devolucoesEmprestimosAtivos = await _context.DevolucaoEmprestimos
                    .Include(d => d.Emprestimo)
                    .Where(d => d.Emprestimo.Status == StatusEmprestimo.Ativo)
                    .SumAsync(d => (decimal?)d.ValorDevolvido) ?? 0;

                _logger.LogInformation($"Total devolvido dos ativos: {devolucoesEmprestimosAtivos:C}");

                // PASSO 4: Calcular Saldo Devedor Atual
                var saldoDevedorAtivos = totalEmprestimosAtivos - devolucoesEmprestimosAtivos;
                _logger.LogInformation($"Saldo devedor atual: {saldoDevedorAtivos:C}");

                // PASSO 5: Calcular Saldo Disponível no Fundo
                var saldoFundo = totalEntradasFundo - saldoDevedorAtivos;

                _logger.LogInformation($"=== SALDO FINAL DO FUNDO: {saldoFundo:C} ===");
                _logger.LogInformation($"Cálculo: {totalEntradasFundo:C} (entradas) - {saldoDevedorAtivos:C} (saldo devedor) = {saldoFundo:C}");

                return saldoFundo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular saldo do fundo");
                return 0;
            }
        }

        // MÉTODO MELHORADO: Obter Detalhamento do Saldo
        public async Task<object> ObterDetalhamentoSaldoFundo()
        {
            try
            {
                var centroCustoFundo = await _context.CentrosCusto
                    .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("FUNDO") ||
                                             c.Nome.ToUpper().Contains("REPASSE") ||
                                             c.Nome.ToUpper().Contains("DÍZIMO DOS DÍZIMOS") ||
                                             c.Nome.ToUpper().Contains("DIZIMO DOS DIZIMOS"));

                decimal totalEntradasFundo = 0;
                int quantidadeFechamentosAprovados = 0;
                string nomeCentroCustoFundo = "Não encontrado";

                if (centroCustoFundo != null)
                {
                    nomeCentroCustoFundo = centroCustoFundo.Nome;

                    var rateiosFundo = await _context.ItensRateioFechamento
                        .Include(i => i.FechamentoPeriodo)
                            .ThenInclude(f => f.CentroCusto)
                        .Include(i => i.RegraRateio)
                        .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                                   i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                        .ToListAsync();

                    totalEntradasFundo = rateiosFundo.Sum(i => i.ValorRateio);
                    quantidadeFechamentosAprovados = rateiosFundo.Select(i => i.FechamentoPeriodoId).Distinct().Count();
                }

                var totalEmprestimosAtivos = await _context.Emprestimos
                    .Where(e => e.Status == StatusEmprestimo.Ativo)
                    .SumAsync(e => (decimal?)e.ValorTotal) ?? 0;

                var devolucoesEmprestimosAtivos = await _context.DevolucaoEmprestimos
                    .Include(d => d.Emprestimo)
                    .Where(d => d.Emprestimo.Status == StatusEmprestimo.Ativo)
                    .SumAsync(d => (decimal?)d.ValorDevolvido) ?? 0;

                var saldoDevedorAtivos = totalEmprestimosAtivos - devolucoesEmprestimosAtivos;
                var saldoFundo = totalEntradasFundo - saldoDevedorAtivos;

                var quantidadeEmprestimosAtivos = await _context.Emprestimos
                    .CountAsync(e => e.Status == StatusEmprestimo.Ativo);

                var quantidadeEmprestimosQuitados = await _context.Emprestimos
                    .CountAsync(e => e.Status == StatusEmprestimo.Quitado);

                var totalQuitado = await _context.Emprestimos
                    .Where(e => e.Status == StatusEmprestimo.Quitado)
                    .SumAsync(e => (decimal?)e.ValorTotal) ?? 0;

                return new
                {
                    // Informações do Centro de Custo
                    nomeCentroCustoFundo = nomeCentroCustoFundo,
                    centroCustoFundoEncontrado = centroCustoFundo != null,

                    // Entradas no Fundo
                    totalEntradasFundo = totalEntradasFundo,
                    quantidadeFechamentosAprovados = quantidadeFechamentosAprovados,

                    // Empréstimos Ativos
                    quantidadeEmprestimosAtivos = quantidadeEmprestimosAtivos,
                    totalEmprestimosAtivos = totalEmprestimosAtivos,
                    devolucoesEmprestimosAtivos = devolucoesEmprestimosAtivos,
                    saldoDevedorAtivos = saldoDevedorAtivos,

                    // Empréstimos Quitados
                    quantidadeEmprestimosQuitados = quantidadeEmprestimosQuitados,
                    totalQuitado = totalQuitado,

                    // Saldo Final
                    saldoDisponivel = saldoFundo,
                    percentualComprometido = totalEntradasFundo > 0
                        ? Math.Round((saldoDevedorAtivos / totalEntradasFundo) * 100, 2)
                        : 0
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
                    saldo = saldo,
                    detalhamento = detalhamento
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saldo do fundo via AJAX");
                return Json(new { erro = ex.Message });
            }
        }

        // NOVO: Visualizar Detalhamento Completo
        [HttpGet]
        public async Task<IActionResult> DetalhamentoFundo()
        {
            var detalhamento = await ObterDetalhamentoSaldoFundo();

            // Buscar histórico de rateios aprovados
            var historico = await _context.ItensRateioFechamento
                .Include(i => i.FechamentoPeriodo)
                    .ThenInclude(f => f.CentroCusto)
                .Include(i => i.RegraRateio)
                    .ThenInclude(r => r.CentroCustoDestino)
                .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                .OrderByDescending(i => i.FechamentoPeriodo.DataAprovacao)
                .Take(20)
                .ToListAsync();

            ViewBag.Detalhamento = detalhamento;
            ViewBag.Historico = historico;

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