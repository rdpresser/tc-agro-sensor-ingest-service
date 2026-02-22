using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    internal sealed class OwnerSnapshotConfiguration : IEntityTypeConfiguration<OwnerSnapshot>
    {
        public void Configure(EntityTypeBuilder<OwnerSnapshot> builder)
        {
            builder.ToTable("owner_snapshots");

            builder.HasKey(o => o.Id);
            builder.Property(x => x.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(o => o.Email)
                .IsUnique();

            // Soft delete / active flag
            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(o => o.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(o => o.UpdatedAt)
                .HasColumnType("timestamptz");

            // Navigation property to SensorSnapshots owned by this owner
            builder.HasMany(o => o.Sensors)
                .WithOne(s => s.Owner)
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
