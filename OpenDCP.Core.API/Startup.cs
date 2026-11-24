using OpenDCP.Core.API.Converters;
using OpenDCP.Core.API.Middleware;
using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.Core.Services;
using OpenDCP.Core.Core.Services.Data;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Banner;
using OpenDCP.Framework.Configuration.Model.Entities.ModuleSettings;
using OpenDCP.Framework.Configuration.Model.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Constants;
using OpenDCP.Framework.ConfigurationProvider.Factories;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Models;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Datasource.Base.Interfaces;
using OpenDCP.Framework.DataTypes.Analytical.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.Messaging.Factories;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Factories;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;

namespace OpenDCP.Core.API
{
    /// <summary>
    /// Configures the application's services and HTTP request pipeline.
    /// </summary>
    public class Startup
    {
        private IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration.</param>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Adds services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Set the default thread culture to Invariant
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            services.AddCors(options =>
            {
                options.AddPolicy("default", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            var logger = new LoggerConfiguration()
              .ReadFrom.Configuration(_configuration)
              .Enrich.FromLogContext()
              .CreateLogger();

            //Add logging
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));

            services.AddHttpContextAccessor();

            services.AddTransient<ITokenService, TokenService>();
            services.AddTransient<IConfigurationService, ConfigurationService>();
            services.AddTransient<ISiteService, SiteService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IUserClientService, UserClientService>();
            services.AddTransient<IBannerService, BannerService>();
            services.AddTransient<IAuditTrailService, AuditTrailService>();
            services.AddTransient<IVersionService, VersionService>();
            services.AddTransient<IShareLinkService, ShareLinkService>();
            services.AddTransient<IEventSubscriptionService, EventSubscriptionService>();
            services.AddTransient<ILegacyService, LegacyService>();
            services.AddTransient<IConnectionBridgeService, ConnectionBridgeService>();
            services.AddTransient<IDashboardService, DashboardService>();
            services.AddTransient<IModuleService, ModuleService>();
            services.AddTransient<IDatasourceService, DatasourceService>();

            //Register data services
            services.AddTransient<IAssetHierarchyDataService, AssetHierarchyDataService>();
            services.AddTransient<ISensorTelemetryDataService, SensorTelemetryDataService>();
            services.AddTransient<IAnalyticalDataService, AnalyticalDataService>();
            services.AddTransient<IEventframeContextDataService, EventframeContextDataService>();
            services.AddTransient<IBundleDataService, BundleDataService>();

            //Register DCP providers
            //Service discovery
            services.AddSingleton<IServiceDiscoveryConfiguration>(provider =>
            {
                var serviceDiscoveryFactory = new ServiceDiscoveryConfigurationFactory();
                return serviceDiscoveryFactory.Load(_configuration["ServiceDiscoveryProvider:Driver"]);
            });

            services.AddSingleton<IServiceDiscovery>(provider =>
            {
                var serviceDiscoveryFactory = new ServiceDiscoveryFactory();
                return serviceDiscoveryFactory.Load(_configuration["ServiceDiscoveryProvider:Driver"], provider);
            });


            // Build the provider to resolve an instance
#pragma warning disable ASP0000 //Need this flow to build the service container
            var serviceProvider = services.BuildServiceProvider();
            var sharedConfigurationProvider = serviceProvider.GetRequiredService<IServiceDiscoveryConfiguration>();

            _configuration = sharedConfigurationProvider.AddSharedConfiguration(
                services,
                _configuration,
                new SharedConfigurationOptions
                {
                    AddHeaders = true,
                    AddMemoryCache = true,
                    AddMessageBus = true,
                    AddSecretProvider = true,
                    AddIdP = false,
                    AddSupportInfo = true,
                }
            );

            //SecretConfiguration
            services.AddSingleton<ISecretConfigurationProvider>(provider =>
            {
                var secretConfigurationFactory = new SecretConfigurationProviderFactory();
                return secretConfigurationFactory.Load(_configuration["SecretConfigurationProvider:Driver"], null, _configuration);
            });

            //MessageBus
            services.AddSingleton<IMessageBus>(provider =>
            {
                var messageBusFactory = new MessageBusFactory();
                return messageBusFactory.Load(_configuration["MessageBusProvider:Driver"], _configuration);
            });

            services.AddMemoryCache();
            services.AddHealthChecks();

            // Build the provider to resolve an instance
            serviceProvider = services.BuildServiceProvider();
            var secretConfigurationProvider = serviceProvider.GetRequiredService<ISecretConfigurationProvider>();
#pragma warning restore ASP0000

            var secretConfigurationProviderOptions = new SecretConfigurationProviderOptions
            {
                AddEncryptionKey = true,
                AddModuleClient = true,
                AddAudience = true,
                AddHeaders = true,
                AddLegacyHeaders = true,
                AddMessageBusConfiguration = true,
                MessageBusType = MessageBusType.RabbitMQ
            };

            if (string.Equals(_configuration["IdP:Driver"], "ad", StringComparison.OrdinalIgnoreCase))
            {
                secretConfigurationProviderOptions.AddLDAP = true;
            }

            _configuration = secretConfigurationProvider.AddSecretsToConfiguration(
                _configuration,
                secretConfigurationProviderOptions
            );

            var serviceDiscovery = serviceProvider.GetRequiredService<IServiceDiscovery>();

            string env = _configuration["Host:EnvironmentSlug"] ?? "dev";
            List<string> list = new List<string> { "frontend", "spa", };

            var platformUrl = (serviceDiscovery.QueryActiveServices(list, env)).Result
                .Select((RegisteredService x) => $"https://{x.Host}")
                .FirstOrDefault();
            _configuration["Host:App:Main"] = platformUrl;


            services.AddSingleton<IConfiguration>(_configuration);

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            if (!string.Equals(_configuration["DevelopmentMode"], "true", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(60);
                });

                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = 8187;
                });
            }

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                o.IncludeXmlComments(xmlPath);

                // Models project XML
                var modelsAssembly = typeof(BannerDTO).Assembly;
                var modelsXmlFile = $"{modelsAssembly.GetName().Name}.xml";
                var modelsXmlPath = Path.Combine(AppContext.BaseDirectory, modelsXmlFile);
                o.IncludeXmlComments(modelsXmlPath);

                o.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            services.AddMvc();

            services.AddDbContext<DCPCoreDbContext>(options =>
                options.UseNpgsql(secretConfigurationProvider.GetConnectionString(databaseType: "PostgreSQL"),
                sqlOptions => sqlOptions.CommandTimeout(60)),
                ServiceLifetime.Scoped
            );

            // Configure JWT Authentication
            var audienceSecret = _configuration["IdP:Settings:Audience:Secret"];
            if (audienceSecret is null)
                throw new InvalidConfigurationException("Audience:Secret missing in configuration");

            services.AddCertificateForwarding(options =>
            {
                options.CertificateHeader = "X-Client-Cert";
                options.HeaderConverter = headerValue =>
                    string.IsNullOrEmpty(headerValue)
                        ? null!
                        : X509CertificateLoader.LoadCertificate(Convert.FromBase64String(headerValue));
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.All;
                options.ValidateCertificateUse = true;
                options.ValidateValidityPeriod = true;
                options.RevocationMode = X509RevocationMode.NoCheck;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["IdP:Settings:Issuer"],
                    ValidAudience = _configuration["IdP:Settings:Audience:Id"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(audienceSecret))
                };

                if (!string.Equals(_configuration["Debug:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var typeClaim = context.Principal?.Claims
                                .FirstOrDefault(c => c.Type == "persistedGrantsType")?.Value;

                            if (typeClaim != "1")
                                return;

                            var userId = context.Principal?.Identity?.Name;

                            if (string.IsNullOrEmpty(userId))
                            {
                                context.Fail("Missing user id");
                                return;
                            }

                            // Resolve services from DI
                            var tokenService = context.HttpContext.RequestServices
                                .GetRequiredService<ITokenService>();

                            var isAllowed = await tokenService.IsAllowedAsync(userId);

                            if (!isAllowed)
                            {
                                context.Fail(ErrorMessages.SessionExpired);
                            }
                        }
                    };
                }
            })
            .AddJwtBearer("JwtIgnoreExpiration", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["IdP:Settings:Issuer"],
                    ValidAudience = _configuration["IdP:Settings:Audience:Id"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(audienceSecret))
                };
            });

            //Add the Authentication provider
            var idpDriver = _configuration["IdP:Driver"];
            if (idpDriver is null)
                throw new InvalidConfigurationException("IdP:Driver missing in configuration");

            services.AddTransient<IAuthenticationProvider>(provider =>
            {
                return AuthenticationProviderFactory.Load(idpDriver, _configuration);
            });

            services.AddControllers(config =>
            {
                if (!_configuration.GetValue<bool>("Debug:DisableAuth", false))
                {
                    config.Filters.Add(new AuthorizeFilter());
                }
            }).AddJsonOptions(options =>
            {
                // Keep PascalCase property names
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                // Enforce ISO8601 format with 3 digits and the Z format
                options.JsonSerializerOptions.Converters.Add(new Iso8601Converter());
                // Configure enums to be serialized as strings
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                //Configure handling of numbers
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            });

            //Register the service
            var serviceDiscoveryConfig = serviceProvider.GetRequiredService<IServiceDiscoveryConfiguration>();
            serviceDiscoveryConfig.ConfigureServiceDiscovery(services, _configuration);

            //Register datasources
            var pluginPath = _configuration["Host:PluginsPath"];
            if (pluginPath is null || !Directory.Exists(pluginPath))
            {
                throw new InvalidConfigurationException("Plugin directory not existing");
            }

            services.AddSingleton(sp =>
            {
                return new DatasourceCache(
                    pluginPath,
                    _configuration.GetValue("Debug:Enabled", false),
                    typeof(ISensorTelemetryDataSource),
                    typeof(IEventframeContextDataSource),
                    typeof(IAssetHierarchyDataSource),
                    typeof(IAnalyticalDataSource),
                    typeof(IDatasource),
                    typeof(IEventTranslation),
                    typeof(IRelationDataSource)
                );
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline for the application.
        /// This method is called by the runtime and sets up middleware, routing, authentication,
        /// authorization, response compression, Swagger UI, static file serving, CORS, health checks,
        /// and service discovery.
        /// </summary>
        /// <param name="app">The application builder used to configure the middleware pipeline.</param>
        /// <param name="serviceDiscovery">
        /// The service discovery configuration used to register and enable service discovery
        /// for this application, including module metadata and active service registration.
        /// </param>
        /// <param name="messageBus">
        /// The message bus used for sending user based messages
        /// </param>
        /// <remarks>
        /// Middleware and services configured in this method include:
        /// <list type="bullet">
        ///   <item><description>Swagger UI for API documentation.</description></item>
        ///   <item><description>CORS policy named "default".</description></item>
        ///   <item><description>Health checks endpoint at "/health".</description></item>
        ///   <item><description>Static file serving for login forms and other assets.</description></item>
        ///   <item><description>Developer exception page in debug mode or HTTPS redirection in production.</description></item>
        ///   <item><description>Routing, authentication, and authorization middleware.</description></item>
        ///   <item><description>Controller endpoints mapped.</description></item>
        ///   <item><description>Response compression using Brotli and Gzip.</description></item>
        ///   <item><description>Optional Swagger request validation based on configuration headers.</description></item>
        ///   <item><description>Service discovery registration using module metadata.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if required configuration or plugin directories are missing (handled in other parts of Startup).
        /// </exception>
        public void Configure(IApplicationBuilder app, IServiceDiscoveryConfiguration serviceDiscovery, IMessageBus messageBus)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("default");
            app.UseHealthChecks("/health");

            // Enable serving static files - for Login form
            app.UseStaticFiles();

            if (!_configuration.GetValue<bool>("Debug:Enabled"))
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseCertificateForwarding();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseResponseCompression();

            messageBus.Connect("Core API");

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.HasValue
                    && context.Request.Path.Value.StartsWith("/swagger", StringComparison.InvariantCultureIgnoreCase)
                    && !string.Equals(_configuration["Debug:EnableSwagger"], "true", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrEmpty(_configuration["Headers:Swagger:Name"])
                    && !string.Equals(
                            _configuration["Headers:Swagger:Name"],
                            _configuration["Headers:Swagger:Value"],
                            StringComparison.OrdinalIgnoreCase
                          ))
                {
                    context.Response.Redirect(context.Request.Host.Value);
                    return;
                }
                await next.Invoke();
            });

            //Register service
            var metaData = LoadModuleConfiguration();
            serviceDiscovery.UseServiceDiscovery(app, _configuration, metaData);
        }

        private static ModuleMetaData LoadModuleConfiguration()
        {

            return new ModuleMetaData
            {
                CalculationNodeLibraries = new List<string>(),
                DataTypes = new List<ModuleDataTypeConfigurationDTO>(),
                Resources = new List<ModuleResourceDTO>(),
                ModuleName = "Core"
            };
        }
    }
}
