namespace TC.Agro.SensorIngest.Infrastructure.Repositories
{
    public sealed class AlertAggregateRepository
        : BaseRepository<AlertAggregate, ApplicationDbContext>, IAlertAggregateRepository
    {
        public AlertAggregateRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
