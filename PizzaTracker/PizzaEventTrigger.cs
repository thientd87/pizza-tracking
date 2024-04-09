using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PizzaTracker;

public class PizzaEventTrigger
{
    private readonly ILogger _logger;

    public PizzaEventTrigger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PizzaEventTrigger>();
    }

    [Function("PizzaEventTrigger")]
    public void Run([CosmosDBTrigger(
            databaseName: "pizza-es-db",
            containerName: "Events",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "pizza-leases", CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Event> input, FunctionContext context)
    {
        if (input != null && input.Count > 0)
        {
            _logger.LogInformation("Documents modified: " + input.Count);
            _logger.LogInformation("First document Id: " + input[0].Id);
        }

        
    }
}

public class Event
{
    public string Id { get; set; }

    public string pizzaId { get; set; }

    public string eventType { get; set; }

}