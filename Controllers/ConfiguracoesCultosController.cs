using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Policy = "OperacoesFinanceiras")]
    public class ConfiguracoesCultosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracoesCultosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ConfiguracoesCultos
        public async Task<IActionResult> Index()
        {
            var configuracoes = await _context.ConfiguracoesCultos
                .OrderBy(c => c.DataEspecifica.HasValue ? 0 : 1) // Datas específicas primeiro
                .ThenBy(c => c.DataEspecifica)
                .ThenBy(c => c.DiaSemana)
                .ToListAsync();

            return View(configuracoes);
        }

        // GET: ConfiguracoesCultos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ConfiguracoesCultos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConfiguracaoCulto configuracao)
        {
            // Validação customizada: deve ter data específica OU dia da semana
            if (!configuracao.DataEspecifica.HasValue && !configuracao.DiaSemana.HasValue)
            {
                ModelState.AddModelError("", "Você deve informar uma Data Específica ou um Dia da Semana.");
                return View(configuracao);
            }

            // Validação: não pode ter ambos preenchidos
            if (configuracao.DataEspecifica.HasValue && configuracao.DiaSemana.HasValue)
            {
                ModelState.AddModelError("", "Você deve escolher APENAS uma opção: Data Específica OU Dia da Semana.");
                return View(configuracao);
            }

            // Validação: data específica não pode ser no passado
            if (configuracao.DataEspecifica.HasValue && configuracao.DataEspecifica.Value.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("DataEspecifica", "A data específica não pode ser no passado.");
                return View(configuracao);
            }

            if (ModelState.IsValid)
            {
                // Verificar se já existe configuração para este dia/data
                bool jaExiste;

                if (configuracao.DataEspecifica.HasValue)
                {
                    // Verificar se já existe configuração para esta data específica
                    jaExiste = await _context.ConfiguracoesCultos
                        .AnyAsync(c => c.DataEspecifica.HasValue &&
                                      c.DataEspecifica.Value.Date == configuracao.DataEspecifica.Value.Date &&
                                      c.Ativo);

                    if (jaExiste)
                    {
                        ModelState.AddModelError("DataEspecifica", "Já existe uma configuração ativa para esta data específica.");
                        return View(configuracao);
                    }
                }
                else if (configuracao.DiaSemana.HasValue)
                {
                    // Verificar se já existe configuração para este dia da semana
                    jaExiste = await _context.ConfiguracoesCultos
                        .AnyAsync(c => c.DiaSemana.HasValue &&
                                      c.DiaSemana.Value == configuracao.DiaSemana.Value &&
                                      c.Ativo);

                    if (jaExiste)
                    {
                        ModelState.AddModelError("DiaSemana", "Já existe uma configuração ativa para este dia da semana.");
                        return View(configuracao);
                    }
                }

                configuracao.DataCadastro = DateTime.Now;
                _context.Add(configuracao);
                await _context.SaveChangesAsync();

                var tipoConfig = configuracao.DataEspecifica.HasValue
                    ? $"data {configuracao.DataEspecifica.Value:dd/MM/yyyy}"
                    : $"dia {configuracao.DiaSemana}";

                TempData["SuccessMessage"] = $"Configuração cadastrada com sucesso para {tipoConfig}!";
                return RedirectToAction(nameof(Index));
            }
            return View(configuracao);
        }

        // GET: ConfiguracoesCultos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configuracao = await _context.ConfiguracoesCultos.FindAsync(id);
            if (configuracao == null)
            {
                return NotFound();
            }
            return View(configuracao);
        }

        // POST: ConfiguracoesCultos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConfiguracaoCulto configuracao)
        {
            if (id != configuracao.Id)
            {
                return NotFound();
            }

            // Validação customizada: deve ter data específica OU dia da semana
            if (!configuracao.DataEspecifica.HasValue && !configuracao.DiaSemana.HasValue)
            {
                ModelState.AddModelError("", "Você deve informar uma Data Específica ou um Dia da Semana.");
                return View(configuracao);
            }

            // Validação: não pode ter ambos preenchidos
            if (configuracao.DataEspecifica.HasValue && configuracao.DiaSemana.HasValue)
            {
                ModelState.AddModelError("", "Você deve escolher APENAS uma opção: Data Específica OU Dia da Semana.");
                return View(configuracao);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verificar se já existe outra configuração para este dia/data
                    bool jaExiste;

                    if (configuracao.DataEspecifica.HasValue)
                    {
                        jaExiste = await _context.ConfiguracoesCultos
                            .AnyAsync(c => c.Id != id &&
                                          c.DataEspecifica.HasValue &&
                                          c.DataEspecifica.Value.Date == configuracao.DataEspecifica.Value.Date &&
                                          c.Ativo);

                        if (jaExiste)
                        {
                            ModelState.AddModelError("DataEspecifica", "Já existe outra configuração ativa para esta data específica.");
                            return View(configuracao);
                        }
                    }
                    else if (configuracao.DiaSemana.HasValue)
                    {
                        jaExiste = await _context.ConfiguracoesCultos
                            .AnyAsync(c => c.Id != id &&
                                          c.DiaSemana.HasValue &&
                                          c.DiaSemana.Value == configuracao.DiaSemana.Value &&
                                          c.Ativo);

                        if (jaExiste)
                        {
                            ModelState.AddModelError("DiaSemana", "Já existe outra configuração ativa para este dia da semana.");
                            return View(configuracao);
                        }
                    }

                    _context.Update(configuracao);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Configuração atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfiguracaoExists(configuracao.Id))
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
            return View(configuracao);
        }

        // GET: ConfiguracoesCultos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configuracao = await _context.ConfiguracoesCultos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (configuracao == null)
            {
                return NotFound();
            }

            return View(configuracao);
        }

        // POST: ConfiguracoesCultos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var configuracao = await _context.ConfiguracoesCultos.FindAsync(id);
            if (configuracao != null)
            {
                _context.ConfiguracoesCultos.Remove(configuracao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuração excluída com sucesso!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ConfiguracaoExists(int id)
        {
            return _context.ConfiguracoesCultos.Any(e => e.Id == id);
        }
    }
}
