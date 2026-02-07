global using System.Diagnostics.CodeAnalysis;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Migrations;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Serilog;

global using Wolverine;
global using Wolverine.EntityFrameworkCore;

global using TC.Agro.SensorIngest.Application.Abstractions.Ports;
global using TC.Agro.SensorIngest.Domain.Aggregates;
global using TC.Agro.SensorIngest.Domain.ValueObjects;
global using TC.Agro.SensorIngest.Infrastructure.Configurations;
global using TC.Agro.SensorIngest.Infrastructure.Messaging;
global using TC.Agro.SensorIngest.Infrastructure.Persistence;
global using TC.Agro.SensorIngest.Infrastructure.Repositories;

global using TC.Agro.SharedKernel.Application.Ports;
global using TC.Agro.SharedKernel.Domain.Aggregate;
global using TC.Agro.SharedKernel.Domain.Events;
global using TC.Agro.SharedKernel.Infrastructure.Database;
global using TC.Agro.SharedKernel.Infrastructure.Database.EfCore;
global using TC.Agro.SharedKernel.Infrastructure.Messaging.Outbox;
