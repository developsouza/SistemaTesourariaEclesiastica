using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaTesourariaEclesiastica.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        // CRÍTICO: Configuração de serialização para evitar erros com navigation properties
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles, // Ignora referências circulares
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignora propriedades nulas
            MaxDepth = 3 // Limita a profundidade da serialização
        };

        public async Task LogAuditAsync(string userId, string action, string entityName, string entityId, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

                var log = new LogAuditoria
                {
                    UsuarioId = userId,
                    Acao = action,
                    Entidade = entityName,
                    EntidadeId = entityId,
                    DataHora = DateTime.Now,
                    Detalhes = details,
                    EnderecoIP = ipAddress,
                    UserAgent = userAgent
                };

                _context.LogsAuditoria.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Log de auditoria não deve quebrar o fluxo principal
                // Apenas ignora o erro silenciosamente
            }
        }

        public async Task LogAuditAsync(ApplicationUser user, string action, string entityName, string entityId, string? details = null)
        {
            await LogAuditAsync(user.Id, action, entityName, entityId, details);
        }

        public async Task LogAsync(string action, string entityName, string? details = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(httpContext.User);
                if (user != null)
                {
                    await LogAuditAsync(user.Id, action, entityName, "0", details);
                }
            }
        }

        public async Task LogCreateAsync<T>(string userId, T entity, string entityId) where T : class
        {
            try
            {
                var entityName = typeof(T).Name;

                // Serializa com as opções seguras
                var details = JsonSerializer.Serialize(entity, SerializerOptions);

                await LogAuditAsync(userId, "CREATE", entityName, entityId, details);
            }
            catch (Exception)
            {
                // Se falhar a serialização, registra sem detalhes
                await LogAuditAsync(userId, "CREATE", typeof(T).Name, entityId, "Detalhes não disponíveis (erro na serialização)");
            }
        }

        public async Task LogUpdateAsync<T>(string userId, T oldEntity, T newEntity, string entityId) where T : class
        {
            try
            {
                var entityName = typeof(T).Name;
                var changes = GetChanges(oldEntity, newEntity);

                // Serializa com as opções seguras
                var details = JsonSerializer.Serialize(changes, SerializerOptions);

                await LogAuditAsync(userId, "UPDATE", entityName, entityId, details);
            }
            catch (Exception)
            {
                // Se falhar a serialização, registra sem detalhes
                await LogAuditAsync(userId, "UPDATE", typeof(T).Name, entityId, "Detalhes não disponíveis (erro na serialização)");
            }
        }

        public async Task LogDeleteAsync<T>(string userId, T entity, string entityId) where T : class
        {
            try
            {
                var entityName = typeof(T).Name;

                // Serializa com as opções seguras
                var details = JsonSerializer.Serialize(entity, SerializerOptions);

                await LogAuditAsync(userId, "DELETE", entityName, entityId, details);
            }
            catch (Exception)
            {
                // Se falhar a serialização, registra sem detalhes
                await LogAuditAsync(userId, "DELETE", typeof(T).Name, entityId, "Detalhes não disponíveis (erro na serialização)");
            }
        }

        public async Task LogLoginAsync(string userId, bool success, string? reason = null)
        {
            var action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
            await LogAuditAsync(userId, action, "Authentication", userId, reason);
        }

        public async Task LogLogoutAsync(string userId)
        {
            await LogAuditAsync(userId, "LOGOUT", "Authentication", userId);
        }

        public async Task<IEnumerable<LogAuditoria>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, string? entityName = null, int page = 1, int pageSize = 50)
        {
            var query = _context.LogsAuditoria.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.DataHora >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.DataHora <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UsuarioId == userId);

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(l => l.Entidade == entityName);

            return await query
                .OrderByDescending(l => l.DataHora)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetAuditLogsCountAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, string? entityName = null)
        {
            var query = _context.LogsAuditoria.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.DataHora >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.DataHora <= endDate.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UsuarioId == userId);

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(l => l.Entidade == entityName);

            return await query.CountAsync();
        }

        private Dictionary<string, object> GetChanges<T>(T oldEntity, T newEntity) where T : class
        {
            var changes = new Dictionary<string, object>();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                // Ignora navigation properties (propriedades complexas)
                if (property.PropertyType.IsClass &&
                    property.PropertyType != typeof(string) &&
                    !property.PropertyType.IsValueType)
                {
                    continue; // Pula navigation properties
                }

                var oldValue = property.GetValue(oldEntity);
                var newValue = property.GetValue(newEntity);

                if (!Equals(oldValue, newValue))
                {
                    changes[property.Name] = new
                    {
                        Old = oldValue,
                        New = newValue
                    };
                }
            }

            return changes;
        }
    }
}