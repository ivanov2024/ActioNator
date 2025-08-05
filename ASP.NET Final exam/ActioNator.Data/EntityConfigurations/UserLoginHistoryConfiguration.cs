using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ActioNator.Data.Models;

namespace ActioNator.Data.EntityConfigurations
{
    internal class UserLoginHistoryConfiguration : IEntityTypeConfiguration<UserLoginHistory>
    {
        public void Configure(EntityTypeBuilder<UserLoginHistory> userLoginHistory)
        {
            userLoginHistory
                .HasKey(ulh => ulh.Id);

            userLoginHistory
                .HasOne(ulh => ulh.User)
                .WithMany(u => u.LoginHistory)
                .HasForeignKey(ulh => ulh.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
