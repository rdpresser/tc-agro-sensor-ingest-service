namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    public sealed class AlertConfiguration : BaseEntityConfiguration<AlertAggregate>
    {
        public override void Configure(EntityTypeBuilder<AlertAggregate> builder)
        {
            base.Configure(builder);

            builder.ToTable("alerts", DefaultSchemas.Default);

            builder.Property(x => x.Severity)
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => AlertSeverity.FromDb(v).Value);

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.PlotId)
                .IsRequired();

            builder.Property(x => x.PlotName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.SensorId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => AlertStatus.FromDb(v).Value);

            builder.Property(x => x.ResolvedAt)
                .HasColumnType("timestamptz");

            // Indexes
            builder.HasIndex(x => x.Status)
                .HasDatabaseName("ix_alerts_status");

            builder.HasIndex(x => new { x.SensorId, x.CreatedAt })
                .HasDatabaseName("ix_alerts_sensor_created");
        }
    }
}
