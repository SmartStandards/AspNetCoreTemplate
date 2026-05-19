using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Web.UJMW;


namespace TemplateNamespace {

  public class Startup {

    public Startup(IConfiguration configuration) {
      _Configuration = configuration;
    }

    private static SmartStandardsCheapInitializer _AnyIocInitHelper = new SmartStandardsCheapInitializer();

    private static IConfiguration _Configuration = null;

    public void ConfigureServices(IServiceCollection services) {

      _AnyIocInitHelper.ConfigureServices(_Configuration, services);

      //services.AddLogging();
      services.AddSmartStandardsLogging(_Configuration);

      string outDir = AppDomain.CurrentDomain.BaseDirectory;

      services.AddControllers();
 
      MyDemoService myDemoService = new MyDemoService();
      services.AddSingleton<IMyDemoService>(myDemoService);

      UjmwHostConfiguration.UseCombinedDynamicAssembly = true;
      services.AddDynamicUjmwControllers((r) => {

        r.AddControllerFor<IMyDemoService>((options) => {
        });
  
      });
      
      //services.AddSwaggerGenSmartStandardsFlavored();
    }

    public void Configure(
      IApplicationBuilder app, IWebHostEnvironment env,
      ILoggerFactory loggerfactory, IHostApplicationLifetime lifetimeEvents
    ) {

      _AnyIocInitHelper.Configure(app, env, loggerfactory, lifetimeEvents);

      //required for the www-root
      app.UseStaticFiles();

      app.UseAmbientFieldAdapterMiddleware();

      if (!_Configuration.GetValue<bool>("ProdMode")) {
        app.UseDeveloperExceptionPage();
      }

      string baseUrl = _Configuration.GetValue<string>("BaseUrl");
 
      app.UseHttpsRedirection();

      app.UseRouting();

      //CORS: muss zwischen 'UseRouting' und 'UseEndpoints' liegen!
      app.UseCors(p =>
          p.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
      );

      app.UseAuthentication(); //<< WINDOWS-AUTH
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
      });

      //SelfAnnouncementHelper.Configure(
      //  lifetimeEvents, app.ServerFeatures,
      //  (string[] baseUrls, EndpointInfo[] endpoints, bool act, ref string info) => {

      //    var sb = new StringBuilder();
      //    string timestamp = DateTime.Now.ToLongTimeString();

      //    Console.WriteLine("--------------------------------------");
      //    if (act) {
      //      Console.WriteLine("ANNOUNCE:");
      //    }
      //    else {
      //      Console.WriteLine("UN-ANNOUNCE:");
      //    }
      //    Console.WriteLine("--------------------------------------");
      //    foreach (EndpointInfo ep in endpoints) {
      //      foreach (string url in baseUrls) {
      //        Console.WriteLine(ep.ToString(url));
      //        sb.Append(ep.ToString(url));
      //        if (act) {
      //          sb.AppendLine(" >> ONLINE @" + timestamp);
      //        }
      //        else {
      //          sb.AppendLine(" >> offline @" + timestamp);
      //        }

      //      }
      //    }
      //    Console.WriteLine("--------------------------------------");

      //    File.WriteAllText("_AnnouncementInfo.txt", sb.ToString());

      //    info = "was additionally written into file '_AnnouncementInfo.txt'";

      //  },
      //  autoTriggerInterval: 1
      //);

    }

  }

}
