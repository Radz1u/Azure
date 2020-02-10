using System;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Semantics.AzFunction
{
    public static class HttpTriggerCSharp {
        [FunctionName ("HttpTriggerCSharp")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log) {

            var managedIdentityClientId = Environment.GetEnvironmentVariable ("MANAGED_IDENTITY_CLIENT_ID");
            var configurationEndpoint = Environment.GetEnvironmentVariable ("CONFIGURATION_ENDPOINT");
            
            var managedIdentityCredential = new ManagedIdentityCredential (managedIdentityClientId);
            var configurationEndpointUri = new Uri(configurationEndpoint);

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder ();
            configurationBuilder.AddAzureAppConfiguration (options => {
                options.Connect (configurationEndpointUri, managedIdentityCredential);
                options.ConfigureKeyVault (opt => {
                    opt.SetCredential (managedIdentityCredential);
                });
            });
            var name = configurationBuilder.Build () ["tst"];
            log.LogInformation ("C# HTTP trigger function processed a request.");

            return name != null ?
                (ActionResult) new OkObjectResult ($"Hello, {name}") :
                new BadRequestObjectResult ("Please pass a name on the query string or in the request body");
        }
    }
}