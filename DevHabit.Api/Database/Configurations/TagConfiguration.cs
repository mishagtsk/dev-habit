using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(300);
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        
        builder.Property(x => x.Description).HasMaxLength(500);
        
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
