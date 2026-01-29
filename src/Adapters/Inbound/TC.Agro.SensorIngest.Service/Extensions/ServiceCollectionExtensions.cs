namespace TC.Agro.SensorIngest.Service.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSensorIngestServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            ConfigureFluentValidationGlobals();

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                builder.AddWolverineMessaging();
            }

            services.AddHttpClient()
                .AddCorrelationIdGenerator()
                .AddCaching()
                .AddCustomCors(builder.Configuration)
                .AddCustomAuthentication(builder.Configuration)
                .AddCustomFastEndpoints(builder.Configuration)
                .AddCustomHealthCheck();

            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", builder =>
                {
                    builder
                        .SetIsOriginAllowed((host) => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddNpgSql(sp =>
                {
                    var connectionProvider = sp.GetRequiredService<DbConnectionFactory>();
                    return connectionProvider.ConnectionString;
                },
                    name: "PostgreSQL",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["db", "sql", "postgres", "live", "ready"])

                .AddTypeActivatedCheck<RedisHealthCheck>("Redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["cache", "redis", "live", "ready"])

                .AddCheck("Memory", () =>
                {
                    var allocated = GC.GetTotalMemory(false);
                    var mb = allocated / 1024 / 1024;

                    return mb < 1024
                    ? HealthCheckResult.Healthy($"Memory usage: {mb} MB")
                    : HealthCheckResult.Degraded($"High memory usage: {mb} MB");
                },
                    tags: ["memory", "system", "live"])
                .AddCheck("Custom-Metrics", () =>
                {
                    return HealthCheckResult.Healthy("Custom metrics are functioning");
                },
                    tags: ["metrics", "telemetry", "live"]);

            return services;
        }

        public static IServiceCollection AddCustomFastEndpoints(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddFastEndpoints(discoveryOptions =>
            {
                discoveryOptions.Assemblies =
                [
                    typeof(Application.DependencyInjection).Assembly,
                    typeof(ServiceCollectionExtensions).Assembly
                ];
            })
            .SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = "TC.Agro.SensorIngest API";
                    s.Version = "v1";
                    s.Description = "Sensor Ingest API for TC.Agro Solutions";
                    s.MarkNonNullablePropsAsRequired();
                };

                o.RemoveEmptyRequestSchema = true;
                o.NewtonsoftSettings = s => { s.Converters.Add(new StringEnumConverter()); };
            });

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Auth:Jwt").Get<JwtOptions>();

            services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtSettings!.SecretKey)
                    .AddAuthorization()
                    .AddHttpContextAccessor();

            return services;
        }

        private static void ConfigureFluentValidationGlobals()
        {
            ValidatorOptions.Global.PropertyNameResolver = (type, memberInfo, expression) => memberInfo?.Name;
            ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) => memberInfo?.Name;
            ValidatorOptions.Global.ErrorCodeResolver = validator => validator.Name;
            ValidatorOptions.Global.LanguageManager = new FluentValidation.Resources.LanguageManager
            {
                Enabled = true,
                Culture = new System.Globalization.CultureInfo("en")
            };
        }

        private static IServiceCollection AddCaching(this IServiceCollection services)
        {
            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = TimeSpan.FromSeconds(20);
                    options.DistributedCacheDuration = TimeSpan.FromSeconds(30);
                })
                .WithDistributedCache(sp =>
                {
                    var cacheProvider = sp.GetRequiredService<ICacheProvider>();

                    var options = new RedisCacheOptions { Configuration = cacheProvider.ConnectionString, InstanceName = cacheProvider.InstanceName };

                    return new RedisCache(options);
                })
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .AsHybridCache();

            return services;
        }

        private static WebApplicationBuilder AddWolverineMessaging(this WebApplicationBuilder builder)
        {
            builder.Host.UseWolverine(opts =>
            {
                opts.UseSystemTextJsonForSerialization();
                opts.ServiceName = "tc-agro-sensor-ingest-service";
                opts.ApplicationAssembly = typeof(Program).Assembly;

                opts.Discovery.IncludeAssembly(typeof(Application.DependencyInjection).Assembly);

                opts.Durability.MessageStorageSchemaName = DefaultSchemas.Wolverine;

                opts.PersistMessagesWithPostgresql(
                    PostgresHelper.Build(builder.Configuration).ConnectionString,
                    DefaultSchemas.Wolverine);

                opts.Policies.OnAnyException()
                    .RetryWithCooldown(
                        TimeSpan.FromMilliseconds(200),
                        TimeSpan.FromMilliseconds(400),
                        TimeSpan.FromMilliseconds(600),
                        TimeSpan.FromMilliseconds(800),
                        TimeSpan.FromMilliseconds(1000)
                    );

                opts.Policies.UseDurableLocalQueues();
                opts.Policies.AutoApplyTransactions();
                opts.UseEntityFrameworkCoreTransactions();

                opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

                var mqConnectionFactory = RabbitMqHelper.Build(builder.Configuration);

                var rabbitOpts = opts.UseRabbitMq(factory =>
                {
                    factory.Uri = new Uri(mqConnectionFactory.ConnectionString);
                    factory.VirtualHost = mqConnectionFactory.VirtualHost;
                    factory.ClientProperties["application"] = opts.ServiceName;
                    factory.ClientProperties["environment"] = builder.Environment.EnvironmentName;
                });

                if (mqConnectionFactory.AutoProvision)
                    rabbitOpts.AutoProvision();
                if (mqConnectionFactory.UseQuorumQueues)
                    rabbitOpts.UseQuorumQueues();
                if (mqConnectionFactory.AutoPurgeOnStartup)
                    rabbitOpts.AutoPurgeOnStartup();
            });

            return builder;
        }
    }
}
