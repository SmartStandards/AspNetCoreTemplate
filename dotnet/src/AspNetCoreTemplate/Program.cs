using Logging.SmartStandards; 
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;
using System.Web.UJMW;

namespace TemplateNamespace {

  public static partial class Program {

    static partial void OnConfigureServices(
      IServiceCollection services,
      IConfiguration config
    );

    static partial void OnRunApplication(
      WebApplication app,
      IConfiguration config,
      IServiceProvider services,
      IWebHostEnvironment environment,
      IHostApplicationLifetime lifetime
    );

    public static void Main(string[] args) {

      string explicitLaunchProfile = null;
      LaunchProfileHelper launchProfileHelper;
      if (LaunchProfileHelper.TryPickLaunchProfileFromCommandlineArgs(args, out explicitLaunchProfile)) {
        launchProfileHelper = LaunchProfileHelper.CreateForCurrentLaunchsettingsJson();
      }
      else {
        launchProfileHelper = LaunchProfileHelper.CreateEmpty();
      }

      WebApplicationBuilder builder;
      // :IHostApplicationBuilder

      if (launchProfileHelper.TryGetAspEnvironmentNameFromLaunchProfile(explicitLaunchProfile, out string aspEnvironmentName)) {
        builder = WebApplication.CreateBuilder(
          new WebApplicationOptions {Args = args, EnvironmentName = aspEnvironmentName }
        );
      }
      else {
        builder = WebApplication.CreateBuilder(args);
      }

      builder.AddBranchSpecificConfigurationFiles();

      IWebHostBuilder webHostBuilder = builder.WebHost;

      if (launchProfileHelper.TryGetUrlsFromLaunchProfile(explicitLaunchProfile, out string[] explicitUrls)) {
        webHostBuilder.UseUrls(explicitUrls);
      }

      //webHostBuilder.UseUrls(...);
      //webHostBuilder.UseContentRoot(AppContext.BaseDirectory);
      //webHostBuilder.UseWebRoot("wwwRoot");
      //webHostBuilder.UseStaticWebAssets();
      //webHostBuilder.UseEnvironment("");
      //webHostBuilder.UseSetting(overwrride configuration)
      //webHostBuilder.UseStartup<Startup>(); //LEGACY!!!

      IWebHostEnvironment webHostEnvironment = builder.Environment;
      // :IHostEnvironment

      //webHostEnvironment.ContentRootFileProvider
      //webHostEnvironment.WebRootFileProvider
      //webHostEnvironment.ApplicationName
      //webHostEnvironment.IsDevelopment();
      //webHostEnvironment.IsProduction();

      IConfiguration config = builder.Configuration;

      OnConfigureServices(builder.Services, config);

      WebApplication app = builder.Build();
      // :IHost 
      // :IApplicationBuilder

      //ushell hosting ook, aber nicht zus dateien
      app.UseDefaultFiles();

      OnRunApplication(app, config, app.Services, app.Environment, app.Lifetime);

      app.Run();
    }

  }

}
