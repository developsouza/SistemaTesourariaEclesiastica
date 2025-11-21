using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Style;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using SistemaTesourariaEclesiastica.Services;

namespace SistemaTesourariaEclesiastica.Controllers
{
    [Authorize]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditoriaController(ApplicationDbContext context, AuditService auditService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: Auditoria
        public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, string? usuarioId, string? entidade, int page = 1, int pageSize = 50)
        {
            if (!dataInicio.HasValue)
            {
                dataInicio = DateTime.Now.AddDays(-30);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            }

            ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

            // Filtros para dropdowns
            var usuarios = await _userManager.Users.ToListAsync();
            ViewBag.Usuarios = new SelectList(usuarios, "Id", "Email", usuarioId);

            var entidades = await _context.LogsAuditoria
                .Select(l => l.Entidade)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
            ViewBag.Entidades = new SelectList(entidades, entidade);

            var logs = await _auditService.GetAuditLogsAsync(dataInicio, dataFim, usuarioId, entidade, page, pageSize);
            var totalLogs = await _auditService.GetAuditLogsCountAsync(dataInicio, dataFim, usuarioId, entidade);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
            ViewBag.TotalLogs = totalLogs;

            // Carregar informações dos usuários
            var logsWithUsers = new List<dynamic>();
            foreach (var log in logs)
            {
                var user = await _userManager.FindByIdAsync(log.UsuarioId);
                logsWithUsers.Add(new
                {
                    Log = log,
                    Usuario = user
                });
            }

            ViewBag.LogsWithUsers = logsWithUsers;

            return View(logs);
        }

        // GET: Auditoria/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            await _auditService.LogAsync("AUDIT_DASHBOARD_ACCESS", "Auditoria", "Acesso ao dashboard de auditoria");

            try
            {
                var hoje = DateTime.Now.Date;
                var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);
                var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

