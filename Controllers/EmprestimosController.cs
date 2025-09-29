using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaTesourariaEclesiastica.Controllers
{
    public class EmprestimosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmprestimosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Emprestimos
        public async Task<IActionResult> Index(string filtro = "todos")
        {
            var saldoFundo = await CalcularSaldoFundo();
            ViewBag.SaldoFundo = saldoFundo;
            ViewBag.FiltroAtual = filtro;

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
                    // "todos" não precisa de filtro adicional
            }

            var emprestimos = await query
                .OrderByDescending(e => e.DataEmprestimo)
                .ToListAsync();

            return View(emprestimos);
        }

        // GET: Emprestimos/Create
        public async Task<IActionResult> Create()
        {
            var saldoFundo = await CalcularSaldoFundo();
            ViewBag.SaldoFundo = saldoFundo;
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
                return View(emprestimo);
            }

            if (ModelState.IsValid)
            {
                emprestimo.Status = StatusEmprestimo.Ativo;
                emprestimo.DataEmprestimo = DateTime.Now;

                _context.Add(emprestimo);
                await _context.SaveChangesAsync();

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

            // Validação: não pode devolver mais do que deve
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

            // Atualizar status se for quitação total
            var novoSaldoDevedor = saldoDevedor - valorDevolvido;
            if (novoSaldoDevedor == 0)
            {
                emprestimo.Status = StatusEmprestimo.Quitado;
                emprestimo.DataQuitacao = dataDevolucao;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                mensagem = "Devolução registrada com sucesso!",
                novoSaldoDevedor = novoSaldoDevedor,
                percentualDevolvido = emprestimo.PercentualDevolvido,
                status = emprestimo.Status.ToString()
            });
        }

        // Método auxiliar para calcular o saldo do fundo
        private async Task<decimal> CalcularSaldoFundo()
        {
            // ==================================================
            // PASSO 1: Buscar o Centro de Custo do Fundo
            // ==================================================
            var centroCustoFundo = await _context.CentrosCusto
                .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("FUNDO") ||
                                         c.Nome.ToUpper().Contains("REPASSE") ||
                                         c.Nome.ToUpper().Contains("DÍZIMO DOS DÍZIMOS") ||
                                         c.Nome.ToUpper().Contains("DIZIMO DOS DIZIMOS"));

            decimal totalEntradasFundo = 0;

            // ==================================================
            // PASSO 2: Calcular Total de Entradas no Fundo
            // ==================================================
            if (centroCustoFundo != null)
            {
                // MÉTODO CORRETO: Somar APENAS os rateios aprovados destinados ao fundo
                totalEntradasFundo = await _context.ItensRateioFechamento
                    .Include(i => i.FechamentoPeriodo)
                    .Include(i => i.RegraRateio)
                    .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                               i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                    .SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

                
            }
            else
            {

                var regrasFundo = await _context.RegrasRateio
                    .Where(r => r.Ativo &&
                               (r.Nome.ToUpper().Contains("FUNDO") ||
                                r.Nome.ToUpper().Contains("REPASSE") ||
                                r.Nome.ToUpper().Contains("DÍZIMO") ||
                                r.Nome.ToUpper().Contains("DIZIMO")))
                    .ToListAsync();

                if (regrasFundo.Any())
                {
                    var idsRegras = regrasFundo.Select(r => r.Id).ToList();

                    totalEntradasFundo = await _context.ItensRateioFechamento
                        .Include(i => i.FechamentoPeriodo)
                        .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                                   idsRegras.Contains(i.RegraRateioId))
                        .SumAsync(i => (decimal?)i.ValorRateio) ?? 0;

                }
                else
                {
                                        // Retornar 0 para evitar empréstimos indevidos
                    return 0;
                }
            }

            // ==================================================
            // PASSO 3: Calcular Total Emprestado (Ativo)
            // ==================================================
            var totalEmprestimosAtivos = await _context.Emprestimos
                .Where(e => e.Status == StatusEmprestimo.Ativo)
                .SumAsync(e => (decimal?)e.ValorTotal) ?? 0;

            // ==================================================
            // PASSO 4: Calcular Total Devolvido (dos Ativos)
            // ==================================================
            var devolucoesEmprestimosAtivos = await _context.DevolucaoEmprestimos
                .Include(d => d.Emprestimo)
                .Where(d => d.Emprestimo.Status == StatusEmprestimo.Ativo)
                .SumAsync(d => (decimal?)d.ValorDevolvido) ?? 0;

            // ==================================================
            // PASSO 5: Calcular Saldo Devedor Atual
            // ==================================================
            var saldoDevedorAtivos = totalEmprestimosAtivos - devolucoesEmprestimosAtivos;

            // ==================================================
            // PASSO 6: Calcular Saldo Disponível no Fundo
            // ==================================================
            // Fórmula: Entradas do Fundo - Saldo Devedor Atual
            var saldoFundo = totalEntradasFundo - saldoDevedorAtivos;

            return saldoFundo;
        }

        // ==================================================
        // MÉTODO AUXILIAR: Obter Detalhamento do Saldo (Para Dashboard)
        // ==================================================
        public async Task<object> ObterDetalhamentoSaldoFundo()
        {
            var centroCustoFundo = await _context.CentrosCusto
                .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("FUNDO") ||
                                         c.Nome.ToUpper().Contains("REPASSE") ||
                                         c.Nome.ToUpper().Contains("DÍZIMO DOS DÍZIMOS"));

            decimal totalEntradasFundo = 0;
            int quantidadeFechamentosAprovados = 0;

            if (centroCustoFundo != null)
            {
                var rateiosFundo = await _context.ItensRateioFechamento
                    .Include(i => i.FechamentoPeriodo)
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

        // GET: Emprestimos/ObterSaldoFundo (para AJAX)
        [HttpGet]
        public async Task<IActionResult> ObterSaldoFundo()
        {
            var saldo = await CalcularSaldoFundo();
            return Json(new { saldo = saldo });
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

            // Não permitir excluir se já houver devoluções
            if (emprestimo.Devolucoes.Any())
            {
                TempData["Erro"] = "Não é possível excluir um empréstimo que já possui devoluções registradas.";
                return RedirectToAction(nameof(Index));
            }

            emprestimo.Status = StatusEmprestimo.Cancelado;
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Empréstimo cancelado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}