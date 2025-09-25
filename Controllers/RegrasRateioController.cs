using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class RegrasRateioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegrasRateioController(
            ApplicationDbContext context,
            AuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: RegrasRateio
        public async Task<IActionResult> Index()
        {
            var regras = await _context.RegrasRateio
                .Include(r => r.CentroCustoOrigem)
                .Include(r => r.CentroCustoDestino)
                .OrderBy(r => r.CentroCustoOrigem.Nome)
                .ThenBy(r => r.Nome)
                .ToListAsync();

            await _auditService.LogAsync("Visualização", "RegraRateio", "Listagem de regras de rateio visualizada");
            return View(regras);
        }

        // GET: RegrasRateio/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var regraRateio = await _context.RegrasRateio
                .Include(r => r.CentroCustoOrigem)
                .Include(r => r.CentroCustoDestino)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (regraRateio == null)
            {
                return NotFound();
            }

            await _auditService.LogAsync("Visualização", "RegraRateio", $"Detalhes da regra {regraRateio.Nome} visualizados");
            return View(regraRateio);
        }

        // GET: RegrasRateio/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: RegrasRateio/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao,CentroCustoOrigemId,CentroCustoDestinoId,Percentual")] RegraRateio regraRateio)
        {
            ModelState.Remove("CentroCustoOrigem");
            ModelState.Remove("CentroCustoDestino");
            if (ModelState.IsValid)
            {
                // Validar se origem e destino são diferentes
                if (regraRateio.CentroCustoOrigemId == regraRateio.CentroCustoDestinoId)
                {
                    ModelState.AddModelError("CentroCustoDestinoId", "O centro de custo destino deve ser diferente do origem.");
                    await PopulateDropdowns(regraRateio);
                    return View(regraRateio);
                }

                // Verificar se já existe regra para esta combinação
                var regraExistente = await _context.RegrasRateio
                    .AnyAsync(r => r.CentroCustoOrigemId == regraRateio.CentroCustoOrigemId &&
                                   r.CentroCustoDestinoId == regraRateio.CentroCustoDestinoId &&
                                   r.Ativo);

                if (regraExistente)
                {
                    ModelState.AddModelError("", "Já existe uma regra ativa para esta combinação de centros de custo.");
                    await PopulateDropdowns(regraRateio);
                    return View(regraRateio);
                }

                _context.Add(regraRateio);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Criação", "RegraRateio", $"Regra de rateio {regraRateio.Nome} criada");
                TempData["SuccessMessage"] = "Regra de rateio criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(regraRateio);
            return View(regraRateio);
        }

        // GET: RegrasRateio/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var regraRateio = await _context.RegrasRateio.FindAsync(id);
            if (regraRateio == null)
            {
                return NotFound();
            }

            await PopulateDropdowns(regraRateio);
            return View(regraRateio);
        }

        // POST: RegrasRateio/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao,CentroCustoOrigemId,CentroCustoDestinoId,Percentual,Ativo,DataCriacao")] RegraRateio regraRateio)
        {
            if (id != regraRateio.Id)
            {
                return NotFound();
            }

            ModelState.Remove("CentroCustoOrigem");
            ModelState.Remove("CentroCustoDestino");
            if (ModelState.IsValid)
            {
                try
                {
                    // Validar se origem e destino são diferentes
                    if (regraRateio.CentroCustoOrigemId == regraRateio.CentroCustoDestinoId)
                    {
                        ModelState.AddModelError("CentroCustoDestinoId", "O centro de custo destino deve ser diferente do origem.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    _context.Update(regraRateio);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Edição", "RegraRateio", $"Regra de rateio {regraRateio.Nome} editada");
                    TempData["SuccessMessage"] = "Regra de rateio atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RegraRateioExists(regraRateio.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(regraRateio);
            return View(regraRateio);
        }

        // GET: RegrasRateio/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var regraRateio = await _context.RegrasRateio
                .Include(r => r.CentroCustoOrigem)
                .Include(r => r.CentroCustoDestino)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (regraRateio == null)
            {
                return NotFound();
            }

            return View(regraRateio);
        }

        // POST: RegrasRateio/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var regraRateio = await _context.RegrasRateio.FindAsync(id);
            if (regraRateio != null)
            {
                // Verificar se a regra está sendo usada
                var emUso = await _context.ItensRateioFechamento
                    .AnyAsync(i => i.RegraRateioId == id);

                if (emUso)
                {
                    TempData["ErrorMessage"] = "Esta regra não pode ser excluída pois está sendo utilizada em fechamentos.";
                    return RedirectToAction(nameof(Index));
                }

                _context.RegrasRateio.Remove(regraRateio);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Exclusão", "RegraRateio", $"Regra de rateio {regraRateio.Nome} excluída");
                TempData["SuccessMessage"] = "Regra de rateio excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(RegraRateio? regraRateio = null)
        {
            ViewData["CentroCustoOrigemId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", regraRateio?.CentroCustoOrigemId);

            ViewData["CentroCustoDestinoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(),
                "Id", "Nome", regraRateio?.CentroCustoDestinoId);
        }

        private bool RegraRateioExists(int id)
        {
            return _context.RegrasRateio.Any(e => e.Id == id);
        }
    }
}
