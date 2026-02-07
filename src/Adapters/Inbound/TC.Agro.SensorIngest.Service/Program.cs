using TC.Agro.SensorIngest.Application;
using TC.Agro.SensorIngest.Service.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog as logging provider (using SharedKernel extension)
builder.Host.UseCustomSerilog(
    builder.Configuration,
    TelemetryConstants.ServiceName,
    TelemetryConstants.ServiceNamespace,
    TelemetryConstants.Version);

builder.Services.AddSensorIngestServices(builder);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!builder.Environment.IsEnvironment("Testing"))
{
    await app.ApplyMigrations().ConfigureAwait(false);
}

// Configure the HTTP request pipeline.
app.UseIngressPathBase(app.Configuration);

// Cross-Origin Resource Sharing (CORS)
app.UseCors("DefaultCorsPolicy");

// Use metrics authentication middleware extension
app.UseMetricsAuthentication();

app.MapHub<SensorHub>("/sensorHub");

app.UseAuthentication()
  .UseAuthorization()
  .UseCustomFastEndpoints(app.Configuration)
  .UseCustomMiddlewares();

await app.RunAsync();
