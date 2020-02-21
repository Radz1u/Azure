using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function {
    public static class HttpTriggerCSharp {
        [FunctionName ("HttpTriggerCSharp")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log) {
            var blobUrl = Environment.GetEnvironmentVariable ("BLOB_URL");
            var query = GenerateSASToken ();
            log.LogInformation ("C# HTTP trigger function processed a request.");

            var blobUri = new UriBuilder (blobUrl) { Query = query }.Uri;
            string name = "";

            var blobClient2 = new BlobClient (blobUri);
            using (var streamReader = new StreamReader (blobClient2.Download ().Value.Content)) {
                name = streamReader.ReadToEnd ();
            }

            return name != null ?
                (ActionResult) new OkObjectResult ($"Hello, {name}") :
                new BadRequestObjectResult ("Please pass a name on the query string or in the request body");
        }

        private static string GenerateSASToken () {
            var clientId = Environment.GetEnvironmentVariable ("IDENTITY_CLIENT_ID");
            var storageUrl = Environment.GetEnvironmentVariable ("STORAGE_ACCOUNT_URL");
            var blobUrl = Environment.GetEnvironmentVariable ("BLOB_URL");
            var identity = new ManagedIdentityCredential (clientId);
            var storageUri = new Uri (storageUrl);
            var blobUri = new Uri (blobUrl);
            var blobClient = new BlobClient (blobUri, identity);
            var blobSasBuilder = new Azure.Storage.Sas.BlobSasBuilder ();
            blobSasBuilder.BlobContainerName = blobClient.BlobContainerName;
            blobSasBuilder.BlobName = blobClient.Name;
            blobSasBuilder.SetPermissions (Azure.Storage.Sas.BlobSasPermissions.All);
            blobSasBuilder.ExpiresOn = new DateTimeOffset (DateTime.Now.AddDays (1));
            var blobServiceClient = new BlobServiceClient (storageUri, identity);
            var userDelegationKey = blobServiceClient.GetUserDelegationKey (new DateTimeOffset (DateTime.Now), new DateTimeOffset (DateTime.Now.AddDays (2)));
            var parameters = blobSasBuilder.ToSasQueryParameters (userDelegationKey, blobClient.AccountName);

            return parameters.ToString ();
        }
    }
}