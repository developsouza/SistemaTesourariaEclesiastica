using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaTesourariaEclesiastica.Data;
using SistemaTesourariaEclesiastica.Models;

namespace SistemaTesourariaEclesiastica.Services
{
    public static class RoleInitializerService
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Criar roles conforme especificação do projeto
            var roleNames = new[]
            {
                "Administrador",     // Acesso total
                "TesoureiroGeral",   // Gerencia a Sede, aprova prestações de contas, visualização consolidada
                "TesoureiroLocal",   // Gerencia sua congregação e submete os fechamentos de período
                "Pastor"             // Acesso de consulta aos relatórios
            };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Erro ao criar role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Criar centro de custo padrão (Sede) se não existir
            var sede = await context.CentrosCusto.FirstOrDefaultAsync(c => c.Nome == "Sede");
            if (sede == null)
            {
                sede = new CentroCusto
                {
                    Nome = "Sede",
                    Descricao = "Centro de custo principal da igreja",
                    Ativo = true
                };
                context.CentrosCusto.Add(sede);
                await context.SaveChangesAsync();
            }

            // Criar usuário administrador padrão se não existir
            var adminEmail = "admin@tesouraria.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    NomeCompleto = "Administrador do Sistema",
                    Ativo = true,
                    DataCriacao = DateTime.Now,
                    EmailConfirmed = true,
                    CentroCustoId = sede.Id
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrador");
                }
                else
                {
                    throw new Exception($"Erro ao criar usuário administrador: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Garantir que o admin tenha a role correta
                var roles = await userManager.GetRolesAsync(adminUser);
                if (!roles.Contains("Administrador"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }

                // Garantir que o admin tenha um centro de custo
                if (adminUser.CentroCustoId == null)
                {
                    adminUser.CentroCustoId = sede.Id;
                    await userManager.UpdateAsync(adminUser);
                }
            }

            // Criar usuário de teste para Tesoureiro Geral se em desenvolvimento
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                await CriarUsuarioTeste(userManager, sede.Id, "tesoureiro@tesouraria.com", "Tesoureiro Geral", "TesoureiroGeral", "Tesoureiro@123");

                // Criar congregação de teste
                var congregacaoTeste = await context.CentrosCusto.FirstOrDefaultAsync(c => c.Nome == "Congregação Exemplo");
                if (congregacaoTeste == null)
                {
                    congregacaoTeste = new CentroCusto
                    {
                        Nome = "Congregação Exemplo",
                        Descricao = "Congregação de exemplo para testes",
                        Ativo = true
                    };
                    context.CentrosCusto.Add(congregacaoTeste);
                    await context.SaveChangesAsync();
                }

                await CriarUsuarioTeste(userManager, congregacaoTeste.Id, "local@tesouraria.com", "Tesoureiro Local", "TesoureiroLocal", "Local@123");
                await CriarUsuarioTeste(userManager, congregacaoTeste.Id, "pastor@tesouraria.com", "Pastor da Igreja", "Pastor", "Pastor@123");
            }
        }

        private static async Task CriarUsuarioTeste(UserManager<ApplicationUser> userManager, int centroCustoId,
            string email, string nomeCompleto, string role, string senha)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                var novoUsuario = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    NomeCompleto = nomeCompleto,
                    Ativo = true,
                    DataCriacao = DateTime.Now,
                    EmailConfirmed = true,
                    CentroCustoId = centroCustoId
                };

                var result = await userManager.CreateAsync(novoUsuario, senha);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(novoUsuario, role);
                }
            }
        }

        /// <summary>
        /// Remove roles antigas que não são mais utilizadas
        /// </summary>
        public static async Task RemoverRolesAntigasAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var rolesAntigas = new[] { "Tesoureiro", "Secretario", "Usuario" };

            foreach (var roleAntiga in rolesAntigas)
            {
                var role = await roleManager.FindByNameAsync(roleAntiga);
                if (role != null)
                {
                    await roleManager.DeleteAsync(role);
                }
            }
        }
    }
}