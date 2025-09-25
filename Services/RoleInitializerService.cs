using Microsoft.AspNetCore.Identity;
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

            // Criar roles padrão
            string[] roleNames = { "Administrador", "TesoureiroGeral", "Tesoureiro", "Secretario", "Usuario" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
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
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrador");
                }
            }
        }
    }
}
