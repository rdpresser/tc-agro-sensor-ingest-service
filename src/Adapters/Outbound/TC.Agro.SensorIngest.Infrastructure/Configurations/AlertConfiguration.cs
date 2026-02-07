using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    public sealed class AlertConfiguration : BaseEntityConfiguration<AlertAggregate>
    {
        public override void Configure(EntityTypeBuilder<AlertAggregate> builder)
        {
            base.Configure(builder);

            builder.ToTable("alerts", DefaultSchemas.Default);

            builder.Property(x => x.Severity)
                .HasColumnName("severity")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => AlertSeverity.FromDb(v).Value);

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasColumnName("message")
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.PlotId)
                .HasColumnName("plot_id")
                .IsRequired();

            builder.Property(x => x.PlotName)
                .HasColumnName("plot_name")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.SensorId)
                .HasColumnName("sensor_id")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => AlertStatus.FromDb(v).Value);

            builder.Property(x => x.ResolvedAt)
                .HasColumnName("resolved_at")
                .HasColumnType("timestamptz");

            // Indexes
            builder.HasIndex(x => x.Status)
                .HasDatabaseName("ix_alerts_status");

            builder.HasIndex(x => new { x.SensorId, x.CreatedAt })
                .HasDatabaseName("ix_alerts_sensor_created");
        }
    }
}
