using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Policy = "OperacoesFinanceiras")]
    public class PorteirosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PorteirosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Porteiros
        public async Task<IActionResult> Index()
        {
            var porteiros = await _context.Porteiros
                .OrderBy(p => p.Nome)
                .ToListAsync();

            return View(porteiros);
        }

        // GET: Porteiros/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Porteiros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Porteiro porteiro)
        {
            if (ModelState.IsValid)
            {
                porteiro.DataCadastro = DateTime.Now;
                porteiro.SalvarDisponibilidade();
                _context.Add(porteiro);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Porteiro cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(porteiro);
        }

        // GET: Porteiros/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var porteiro = await _context.Porteiros.FindAsync(id);
            if (porteiro == null)
            {
                return NotFound();
            }

            porteiro.CarregarDisponibilidade();
            return View(porteiro);
        }

        // POST: Porteiros/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Porteiro porteiro)
        {
            if (id != porteiro.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    porteiro.SalvarDisponibilidade();
                    _context.Update(porteiro);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Porteiro atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PorteiroExists(porteiro.Id))
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
            return View(porteiro);
        }

        // GET: Porteiros/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var porteiro = await _context.Porteiros
                .FirstOrDefaultAsync(m => m.Id == id);
            if (porteiro == null)
            {
                return NotFound();
            }

            return View(porteiro);
        }

        // POST: Porteiros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var porteiro = await _context.Porteiros.FindAsync(id);
            if (porteiro != null)
            {
                var temEscalas = await _context.EscalasPorteiros.AnyAsync(e => e.PorteiroId == id || e.Porteiro2Id == id);
                if (temEscalas)
                {
                    porteiro.Ativo = false;
                    _context.Update(porteiro);
                    TempData["WarningMessage"] = "Porteiro desativado (há escalas associadas).";
                }
                else
                {
                    _context.Porteiros.Remove(porteiro);
                    TempData["SuccessMessage"] = "Porteiro excluído com sucesso!";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PorteiroExists(int id)
        {
            return _context.Porteiros.Any(e => e.Id == id);
        }
    }
}
