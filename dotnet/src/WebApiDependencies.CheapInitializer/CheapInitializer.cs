using DistributedDataFlow;
using Logging.SmartStandards;
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Web.UJMW;

[assembly: AssemblyMetadata("SourceContext", "SmartStandards.WebApiDependencies.CheapInitializer")]

namespace Microsoft.AspNetCore {

  public static class SmartStandardsCheapInitializer {

    public static void OnConfigureServices(IServiceCollection services, IConfiguration config) {

      services.AddSmartStandardsLogging(config);

      services.AddControllers();

      UjmwHostConfiguration.UseCombinedDynamicAssembly = true;

      //services.AddOpenApi();

      //string applicationAssemblyName = Assembly.GetCallingAssembly().GetName().Name;

      //UjmwHostConfiguration.AuthHeaderEvaluator =
      //  Security.AccessTokenHandling.AccessTokenValidator.TryValidateHttpAuthHeader;

      //TODO: addswagger

      //AmbienceHub.DefineFlowingContract(
      //  "tenant-identifiers",
      //  (contract) => {
      //    contract.IncludeExposedAmbientFieldInstances("currentTenant");
      //    contract.IncludeExposedAmbientFieldInstances("dtHandle");
      //  }
      //);

      services.AddSwaggerGenSmartStandardsFlavored();

    }

    public static void OnRunApplication(
      WebApplication app, IConfiguration config, IServiceProvider services,
      IWebHostEnvironment environment, IHostApplicationLifetime lifetime
    ) {

      ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();

      // Configure the HTTP request pipeline.
      if (environment.IsDevelopment()) {
        //app.MapOpenApi();
      }

      //required for the www-root
      app.UseStaticFiles();

      app.UseAmbientFieldAdapterMiddleware();

      if (!config.GetValue<bool>("ProdMode")) {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      //CORS: muss zwischen 'UseRouting' und 'UseEndpoints' liegen!
      app.UseCors(
        (p) => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
      );

      //app.UseAuthentication(); //<< WINDOWS-AUTH
      app.UseAuthorization();

      app.MapControllers();

    }

  }

}
