using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using System.Text;

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
                var totalEntradasFundo = await _context.ItensRateioFechamento
                    .Include(i => i.FechamentoPeriodo)
                    .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                    .SumAsync(i => i.ValorRateio);

                var totalEmprestimos = await _context.Emprestimos
                    .Where(e => e.Status == StatusEmprestimo.Ativo)
                    .SumAsync(e => e.SaldoDevedor);

                var saldoDisponivel = totalEntradasFundo - totalEmprestimos;

                _logger.LogInformation($"Cálculo Saldo Fundo - Entradas: {totalEntradasFundo:C}, Empréstimos Ativos: {totalEmprestimos:C}, Disponível: {saldoDisponivel:C}");

                return saldoDisponivel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular saldo do fundo");
                return 0;
            }
        }

        // Obter Detalhamento do Saldo do Fundo
        private async Task<object> ObterDetalhamentoSaldoFundo()
        {
            try
            {
                var totalEntradasFundo = await _context.ItensRateioFechamento
                    .Include(i => i.FechamentoPeriodo)
                    .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado)
                    .SumAsync(i => i.ValorRateio);

                var emprestimosAtivos = await _context.Emprestimos
                    .Where(e => e.Status == StatusEmprestimo.Ativo)
                    .ToListAsync();

                var saldoDevedorAtivos = emprestimosAtivos.Sum(e => e.SaldoDevedor);
                var totalDevolvido = emprestimosAtivos.Sum(e => e.ValorDevolvido);

                return new
                {
                    totalEntradasFundo,
                    quantidadeEmprestimosAtivos = emprestimosAtivos.Count,
                    saldoDevedorAtivos,
                    totalDevolvido,
                    saldoDisponivel = totalEntradasFundo - saldoDevedorAtivos,
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