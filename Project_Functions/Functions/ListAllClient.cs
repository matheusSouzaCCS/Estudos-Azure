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
using System.Collections.Generic;

namespace Project_Functions.Functions
{
    public static class ListAllClient
    {
        const string EndpointUrl = "https://project-azure-functions.documents.azure.com:443/";
        const string AuthorizationKey = "OlAwvjrJp2LJfjYN5RCmQtwSnpOGPISpIWoBSiLHckQkZpN7OX1yvFxpVHlUDAM4IJEVWZVHUjUQRCavQdj3oQ==";
        const string DatabaseId = "ClientContainer";
        const string ContainerId = "client_ID";

        [FunctionName("ListAllClient")]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);

            var sqlQueryText = "SELECT * FROM c";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<Client> clients = new List<Client>();

            using (FeedIterator<Client> feedIterator = container.GetItemQueryIterator<Client>(
                queryDefinition,
                null,
                new QueryRequestOptions()))
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync())
                    {
                        {
                            clients.Add(item);
                        }
                    }
                }
            }

            Console.WriteLine(clients.Count);
            Console.WriteLine(clients[0].name);

            return new OkObjectResult(clients);
        }
    }
}
