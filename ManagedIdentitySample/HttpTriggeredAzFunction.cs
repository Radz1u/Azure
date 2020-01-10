using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;


namespace Semantics.AzFunction {
    public static class HttpTriggeredAzFunction {
        [FunctionName ("HttpTriggeredAzFunction")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log) {
            var name = System.Environment.GetEnvironmentVariable ("AzureServicesAuthConnectionString");
            var azureServiceProvider = new AzureServiceTokenProvider (name);
            var authenticationCallback = new KeyVaultClient.AuthenticationCallback (azureServiceProvider.KeyVaultTokenCallback);
            var keyVaultClient = new KeyVaultClient (authenticationCallback);
            var secret = await keyVaultClient.GetSecretAsync ("https://test-eun-key-vault.vault.azure.net/secrets/test-secret/249a9ea8b4b74fbe903a76c8ab68589a");

            name = secret.Value;

            var appconfigConnectionString = await keyVaultClient.GetSecretAsync ("https://test-eun-key-vault.vault.azure.net/secrets/testapp0901-appconfig-connectionstring/f53818b806de476e8fcdeb8e062f4578");
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddAzureAppConfiguration(appconfigConnectionString.Value);
            var config = configurationBuilder.Build();

            name = config["test-key"];

            log.LogInformation ("C# HTTP trigger function processed a request.");

            return name != null ?
                (ActionResult) new OkObjectResult ($"Hello, {name}") :
                new BadRequestObjectResult ("Please pass a name on the query string or in the request body");
        }
    }
}