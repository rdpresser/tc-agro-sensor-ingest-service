namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    public sealed class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReadingAggregate>
    {
        public void Configure(EntityTypeBuilder<SensorReadingAggregate> builder)
        {
            builder.ToTable("sensor_readings", DefaultSchemas.Default);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.SensorId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.PlotId)
                .IsRequired();

            builder.Property(x => x.Time)
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(x => x.Temperature)
                .HasColumnType("double precision");

            builder.Property(x => x.Humidity)
                .HasColumnType("double precision");

            builder.Property(x => x.SoilMoisture)
                .HasColumnType("double precision");

            builder.Property(x => x.Rainfall)
                .HasColumnType("double precision");

            builder.Property(x => x.BatteryLevel)
                .HasColumnType("double precision");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamptz");

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
