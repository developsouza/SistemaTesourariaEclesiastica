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
            // OPÇÃO 1: Buscar o Centro de Custo "Fundo de Empréstimo" ou "Repasse Central"
            // Se você tem um centro de custo específico para o fundo de empréstimos
            var centroCustoFundo = await _context.CentrosCusto
                .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("FUNDO") ||
                                         c.Nome.ToUpper().Contains("REPASSE CENTRAL") ||
                                         c.Nome.ToUpper().Contains("DÍZIMO DOS DÍZIMOS"));

            decimal totalEntradasFundo = 0;

            if (centroCustoFundo != null)
            {
                // MÉTODO 1: Buscar pelos rateios destinados ao fundo de empréstimos
                // Soma todos os rateios de fechamentos APROVADOS destinados ao fundo
                totalEntradasFundo = await _context.ItensRateioFechamento
                    .Include(i => i.FechamentoPeriodo)
                    .Include(i => i.RegraRateio)
                    .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                               i.RegraRateio.CentroCustoDestinoId == centroCustoFundo.Id)
                    .SumAsync(i => i.ValorRateio);
            }
            else
            {
                // MÉTODO 2 (ALTERNATIVO): Se não houver centro de custo específico,
                // busca por uma regra de rateio específica pelo nome
                var regraFundo = await _context.RegrasRateio
                    .FirstOrDefaultAsync(r => r.Nome.ToUpper().Contains("FUNDO") ||
                                             r.Nome.ToUpper().Contains("REPASSE CENTRAL") ||
                                             r.Nome.ToUpper().Contains("DÍZIMO") ||
                                             r.Percentual == 20); // Assumindo 20% como padrão

                if (regraFundo != null)
                {
                    totalEntradasFundo = await _context.ItensRateioFechamento
                        .Include(i => i.FechamentoPeriodo)
                        .Where(i => i.FechamentoPeriodo.Status == StatusFechamentoPeriodo.Aprovado &&
                                   i.RegraRateioId == regraFundo.Id)
                        .SumAsync(i => i.ValorRateio);
                }
                else
                {
                    // MÉTODO 3 (FALLBACK): Se nada foi encontrado, calcula 20% de todos os fechamentos aprovados da Sede
                    var centroCustoSede = await _context.CentrosCusto
                        .FirstOrDefaultAsync(c => c.Nome.ToUpper().Contains("SEDE") ||
                                                 c.Nome.ToUpper().Contains("GERAL"));

                    if (centroCustoSede != null)
                    {
                        var fechamentosAprovados = await _context.FechamentosPeriodo
                            .Where(f => f.CentroCustoId == centroCustoSede.Id &&
                                       f.Status == StatusFechamentoPeriodo.Aprovado)
                            .SumAsync(f => f.BalancoDigital);

                        totalEntradasFundo = fechamentosAprovados * 0.20m; // 20% do balanço
                    }
                }
            }

            // Total de empréstimos ativos (valor total emprestado e ainda não quitado)
            var totalEmprestimosAtivos = await _context.Emprestimos
                .Where(e => e.Status == StatusEmprestimo.Ativo)
                .SumAsync(e => (decimal?)e.ValorTotal) ?? 0;

            // Total de devoluções de empréstimos ativos (para calcular o saldo devedor real)
            var devolucoesEmprestimosAtivos = await _context.DevolucaoEmprestimos
                .Where(d => d.Emprestimo.Status == StatusEmprestimo.Ativo)
                .SumAsync(d => (decimal?)d.ValorDevolvido) ?? 0;

            // Saldo devedor real dos empréstimos ativos
            var saldoDevedorAtivos = totalEmprestimosAtivos - devolucoesEmprestimosAtivos;

            // Saldo = (Entradas do Fundo) - (Empréstimos Ativos - Devoluções já feitas)
            // Ou seja: Entradas do Fundo - Saldo Devedor Real
            var saldoFundo = totalEntradasFundo - saldoDevedorAtivos;

            return saldoFundo;
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