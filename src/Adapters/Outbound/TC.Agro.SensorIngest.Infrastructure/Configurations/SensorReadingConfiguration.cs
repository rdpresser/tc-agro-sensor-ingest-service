namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    public sealed class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReadingAggregate>
    {
        public void Configure(EntityTypeBuilder<SensorReadingAggregate> builder)
        {
            builder.ToTable("sensor_readings", DefaultSchemas.Default);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(x => x.SensorId)
                .HasColumnName("sensor_id")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PlotId)
                .HasColumnName("plot_id")
                .IsRequired();

            builder.Property(x => x.Time)
                .HasColumnName("time")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(x => x.Temperature)
                .HasColumnName("temperature")
                .HasColumnType("double precision");

            builder.Property(x => x.Humidity)
                .HasColumnName("humidity")
                .HasColumnType("double precision");

            builder.Property(x => x.SoilMoisture)
                .HasColumnName("soil_moisture")
                .HasColumnType("double precision");

            builder.Property(x => x.Rainfall)
                .HasColumnName("rainfall")
                .HasColumnType("double precision");

            builder.Property(x => x.BatteryLevel)
                .HasColumnName("battery_level")
                .HasColumnType("double precision");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz");

            builder.Property(x => x.IsActive)
                .HasColumnName("is_active");

            // Indexes for common queries
            builder.HasIndex(x => new { x.SensorId, x.Time })
                .HasDatabaseName("ix_sensor_readings_sensor_id_time");

            builder.HasIndex(x => new { x.PlotId, x.Time })
                .HasDatabaseName("ix_sensor_readings_plot_id_time");

            builder.HasIndex(x => x.Time)
                .HasDatabaseName("ix_sensor_readings_time");

            // Ignore domain events (not persisted)
            builder.Ignore(x => x.UncommittedEvents);
        }
    }
}
