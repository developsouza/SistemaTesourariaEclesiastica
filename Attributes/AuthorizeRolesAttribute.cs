using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SistemaTesourariaEclesiastica.Attributes
{
    /// <summary>
    /// Atributo de autorização customizado baseado nos perfis do sistema
    /// </summary>
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles) : base()
        {
            Roles = string.Join(",", roles);
        }
    }

    /// <summary>
    /// Constantes das roles do sistema conforme especificação do projeto
    /// </summary>
    public static class Roles
    {
        // Roles principais conforme especificação
        public const string Administrador = "Administrador";
        public const string TesoureiroGeral = "TesoureiroGeral";
        public const string TesoureiroLocal = "TesoureiroLocal";
        public const string Pastor = "Pastor";

        // Combinações comuns para facilitar o uso
        public const string AdminOnly = Administrador;
        public const string Tesoureiros = Administrador + "," + TesoureiroGeral + "," + TesoureiroLocal;
        public const string TesoureiroGeralEAdmin = Administrador + "," + TesoureiroGeral;
        public const string TodosComAcessoRelatorios = Administrador + "," + TesoureiroGeral + "," + TesoureiroLocal + "," + Pastor;
        public const string OperacoesFinanceiras = Administrador + "," + TesoureiroGeral + "," + TesoureiroLocal;
        public const string AprovacaoPrestacoes = Administrador + "," + TesoureiroGeral;

        // Para compatibilidade com código existente
        public const string AdminOuTesoureiro = Administrador + "," + TesoureiroGeral + "," + TesoureiroLocal;
        public const string AdminOuTesoureiroGeral = Administrador + "," + TesoureiroGeral;
        public const string TodosExcetoUsuario = Administrador + "," + TesoureiroGeral + "," + TesoureiroLocal + "," + Pastor;
    }

    /// <summary>
    /// Filtro de ação personalizado para controle fino de acesso
    /// </summary>
    public class RequirePermissionAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;
        private readonly bool _requireSameCentroCusto;

        public RequirePermissionAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
            _requireSameCentroCusto = false;
        }

        public RequirePermissionAttribute(bool requireSameCentroCusto, params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
            _requireSameCentroCusto = requireSameCentroCusto;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Verificar se tem alguma das roles permitidas
            var hasRequiredRole = _allowedRoles.Any(role => user.IsInRole(role));

            if (!hasRequiredRole)
            {
                context.Result = new ForbidResult();
                return;
            }

            // Se requer mesmo centro de custo e não é Admin/TesoureiroGeral
            if (_requireSameCentroCusto && !user.IsInRole(Roles.Administrador) && !user.IsInRole(Roles.TesoureiroGeral))
            {
                // Aqui você pode implementar lógica adicional para verificar centro de custo
                // Por exemplo, comparar o centro de custo do usuário com o recurso sendo acessado
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Atributo para controlar acesso baseado no centro de custo do usuário
    /// </summary>
    public class RequireCentroCustoAccessAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            // Administrador e Tesoureiro Geral têm acesso a tudo
            if (user.IsInRole(Roles.Administrador) || user.IsInRole(Roles.TesoureiroGeral))
            {
                base.OnActionExecuting(context);
                return;
            }

            // Para Tesoureiros Locais e Pastores, verificar se estão acessando dados do seu centro de custo
            // Esta lógica pode ser expandida conforme necessário
            var centroCustoId = context.RouteData.Values["centroCustoId"]?.ToString() ??
                               context.HttpContext.Request.Query["centroCustoId"].FirstOrDefault();

            if (!string.IsNullOrEmpty(centroCustoId))
            {
                var userCentroCustoId = user.FindFirst("CentroCustoId")?.Value;

                if (userCentroCustoId != centroCustoId && !user.IsInRole(Roles.TesoureiroGeral))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}