using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class ExerciseTemplateConfiguration : IEntityTypeConfiguration<ExerciseTemplate>
    {
        public void Configure(EntityTypeBuilder<ExerciseTemplate> exerciseTemplate)
        {
            exerciseTemplate
                .HasKey(et => et.Id);
        }
    }
}
