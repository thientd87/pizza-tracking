using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PizzaTracker.Models;

namespace PizzaTracker;

public class PizzaEventTrigger
{
    private readonly ILogger _logger;
    private static CosmosClient cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
    private static Container container = cosmosClient.GetContainer("pizza-es-db", "PizzaOrders");

    public PizzaEventTrigger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PizzaEventTrigger>();
    }

    [Function("PizzaEventTrigger")]
    public async Task Run([CosmosDBTrigger(
            databaseName: "pizza-es-db",
            containerName: "Events",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "pizza-leases", CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Event> input , FunctionContext context)
    {
        
        if (input != null && input.Count > 0)
        {
            foreach (var eventInfo in input)
            {
                try
                {
                   
                    var query = new QueryDefinition("SELECT * FROM PizzaOrders c WHERE c.PizzaId = @pizzaId")
                        .WithParameter("@pizzaId", eventInfo.PizzaId);

                    // Attempt to retrieve an existing PizzaOrderEntity based on PizzaId
                    List<PizzaOrderEntity> results = new List<PizzaOrderEntity>();
                    using (FeedIterator<PizzaOrderEntity> feedIterator = container.GetItemQueryIterator<PizzaOrderEntity>(
                               query,
                               requestOptions: new QueryRequestOptions()))
                    {
                        while (feedIterator.HasMoreResults)
                        {
                            FeedResponse<PizzaOrderEntity> response = await feedIterator.ReadNextAsync();
                            results.AddRange(response.Resource);
                        }
                    }
                    var pizzaOrder = results.FirstOrDefault();
                    if (pizzaOrder != null)
                    { 
                        // Update the existing order
                        pizzaOrder.Status = eventInfo.EventType;
                        pizzaOrder.Events.Add(eventInfo.EventType);

                        await container.ReplaceItemAsync(pizzaOrder, pizzaOrder.id, new PartitionKey(pizzaOrder.PizzaId));
                        
                    }
                    else
                    {
                        // If not found, create a new order
                        var newPizzaOrder = new PizzaOrderEntity
                        {
                            id = Guid.NewGuid().ToString(),
                            PizzaId = eventInfo.PizzaId,
                            Status = eventInfo.EventType,
                            Events = new List<string> { eventInfo.EventType }
                        };

                        await container.CreateItemAsync(newPizzaOrder, new PartitionKey(newPizzaOrder.PizzaId));
                    }

                   
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}

