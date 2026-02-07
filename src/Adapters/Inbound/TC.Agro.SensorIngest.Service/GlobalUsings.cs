// System
global using System.Diagnostics.CodeAnalysis;
global using System.Net;

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

// SignalR
global using Microsoft.AspNetCore.SignalR;

// Project - Application
global using Application = TC.Agro.SensorIngest.Application;
global using TC.Agro.SensorIngest.Application.Abstractions;
global using TC.Agro.SensorIngest.Application.Abstractions.Ports;
global using TC.Agro.SensorIngest.Application.UseCases.CreateReading;
global using TC.Agro.SensorIngest.Application.UseCases.CreateBatchReadings;
global using TC.Agro.SensorIngest.Application.UseCases.GetLatestReadings;
global using TC.Agro.SensorIngest.Application.UseCases.RegisterSensor;
global using TC.Agro.SensorIngest.Application.UseCases.GetSensorList;
global using TC.Agro.SensorIngest.Application.UseCases.GetReadingsHistory;
global using TC.Agro.SensorIngest.Application.UseCases.CreateAlert;
global using TC.Agro.SensorIngest.Application.UseCases.GetAlertList;
global using TC.Agro.SensorIngest.Application.UseCases.ResolveAlert;
global using TC.Agro.SensorIngest.Application.UseCases.GetDashboardStats;

// Project - Infrastructure
global using TC.Agro.SensorIngest.Infrastructure;
global using TC.Agro.SensorIngest.Infrastructure.Persistence;

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
