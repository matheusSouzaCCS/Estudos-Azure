using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Project_Functions.Models;

namespace Project_Functions.Functions
{
    public static class UpdateClient
    {
        const string EndpointUrl = "https://project-azure-functions.documents.azure.com:443/";
        const string AuthorizationKey = "OlAwvjrJp2LJfjYN5RCmQtwSnpOGPISpIWoBSiLHckQkZpN7OX1yvFxpVHlUDAM4IJEVWZVHUjUQRCavQdj3oQ==";
        const string DatabaseId = "ClientContainer";
        const string ContainerId = "client_ID";

        [FunctionName("UpdateClient")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "UpdateClient/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);

            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);
            
            // nao entendi oq ele faz
            ItemResponse<Client> clientResponse = await container.ReadItemAsync<Client>(id , new PartitionKey("The Naburu"));
            Client itemBody = clientResponse;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Client data = JsonConvert.DeserializeObject<Client>(requestBody);

            itemBody.name = data.name;
            itemBody.surname = data.surname;
            itemBody.email = data.email;

            // nao é possivel atualizar a partition key, pq?

            // replace the item with the updated content
            clientResponse = await container.ReplaceItemAsync<Client>(itemBody, itemBody.id, new PartitionKey(itemBody.clientKey));
            Console.WriteLine("Updated Family [{0},{1}].\n \tBody is now: {2}\n", itemBody.clientKey, itemBody.id, clientResponse.Resource);

            return new OkObjectResult("");
        }
    }
}
