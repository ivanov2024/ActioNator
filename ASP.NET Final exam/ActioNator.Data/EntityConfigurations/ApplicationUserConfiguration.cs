using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> applicationUser)
        {
            applicationUser
                .Property(au => au.IsVerifiedCoach)
                .HasDefaultValue(false);

            applicationUser
                .Property(au => au.IsDeleted)
                .HasDefaultValue(false);

            applicationUser
                .HasQueryFilter(au => au.IsDeleted == false);
        }
    }
}
