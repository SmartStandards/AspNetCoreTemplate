using Logging.SmartStandards; 
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using System.Reflection;
using System.Web.UJMW;

namespace TemplateNamespace {

  public class Program {

    private static SmartStandardsCheapInitializer _AnyIocInitHelper = new SmartStandardsCheapInitializer();

    private static IConfiguration _Configuration = null;

    public static void Main(string[] args) {

      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      _Configuration = builder.Configuration;

      #region " configure Services "
      //////////////////////////////////////////////////////////////////////////////////////
      
      _AnyIocInitHelper.ConfigureServices(_Configuration, builder.Services);

      //builder.Services.AddLogging();
      builder.Services.AddSmartStandardsLogging(_Configuration);

      MyDemoService myDemoService = new MyDemoService();

      builder.Services.AddSingleton<IMyDemoService>(myDemoService);

      builder.Services.AddControllers();

      UjmwHostConfiguration.UseCombinedDynamicAssembly = true;
      builder.Services.AddDynamicUjmwControllers((r) => {

        r.AddControllerFor<IMyDemoService>((options) => {
        });

      });

      // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
      builder.Services.AddOpenApi();

      //////////////////////////////////////////////////////////////////////////////////////
      #endregion

      WebApplication app = builder.Build();

      #region " (configure) Application-Init "
      //////////////////////////////////////////////////////////////////////////////////////
      
      // pick some usually used services (which were recently used by Configure methods
      IWebHostEnvironment env = app.Environment;
      IHostApplicationLifetime applicationLifetime = app.Lifetime;
      IApplicationBuilder appBuilder = app; //ugly naming: WebApplication==IApplicationBuilder != WebApplicationBuilder
      ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

      _AnyIocInitHelper.Configure(appBuilder, env, loggerFactory, applicationLifetime);

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment()) {
        app.MapOpenApi();
      }

      //required for the www-root
      app.UseStaticFiles();

      app.UseAmbientFieldAdapterMiddleware();

      if (!_Configuration.GetValue<bool>("ProdMode")) {
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

      DevLogger.LogInformation(2090995669573058480L, EventKind.WebApplicationStarted);

      //////////////////////////////////////////////////////////////////////////////////////
      #endregion

      app.Run();
    }

  }

}