                var stats = new
                {
                    LogsHoje = await _context.LogsAuditoria
                        .Where(l => l.DataHora.Date == hoje)
                        .CountAsync(),
                    LogsSemana = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= inicioSemana)
                        .CountAsync(),
                    LogsMes = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= inicioMes)
                        .CountAsync(),
                    UsuariosAtivosMes = await _context.LogsAuditoria
                        .Where(l => l.DataHora >= inicioMes)
                        .Select(l => l.UsuarioId)
                        .Distinct()
                        .CountAsync()
                };

                // Atividade por hora (últimas 24h)
                var ultimasHoras = DateTime.Now.AddHours(-24);
                var atividadePorHora = await _context.LogsAuditoria
                    .Where(l => l.DataHora >= ultimasHoras)
                    .GroupBy(l => l.DataHora.Hour)
                    .Select(g => new { Hora = g.Key, Quantidade = g.Count() })
                    .OrderBy(x => x.Hora)
                    .ToListAsync();

                // Preparar dados para todas as 24 horas (incluir horas sem atividade)
                var todasHoras = new List<object>();
                for (int hora = 0; hora < 24; hora++)
                {
                    var atividade = atividadePorHora.FirstOrDefault(a => a.Hora == hora);
                    todasHoras.Add(new
                    {
                        Hora = hora,
                        Quantidade = atividade?.Quantidade ?? 0
                    });
                }

                ViewBag.Stats = stats;
                ViewBag.AtividadePorHora = todasHoras;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erro ao carregar estatísticas";

                // Valores padrão em caso de erro
                ViewBag.Stats = new
                {
                    LogsHoje = 0,
                    LogsSemana = 0,
                    LogsMes = 0,
                    UsuariosAtivosMes = 0
                };
                ViewBag.AtividadePorHora = new List<object>();
            }

            return View();
        }

        // GET: Auditoria/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var logAuditoria = await _context.LogsAuditoria
                .FirstOrDefaultAsync(m => m.Id == id);

            if (logAuditoria == null)
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(logAuditoria.UsuarioId);
            ViewBag.Usuario = usuario;

            return View(logAuditoria);
        }

        // GET: Auditoria/Estatisticas
        public async Task<IActionResult> Estatisticas(DateTime? dataInicio, DateTime? dataFim)
        {
            if (!dataInicio.HasValue)
            {
                dataInicio = DateTime.Now.AddDays(-30);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            }

            ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

            var query = _context.LogsAuditoria
                .Where(l => l.DataHora >= dataInicio && l.DataHora <= dataFim);

            // Estatísticas gerais
            var totalLogs = await query.CountAsync();
            var totalUsuarios = await query.Select(l => l.UsuarioId).Distinct().CountAsync();

            // Ações mais comuns
            var acoesMaisComuns = await query
                .GroupBy(l => l.Acao)
                .Select(g => new { Acao = g.Key, Quantidade = g.Count() })
                .OrderByDescending(x => x.Quantidade)
                .Take(10)
                .ToListAsync();

            // Entidades mais acessadas
            var entidadesMaisAcessadas = await query
                .GroupBy(l => l.Entidade)
                .Select(g => new { Entidade = g.Key, Quantidade = g.Count() })
                .OrderByDescending(x => x.Quantidade)
                .Take(10)
                .ToListAsync();

            // Usuários mais ativos
            var usuariosMaisAtivos = await query
                .GroupBy(l => l.UsuarioId)
                .Select(g => new { UsuarioId = g.Key, Quantidade = g.Count() })
                .OrderByDescending(x => x.Quantidade)
                .Take(10)
                .ToListAsync();

            // Carregar nomes dos usuários
            var usuariosComNomes = new List<dynamic>();
            foreach (var item in usuariosMaisAtivos)
            {
                var user = await _userManager.FindByIdAsync(item.UsuarioId);
                usuariosComNomes.Add(new
                {
                    Usuario = user?.Email ?? "Usuário não encontrado",
                    Quantidade = item.Quantidade
                });
            }

            // Atividade por hora do dia
            var atividadePorHora = await query
                .GroupBy(l => l.DataHora.Hour)
                .Select(g => new { Hora = g.Key, Quantidade = g.Count() })
                .OrderBy(x => x.Hora)
                .ToListAsync();

            // Atividade por dia da semana
            var logsParaDiaSemana = await query.ToListAsync();
            var atividadePorDiaSemana = logsParaDiaSemana
                .GroupBy(l => l.DataHora.DayOfWeek)
                .Select(g => new { DiaSemana = g.Key, Quantidade = g.Count() })
                .OrderBy(x => x.DiaSemana)
                .ToList();

            ViewBag.TotalLogs = totalLogs;
            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.AcoesMaisComuns = acoesMaisComuns;
            ViewBag.EntidadesMaisAcessadas = entidadesMaisAcessadas;
            ViewBag.UsuariosMaisAtivos = usuariosComNomes;
            ViewBag.AtividadePorHora = atividadePorHora;
            ViewBag.AtividadePorDiaSemana = atividadePorDiaSemana;

            return View();
        }

        // GET: Auditoria/ExportarExcel
        public async Task<IActionResult> ExportarExcel(DateTime? dataInicio, DateTime? dataFim, string? usuarioId, string? entidade)
        {
            if (!dataInicio.HasValue)
            {
                dataInicio = DateTime.Now.AddDays(-30);
            }
            if (!dataFim.HasValue)
            {
                dataFim = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            }

            var logs = await _auditService.GetAuditLogsAsync(dataInicio, dataFim, usuarioId, entidade, 1, 10000);

            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Logs de Auditoria");

            // Cabeçalhos
            worksheet.Cells[1, 1].Value = "Data/Hora";
            worksheet.Cells[1, 2].Value = "Usuário";
            worksheet.Cells[1, 3].Value = "Ação";
            worksheet.Cells[1, 4].Value = "Entidade";
            worksheet.Cells[1, 5].Value = "ID da Entidade";
            worksheet.Cells[1, 6].Value = "Endereço IP";
            worksheet.Cells[1, 7].Value = "Detalhes";

            // Estilo do cabeçalho
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Dados
            int row = 2;
            foreach (var log in logs)
            {
                var user = await _userManager.FindByIdAsync(log.UsuarioId);

                worksheet.Cells[row, 1].Value = log.DataHora.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cells[row, 2].Value = user?.Email ?? "Usuário não encontrado";
                worksheet.Cells[row, 3].Value = log.Acao;
                worksheet.Cells[row, 4].Value = log.Entidade;
                worksheet.Cells[row, 5].Value = log.EntidadeId;
                worksheet.Cells[row, 6].Value = log.EnderecoIP ?? "";
                worksheet.Cells[row, 7].Value = log.Detalhes ?? "";

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"LogsAuditoria_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
