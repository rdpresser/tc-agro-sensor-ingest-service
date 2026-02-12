namespace TC.Agro.SensorIngest.Service.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class ApplicationBuilderExtensions
    {
        public static async Task ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();

            await dbContext.Database.MigrateAsync().ConfigureAwait(false);
            await EnsureTimescaleDbAsync(dbContext, logger).ConfigureAwait(false);
        }

        private static async Task EnsureTimescaleDbAsync(ApplicationDbContext dbContext, Microsoft.Extensions.Logging.ILogger? logger = null)
        {
            var conn = dbContext.Database.GetDbConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            try
            {
                using var extCmd = conn.CreateCommand();
                extCmd.CommandText = "CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;";
                await extCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = """
                    SELECT EXISTS (
                        SELECT 1 FROM timescaledb_information.hypertables
                        WHERE hypertable_name = 'sensor_readings'
                    );
                    """;
                var isHypertable = (bool)(await checkCmd.ExecuteScalarAsync().ConfigureAwait(false))!;

                if (!isHypertable)
                {
                    using var htCmd = conn.CreateCommand();
                    htCmd.CommandText = "SELECT create_hypertable('sensor_readings', 'time', migrate_data => true, if_not_exists => true);";
                    await htCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to ensure TimescaleDB setup. This may require manual provisioning in managed environments");
            }
            finally
            {
                await conn.CloseAsync().ConfigureAwait(false);
            }
        }

        public static IApplicationBuilder UseIngressPathBase(this IApplicationBuilder app, IConfiguration configuration)
        {
            var configuredBasePath = configuration["ASPNETCORE_APPL_PATH"] ?? configuration["PathBase"];

            if (!string.IsNullOrWhiteSpace(configuredBasePath))
            {
                app.UsePathBase(configuredBasePath);
            }

            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefixValues))
                {
                    var prefix = prefixValues.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        var normalized = prefix.StartsWith('/') ? prefix : $"/{prefix}";
                        normalized = normalized.TrimEnd('/');

                        context.Request.PathBase = new PathString(normalized);
                        context.Items["OriginalPathBase"] = normalized;
                    }
                }
                else if (context.Request.Headers.TryGetValue("X-Original-URI", out var originalUriValues))
                {
                    var originalUri = originalUriValues.FirstOrDefault() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(originalUri))
                    {
                        var segments = originalUri.TrimStart('/').Split('/');
                        if (segments.Length > 0 && segments[0] != "api" && segments[0] != "swagger")
                        {
                            var pathBase = $"/{segments[0]}";
                            context.Request.PathBase = new PathString(pathBase);
                            context.Items["OriginalPathBase"] = pathBase;
                        }
                    }
                }

                await next().ConfigureAwait(false);
            });

            return app;
        }

        public static IApplicationBuilder UseCustomFastEndpoints(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseFastEndpoints(c =>
            {
                c.Security.RoleClaimType = "role";
                c.Endpoints.RoutePrefix = "api";
                c.Endpoints.ShortNames = true;
                c.Errors.ProducesMetadataType = typeof(Microsoft.AspNetCore.Mvc.ProblemDetails);
                c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
                {
                    var errors = failures.Select(f => new
                    {
                        name = f.PropertyName.ToPascalCaseFirst(),
                        reason = f.ErrorMessage,
                        code = f.ErrorCode
                    }).ToArray();

                    string title = statusCode switch
                    {
                        400 => "Validation Error",
                        404 => "Not Found",
                        403 => "Forbidden",
                        _ => "One or more errors occurred!"
                    };

                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = statusCode,
                        Instance = ctx.Request.Path.Value ?? string.Empty,
                        Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
                        Title = title,
                    };

                    problemDetails.Extensions["traceId"] = ctx.TraceIdentifier;
                    problemDetails.Extensions["errors"] = errors;

                    return problemDetails;
                };
            });

            var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH")
                ?? configuration["ASPNETCORE_APPL_PATH"]
                ?? configuration["PathBase"]
                ?? string.Empty;

            var normalizedPathBase = NormalizePathBase(pathBase);

            app.UseOpenApi(o =>
            {
                o.PostProcess = (doc, req) =>
                {
                    doc.Servers.Clear();

                    var requestPathBase = req.HttpContext.Request.PathBase.ToString();

                    if (!string.IsNullOrWhiteSpace(requestPathBase))
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = NormalizePathBase(requestPathBase) });
                    }
                    else if (!string.IsNullOrEmpty(normalizedPathBase))
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = normalizedPathBase });
                    }
                    else
                    {
                        doc.Servers.Add(new NSwag.OpenApiServer { Url = "/" });
                    }
                };
            });

            app.UseSwaggerUi(c =>
            {
                c.SwaggerRoutes.Clear();

                var swaggerJsonPath = string.IsNullOrEmpty(normalizedPathBase)
                    ? "/swagger/v1/swagger.json"
                    : $"{normalizedPathBase}/swagger/v1/swagger.json";

                c.SwaggerRoutes.Add(new SwaggerUiRoute("v1", swaggerJsonPath));

                c.ConfigureDefaults();
            });

            return app;
        }

        public static IApplicationBuilder UseCustomMiddlewares(this IApplicationBuilder app)
        {
            app.UseCustomExceptionHandler()
                .UseCorrelationMiddleware()
                .UseSerilogRequestLogging()
                .UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .UseHealthChecks("/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .UseHealthChecks("/live", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("live"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            return app;
        }

        public static async Task<IApplicationBuilder> CreateMessageDatabase(this IApplicationBuilder app)
        {
            var connProvider = app.ApplicationServices.GetRequiredService<DbConnectionFactory>();
            await PostgresDatabaseHelper.EnsureDatabaseExists(connProvider);

            return app;
        }

        private static string NormalizePathBase(string? pathBase)
        {
            if (string.IsNullOrWhiteSpace(pathBase))
            {
                return string.Empty;
            }

            var trimmed = pathBase.Trim().Trim('/');

            if (string.IsNullOrEmpty(trimmed))
            {
                return string.Empty;
            }

            return $"/{trimmed}";
        }
    }
}
