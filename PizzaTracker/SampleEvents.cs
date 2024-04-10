using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PizzaTracker;

public class SampleEvents
{
    private readonly ILogger _logger;
    
    private static readonly string[] EventTypes = new[] { "ordered", "making", "baking", "baked", "packaging", "picked", "shipping", "delivered", "taken" };
    
    private readonly CosmosClient _cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));

    public SampleEvents(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SampleEvents>();
    }

    [Function("SampleEvents")]
    public async Task Run([TimerTrigger("0 */120 * * * *")] TimerInfo myTimer, FunctionContext context)
    {
        
        var logger = context.GetLogger("PizzaEventsFunction");
        logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            var container = _cosmosClient.GetContainer("pizza-es-db", "Events");

            // Gửi các sự kiện theo thứ tự và số lượng trong EventTypes
            foreach (var eventType in EventTypes)
            {
                var newEvent = new Event
                {
                    Id = Guid.NewGuid().ToString(),
                    PizzaId = "pizza-111",
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Payload = $"Payload for {eventType}"
                };

                await container.CreateItemAsync(newEvent, new PartitionKey(newEvent.PizzaId));
                logger.LogInformation($"Inserted event {eventType} into CosmosDB at {DateTime.UtcNow.ToString("o")}.");

                // Đợi 10 giây trước khi gửi sự kiện tiếp theo
                await Task.Delay(10000);
            }
        }
    }
}