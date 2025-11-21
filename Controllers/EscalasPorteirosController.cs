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

                // Log dos dias selecionados para debug
                foreach (var dia in model.DiasSelecionados)
                {
                    _logger.LogInformation("Dia selecionado: {Data}, TipoCulto: {TipoCulto}", dia.Data, dia.TipoCulto);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar escala de porteiros");
                return Json(new { success = false, message = $"Erro ao gerar escala: {ex.Message}" });
            }
        }

        // GET: EscalasPorteiros/Visualizar
        public async Task<IActionResult> Visualizar(DateTime? dataInicio, DateTime? dataFim)
        {
            dataInicio ??= DateTime.Today;
            dataFim ??= DateTime.Today.AddMonths(1);

            var escalas = await _context.EscalasPorteiros
                .Include(e => e.Porteiro)
                .Include(e => e.Responsavel)
                .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                .OrderBy(e => e.DataCulto)
                .ToListAsync();

            var todosPorteiros = await _context.Porteiros
                .Where(p => escalas.Select(e => e.PorteiroId).Contains(p.Id))
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
                    .Include(e => e.Responsavel)
                    .Where(e => e.DataCulto >= dataInicio && e.DataCulto <= dataFim)
                    .OrderBy(e => e.DataCulto)
                    .ToListAsync();

                if (!escalas.Any())
                {
                    TempData["Erro"] = "Não há escalas cadastradas para o período selecionado.";
                    return RedirectToAction(nameof(Index));
                }

                var todosPorteiros = await _context.Porteiros
                    .Where(p => escalas.Select(e => e.PorteiroId).Contains(p.Id))
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

        private bool EscalaExists(int id)
        {
            return _context.EscalasPorteiros.Any(e => e.Id == id);
        }
    }
}
