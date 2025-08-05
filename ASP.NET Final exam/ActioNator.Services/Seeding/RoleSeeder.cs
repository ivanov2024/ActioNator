using Microsoft.AspNetCore.Identity;

namespace ActioNator.Services.Seeding
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        public RoleSeeder(RoleManager<IdentityRole<Guid>> roleManager)
            => _roleManager = roleManager;
        
        public async Task SeedRolesAsync()
        {
            string[] roles = ["Admin", "Coach", "User"];
            foreach (string role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    IdentityRole<Guid> identityRole = new(role)
                    {
                        NormalizedName = role.ToUpper()
                    };

                    IdentityResult result 
                        = await _roleManager
                        .CreateAsync(identityRole);

                    if (!result.Succeeded)
                    {
                        throw new System.Exception($"Failed to create role '{role}': {string.Join(", ", result.Errors)}");
                    }
                }
            }
        }
    }
}
