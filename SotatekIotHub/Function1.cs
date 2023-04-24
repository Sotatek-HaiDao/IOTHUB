using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Newtonsoft.Json.Linq;
using Azure.Identity;
using System.Net.Http;
using Azure;

namespace SotatekIotHub
{
    public static class Function1
    {
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_URL");
        private static readonly HttpClient singletonHttpClientInstance = new HttpClient();
        [FunctionName("IOTHubtoTwins")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, [FromQuery] string deviceId)
        {
            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");
            try
            {
                var cred = new ManagedIdentityCredential("https://digitaltwins.azure.net");

                var client = new DigitalTwinsClient(
                new Uri(adtInstanceUrl),
                cred,
                new DigitalTwinsClientOptions
                {
                    Transport = new HttpClientTransport(singletonHttpClientInstance)
                });
                log.LogInformation($"ADT service client connection created.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);   
                var temperature = data?.Temperature;
                //log the temperature and humidity
                log.LogInformation($"Temperature is:{temperature}");

                // Update twin with temperature and humidity fro our raspberry pi>
                var updateTwinData = new JsonPatchDocument();
                updateTwinData.AppendReplace("/Temperature", temperature.Value<double>());
                await client.UpdateDigitalTwinAsync(deviceId, updateTwinData);
                return new OkObjectResult(data?.Temperature);
            }
            catch (Exception ex)
            {
                log.LogError($"Error in ingest function: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
           
        }
    }
}
