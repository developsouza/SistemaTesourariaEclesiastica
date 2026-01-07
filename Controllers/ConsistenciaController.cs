using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTesourariaEclesiastica.Attributes;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    [AuthorizeRoles(Roles.AdminOnly)]
    public class ConsistenciaController : Controller
    {
        private readonly ConsistenciaService _consistenciaService;
        private readonly AuditService _auditService;
        private readonly ILogger<ConsistenciaController> _logger;

        public ConsistenciaController(
            ConsistenciaService consistenciaService,
            AuditService auditService,
            ILogger<ConsistenciaController> logger)
        {
            _consistenciaService = consistenciaService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard principal de consistência
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Executa diagnóstico completo de consistência
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExecutarDiagnostico()
        {
            try
            {
                _logger.LogInformation("Iniciando diagnóstico de consistência via interface web");

                var relatorio = await _consistenciaService.ExecutarDiagnosticoCompleto();

                await _auditService.LogAsync("Diagnóstico", "Consistencia",
                    $"Diagnóstico executado: {relatorio.TotalInconsistencias} inconsistências encontradas " +
                    $"({relatorio.InconsistenciasCriticas} críticas, {relatorio.InconsistenciasAvisos} avisos)");

                TempData["Sucesso"] = $"Diagnóstico concluído: {relatorio.TotalInconsistencias} inconsistência(s) encontrada(s).";

                return View("Resultado", relatorio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar diagnóstico de consistência");
                TempData["Erro"] = $"Erro ao executar diagnóstico: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Exporta relatório de consistência para JSON
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportarJson()
        {
            try
            {
                var relatorio = await _consistenciaService.ExecutarDiagnosticoCompleto();

                await _auditService.LogAsync("Exportação", "Consistencia", "Relatório de consistência exportado em JSON");

                return Json(relatorio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar relatório de consistência");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
