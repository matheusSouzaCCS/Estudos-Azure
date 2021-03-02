using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;
using Microsoft.Azure.Cosmos;
using Project_Functions.Models;
using System.Net;

namespace Project_Functions
{
    public static class RegisterClient
    {
        const string EndpointUrl = "https://project-azure-functions.documents.azure.com:443/";
        const string AuthorizationKey = "OlAwvjrJp2LJfjYN5RCmQtwSnpOGPISpIWoBSiLHckQkZpN7OX1yvFxpVHlUDAM4IJEVWZVHUjUQRCavQdj3oQ==";
        const string DatabaseId = "ClientContainer";
        const string ContainerId = "client_ID";

        [FunctionName("RegisterClient")]
        public static async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
        ILogger log)
        {
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Client data = JsonConvert.DeserializeObject<Client>(requestBody);

            Microsoft.Azure.Cosmos.Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

            Client client = new Client
            {
                name = data.name,
                surname = data.surname,
                email = data.email,
                clientKey = data.clientKey,
                id = Guid.NewGuid().ToString()
            };

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Client> clientResponse = await container.ReadItemAsync<Client>(client.id, new PartitionKey(client.clientKey));
                Console.WriteLine("Item in database with client: {0} already exists\n", client.name);
                return new OkObjectResult($"Item in database with client: {client.name} already exists\n");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                ItemResponse<Client> clientResponse = await container.CreateItemAsync<Client>(client, new PartitionKey(client.clientKey));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse.
                Console.WriteLine("Created item in database with client: {0}\n", client.name);
                return new OkObjectResult($"Created item in database with client: {client.name}\n");
            }
        }
    }
}
