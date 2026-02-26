namespace TC.Agro.SensorIngest.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public DbSet<SensorReadingAggregate> SensorReadings { get; set; } = default!;
        public DbSet<OwnerSnapshot> OwnerSnapshots { get; set; } = default!;
        public DbSet<SensorSnapshot> SensorSnapshots { get; set; } = default!;

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

            // -------------------------------
            // Global Query Filters
            // -------------------------------
            modelBuilder.Entity<SensorReadingAggregate>().HasQueryFilter(p => p.IsActive);
        }

        /// <inheritdoc />
        async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct)
        {
            return await base.SaveChangesAsync(ct);
        }
    }
}
