namespace TC.Agro.SensorIngest.Infrastructure.Configurations
{
    public sealed class SensorReadingAggregateConfiguration : BaseEntityConfiguration<SensorReadingAggregate>
    {
        public override void Configure(EntityTypeBuilder<SensorReadingAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("sensor_readings");

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            // SensorId is a Guid (FK to sensor_snapshots.id)
            builder.Property(x => x.SensorId)
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

            // Relationship with SensorSnapshot
            builder.HasOne(sr => sr.Sensor)
                .WithMany(s => s.SensorReadings)
                .HasForeignKey(sr => sr.SensorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for common queries
            builder.HasIndex(x => new { x.SensorId, x.Time })
                .HasDatabaseName("ix_sensor_readings_sensor_id_time");

            builder.HasIndex(x => x.Time)
                .HasDatabaseName("ix_sensor_readings_time");

            // Ignore domain events (not persisted)
            builder.Ignore(x => x.UncommittedEvents);
        }
    }
}
