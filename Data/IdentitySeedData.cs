using Microsoft.AspNetCore.Identity;
using NasIndexer.Model;

namespace NasIndexer.Data
{
    public static class IdentitySeedData
    {
        public const string AdminRole = "Admin";
        public const string ManagerRole = "Manager";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeedData));

            await EnsureRoleAsync(roleManager, AdminRole);
            await EnsureRoleAsync(roleManager, ManagerRole);

            await AssignConfiguredRoleAsync(userManager, configuration, logger, "IdentitySeed:AdminEmails", AdminRole);
            await AssignConfiguredRoleAsync(userManager, configuration, logger, "IdentitySeed:ManagerEmails", ManagerRole);
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static async Task AssignConfiguredRoleAsync(
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            ILogger logger,
            string configurationKey,
            string roleName)
        {
            var emails = configuration.GetSection(configurationKey).Get<string[]>() ?? Array.Empty<string>();

            foreach (var email in emails.Where(email => !string.IsNullOrWhiteSpace(email)).Select(email => email.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var user = await userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    logger.LogWarning("Configured Identity role assignment skipped because user {Email} does not exist.", email);
                    continue;
                }

                if (!await userManager.IsInRoleAsync(user, roleName))
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
        }
    }
}
