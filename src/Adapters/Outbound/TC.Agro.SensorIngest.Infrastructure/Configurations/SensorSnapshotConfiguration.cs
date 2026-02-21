using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    internal sealed class SensorSnapshotConfiguration : IEntityTypeConfiguration<SensorSnapshot>
    {
        public void Configure(EntityTypeBuilder<SensorSnapshot> builder)
        {
            builder.ToTable("sensor_snapshots");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(s => s.OwnerId)
                .IsRequired();

            builder.Property(s => s.PropertyId)
                .IsRequired();

            builder.Property(s => s.PlotId)
                .IsRequired();

            builder.Property(s => s.Label)
                .HasMaxLength(200);

            builder.Property(s => s.PlotName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.PropertyName)
                .IsRequired()
                .HasMaxLength(200);

            // Soft delete / active flag
            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(s => s.UpdatedAt)
                .HasColumnType("timestamptz");

            // Relationship with OwnerSnapshot
            builder.HasOne(s => s.Owner)
                .WithMany(x => x.Sensors)
                .HasForeignKey(s => s.OwnerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // NOTE: Relationship with SensorReadingAggregate is configured in SensorReadingAggregateConfiguration
            // to avoid duplicate configuration (EF Core rule: configure relationships on ONE side only)

            // Indexes
            builder.HasIndex(s => s.OwnerId);
            builder.HasIndex(s => s.PlotId);
            builder.HasIndex(s => new { s.PlotId, s.IsActive });
            builder.HasIndex(s => new { s.OwnerId, s.IsActive });
        }
    }
}
