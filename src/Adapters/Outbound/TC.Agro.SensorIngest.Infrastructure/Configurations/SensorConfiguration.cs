using TC.Agro.SensorIngest.Domain.ValueObjects;

namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    public sealed class SensorConfiguration : BaseEntityConfiguration<SensorAggregate>
    {
        public override void Configure(EntityTypeBuilder<SensorAggregate> builder)
        {
            base.Configure(builder);

            builder.ToTable("sensors", DefaultSchemas.Default);

            builder.Property(x => x.SensorId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PlotId)
                .IsRequired();

            builder.Property(x => x.PlotName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => SensorStatus.FromDb(v).Value);

            builder.Property(x => x.Battery)
                .HasColumnType("double precision")
                .IsRequired();

            builder.Property(x => x.LastReadingAt)
                .HasColumnType("timestamptz");

            builder.Property(x => x.LastTemperature)
                .HasColumnType("double precision");

            builder.Property(x => x.LastHumidity)
                .HasColumnType("double precision");

            builder.Property(x => x.LastSoilMoisture)
                .HasColumnType("double precision");

            // Indexes
            builder.HasIndex(x => x.SensorId)
                .IsUnique()
                .HasDatabaseName("ix_sensors_sensor_id");

            builder.HasIndex(x => x.PlotId)
                .HasDatabaseName("ix_sensors_plot_id");
        }
    }
}
