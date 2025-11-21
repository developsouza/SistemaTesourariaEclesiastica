using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Policy = "OperacoesFinanceiras")]
    public class ResponsaveisPorteirosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResponsaveisPorteirosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ResponsaveisPorteiros
        public async Task<IActionResult> Index()
        {
            var responsaveis = await _context.ResponsaveisPorteiros
                .OrderBy(r => r.Nome)
                .ToListAsync();

            return View(responsaveis);
        }

        // GET: ResponsaveisPorteiros/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ResponsaveisPorteiros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResponsavelPorteiro responsavel)
        {
            if (ModelState.IsValid)
            {
                responsavel.DataCadastro = DateTime.Now;
                _context.Add(responsavel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Responsável cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            return View(responsavel);
        }

        // GET: ResponsaveisPorteiros/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var responsavel = await _context.ResponsaveisPorteiros.FindAsync(id);
            if (responsavel == null)
            {
                return NotFound();
            }
            return View(responsavel);
        }

        // POST: ResponsaveisPorteiros/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResponsavelPorteiro responsavel)
        {
            if (id != responsavel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(responsavel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Responsável atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResponsavelExists(responsavel.Id))
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
            return View(responsavel);
        }

        // GET: ResponsaveisPorteiros/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var responsavel = await _context.ResponsaveisPorteiros
                .FirstOrDefaultAsync(m => m.Id == id);
            if (responsavel == null)
            {
                return NotFound();
            }

            return View(responsavel);
        }

        // POST: ResponsaveisPorteiros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var responsavel = await _context.ResponsaveisPorteiros.FindAsync(id);
            if (responsavel != null)
            {
                // Verificar se há escalas associadas
                var temEscalas = await _context.EscalasPorteiros.AnyAsync(e => e.ResponsavelId == id);
                if (temEscalas)
                {
                    // Ao invés de deletar, desativar
                    responsavel.Ativo = false;
                    _context.Update(responsavel);
                    TempData["WarningMessage"] = "Responsável desativado (há escalas associadas).";
                }
                else
                {
                    _context.ResponsaveisPorteiros.Remove(responsavel);
                    TempData["SuccessMessage"] = "Responsável excluído com sucesso!";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ResponsavelExists(int id)
        {
            return _context.ResponsaveisPorteiros.Any(e => e.Id == id);
        }
    }
}
