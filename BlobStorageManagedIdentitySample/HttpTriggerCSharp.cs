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
            var sasToken = GenerateSASToken ();
            log.LogInformation ("C# HTTP trigger function processed a request.");

            var blobUri = new UriBuilder (blobUrl) { Query = sasToken }.Uri;
            string content = "";

            var blobClient = new BlobClient (blobUri);
            using (var streamReader = new StreamReader (blobClient.Download ().Value.Content)) {
                content = streamReader.ReadToEnd ();
            }

            return String.IsNullOrWhiteSpace(content)?
                new BadRequestObjectResult ("There is no content, or the SAS token is invalid"):
                (ActionResult) new OkObjectResult ($"Content: {content}") ;
        }

        private static string GenerateSASToken () {
            var storageUrl = Environment.GetEnvironmentVariable ("STORAGE_ACCOUNT_URL");
            var blobUrl = Environment.GetEnvironmentVariable ("BLOB_URL");
            var identity = CreateManagedIdentityCredential ();
            var blobClient = new BlobClient (new Uri (blobUrl), identity);
            var blobSasBuilder = new Azure.Storage.Sas.BlobSasBuilder ();
            blobSasBuilder.BlobContainerName = blobClient.BlobContainerName;
            blobSasBuilder.BlobName = blobClient.Name;
            blobSasBuilder.SetPermissions (Azure.Storage.Sas.BlobSasPermissions.All);
            blobSasBuilder.ExpiresOn = new DateTimeOffset (DateTime.Now.AddDays (1));
            var blobServiceClient = new BlobServiceClient (new Uri (storageUrl), identity);
            var userDelegationKey = blobServiceClient.GetUserDelegationKey (new DateTimeOffset (DateTime.Now), new DateTimeOffset (DateTime.Now.AddDays (2)));
            var parameters = blobSasBuilder.ToSasQueryParameters (userDelegationKey, blobClient.AccountName);

            return parameters.ToString ();
        }

        private static ManagedIdentityCredential CreateManagedIdentityCredential () {
            var clientId = Environment.GetEnvironmentVariable ("IDENTITY_CLIENT_ID");
            return new ManagedIdentityCredential (clientId);
        }
    }
}