using Api.Models;
using Microsoft.AspNetCore.Identity;
using DotNetEnv;

namespace Api.Extensions
{   
    // Admin nikad iz FE, vec iz BE postavljam 
    public static class SeedAdminExtensions
    {
        public static async Task SeedAdminAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Ovo sam vec seedovao u OnModelCreating, pa nece uraditi ove ifove, ali dobro je da bude i ovde za svaki slucaj ako tamo izbrisem
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole { Id = "8d04dce2-969a-435d-bba4-df3f325983dc" , Name = "Admin", NormalizedName = "ADMIN"});
            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole { Id = "de1287c0-4b3e-4a3b-a7b5-5e221a57d55d", Name = "User", NormalizedName = "USER" });

            var adminEmail = Env.GetString("ADMIN_EMAIL");
            var adminUserExists = await userManager.FindByEmailAsync(adminEmail); // Sigurnije nego FindByNameAsync u slucaju admin
            // Ovaj kod je pozvan u Program.cs => izvrsva se uvek kad app startuje, pa da proverim da l je admin vec dodat, da ga ne dodajem svaki put
            if (adminUserExists is null)
            {
                var newAdmin = new AppUser { UserName = "Admin", Email = adminEmail};
                var createdAdmin = await userManager.CreateAsync(newAdmin, Env.GetString("ADMIN_PASSWORD"));
                if (createdAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}
