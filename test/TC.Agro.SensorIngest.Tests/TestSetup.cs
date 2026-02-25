using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace TC.Agro.SensorIngest.Tests;

/// <summary>
/// Initializes FastEndpoints internals (ValidationContext, etc.) required by
/// BaseHandler-derived classes. Without this, any test that instantiates a handler
/// inheriting from BaseHandler will fail with "Service resolver is null!".
/// In FastEndpoints 7.x, Factory.RegisterTestServices replaces the old ServiceResolver.
/// </summary>
internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        FastEndpoints.Factory.RegisterTestServices(s => s.AddLogging());
    }
}
