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
                .Property(au => au.RegisteredAt)
                .HasDefaultValueSql("GETUTCDATE()");

            applicationUser
                .Property(au => au.LastLoginAt)
                .HasDefaultValueSql("GETUTCDATE()");

            applicationUser
                .Property(au => au.ProfilePictureUrl)
                .HasDefaultValue("https://static.vecteezy.com/system/resources/thumbnails/020/765/399/small_2x/default-profile-account-unknown-icon-black-silhouette-free-vector.jpg");

            applicationUser
                .Property(au => au.IsDeleted)
                .HasDefaultValue(false);

            applicationUser
                .HasQueryFilter(au => au.IsDeleted == false);
        }
    }
}
