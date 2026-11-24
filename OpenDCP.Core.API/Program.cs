#pragma warning disable CS1591

using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace OpenDCP.Core.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
              Host.CreateDefaultBuilder(args)
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                     {
                         config
                              .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                              .AddJsonFile("appsettings.json", true, true)
                              .AddJsonFile("debug.user.json", true, true)
                         .AddEnvironmentVariables();

                     })
                     .ConfigureKestrel(options =>
                     {
                         options.ConfigureHttpsDefaults(httpsOptions =>
                         {
                             httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                         });
                     })
                     .UseStartup<Startup>();
                 });
    }
}

#pragma warning restore CS1591
