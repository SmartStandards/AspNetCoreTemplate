using DistributedDataFlow;
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Web.UJMW;

namespace Microsoft.AspNetCore {

  public class SmartStandardsCheapInitializer {

    /// <summary>
    /// to be used during Startup.cs
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="serviceCollection"></param>
    public virtual void ConfigureServices(
      IConfiguration configuration,
      IServiceCollection serviceCollection
    ) {


      string applicationAssemblyName = Assembly.GetCallingAssembly().GetName().Name;

      UjmwHostConfiguration.AuthHeaderEvaluator =
        Security.AccessTokenHandling.AccessTokenValidator.TryValidateHttpAuthHeader;

      serviceCollection.AddSmartStandardsLogging(configuration, applicationAssemblyName);

      //TODO: addswagger

      //AmbienceHub.DefineFlowingContract(
      //  "tenant-identifiers",
      //  (contract) => {
      //    contract.IncludeExposedAmbientFieldInstances("currentTenant");
      //    contract.IncludeExposedAmbientFieldInstances("dtHandle");
      //  }
      //);

    }

    /// <summary>
    /// to be used during Startup.cs
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    /// <param name="loggerfactory"></param>
    /// <param name="lifetimeEvents"></param>
    public virtual void Configure(
      IApplicationBuilder app,
      IWebHostEnvironment env,
      ILoggerFactory loggerfactory,
      IHostApplicationLifetime lifetimeEvents
    ) {



    }

  }

}
