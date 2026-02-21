using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public DbSet<SensorReadingAggregate> SensorReadings { get; set; }
        public DbSet<AlertAggregate> Alerts { get; set; }
        public DbSet<OwnerSnapshot> OwnerSnapshots { get; set; }
        public DbSet<SensorSnapshot> SensorSnapshots { get; set; }

        /// <inheritdoc />
        public DbContext DbContext => this;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(DefaultSchemas.Default);

            modelBuilder.Ignore<BaseDomainEvent>();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global query filters for soft delete
            modelBuilder.Entity<AlertAggregate>().HasQueryFilter(x => x.IsActive);
        }

        /// <inheritdoc />
        async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct)
        {
            return await base.SaveChangesAsync(ct);
        }
    }
}
