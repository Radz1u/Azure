using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Semantics.AzFunction
{
    public static class HttpTriggeredAzFunction {
        [FunctionName ("HttpTriggeredAzFunction")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log) {

            var azureServiceProvider = new AzureServiceTokenProvider ();
            var authenticationCallback = new KeyVaultClient.AuthenticationCallback (azureServiceProvider.KeyVaultTokenCallback);
            var keyVaultClient = new KeyVaultClient (authenticationCallback);
            
            var appconfigConnectionStringSecretUri = Environment.GetEnvironmentVariable ("appconfig-kv-secret-uri");
            var appconfigConnectionString = await keyVaultClient.GetSecretAsync (appconfigConnectionStringSecretUri);
            var configurationBuilder = new ConfigurationBuilder ();
            configurationBuilder.AddAzureAppConfiguration (appconfigConnectionString.Value);
            var config = configurationBuilder.Build ();

            var name = config["test-key"];

            log.LogInformation ("C# HTTP trigger function processed a request.");

            return name != null ?
                (ActionResult) new OkObjectResult ($"Hello, {name}") :
                new BadRequestObjectResult ("Please pass a name on the query string or in the request body");
        }
    }
}