using System.Reflection;
using TC.Agro.SensorIngest.Application.UseCases.CreateReading;
using TC.Agro.SensorIngest.Domain.Aggregates;
using TC.Agro.SensorIngest.Infrastructure;
using TC.Agro.SensorIngest.Service;

namespace TC.Agro.SensorIngest.Architecture.Tests;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(SensorReadingAggregate).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(CreateReadingCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}