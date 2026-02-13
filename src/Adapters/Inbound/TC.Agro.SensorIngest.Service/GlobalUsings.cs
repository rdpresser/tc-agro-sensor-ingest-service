// System
global using System.Diagnostics.CodeAnalysis;
global using System.Text;
// FastEndpoints
global using FastEndpoints;
global using FastEndpoints.Swagger;
// FluentValidation
global using FluentValidation;
// HealthChecks
global using HealthChecks.UI.Client;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
// Microsoft
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// SignalR
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Caching.StackExchangeRedis;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Microsoft.IdentityModel.JsonWebTokens;
global using Microsoft.IdentityModel.Tokens;
// Newtonsoft
global using Newtonsoft.Json.Converters;
global using Npgsql;
global using NSwag.AspNetCore;
// OpenTelemetry
global using OpenTelemetry;
global using OpenTelemetry.Logs;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Resources;
global using OpenTelemetry.Trace;
// Serilog
global using Serilog;
global using TC.Agro.SensorIngest.Application;
// Project - Application
global using TC.Agro.SensorIngest.Application.Abstractions;
global using TC.Agro.SensorIngest.Application.Abstractions.Ports;
global using TC.Agro.SensorIngest.Application.UseCases.CreateAlert;
global using TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings;
global using TC.Agro.SensorIngest.Application.UseCases.CreateReading;
global using TC.Agro.SensorIngest.Application.UseCases.GetAlertList;
global using TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats;
global using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
global using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;
global using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;
global using TC.Agro.SensorIngest.Application.UseCases.RegisterSensor;
global using TC.Agro.SensorIngest.Application.UseCases.ResolveAlert;
// Project - Infrastructure
global using TC.Agro.SensorIngest.Infrastructure;
// Project - Service
global using TC.Agro.SensorIngest.Service.Extensions;
global using TC.Agro.SensorIngest.Service.Hubs;
global using TC.Agro.SensorIngest.Service.Telemetry;
// Project - SharedKernel
global using TC.Agro.SharedKernel.Api.Endpoints;
global using TC.Agro.SharedKernel.Api.Extensions;
global using TC.Agro.SharedKernel.Application.Behaviors;
global using TC.Agro.SharedKernel.Extensions;
global using TC.Agro.SharedKernel.Infrastructure.Authentication;
global using TC.Agro.SharedKernel.Infrastructure.Caching.HealthCheck;
global using TC.Agro.SharedKernel.Infrastructure.Caching.Provider;
global using TC.Agro.SharedKernel.Infrastructure.Database;
global using TC.Agro.SharedKernel.Infrastructure.MessageBroker;
global using TC.Agro.SharedKernel.Infrastructure.Middleware;
global using TC.Agro.SharedKernel.Infrastructure.Telemetry;
// Wolverine
global using Wolverine;
global using Wolverine.EntityFrameworkCore;
global using Wolverine.ErrorHandling;
global using Wolverine.Postgresql;
global using Wolverine.RabbitMQ;
// ZiggyCreatures FusionCache
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
global using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
