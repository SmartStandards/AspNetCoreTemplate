using Logging.SmartStandards;
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Security.AccessTokenHandling;
using System;
using System.Reflection;
using System.Text;
using System.Web.UJMW;

namespace TemplateNamespace {

  public static partial class Program {

    static partial void OnConfigureServices(IServiceCollection services, IConfiguration config) {

      //optional: will do some standardized basics for you...
      SmartStandardsCheapInitializer.OnConfigureServices(services, config);

      //services.AddSmartStandardsLogging(config);

      MyDemoService myDemoService = new MyDemoService();

      services.AddSingleton<IMyDemoService>(myDemoService);

      UjmwHostConfiguration.AuthHeaderEvaluator = AccessTokenValidator.TryValidateHttpAuthHeader;
      AccessTokenValidator.ConfigureTokenValidation(
        new LocalJwtIntrospector(Encoding.UTF8.GetBytes("MyDemoJwtH256SignKey")),
        (options) => {
          //this makes tokens optional (if none is provided, were acting as subject '(anonymous)')
          options.EnableAnonymousSubject("(anonymous)");
        }
      );

      //services.AddControllers();

      //UjmwHostConfiguration.UseCombinedDynamicAssembly = true;
      services.AddDynamicUjmwControllers((r) => {

        r.AddControllerFor<IMyDemoService>((options) => {
          options.ApiGroupName = "Demo";
        });

      });

      //services.AddSwaggerGenSmartStandardsFlavored();

    }

    static partial void OnRunApplication(
      WebApplication app, IConfiguration config, IServiceProvider services,
      IWebHostEnvironment environment, IHostApplicationLifetime lifetime
    ) {

      //optional: will do some standardized basics for you...
      SmartStandardsCheapInitializer.OnRunApplication(app, config, services, environment, lifetime);

      //ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();

      //// Configure the HTTP request pipeline.
      //if (environment.IsDevelopment()) {
      //  //app.MapOpenApi();
      //}

      ////required for the www-root
      //app.UseStaticFiles();

      ////app.UseAmbientFieldAdapterMiddleware();

      //if (!config.GetValue<bool>("ProdMode")) {
      //  app.UseDeveloperExceptionPage();
      //}

      //app.UseHttpsRedirection();

      //app.UseRouting();

      ////CORS: muss zwischen 'UseRouting' und 'UseEndpoints' liegen!
      //app.UseCors(
      //  (p) => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
      //);

      ////app.UseAuthentication(); //<< WINDOWS-AUTH
      //app.UseAuthorization();

      //app.MapControllers();

      DevLogger.LogInformation(2090995669573058480L, EventKind.WebApplicationStarted);

    }

  }

}
