using Microsoft.AspNetCore.Authorization;

namespace SistemaTesourariaEclesiastica.Attributes
{
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles) : base()
        {
            Roles = string.Join(",", roles);
        }
    }
    
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string TesoureiroGeral = "TesoureiroGeral";
        public const string Tesoureiro = "Tesoureiro";
        public const string Secretario = "Secretario";
        public const string Usuario = "Usuario";
        
        // Combinações comuns
        public const string AdminOuTesoureiroGeral = Administrador + "," + TesoureiroGeral;
        public const string AdminOuTesoureiro = Administrador + "," + TesoureiroGeral + "," + Tesoureiro;
        public const string TodosExcetoUsuario = Administrador + "," + TesoureiroGeral + "," + Tesoureiro + "," + Secretario;
    }
}
