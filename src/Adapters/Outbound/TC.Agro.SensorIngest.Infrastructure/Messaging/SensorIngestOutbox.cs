namespace TC.Agro.SensorIngest.Infrastructure.Messaging
{
    public sealed class SensorIngestOutbox : WolverineEfCoreOutbox<ApplicationDbContext>
    {
        public SensorIngestOutbox(IDbContextOutbox<ApplicationDbContext> outbox)
            : base(outbox)
        {
        }
    }
}
