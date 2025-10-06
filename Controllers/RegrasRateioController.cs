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
        private readonly ILogger<RegrasRateioController> _logger;

        public RegrasRateioController(
            ApplicationDbContext context,
            AuditService auditService,
            UserManager<ApplicationUser> userManager,
            ILogger<RegrasRateioController> logger)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: RegrasRateio
        public async Task<IActionResult> Index()
        {
            try
            {
                // PASSO 1: Buscar as regras SEM manipulação
                var regras = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .OrderBy(r => r.CentroCustoOrigem.Nome)
                    .ThenBy(r => r.Nome)
                    .ToListAsync();  // ✅ Executar query PRIMEIRO

                // PASSO 2: DEPOIS manipular em memória
                // Adicionar informações sobre reconhecimento de SEDE/FUNDO
                foreach (var regra in regras)
                {
                    // ✅ CORRIGIDO - passar boolean em vez de enum
                    regra.CentroCustoOrigem.Nome = AdicionarBadgeReconhecimento(
                        regra.CentroCustoOrigem.Nome,
                        true);  // true = origem

                    regra.CentroCustoDestino.Nome = AdicionarBadgeReconhecimento(
                        regra.CentroCustoDestino.Nome,
                        false);  // false = destino
                }

                await _auditService.LogAsync("Visualização", "RegraRateio", "Listagem de regras de rateio visualizada");
                _logger.LogInformation($"Listagem de regras de rateio acessada. Total: {regras.Count}");

                return View(regras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar listagem de regras de rateio");
                TempData["ErrorMessage"] = "Erro ao carregar regras de rateio. Tente novamente.";
                return View(new List<RegraRateio>());
            }
        }

        // GET: RegrasRateio/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Tentativa de acessar detalhes de regra com ID nulo");
                return NotFound();
            }

            try
            {
                var regraRateio = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (regraRateio == null)
                {
                    _logger.LogWarning($"Regra de rateio ID {id} não encontrada");
                    return NotFound();
                }

                // Verificar quantos fechamentos usam esta regra
                var quantidadeUsos = await _context.ItensRateioFechamento
                    .CountAsync(i => i.RegraRateioId == id);

                ViewBag.QuantidadeUsos = quantidadeUsos;

                await _auditService.LogAsync("Visualização", "RegraRateio", $"Detalhes da regra {regraRateio.Nome} visualizados");
                _logger.LogInformation($"Detalhes da regra ID {id} acessados. Usos: {quantidadeUsos}");

                return View(regraRateio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar detalhes da regra ID {id}");
                TempData["ErrorMessage"] = "Erro ao carregar detalhes da regra.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: RegrasRateio/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await PopulateDropdowns();

                // Verificar se existem centros de custo SEDE e FUNDO
                var temSede = await _context.CentrosCusto
                    .AnyAsync(c => c.Ativo &&
                        (c.Nome.ToUpper().Contains("SEDE") ||
                         c.Nome.ToUpper().Contains("GERAL") ||
                         c.Nome.ToUpper().Contains("PRINCIPAL") ||
                         c.Nome.ToUpper().Contains("CENTRAL")));

                var temFundo = await _context.CentrosCusto
                    .AnyAsync(c => c.Ativo &&
                        (c.Nome.ToUpper().Contains("FUNDO") ||
                         c.Nome.ToUpper().Contains("REPASSE") ||
                         c.Nome.ToUpper().Contains("DÍZIMO") ||
                         c.Nome.ToUpper().Contains("DIZIMO")));

                if (!temSede)
                {
                    TempData["WarningMessage"] = "Aviso: Não foi encontrado um Centro de Custo SEDE. Crie um centro com nome contendo 'SEDE' ou 'GERAL'.";
                    _logger.LogWarning("Tentativa de criar regra sem Centro de Custo SEDE");
                }

                if (!temFundo)
                {
                    TempData["WarningMessage"] += " Não foi encontrado um Centro de Custo FUNDO. Crie um centro com nome contendo 'FUNDO' ou 'REPASSE'.";
                    _logger.LogWarning("Tentativa de criar regra sem Centro de Custo FUNDO");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar formulário de criação de regra");
                TempData["ErrorMessage"] = "Erro ao carregar formulário.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RegrasRateio/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao,CentroCustoOrigemId,CentroCustoDestinoId,Percentual")] RegraRateio regraRateio)
        {
            // CRÍTICO: Remover navigation properties da validação do ModelState
            ModelState.Remove("CentroCustoOrigem");
            ModelState.Remove("CentroCustoDestino");
            ModelState.Remove("ItensRateio");

            // LOG para debug - ADICIONE ISTO TEMPORARIAMENTE
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validação 1: Origem e destino diferentes
                    if (regraRateio.CentroCustoOrigemId == regraRateio.CentroCustoDestinoId)
                    {
                        ModelState.AddModelError("CentroCustoDestinoId",
                            "O centro de custo destino deve ser diferente do origem.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    // Validação 2: Centros de custo existem e estão ativos
                    var centroCustoOrigem = await _context.CentrosCusto
                        .FirstOrDefaultAsync(c => c.Id == regraRateio.CentroCustoOrigemId);

                    var centroCustoDestino = await _context.CentrosCusto
                        .FirstOrDefaultAsync(c => c.Id == regraRateio.CentroCustoDestinoId);

                    if (centroCustoOrigem == null || centroCustoDestino == null)
                    {
                        ModelState.AddModelError("", "Um ou ambos os centros de custo selecionados não foram encontrados.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    if (!centroCustoOrigem.Ativo || !centroCustoDestino.Ativo)
                    {
                        ModelState.AddModelError("", "Não é possível criar regra com centros de custo inativos.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    // Validação 3: Verificar regra duplicada
                    var regraExistente = await _context.RegrasRateio
                        .AnyAsync(r => r.CentroCustoOrigemId == regraRateio.CentroCustoOrigemId &&
                                       r.CentroCustoDestinoId == regraRateio.CentroCustoDestinoId &&
                                       r.Ativo);

                    if (regraExistente)
                    {
                        ModelState.AddModelError("",
                            "Já existe uma regra ativa para esta combinação de centros de custo. " +
                            "Desative a regra existente primeiro ou edite-a.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    // Criar regra
                    regraRateio.Ativo = true; // Nova regra sempre começa ativa
                    regraRateio.DataCriacao = DateTime.Now;

                    _context.Add(regraRateio);
                    await _context.SaveChangesAsync();

                    // Log detalhado
                    await _auditService.LogAsync("Criação", "RegraRateio",
                        $"Regra '{regraRateio.Nome}' criada: {centroCustoOrigem.Nome} → {centroCustoDestino.Nome} ({regraRateio.Percentual:F2}%)");

                    _logger.LogInformation(
                        $"Regra de rateio criada: ID={regraRateio.Id}, " +
                        $"Origem={centroCustoOrigem.Nome} (ID={centroCustoOrigem.Id}), " +
                        $"Destino={centroCustoDestino.Nome} (ID={centroCustoDestino.Id}), " +
                        $"Percentual={regraRateio.Percentual:F2}%");

                    // Alertas úteis
                    var avisos = new List<string>();

                    if (!EhReconhecidoComoSede(centroCustoOrigem.Nome))
                    {
                        avisos.Add($"O centro de custo '{centroCustoOrigem.Nome}' não é reconhecido como SEDE. " +
                                  "Rateios só são aplicados em centros com nome contendo: SEDE, GERAL, PRINCIPAL ou CENTRAL.");
                    }

                    if (!EhReconhecidoComoFundo(centroCustoDestino.Nome))
                    {
                        avisos.Add($"O centro de custo destino '{centroCustoDestino.Nome}' não é reconhecido como FUNDO. " +
                                  "Se este for o fundo de empréstimos, renomeie para conter: FUNDO, REPASSE ou DÍZIMO.");
                    }

                    TempData["SuccessMessage"] = "Regra de rateio criada com sucesso!";

                    if (avisos.Any())
                    {
                        TempData["InfoMessage"] = string.Join(" ", avisos);
                    }

                    return RedirectToAction(nameof(Details), new { id = regraRateio.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar regra de rateio");
                    ModelState.AddModelError("", $"Erro ao criar regra: {ex.Message}");
                }
            }
            else
            {
                // LOG ADICIONAL para debug
                _logger.LogWarning("ModelState inválido ao tentar criar regra de rateio");
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

            try
            {
                var regraRateio = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (regraRateio == null)
                {
                    _logger.LogWarning($"Regra ID {id} não encontrada para edição");
                    return NotFound();
                }

                // Verificar se está sendo usada
                var quantidadeUsos = await _context.ItensRateioFechamento
                    .CountAsync(i => i.RegraRateioId == id);

                ViewBag.QuantidadeUsos = quantidadeUsos;

                if (quantidadeUsos > 0)
                {
                    ViewBag.AvisoUso = $"Esta regra já foi aplicada em {quantidadeUsos} fechamento(s). " +
                                      "Alterações afetarão apenas novos fechamentos.";
                }

                await PopulateDropdowns(regraRateio);
                return View(regraRateio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar regra ID {id} para edição");
                TempData["ErrorMessage"] = "Erro ao carregar regra para edição.";
                return RedirectToAction(nameof(Index));
            }
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
                    // Validar centros de custo
                    if (regraRateio.CentroCustoOrigemId == regraRateio.CentroCustoDestinoId)
                    {
                        ModelState.AddModelError("CentroCustoDestinoId",
                            "O centro de custo destino deve ser diferente do origem.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    // Buscar regra original
                    var regraOriginal = await _context.RegrasRateio
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (regraOriginal == null)
                    {
                        return NotFound();
                    }

                    // Verificar conflito com outras regras
                    var conflito = await _context.RegrasRateio
                        .AnyAsync(r => r.Id != id &&
                                       r.CentroCustoOrigemId == regraRateio.CentroCustoOrigemId &&
                                       r.CentroCustoDestinoId == regraRateio.CentroCustoDestinoId &&
                                       r.Ativo);

                    if (conflito && regraRateio.Ativo)
                    {
                        ModelState.AddModelError("",
                            "Já existe outra regra ativa para esta combinação de centros de custo.");
                        await PopulateDropdowns(regraRateio);
                        return View(regraRateio);
                    }

                    _context.Update(regraRateio);
                    await _context.SaveChangesAsync();

                    // Log das alterações
                    var mudancas = new List<string>();

                    if (regraOriginal.Nome != regraRateio.Nome)
                        mudancas.Add($"Nome: '{regraOriginal.Nome}' → '{regraRateio.Nome}'");

                    if (regraOriginal.Percentual != regraRateio.Percentual)
                        mudancas.Add($"Percentual: {regraOriginal.Percentual:F2}% → {regraRateio.Percentual:F2}%");

                    if (regraOriginal.Ativo != regraRateio.Ativo)
                        mudancas.Add($"Status: {(regraOriginal.Ativo ? "Ativo" : "Inativo")} → {(regraRateio.Ativo ? "Ativo" : "Inativo")}");

                    await _auditService.LogAsync("Edição", "RegraRateio",
                        $"Regra '{regraRateio.Nome}' editada. Mudanças: {string.Join(", ", mudancas)}");

                    _logger.LogInformation($"Regra ID {id} editada. Mudanças: {string.Join(", ", mudancas)}");

                    TempData["SuccessMessage"] = "Regra de rateio atualizada com sucesso!";

                    if (!regraRateio.Ativo)
                    {
                        TempData["InfoMessage"] = "A regra foi desativada e não será mais aplicada em novos fechamentos.";
                    }

                    return RedirectToAction(nameof(Details), new { id = regraRateio.Id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!RegraRateioExists(regraRateio.Id))
                    {
                        _logger.LogWarning($"Regra ID {regraRateio.Id} não existe mais (concorrência)");
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, $"Erro de concorrência ao editar regra ID {regraRateio.Id}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao editar regra ID {regraRateio.Id}");
                    ModelState.AddModelError("", $"Erro ao atualizar regra: {ex.Message}");
                }
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

            try
            {
                var regraRateio = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (regraRateio == null)
                {
                    _logger.LogWarning($"Regra ID {id} não encontrada para exclusão");
                    return NotFound();
                }

                // Verificar se está em uso
                var quantidadeUsos = await _context.ItensRateioFechamento
                    .CountAsync(i => i.RegraRateioId == id);

                ViewBag.QuantidadeUsos = quantidadeUsos;
                ViewBag.PodeExcluir = quantidadeUsos == 0;

                if (quantidadeUsos > 0)
                {
                    ViewBag.MensagemUso = $"Esta regra foi utilizada em {quantidadeUsos} fechamento(s) e não pode ser excluída. " +
                                         "Considere desativá-la em vez de excluir.";
                }

                return View(regraRateio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar regra ID {id} para exclusão");
                TempData["ErrorMessage"] = "Erro ao carregar regra para exclusão.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RegrasRateio/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var regraRateio = await _context.RegrasRateio
                    .Include(r => r.CentroCustoOrigem)
                    .Include(r => r.CentroCustoDestino)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (regraRateio == null)
                {
                    _logger.LogWarning($"Tentativa de excluir regra ID {id} inexistente");
                    return NotFound();
                }

                // Verificar se está em uso
                var emUso = await _context.ItensRateioFechamento
                    .AnyAsync(i => i.RegraRateioId == id);

                if (emUso)
                {
                    var quantidade = await _context.ItensRateioFechamento
                        .CountAsync(i => i.RegraRateioId == id);

                    TempData["ErrorMessage"] = $"Esta regra não pode ser excluída pois está sendo utilizada em {quantidade} fechamento(s). " +
                                              "Desative a regra em vez de excluí-la.";

                    _logger.LogWarning($"Tentativa de excluir regra ID {id} que está em uso ({quantidade} fechamentos)");
                    return RedirectToAction(nameof(Delete), new { id });
                }

                // Salvar informações para log antes de excluir
                var nomeRegra = regraRateio.Nome;
                var origemNome = regraRateio.CentroCustoOrigem.Nome;
                var destinoNome = regraRateio.CentroCustoDestino.Nome;
                var percentual = regraRateio.Percentual;

                _context.RegrasRateio.Remove(regraRateio);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Exclusão", "RegraRateio",
                    $"Regra '{nomeRegra}' excluída: {origemNome} → {destinoNome} ({percentual:F2}%)");

                _logger.LogInformation($"Regra ID {id} ('{nomeRegra}') excluída com sucesso");

                TempData["SuccessMessage"] = "Regra de rateio excluída com sucesso!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir regra ID {id}");
                TempData["ErrorMessage"] = $"Erro ao excluir regra: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para popular dropdowns - VERSÃO CORRIGIDA
        private async Task PopulateDropdowns(RegraRateio? regraRateio = null)
        {
            // PASSO 1: Buscar os centros de custo SEM manipulação de string
            var centrosCusto = await _context.CentrosCusto
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .Select(c => new
                {
                    c.Id,
                    c.Nome  // ← SEM manipulação aqui
                })
                .ToListAsync();  // ← Executar a query PRIMEIRO

            // PASSO 2: AGORA sim, manipular em memória (depois da query)
            var centrosCustoComBadges = centrosCusto.Select(c => new
            {
                c.Id,
                Nome = c.Nome +
                       (EhReconhecidoComoSede(c.Nome) ? " ⭐ SEDE" : "") +
                       (EhReconhecidoComoFundo(c.Nome) ? " 💰 FUNDO" : "")
            }).ToList();

            // PASSO 3: Criar os SelectList
            ViewData["CentroCustoOrigemId"] = new SelectList(
                centrosCustoComBadges,
                "Id",
                "Nome",
                regraRateio?.CentroCustoOrigemId
            );

            ViewData["CentroCustoDestinoId"] = new SelectList(
                centrosCustoComBadges,
                "Id",
                "Nome",
                regraRateio?.CentroCustoDestinoId
            );
        }

        // Métodos auxiliares de validação - MANTÉM COMO ESTÃO
        private bool RegraRateioExists(int id)
        {
            return _context.RegrasRateio.Any(e => e.Id == id);
        }

        private bool EhReconhecidoComoSede(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return false;

            var nomeUpper = nome.ToUpper().Trim();
            return nomeUpper.Contains("SEDE") ||
                   nomeUpper.Contains("GERAL") ||
                   nomeUpper.Contains("PRINCIPAL") ||
                   nomeUpper.Contains("CENTRAL");
        }

        private bool EhReconhecidoComoFundo(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return false;

            var nomeUpper = nome.ToUpper().Trim();
            return nomeUpper.Contains("FUNDO") ||
                   nomeUpper.Contains("REPASSE") ||
                   nomeUpper.Contains("DÍZIMO") ||
                   nomeUpper.Contains("DIZIMO");
        }

        private string AdicionarBadgeReconhecimento(string nome, bool ehOrigem)
        {
            if (ehOrigem && EhReconhecidoComoSede(nome))
            {
                return nome; // Já será marcado visualmente na view
            }

            if (!ehOrigem && EhReconhecidoComoFundo(nome))
            {
                return nome; // Já será marcado visualmente na view
            }

            return nome;
        }
    }
}