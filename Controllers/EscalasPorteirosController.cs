using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Helpers;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.ViewModels;
using System.Security.Claims;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize(Policy = "OperacoesFinanceiras")]
    public class EscalasPorteirosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EscalaPorteiroService _escalaService;
        private readonly ILogger<EscalasPorteirosController> _logger;

        public EscalasPorteirosController(
            ApplicationDbContext context,
            EscalaPorteiroService escalaService,
            ILogger<EscalasPorteirosController> logger)
        {
            _context = context;
            _escalaService = escalaService;
            _logger = logger;
        }

        // GET: EscalasPorteiros
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Today.AddDays(-30);
            dataFim ??= DateTime.Today.AddDays(60);

            var escalas = await _context.EscalasPorteiros
                .Include(e => e.Porteiro)
                .Include(e => e.Responsavel)
                .Include(e => e.UsuarioGeracao)
                .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                .OrderBy(e => e.DataCulto)
                .ToListAsync();

            ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

            return View(escalas);
        }

        // GET: EscalasPorteiros/Gerar
        public async Task<IActionResult> Gerar()
        {
            var viewModel = new GerarEscalaViewModel
            {
                DataInicio = DateTime.Today,
                DataFim = DateTime.Today.AddMonths(1),
                PorteirosDisponiveis = await _context.Porteiros.Where(p => p.Ativo).ToListAsync(),
                ResponsaveisDisponiveis = await _context.ResponsaveisPorteiros.Where(r => r.Ativo).ToListAsync()
            };

            // Sugerir dias baseado nas configurações
            viewModel.DiasSelecionados = await _escalaService.SugerirDiasAsync(
                viewModel.DataInicio,
                viewModel.DataFim);

            return View(viewModel);
        }

        // POST: EscalasPorteiros/Gerar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GerarEscala([FromBody] GerarEscalaViewModel model)
        {
            try
            {
                _logger.LogInformation("Iniciando geração de escala. DataInicio: {DataInicio}, DataFim: {DataFim}, Dias: {Dias}",
                    model.DataInicio, model.DataFim, model.DiasSelecionados?.Count ?? 0);

                // ? VALIDAÇÃO DO MODELSTATE
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();

                    var errorMessage = errors.Any()
                        ? string.Join("; ", errors)
                        : "Dados inválidos. Verifique os campos preenchidos.";

                    _logger.LogWarning("ModelState inválido: {Errors}", errorMessage);
                    return Json(new { success = false, message = errorMessage });
                }

                // Validações básicas
                if (model.DataInicio > model.DataFim)
                {
                    _logger.LogWarning("Data de início posterior à data de fim");
                    return Json(new { success = false, message = "A data de início deve ser anterior à data de fim." });
                }

                if (model.DiasSelecionados == null || !model.DiasSelecionados.Any())
                {
                    _logger.LogWarning("Nenhum dia selecionado");
                    return Json(new { success = false, message = "Selecione pelo menos um dia para gerar a escala." });
                }

                // Validar se há porteiros e responsáveis
                var temPorteiros = await _context.Porteiros.AnyAsync(p => p.Ativo);
                if (!temPorteiros)
                {
                    _logger.LogWarning("Não há porteiros cadastrados");
                    return Json(new { success = false, message = "Não há porteiros cadastrados. Cadastre porteiros antes de gerar a escala." });
                }

                var temResponsaveis = await _context.ResponsaveisPorteiros.AnyAsync(r => r.Ativo);
                if (!temResponsaveis)
                {
                    _logger.LogWarning("Não há responsáveis cadastrados");
                    return Json(new { success = false, message = "Não há responsáveis cadastrados. Cadastre responsáveis antes de gerar a escala." });
                }

                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("Gerando escala para usuário: {UsuarioId}", usuarioId);

                var escalas = await _escalaService.GerarEscalaAsync(model.DiasSelecionados, usuarioId);

                if (escalas.Any())
                {
                    _context.EscalasPorteiros.AddRange(escalas);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Escala gerada com sucesso. Total de dias: {Total}", escalas.Count);

                    return Json(new
                    {
                        success = true,
                        message = $"Escala gerada com sucesso! {escalas.Count} dia(s) de culto cadastrado(s).",
                        redirectUrl = Url.Action("Visualizar", new { dataInicio = model.DataInicio, dataFim = model.DataFim })
                    });
                }

                _logger.LogWarning("Nenhuma escala foi gerada");
                return Json(new { success = false, message = "Nenhuma escala foi gerada. Verifique se já existem escalas para os dias selecionados." });
            }
            catch (ArgumentException argEx)
            {
                // Exceções de validação do serviço (esperadas)
                _logger.LogWarning(argEx, "Erro de validação ao gerar escala");
                return Json(new { success = false, message = argEx.Message });
            }
            catch (InvalidOperationException invEx)
            {
                // Exceções de operação inválida do serviço (esperadas)
                _logger.LogWarning(invEx, "Operação inválida ao gerar escala");
                return Json(new { success = false, message = invEx.Message });
            }
            catch (Exception ex)
            {
                // Exceções inesperadas (não expor detalhes ao usuário)
                _logger.LogError(ex, "Erro inesperado ao gerar escala de porteiros");
                return Json(new
                {
                    success = false,
                    message = "Erro ao processar a solicitação. Tente novamente ou contate o administrador do sistema."
                });
            }
        }

        // GET: EscalasPorteiros/Visualizar
        public async Task<IActionResult> Visualizar(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Today;
            dataFim ??= DateTime.Today.AddMonths(1);

            var escalas = await _context.EscalasPorteiros
                .Include(e => e.Porteiro)
                .Include(e => e.Porteiro2) // ? INCLUIR PORTEIRO2
                .Include(e => e.Responsavel)
                .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                .OrderBy(e => e.DataCulto)
                .ThenBy(e => e.Horario) // ? ORDENAR POR HORÁRIO TAMBÉM
                .ToListAsync();

            // ? CORRIGIR: Buscar TODOS os porteiros que aparecem nas escalas (incluindo Porteiro2)
            var porteirosIds = escalas.Select(e => e.PorteiroId)
                .Union(escalas.Where(e => e.Porteiro2Id.HasValue).Select(e => e.Porteiro2Id!.Value))
                .Distinct()
                .ToList();

            var todosPorteiros = await _context.Porteiros
                .Where(p => porteirosIds.Contains(p.Id))
                .OrderBy(p => p.Nome)
                .ToListAsync();

            var responsavel = escalas.Select(e => e.Responsavel).FirstOrDefault();

            var viewModel = new EscalaGeradaViewModel
            {
                DataInicio = dataInicio.Value,
                DataFim = dataFim.Value,
                Escalas = escalas,
                TodosPorteiros = todosPorteiros,
                Responsavel = responsavel
            };

            return View(viewModel);
        }

        // POST: EscalasPorteiros/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var escala = await _context.EscalasPorteiros.FindAsync(id);
            if (escala != null)
            {
                _context.EscalasPorteiros.Remove(escala);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Escala excluída com sucesso.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: EscalasPorteiros/DeletePeriodo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePeriodo(DateTime dataInicio, DateTime dataFim)
        {
            var escalas = await _context.EscalasPorteiros
                .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                .ToListAsync();

            if (escalas.Any())
            {
                _context.EscalasPorteiros.RemoveRange(escalas);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{escalas.Count} escala(s) excluída(s) com sucesso.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: EscalasPorteiros/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var escala = await _context.EscalasPorteiros
                .Include(e => e.Porteiro)
                .Include(e => e.Responsavel)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (escala == null)
            {
                return NotFound();
            }

            ViewBag.Porteiros = await _context.Porteiros.Where(p => p.Ativo).ToListAsync();
            ViewBag.Responsaveis = await _context.ResponsaveisPorteiros.Where(r => r.Ativo).ToListAsync();

            return View(escala);
        }

        // POST: EscalasPorteiros/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EscalaPorteiro escala)
        {
            if (id != escala.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(escala);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Escala atualizada com sucesso.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EscalaExists(escala.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Porteiros = await _context.Porteiros.Where(p => p.Ativo).ToListAsync();
            ViewBag.Responsaveis = await _context.ResponsaveisPorteiros.Where(r => r.Ativo).ToListAsync();

            return View(escala);
        }

        // GET: EscalasPorteiros/SugerirDias
        [HttpGet]
        public async Task<IActionResult> SugerirDias(DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                _logger.LogInformation("Sugerindo dias. DataInicio: {DataInicio}, DataFim: {DataFim}", dataInicio, dataFim);

                var diasSugeridos = await _escalaService.SugerirDiasAsync(dataInicio, dataFim);

                _logger.LogInformation("Dias sugeridos: {Total}", diasSugeridos.Count);

                return Json(new { success = true, dias = diasSugeridos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sugerir dias");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: EscalasPorteiros/DownloadPdf
        public async Task<IActionResult> DownloadPdf(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                dataInicio ??= DateTime.Today;
                dataFim ??= DateTime.Today.AddMonths(1);

                var escalas = await _context.EscalasPorteiros
                    .Include(e => e.Porteiro)
                    .Include(e => e.Porteiro2) // ? INCLUIR PORTEIRO2
                    .Include(e => e.Responsavel)
                    .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                    .OrderBy(e => e.DataCulto)
                    .ThenBy(e => e.Horario) // ? ORDENAR POR HORÁRIO TAMBÉM
                    .ToListAsync();

                if (!escalas.Any())
                {
                    TempData["Erro"] = "Não há escalas cadastradas para o período selecionado.";
                    return RedirectToAction(nameof(Index));
                }

                // ? CORRIGIR: Buscar TODOS os porteiros que aparecem nas escalas (incluindo Porteiro2)
                var porteirosIds = escalas.Select(e => e.PorteiroId)
                    .Union(escalas.Where(e => e.Porteiro2Id.HasValue).Select(e => e.Porteiro2Id!.Value))
                    .Distinct()
                    .ToList();

                var todosPorteiros = await _context.Porteiros
                    .Where(p => porteirosIds.Contains(p.Id))
                    .OrderBy(p => p.Nome)
                    .ToListAsync();

                var responsavel = escalas.Select(e => e.Responsavel).FirstOrDefault();

                // Gerar PDF
                var pdfBytes = EscalaPorteiroPdfHelper.GerarPdfEscala(
                    escalas,
                    todosPorteiros,
                    responsavel,
                    dataInicio.Value,
                    dataFim.Value
                );

                var nomeArquivo = $"Escala_Porteiros_{dataInicio.Value:yyyy-MM-dd}_a_{dataFim.Value:yyyy-MM-dd}.pdf";

                return File(pdfBytes, "application/pdf", nomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF da escala");
                TempData["Erro"] = "Erro ao gerar PDF. Tente novamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EscalasPorteiros/ManualEscala
        public async Task<IActionResult> ManualEscala(DateTime? dataInicio, DateTime? dataFim)
        {
            try
            {
                dataInicio ??= DateTime.Today;
                dataFim ??= DateTime.Today.AddMonths(1);

                _logger.LogInformation("Carregando página de escala manual. DataInicio: {DataInicio}, DataFim: {DataFim}",
                    dataInicio, dataFim);

                // Buscar porteiros ativos
                var porteiros = await _context.Porteiros
                    .Where(p => p.Ativo)
                    .OrderBy(p => p.Nome)
                    .ToListAsync();

                // Buscar responsáveis ativos
                var responsaveis = await _context.ResponsaveisPorteiros
                    .Where(r => r.Ativo)
                    .OrderBy(r => r.Nome)
                    .ToListAsync();

                if (!porteiros.Any())
                {
                    TempData["ErrorMessage"] = "Não há porteiros cadastrados. Cadastre porteiros antes de criar a escala.";
                    return RedirectToAction(nameof(Index));
                }

                if (!responsaveis.Any())
                {
                    TempData["ErrorMessage"] = "Não há responsáveis cadastrados. Cadastre responsáveis antes de criar a escala.";
                    return RedirectToAction(nameof(Index));
                }

                // Buscar configurações de cultos e sugerir dias
                var diasSugeridos = await _escalaService.SugerirDiasAsync(dataInicio.Value, dataFim.Value);

                // Buscar escalas já existentes no período
                var escalasExistentes = await _context.EscalasPorteiros
                    .Include(e => e.Porteiro)
                    .Include(e => e.Porteiro2)
                    .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                    .ToListAsync();

                // Converter dias sugeridos para modelo de escala manual
                var diasDisponiveis = new List<DiaEscalaManual>();

                foreach (var diaSugerido in diasSugeridos)
                {
                    var diaManual = new DiaEscalaManual
                    {
                        Data = diaSugerido.Data,
                        Horario = diaSugerido.Horario,
                        TipoCulto = diaSugerido.TipoCulto
                    };

                    // Verificar se já existe escala para este dia/horário
                    var escalaExistente = escalasExistentes.FirstOrDefault(e =>
                        e.DataCulto.Date == diaSugerido.Data.Date &&
                        e.Horario == diaSugerido.Horario);

                    if (escalaExistente != null)
                    {
                        // Carregar porteiros já atribuídos
                        if (escalaExistente.PorteiroId > 0)
                        {
                            diaManual.PorteirosAtribuidos.Add(new PorteiroAtribuido
                            {
                                PorteiroId = escalaExistente.PorteiroId,
                                NomePorteiro = escalaExistente.Porteiro?.Nome ?? "Desconhecido",
                                Posicao = 1
                            });
                        }

                        if (escalaExistente.Porteiro2Id.HasValue && escalaExistente.Porteiro2Id.Value > 0)
                        {
                            diaManual.PorteirosAtribuidos.Add(new PorteiroAtribuido
                            {
                                PorteiroId = escalaExistente.Porteiro2Id.Value,
                                NomePorteiro = escalaExistente.Porteiro2?.Nome ?? "Desconhecido",
                                Posicao = 2
                            });
                        }
                    }

                    diasDisponiveis.Add(diaManual);
                }

                var viewModel = new EscalaManualViewModel
                {
                    DataInicio = dataInicio.Value,
                    DataFim = dataFim.Value,
                    DiasDisponiveis = diasDisponiveis,
                    PorteirosDisponiveis = porteiros,
                    ResponsaveisDisponiveis = responsaveis,
                    ResponsavelSelecionadoId = responsaveis.First().Id
                };

                _logger.LogInformation("Página de escala manual carregada com sucesso. {QtdDias} dias, {QtdPorteiros} porteiros",
                    diasDisponiveis.Count, porteiros.Count);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar página de escala manual");
                TempData["ErrorMessage"] = "Erro ao carregar página de escala manual. Tente novamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EscalasPorteiros/SalvarEscalaManual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarEscalaManual([FromBody] SalvarEscalaManualRequest request)
        {
            try
            {
                _logger.LogInformation("Salvando escala manual. DataInicio: {DataInicio}, DataFim: {DataFim}, Escalas: {QtdEscalas}",
                    request.DataInicio, request.DataFim, request.Escalas?.Count ?? 0);

                // Validações
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();

                    var errorMessage = errors.Any()
                        ? string.Join("; ", errors)
                        : "Dados inválidos. Verifique os campos preenchidos.";

                    _logger.LogWarning("ModelState inválido ao salvar escala manual: {Errors}", errorMessage);
                    return Json(new { success = false, message = errorMessage });
                }

                if (request.Escalas == null || !request.Escalas.Any())
                {
                    return Json(new { success = false, message = "Nenhuma escala foi definida. Atribua porteiros aos dias antes de salvar." });
                }

                // Validar responsável
                var responsavel = await _context.ResponsaveisPorteiros.FindAsync(request.ResponsavelId);
                if (responsavel == null)
                {
                    return Json(new { success = false, message = "Responsável inválido." });
                }

                var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var escalasParaSalvar = new List<EscalaPorteiro>();
                var escalasAtualizadas = 0;
                var escalasNovas = 0;

                foreach (var escalaRequest in request.Escalas)
                {
                    // Validar que pelo menos 1 porteiro foi atribuído
                    if (!escalaRequest.PorteiroId.HasValue)
                    {
                        _logger.LogWarning("Escala sem porteiro principal ignorada: {Data} {Horario}",
                            escalaRequest.Data, escalaRequest.Horario);
                        continue;
                    }

                    // Verificar se já existe escala para este dia/horário
                    var escalaExistente = await _context.EscalasPorteiros
                        .FirstOrDefaultAsync(e =>
                            e.DataCulto.Date == escalaRequest.Data.Date &&
                            e.Horario == escalaRequest.Horario);

                    if (escalaExistente != null)
                    {
                        // Atualizar escala existente
                        escalaExistente.PorteiroId = escalaRequest.PorteiroId.Value;
                        escalaExistente.Porteiro2Id = escalaRequest.Porteiro2Id;
                        escalaExistente.ResponsavelId = request.ResponsavelId;
                        escalaExistente.TipoCulto = escalaRequest.TipoCulto;
                        escalaExistente.Observacao = escalaRequest.Observacao;

                        _context.Update(escalaExistente);
                        escalasAtualizadas++;
                    }
                    else
                    {
                        // Criar nova escala
                        var novaEscala = new EscalaPorteiro
                        {
                            DataCulto = escalaRequest.Data,
                            Horario = escalaRequest.Horario,
                            TipoCulto = escalaRequest.TipoCulto,
                            PorteiroId = escalaRequest.PorteiroId.Value,
                            Porteiro2Id = escalaRequest.Porteiro2Id,
                            ResponsavelId = request.ResponsavelId,
                            Observacao = escalaRequest.Observacao ?? "Escala criada manualmente",
                            DataGeracao = DateTime.Now,
                            UsuarioGeracaoId = usuarioId
                        };

                        _context.Add(novaEscala);
                        escalasNovas++;
                    }
                }

                await _context.SaveChangesAsync();

                var mensagem = $"Escala manual salva com sucesso! {escalasNovas} nova(s) escala(s) criada(s), {escalasAtualizadas} escala(s) atualizada(s).";
                _logger.LogInformation(mensagem);

                return Json(new
                {
                    success = true,
                    message = mensagem,
                    redirectUrl = Url.Action("Visualizar", new { dataInicio = request.DataInicio, dataFim = request.DataFim })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar escala manual");
                return Json(new
                {
                    success = false,
                    message = "Erro ao salvar escala manual. Tente novamente ou contate o administrador."
                });
            }
        }

        private bool EscalaExists(int id)
        {
            return _context.EscalasPorteiros.Any(e => e.Id == id);
        }
    }
}
