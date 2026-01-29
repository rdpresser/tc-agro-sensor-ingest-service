// System
global using System.Diagnostics.CodeAnalysis;

// Microsoft
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Caching.StackExchangeRedis;

// FastEndpoints
global using FastEndpoints;
global using FastEndpoints.Security;
global using FastEndpoints.Swagger;
global using NSwag.AspNetCore;

// FluentValidation
global using FluentValidation;

// Newtonsoft
global using Newtonsoft.Json.Converters;

// Serilog
global using Serilog;

// HealthChecks
global using HealthChecks.UI.Client;

// ZiggyCreatures FusionCache
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

// Wolverine
global using Wolverine;
global using Wolverine.Postgresql;
global using Wolverine.EntityFrameworkCore;
global using Wolverine.ErrorHandling;
global using Wolverine.RabbitMQ;
global using Wolverine.Runtime;

// Project - Service
global using TC.Agro.SensorIngest.Service.Extensions;
global using TC.Agro.SensorIngest.Service.Telemetry;

// Project - Application
global using Application = TC.Agro.SensorIngest.Application;

// Project - Infrastructure
global using TC.Agro.SensorIngest.Infrastructure;
global using TC.Agro.SensorIngest.Infrastructure.Persistence;

// Project - SharedKernel
global using TC.Agro.SharedKernel.Api.Extensions;
global using TC.Agro.SharedKernel.Extensions;
global using TC.Agro.SharedKernel.Infrastructure.Authentication;
global using TC.Agro.SharedKernel.Infrastructure.Caching.HealthCheck;
global using TC.Agro.SharedKernel.Infrastructure.Caching.Provider;
global using TC.Agro.SharedKernel.Infrastructure.Database;
global using TC.Agro.SharedKernel.Infrastructure.MessageBroker;
global using TC.Agro.SharedKernel.Infrastructure.Middleware;
