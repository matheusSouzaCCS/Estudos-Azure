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
using System.Net;
using System.Collections.Generic;

namespace CRUD_user
{
    public class CRUD_user
    {

        const string EndpointUrl = "https://picdb.documents.azure.com:443/";
        const string AuthorizationKey = "VKlbIgdcpaXANQfjRC1DmSnu2TigRYeMyCryae0C6D6eqYXtgp1RBp6sGpBQF1zkSHJtvztcwbAKsdR2Z5x3mw==";
        const string DatabaseId = "picData2";
        const string ContainerId = "User1";

        [FunctionName("CRUD_user")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", Route = null)] HttpRequest req,
            ILogger log)
        {

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic client = JsonConvert.DeserializeObject(requestBody);


            CosmosClient cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey);
            //await CreateDatabaseAsync(cosmosClient);
            //await CreateContainerAsync(cosmosClient);
            //await AddItemsToContainerAsync(cosmosClient);
            var clients = await QueryItemsAsync(cosmosClient);
            //await ReplaceFamilyItemAsync(cosmosClient);


            return new OkObjectResult(clients);
        }

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private static async Task AddItemsToContainerAsync(CosmosClient cosmosClient)
        {
            // Create a family object for the Andersen family

            Client client = new Client
            {
                id = Guid.NewGuid().ToString(),
                NameClient = "Matheus",
                userType = "Teste"
            };
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);
            try
            {
                // Read the item to see if it exists.  
                ItemResponse<Client> clientResponse = await container.ReadItemAsync<Client>(client.id, new PartitionKey(client.userType));
                Console.WriteLine("Item in database with id: {0} already exists\n", client.id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
                ItemResponse<Client> clientResponse = await container.CreateItemAsync<Client>(client, new PartitionKey(client.userType));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse.
                Console.WriteLine("Created item in database with id: {0}\n", client.id);
            }
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private static async Task CreateContainerAsync(CosmosClient cosmosClient)
        {
            // Create a new container
            Container container = await cosmosClient.GetDatabase(DatabaseId).CreateContainerIfNotExistsAsync(ContainerId, "/userType");
            Console.WriteLine("Created Container: {0}\n", container.Id);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private static async Task CreateDatabaseAsync(CosmosClient cosmosClient)
        {
            // Create a new database
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
            Console.WriteLine("Created Database: {0}\n", database.Id);
        }

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
                new QueryRequestOptions() { PartitionKey = new PartitionKey("Teste") }))
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
            Console.WriteLine(clients[0].NameClient);
            return clients;
        }

        private static async Task ReplaceFamilyItemAsync(CosmosClient cosmosClient)
        {
            Container container = cosmosClient.GetContainer(DatabaseId, ContainerId);

            ItemResponse<Client> clientResponse = await container.ReadItemAsync<Client>("2cd51646-5b2d-4fcf-8d8e-786410a5b48e", new PartitionKey("Teste"));
            Client itemBody = clientResponse;

            itemBody.NameClient = "Victor";

            // replace the item with the updated content
            clientResponse = await container.ReplaceItemAsync<Client>(itemBody, itemBody.id, new PartitionKey(itemBody.userType));
            Console.WriteLine("Updated Family [{0},{1}].\n \tBody is now: {2}\n", itemBody.userType, itemBody.id, clientResponse.Resource);
        }
    }
}
