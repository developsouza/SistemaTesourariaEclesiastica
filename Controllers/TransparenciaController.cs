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
        private readonly RateLimitService _rateLimitService;
        private readonly ILogger<TransparenciaController> _logger;

        public TransparenciaController(
            ApplicationDbContext context,
            AuditService auditService,
            RateLimitService rateLimitService,
            ILogger<TransparenciaController> logger)
        {
            _context = context;
            _auditService = auditService;
            _rateLimitService = rateLimitService;
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
                // Limpar e formatar dados de entrada COM NORMALIZAÇÃO RIGOROSA
                var nomeCompleto = model.NomeCompleto?.Trim().ToUpperInvariant();
                var cpf = LimparCPF(model.CPF);
                var dataNascimento = model.DataNascimento?.Date;

                _logger.LogInformation($"Tentativa de acesso - Nome digitado: '{nomeCompleto}', CPF digitado: '{cpf}'");

                // ✅ VALIDAÇÃO LGPD: Verificar se todos os dados foram fornecidos
                if (string.IsNullOrWhiteSpace(nomeCompleto) || string.IsNullOrWhiteSpace(cpf) || dataNascimento == null)
                {
                    ModelState.AddModelError(string.Empty, "É necessário informar o nome completo, CPF e data de nascimento para acessar a transparência.");
                    await _auditService.LogAsync("TRANSPARENCIA_ACESSO_NEGADO", "Transparencia",
                        $"Tentativa de acesso sem todos os dados obrigatórios");
                    return View("Index", model);
                }

                // 🔒 RATE LIMITING: Verificar se CPF está bloqueado
                var (bloqueado, dataDesbloqueio, tentativasRestantes) = await _rateLimitService.VerificarBloqueioAsync(cpf);

                if (bloqueado)
                {
                    var tempoRestante = dataDesbloqueio.HasValue
                        ? $"{(dataDesbloqueio.Value - DateTime.Now).TotalMinutes:F0} minutos"
                        : "alguns minutos";

                    ModelState.AddModelError(string.Empty,
                        $"Acesso temporariamente bloqueado devido a múltiplas tentativas falhadas. " +
                        $"Tente novamente em {tempoRestante}. " +
                        $"Se você é o titular dos dados e está tendo dificuldades, entre em contato com a secretaria.");

                    await _rateLimitService.RegistrarTentativaAsync(cpf, false, "Bloqueio por rate limit");
                    await _auditService.LogAsync("TRANSPARENCIA_BLOQUEIO_RATE_LIMIT", "Transparencia",
                        $"Tentativa de acesso bloqueada por rate limit: CPF={cpf?.Substring(0, 3)}***");

                    _logger.LogWarning($"Acesso bloqueado por rate limit: CPF={cpf?.Substring(0, 3)}***, Desbloqueio em: {dataDesbloqueio}");

                    return View("Index", model);
                }

                // ✅ MODIFICADO: Buscar membro com comparação normalizada (case-insensitive e trim)
                var membrosAtivos = await _context.Membros
                    .Include(m => m.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.PlanoDeContas)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.MeioDePagamento)
                    .Where(m => m.Ativo)
                    .ToListAsync();

                // ✅ SEGURANÇA LGPD: Validação com Nome + CPF + Data de Nascimento
                var membro = membrosAtivos.FirstOrDefault(m =>
                    m.NomeCompleto.Trim().ToUpperInvariant() == nomeCompleto &&
                    m.CPF.Replace(".", "").Replace("-", "").Replace(" ", "") == cpf &&
                    m.DataNascimento.HasValue &&
                    m.DataNascimento.Value.Date == dataNascimento.Value);

                if (membro == null)
                {
                    // 🔒 RATE LIMITING: Registrar tentativa falhada
                    await _rateLimitService.RegistrarTentativaAsync(cpf, false, "Dados não conferem");

                    // Verificar tentativas restantes após esta falha
                    var (novoBloqueio, novaDataDesbloqueio, novasTentativasRestantes) =
                        await _rateLimitService.VerificarBloqueioAsync(cpf);

                    var mensagemAviso = novasTentativasRestantes > 0
                        ? $" Você tem {novasTentativasRestantes} tentativa(s) restante(s)."
                        : "";

                    // Log detalhado para debug (sem expor dados sensíveis)
                    _logger.LogWarning($"Acesso negado - Dados não conferem. Tentativas restantes: {novasTentativasRestantes}");

                    ModelState.AddModelError(string.Empty,
                        $"Dados não conferem. Verifique se o nome completo, CPF e data de nascimento estão corretos e correspondem ao mesmo cadastro. " +
                        $"Esta verificação é necessária para proteger seus dados conforme a LGPD.{mensagemAviso}");

                    await _auditService.LogAsync("TRANSPARENCIA_ACESSO_NEGADO", "Transparencia",
                        $"Tentativa de acesso com dados inválidos ou incompletos: CPF={cpf?.Substring(0, 3)}***");

                    return View("Index", model);
                }

                // 🔒 RATE LIMITING: Registrar tentativa bem-sucedida (limpa tentativas falhadas)
                await _rateLimitService.RegistrarTentativaAsync(cpf, true, "Acesso autorizado");

                _logger.LogInformation($"Membro autenticado com sucesso: {membro.NomeCompleto} (ID: {membro.Id})");

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
                    $"Acesso autorizado com autenticação adicional LGPD: Membro={membro.NomeCompleto}, CPF={cpf?.Substring(0, 3)}***");

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
                // Limpar e formatar dados de entrada COM NORMALIZAÇÃO RIGOROSA
                var nome = nomeCompleto?.Trim().ToUpperInvariant();
                var cpfLimpo = LimparCPF(cpf);

                // Validar que ao menos um dos dados foi fornecido
                if (string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(cpfLimpo))
                {
                    TempData["Erro"] = "Informe o nome completo ou o CPF para exportar o PDF.";
                    return RedirectToAction("Index");
                }

                // Buscar membro aceitando nome OU CPF (não é necessário exigir ambos para download)
                var membrosAtivos = await _context.Membros
                    .Include(m => m.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.PlanoDeContas)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.CentroCusto)
                    .Include(m => m.Entradas)
                        .ThenInclude(e => e.MeioDePagamento)
                    .Where(m => m.Ativo)
                    .ToListAsync();

                var membro = membrosAtivos.FirstOrDefault(m =>
                    (!string.IsNullOrWhiteSpace(nome) && m.NomeCompleto.Trim().ToUpperInvariant() == nome) ||
                    (!string.IsNullOrWhiteSpace(cpfLimpo) && m.CPF.Replace(".", "").Replace("-", "").Replace(" ", "") == cpfLimpo));

                if (membro == null)
                {
                    TempData["Erro"] = "Membro não encontrado ou dados não conferem.";
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
                    $"PDF gerado para: Membro={membro.NomeCompleto}, CPF={membro.CPF?.Substring(0, 3)}***");

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
