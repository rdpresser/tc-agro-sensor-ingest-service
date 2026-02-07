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
                .HasColumnName("sensor_id")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PlotId)
                .HasColumnName("plot_id")
                .IsRequired();

            builder.Property(x => x.PlotName)
                .HasColumnName("plot_name")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.Value,
                    v => SensorStatus.FromDb(v).Value);

            builder.Property(x => x.Battery)
                .HasColumnName("battery")
                .HasColumnType("double precision")
                .IsRequired();

            builder.Property(x => x.LastReadingAt)
                .HasColumnName("last_reading_at")
                .HasColumnType("timestamptz");

            builder.Property(x => x.LastTemperature)
                .HasColumnName("last_temperature")
                .HasColumnType("double precision");

            builder.Property(x => x.LastHumidity)
                .HasColumnName("last_humidity")
                .HasColumnType("double precision");

            builder.Property(x => x.LastSoilMoisture)
                .HasColumnName("last_soil_moisture")
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
