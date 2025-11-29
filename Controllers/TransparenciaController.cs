using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Enums;
using SistemaTesourariaEclesiastica.Services;
using SistemaTesourariaEclesiastica.ViewModels;
using System.Text.RegularExpressions;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [AllowAnonymous]
    public class TransparenciaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly ILogger<TransparenciaController> _logger;

        public TransparenciaController(
            ApplicationDbContext context,
            AuditService auditService,
            ILogger<TransparenciaController> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: Transparencia/Index
        [HttpGet]
        public IActionResult Index()
        {
            return View(new TransparenciaValidacaoViewModel());
        }

        // POST: Transparencia/Validar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Validar(TransparenciaValidacaoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                // Limpar e formatar dados de entrada
                var nomeCompleto = model.NomeCompleto?.Trim();
                var cpf = LimparCPF(model.CPF);

                // Buscar membro por nome ou CPF
                var membro = await _context.Membros
                    .Include(m => m.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.PlanoDeContas)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.MeioDePagamento)
                    .FirstOrDefaultAsync(m => m.Ativo &&
                        ((!string.IsNullOrEmpty(nomeCompleto) && m.NomeCompleto.ToLower() == nomeCompleto.ToLower()) ||
                         (!string.IsNullOrEmpty(cpf) && m.CPF == cpf)));

                if (membro == null)
                {
                    ModelState.AddModelError(string.Empty, "Membro não encontrado. Verifique se o nome completo ou CPF estão corretos.");
                    await _auditService.LogAsync("TRANSPARENCIA_ACESSO_NEGADO", "Transparencia",
                        $"Tentativa de acesso com dados inválidos: Nome={nomeCompleto}, CPF={cpf?.Substring(0, 3)}***");
                    return View("Index", model);
                }

                // Buscar apenas entradas APROVADAS do membro
                var idsEntradasAprovadas = await _context.Entradas
                    .Where(e => e.MembroId == membro.Id)
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                var contribuicoes = membro.Entradas
                    .Where(e => idsEntradasAprovadas.Contains(e.Id))
                    .OrderByDescending(e => e.Data)
                    .ToList();

                // Criar ViewModel para exibição
                var viewModel = new TransparenciaHistoricoViewModel
                {
                    MembroNome = membro.NomeCompleto,
                    MembroApelido = membro.Apelido,
                    CentroCustoNome = membro.CentroCusto?.Nome,
                    DataCadastro = membro.DataCadastro,
                    Contribuicoes = contribuicoes,
                    TotalContribuido = contribuicoes.Sum(c => c.Valor),
                    QuantidadeContribuicoes = contribuicoes.Count,
                    UltimaContribuicao = contribuicoes.Any() ? contribuicoes.First().Data : (DateTime?)null
                };

                // Registrar acesso bem-sucedido
                await _auditService.LogAsync("TRANSPARENCIA_ACESSO_SUCESSO", "Transparencia",
                    $"Acesso autorizado: Membro={membro.NomeCompleto}, CPF={cpf?.Substring(0, 3)}***");

                return View("Historico", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar acesso à transparência");
                ModelState.AddModelError(string.Empty, "Erro ao processar solicitação. Tente novamente.");
                return View("Index", model);
            }
        }

        // Método auxiliar para limpar CPF
        private string? LimparCPF(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return null;

            // Remove tudo que não for número
            return Regex.Replace(cpf, @"[^\d]", "");
        }

        // GET: Transparencia/ExportarPdf
        [HttpGet]
        public async Task<IActionResult> ExportarPdf(string? nomeCompleto, string? cpf)
        {
            try
            {
                // Limpar e formatar dados de entrada
                var nome = nomeCompleto?.Trim();
                var cpfLimpo = LimparCPF(cpf);

                // Validar entrada
                if (string.IsNullOrEmpty(nome) && string.IsNullOrEmpty(cpfLimpo))
                {
                    TempData["Erro"] = "Informe o nome completo ou CPF para exportar o PDF.";
                    return RedirectToAction("Index");
                }

                // Buscar membro
                var membro = await _context.Membros
                    .Include(m => m.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.PlanoDeContas)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.MeioDePagamento)
                    .FirstOrDefaultAsync(m => m.Ativo &&
                        ((!string.IsNullOrEmpty(nome) && m.NomeCompleto.ToLower() == nome.ToLower()) ||
                         (!string.IsNullOrEmpty(cpfLimpo) && m.CPF == cpfLimpo)));

                if (membro == null)
                {
                    TempData["Erro"] = "Membro não encontrado.";
                    return RedirectToAction("Index");
                }

                // Buscar apenas entradas APROVADAS do membro
                var idsEntradasAprovadas = await _context.Entradas
                    .Where(e => e.MembroId == membro.Id)
                    .Where(e => _context.FechamentosPeriodo.Any(f =>
                        f.CentroCustoId == e.CentroCustoId &&
                        f.Status == StatusFechamentoPeriodo.Aprovado &&
                        e.Data >= f.DataInicio &&
                        e.Data <= f.DataFim))
                    .Select(e => e.Id)
                    .ToListAsync();

                var contribuicoes = membro.Entradas
                    .Where(e => idsEntradasAprovadas.Contains(e.Id))
                    .OrderByDescending(e => e.Data)
                    .ToList();

                // Gerar PDF
                var pdfBytes = Helpers.TransparenciaPdfHelper.GerarPdfTransparencia(
                    membro.NomeCompleto,
                    membro.Apelido,
                    membro.CentroCusto?.Nome,
                    membro.DataCadastro,
                    contribuicoes.Sum(c => c.Valor),
                    contribuicoes.Count,
                    contribuicoes.Any() ? contribuicoes.First().Data : (DateTime?)null,
                    contribuicoes);

                // Registrar download
                await _auditService.LogAsync("TRANSPARENCIA_PDF_DOWNLOAD", "Transparencia",
                    $"PDF gerado para: Membro={membro.NomeCompleto}, CPF={cpfLimpo?.Substring(0, 3)}***");

                var nomeArquivo = $"Historico_Contribuicoes_{membro.NomeCompleto.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", nomeArquivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar PDF de transparência");
                TempData["Erro"] = "Erro ao gerar PDF. Tente novamente.";
                return RedirectToAction("Index");
            }
        }
    }
}
