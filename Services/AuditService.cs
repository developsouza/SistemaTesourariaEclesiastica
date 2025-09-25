using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task LogAuditAsync(string userId, string action, string entityName, string entityId, string? details = null)
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
            var entityName = typeof(T).Name;
            var details = JsonSerializer.Serialize(entity, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await LogAuditAsync(userId, "CREATE", entityName, entityId, details);
        }

        public async Task LogUpdateAsync<T>(string userId, T oldEntity, T newEntity, string entityId) where T : class
        {
            var entityName = typeof(T).Name;
            var changes = GetChanges(oldEntity, newEntity);
            var details = JsonSerializer.Serialize(changes, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await LogAuditAsync(userId, "UPDATE", entityName, entityId, details);
        }

        public async Task LogDeleteAsync<T>(string userId, T entity, string entityId) where T : class
        {
            var entityName = typeof(T).Name;
            var details = JsonSerializer.Serialize(entity, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await LogAuditAsync(userId, "DELETE", entityName, entityId, details);
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


