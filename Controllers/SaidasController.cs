using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Roles = Roles.AdminOuTesoureiro)]
    public class SaidasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public SaidasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Saidas
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, int? centroCustoId,
            int? planoContasId, int? fornecedorId, TipoDespesa? tipoDespesa, int page = 1, int pageSize = 10)
        {
            ViewData["DataInicio"] = dataInicio?.ToString("yyyy-MM-dd");
            ViewData["DataFim"] = dataFim?.ToString("yyyy-MM-dd");
            ViewData["CentroCustoId"] = centroCustoId;
            ViewData["PlanoContasId"] = planoContasId;
            ViewData["FornecedorId"] = fornecedorId;
            ViewData["TipoDespesa"] = tipoDespesa;

            var user = await _userManager.GetUserAsync(User);

            var saidas = _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .AsQueryable();

            // ==================== FILTRO POR PERFIL DE USUÁRIO ====================
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                // Tesoureiros Locais e Pastores só veem dados do seu centro de custo
                if (user.CentroCustoId.HasValue)
                {
                    saidas = saidas.Where(s => s.CentroCustoId == user.CentroCustoId.Value);
                }
                else
                {
                    // Se não tem centro de custo, não vê nada
                    saidas = saidas.Where(s => false);
                }
            }
            // ======================================================================

            // Filtros de pesquisa
            if (dataInicio.HasValue)
            {
                saidas = saidas.Where(s => s.Data >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                saidas = saidas.Where(s => s.Data <= dataFim.Value);
            }

            if (centroCustoId.HasValue)
            {
                saidas = saidas.Where(s => s.CentroCustoId == centroCustoId);
            }

            if (planoContasId.HasValue)
            {
                saidas = saidas.Where(s => s.PlanoDeContasId == planoContasId);
            }

            if (fornecedorId.HasValue)
            {
                saidas = saidas.Where(s => s.FornecedorId == fornecedorId);
            }

            if (tipoDespesa.HasValue)
            {
                saidas = saidas.Where(s => s.TipoDespesa == tipoDespesa);
            }

            var totalItems = await saidas.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var totalValor = await saidas.SumAsync(s => s.Valor);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalValor = totalValor;

            // Dropdowns para filtros (respeitando permissões)
            var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewBag.CentrosCusto = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", centroCustoId);
            ViewBag.PlanosContas = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa && p.Ativo).ToListAsync(),
                "Id", "Nome", planoContasId);
            ViewBag.Fornecedores = new SelectList(await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(), "Id", "Nome", fornecedorId);

            var paginatedSaidas = await saidas
                .OrderByDescending(s => s.Data)
                .ThenByDescending(s => s.DataCriacao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(paginatedSaidas);
        }

        // GET: Saidas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saida = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (saida == null)
            {
                return NotFound();
            }

            // Verificar permissão de acesso
            if (!await CanAccessSaida(saida))
            {
                return Forbid();
            }

            return View(saida);
        }

        // GET: Saidas/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();

            var user = await _userManager.GetUserAsync(User);
            var saida = new Saida
            {
                Data = DateTime.Today,
                CentroCustoId = user.CentroCustoId ?? 0
            };

            return View(saida);
        }

        // POST: Saidas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,FornecedorId,TipoDespesa,NumeroDocumento,DataVencimento,ComprovanteUrl,Observacoes")] Saida saida)
        {
            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Fornecedor");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");

                // Verificar permissão de acesso ao centro de custo
                if (!await CanAccessCentroCusto(saida.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para criar saídas neste centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    saida.UsuarioId = user!.Id;
                    saida.DataCriacao = DateTime.Now;

                    _context.Add(saida);
                    await _context.SaveChangesAsync();

                    // Log de auditoria
                    if (user != null)
                    {
                        await _auditService.LogCreateAsync(user.Id, saida, saida.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Saída registrada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(saida);
                return View(saida);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Erro interno ao salvar saída. Tente novamente.");
                await PopulateDropdowns(saida);
                return View(saida);
            }
        }

        // GET: Saidas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saida = await _context.Saidas.FindAsync(id);
            if (saida == null)
            {
                return NotFound();
            }

            // Verificar permissão de acesso
            if (!await CanAccessSaida(saida))
            {
                return Forbid();
            }

            await PopulateDropdowns(saida);
            return View(saida);
        }

        // POST: Saidas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Data,Valor,Descricao,MeioDePagamentoId,CentroCustoId,PlanoDeContasId,FornecedorId,TipoDespesa,NumeroDocumento,DataVencimento,ComprovanteUrl,Observacoes")] Saida saida)
        {
            if (id != saida.Id)
            {
                return NotFound();
            }

            try
            {
                // CRÍTICO: Remover navigation properties do ModelState
                ModelState.Remove("MeioDePagamento");
                ModelState.Remove("CentroCusto");
                ModelState.Remove("PlanoDeContas");
                ModelState.Remove("Fornecedor");
                ModelState.Remove("Usuario");
                ModelState.Remove("UsuarioId");
                ModelState.Remove("DataCriacao");

                // Buscar saída original para manter dados de auditoria
                var originalSaida = await _context.Saidas.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                if (originalSaida == null) return NotFound();

                // Verificar permissão de acesso à saída original
                if (!await CanAccessSaida(originalSaida))
                {
                    return Forbid();
                }

                // Verificar permissão de acesso ao novo centro de custo
                if (!await CanAccessCentroCusto(saida.CentroCustoId))
                {
                    ModelState.AddModelError("CentroCustoId", "Você não tem permissão para mover saídas para este centro de custo.");
                }

                if (ModelState.IsValid)
                {
                    // Manter dados originais de auditoria
                    saida.UsuarioId = originalSaida.UsuarioId;
                    saida.DataCriacao = originalSaida.DataCriacao;

                    _context.Update(saida);
                    await _context.SaveChangesAsync();

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _auditService.LogUpdateAsync(user.Id, originalSaida, saida, saida.Id.ToString());
                    }

                    TempData["SuccessMessage"] = "Saída atualizada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns(saida);
                return View(saida);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SaidaExists(saida.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Erro interno ao atualizar saída. Tente novamente.");
                await PopulateDropdowns(saida);
                return View(saida);
            }
        }

        // GET: Saidas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var saida = await _context.Saidas
                .Include(s => s.MeioDePagamento)
                .Include(s => s.CentroCusto)
                .Include(s => s.PlanoDeContas)
                .Include(s => s.Fornecedor)
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (saida == null)
            {
                return NotFound();
            }

            // Verificar permissão de acesso
            if (!await CanAccessSaida(saida))
            {
                return Forbid();
            }

            return View(saida);
        }

        // POST: Saidas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var saida = await _context.Saidas.FindAsync(id);
            if (saida != null)
            {
                // Verificar permissão de acesso
                if (!await CanAccessSaida(saida))
                {
                    return Forbid();
                }

                _context.Saidas.Remove(saida);
                await _context.SaveChangesAsync();

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _auditService.LogAuditAsync(user.Id, "Excluir", "Saida", saida.Id.ToString(), $"Saída de {saida.Valor:C2} excluída.");
                }
                TempData["SuccessMessage"] = "Saída excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Obter dados do fornecedor
        [HttpGet]
        public async Task<IActionResult> ObterDadosFornecedor(int fornecedorId)
        {
            var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Id == fornecedorId);

            if (fornecedor == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                nome = fornecedor.Nome,
                documento = fornecedor.CNPJ ?? fornecedor.CPF,
                telefone = fornecedor.Telefone,
                email = fornecedor.Email
            });
        }

        #region Métodos Auxiliares

        private async Task PopulateDropdowns(Saida? saida = null)
        {
            var user = await _userManager.GetUserAsync(User);

            // Centros de custo (baseado em permissões)
            var centrosCustoQuery = _context.CentrosCusto.Where(c => c.Ativo);
            if (!User.IsInRole(Roles.Administrador) && !User.IsInRole(Roles.TesoureiroGeral))
            {
                if (user.CentroCustoId.HasValue)
                {
                    centrosCustoQuery = centrosCustoQuery.Where(c => c.Id == user.CentroCustoId.Value);
                }
            }

            ViewData["CentroCustoId"] = new SelectList(await centrosCustoQuery.ToListAsync(), "Id", "Nome", saida?.CentroCustoId);

            ViewData["PlanoDeContasId"] = new SelectList(
                await _context.PlanosDeContas.Where(p => p.Tipo == TipoPlanoContas.Despesa && p.Ativo).ToListAsync(),
                "Id", "Nome", saida?.PlanoDeContasId);

            ViewData["MeioDePagamentoId"] = new SelectList(
                await _context.MeiosDePagamento.Where(m => m.Ativo).ToListAsync(),
                "Id", "Nome", saida?.MeioDePagamentoId);

            ViewData["FornecedorId"] = new SelectList(
                await _context.Fornecedores.Where(f => f.Ativo).ToListAsync(),
                "Id", "Nome", saida?.FornecedorId);
        }

        private async Task<bool> CanAccessSaida(Saida saida)
        {
            // Administradores e Tesoureiros Gerais podem acessar tudo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar saídas do seu centro de custo
            return user.CentroCustoId.HasValue && saida.CentroCustoId == user.CentroCustoId.Value;
        }

        private async Task<bool> CanAccessCentroCusto(int centroCustoId)
        {
            // Administradores e Tesoureiros Gerais podem acessar qualquer centro de custo
            if (User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.TesoureiroGeral))
                return true;

            var user = await _userManager.GetUserAsync(User);

            // Outros usuários só podem acessar seu próprio centro de custo
            return user.CentroCustoId.HasValue && centroCustoId == user.CentroCustoId.Value;
        }

        private bool SaidaExists(int id)
        {
            return _context.Saidas.Any(e => e.Id == id);
        }

        #endregion
    }
}