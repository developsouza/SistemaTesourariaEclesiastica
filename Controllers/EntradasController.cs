using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.Attributes;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Roles = Roles.TodosExcetoUsuario)]
    public class EntradasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public EntradasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Entradas
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId, 
            int? planoContasId, int? membroId, int page = 1, int pageSize = 10)
        {
            ViewData["DataInicio"] = dataInicio?.ToString("yyyy-MM-dd");
            ViewData["DataFim"] = dataFim?.ToString("yyyy-MM-dd");
            ViewData["CentroCustoId"] = centroCustoId;
            ViewData["PlanoContasId"] = planoContasId;
            ViewData["MembroId"] = membroId;

            var entradas = _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Include(e => e.CentroCusto)
                .Include(e => e.PlanoDeContas)
                .Include(e => e.Membro)
                .Include(e => e.Usuario)
                .AsQueryable();

            // Filtros
            if (dataInicio.HasValue)
            {
                entradas = entradas.Where(e => e.Data >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                entradas = entradas.Where(e => e.Data <= dataFim.Value);
            }

            if (centroCustoId.HasValue)
            {
                entradas = entradas.Where(e => e.CentroCustoId == centroCustoId);
            }

            if (planoContasId.HasValue)
            {
                entradas = entradas.Where(e => e.PlanoDeContasId == planoContasId);
            }

            if (membroId.HasValue)
            {
                entradas = entradas.Where(e => e.MembroId == membroId);
            }

            var totalItems = await entradas.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var totalValor = await entradas.SumAsync(e => e.Valor);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalValor = totalValor;

            // Dropdowns para filtros
            ViewBag.CentrosCusto = new SelectList(await _context.CentrosCusto.ToListAsync(), "Id", "Nome", centroCustoId);
            ViewBag.PlanosContas = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Receita).ToListAsync(), 
                "Id", "Descricao", planoContasId);
            ViewBag.Membros = new SelectList(await _context.Membros.ToListAsync(), "Id", "NomeCompleto", membroId);

            var paginatedEntradas = await entradas
                .OrderByDescending(e => e.Data)
                .ThenByDescending(e => e.DataCriacao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedEntradas);
        }

        // GET: Entradas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrada = await _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Include(e => e.CentroCusto)
                .Include(e => e.PlanoDeContas)
                .Include(e => e.Membro)
                .Include(e => e.ModeloRateioEntrada)
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (entrada == null)
            {
                return NotFound();
            }

            return View(entrada);
        }

        // GET: Entradas/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            
            var entrada = new Entrada
            {
                Data = DateTime.Today
            };

            return View(entrada);
        }

        // POST: Entradas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,MembroId,ModeloRateioEntradaId,ComprovanteUrl")] Entrada entrada)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                entrada.UsuarioId = user!.Id;
                entrada.DataCriacao = DateTime.Now;

                // Se for dízimo e tiver membro selecionado, pega o centro de custo do membro
                if (entrada.MembroId.HasValue)
                {
                    var membro = await _context.Membros.FindAsync(entrada.MembroId);
                    if (membro != null)
                    {
                        entrada.CentroCustoId = membro.CentroCustoId;
                    }
                }

                _context.Add(entrada);
                await _context.SaveChangesAsync();
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Criar", "Entrada", entrada.Id.ToString(), $"Entrada de {entrada.Valor:C2} registrada.");
                }
                TempData["SuccessMessage"] = "Entrada registrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(entrada);
            return View(entrada);
        }

        // GET: Entradas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrada = await _context.Entradas.FindAsync(id);
            if (entrada == null)
            {
                return NotFound();
            }

            await PopulateDropdowns(entrada);
            return View(entrada);
        }

        // POST: Entradas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,MembroId,ModeloRateioEntradaId,ComprovanteUrl,UsuarioId,DataCriacao")] Entrada entrada)
        {
            if (id != entrada.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Se for dízimo e tiver membro selecionado, pega o centro de custo do membro
                    if (entrada.MembroId.HasValue)
                    {
                        var membro = await _context.Membros.FindAsync(entrada.MembroId);
                        if (membro != null)
                        {
                            entrada.CentroCustoId = membro.CentroCustoId;
                        }
                    }

                    _context.Update(entrada);
                    await _context.SaveChangesAsync();
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogAuditAsync(user.Id, "Editar", "Entrada", entrada.Id.ToString(), $"Entrada de {entrada.Valor:C2} atualizada.");
                    }
                    TempData["SuccessMessage"] = "Entrada atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EntradaExists(entrada.Id))
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

            await PopulateDropdowns(entrada);
            return View(entrada);
        }

        // GET: Entradas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrada = await _context.Entradas
                .Include(e => e.MeioDePagamento)
                .Include(e => e.CentroCusto)
                .Include(e => e.PlanoDeContas)
                .Include(e => e.Membro)
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (entrada == null)
            {
                return NotFound();
            }

            return View(entrada);
        }

        // POST: Entradas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entrada = await _context.Entradas.FindAsync(id);
            if (entrada != null)
            {
                _context.Entradas.Remove(entrada);
                await _context.SaveChangesAsync();
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "Entrada", entrada.Id.ToString(), $"Entrada de {entrada.Valor:C2} excluída.");
                }
                TempData["SuccessMessage"] = "Entrada excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Obter centro de custo do membro
        [HttpGet]
        public async Task<IActionResult> ObterCentroCustoMembro(int membroId)
        {
            var membro = await _context.Membros
                .Include(m => m.CentroCusto)
                .FirstOrDefaultAsync(m => m.Id == membroId);

            if (membro == null)
            {
                return Json(new { success = false });
            }

            return Json(new 
            { 
                success = true, 
                centroCustoId = membro.CentroCustoId,
                centroCustoNome = membro.CentroCusto.Nome
            });
        }

        private async Task PopulateDropdowns(Entrada? entrada = null)
        {
            ViewData["MeioDePagamentoId"] = new SelectList(
                await _context.MeiosDePagamento.Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync(), 
                "Id", "Nome", entrada?.MeioDePagamentoId);

            ViewData["CentroCustoId"] = new SelectList(
                await _context.CentrosCusto.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync(), 
                "Id", "Nome", entrada?.CentroCustoId);

            ViewData["PlanoDeContasId"] = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Receita && p.Ativo).OrderBy(p => p.Nome).ToListAsync(), 
                "Id", "Nome", entrada?.PlanoDeContasId);

            ViewData["MembroId"] = new SelectList(
                await _context.Membros.Where(m => m.Ativo).OrderBy(m => m.NomeCompleto).ToListAsync(), 
                "Id", "NomeCompleto", entrada?.MembroId);

            ViewData["ModeloRateioEntradaId"] = new SelectList(
                await _context.ModelosRateioEntrada.ToListAsync(), 
                "Id", "Nome", entrada?.ModeloRateioEntradaId);
        }

        private bool EntradaExists(int id)
        {
            return _context.Entradas.Any(e => e.Id == id);
        }
    }
}


