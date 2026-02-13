namespace TC.Agro.SensorIngest.Infrastructure.Persistence
{
    [ExcludeFromCodeCoverage]
    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public DbSet<SensorReadingAggregate> SensorReadings => Set<SensorReadingAggregate>();
        public DbSet<SensorAggregate> Sensors => Set<SensorAggregate>();
        public DbSet<AlertAggregate> Alerts => Set<AlertAggregate>();

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
            modelBuilder.Entity<SensorAggregate>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<AlertAggregate>().HasQueryFilter(x => x.IsActive);
        }

        /// <inheritdoc />
        async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct)
        {
            return await base.SaveChangesAsync(ct);
        }
    }
}
