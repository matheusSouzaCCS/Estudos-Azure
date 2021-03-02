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
using System.Net;
using System.Collections.Generic;

namespace Project_Functions
{
    public static class FunctionClient
    {
        const string EndpointUrl = "https://project-azure-functions.documents.azure.com:443/";
        const string AuthorizationKey = "OlAwvjrJp2LJfjYN5RCmQtwSnpOGPISpIWoBSiLHckQkZpN7OX1yvFxpVHlUDAM4IJEVWZVHUjUQRCavQdj3oQ==";
        const string DatabaseId = "ClientContainer";
        const string ContainerId = "client_ID";

        [FunctionName("FunctionClient")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Client")] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Client data = JsonConvert.DeserializeObject<Client>(requestBody);

            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);

            //await RegisterClient(cosmosClient, data);

            //var clients = await QueryItemsAsync(cosmosClient);

            var clients = await Filter(cosmosClient, data);

            //await ReplaceFamilyItemAsync(cosmosClient, data);

            //return new OkObjectResult(clients);

            return new OkObjectResult(clients);

        }

        //[FunctionName("FunctionClient1")]
        //public static async Task<IActionResult> Still(
        //    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        //    ILogger log)
        //{
        //    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //    Client data = JsonConvert.DeserializeObject<Client>(requestBody);

        //    CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);

        //    //await RegisterClient(cosmosClient, data);

        //    //var clients = await QueryItemsAsync(cosmosClient);

        //    //var clients = await Filter(cosmosClient, data);

        //    await ReplaceFamilyItemAsync(cosmosClient, data);

        //    //return new OkObjectResult(clients);

        //    return new OkObjectResult("");

        //}

        [FunctionName("POSTClient")]
        public static async Task RegisterClient (CosmosClient cosmosClient, dynamic data)
        {
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

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
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                ItemResponse<Client> clientResponse = await container.CreateItemAsync<Client>(client, new PartitionKey(client.clientKey));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse.
                Console.WriteLine("Created item in database with client: {0}\n", client.name);
            }
        }

        [FunctionName("GETAllClients")]
        public static async Task<List<Client>> QueryItemsAsync(CosmosClient cosmosClient)
        {
            var sqlQueryText = "SELECT * FROM c";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<Client> clients = new List<Client>();

            using (FeedIterator<Client> feedIterator = container.GetItemQueryIterator<Client>(
                queryDefinition,
                null,
                new QueryRequestOptions() { PartitionKey = new PartitionKey("The King") }))
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
            return clients;
        }

        [FunctionName("GETByNameClient")]
        private static async Task<List<Client>> Filter(CosmosClient cosmosClient, dynamic data)
        {
            var sqlQueryText = $"SELECT * FROM c WHERE c.name = {data.name}";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            List<Client> clients = new List<Client>();

            using (FeedIterator<Client> feedIterator = container.GetItemQueryIterator<Client>(
                queryDefinition,
                null,
                new QueryRequestOptions() { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(data.clientKey) }))
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

            return clients;
        }

        //Nao entendi oq ocorre aq
        [FunctionName("PUTClient")]
        private static async Task ReplaceFamilyItemAsync(CosmosClient cosmosClient, dynamic data)
        {
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

            ItemResponse<Client> clientResponse = await container.ReadItemAsync<Client>("2fdf92a4-98fd-47a3-926c-83e1e39276c8", new PartitionKey("The Naburu"));
            Client itemBody = clientResponse;

            itemBody.name = data.name;

            // replace the item with the updated content
            clientResponse = await container.ReplaceItemAsync<Client>(itemBody, itemBody.id, new PartitionKey(itemBody.clientKey));
            Console.WriteLine("Updated Family [{0},{1}].\n \tBody is now: {2}\n", itemBody.clientKey, itemBody.id, clientResponse.Resource);
        }
    }
}   