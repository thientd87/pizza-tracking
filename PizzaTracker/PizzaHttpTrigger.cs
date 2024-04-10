using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PizzaTracker.Models;

namespace PizzaTracker;

public class PizzaHttpTrigger
{
    private readonly ILogger _logger;

    private static CosmosClient cosmosClient =
        new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));

    private static Container container = cosmosClient.GetContainer("pizza-es-db", "PizzaOrders");

    public PizzaHttpTrigger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PizzaHttpTrigger>();
    }

    [Function("GetPizzaOrders")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "pizzaorders")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");


        try
        {
            var query = container.GetItemLinqQueryable<PizzaOrderEntity>(requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = -1
                })
                .OrderByDescending(p => p.TimeStamp)
                .AsQueryable();

            List<PizzaOrderEntity> orders = new List<PizzaOrderEntity>();
            using (FeedIterator<PizzaOrderEntity> feedIterator = query.ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<PizzaOrderEntity> result = await feedIterator.ReadNextAsync();
                    orders.AddRange(result);
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(orders.Select(x => new
            {
                x.PizzaId,
                x.Status,
                UpdatedDate = ConvertTimestampToLocalTime(x.TimeStamp)
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while processing your request.");
            return errorResponse;
        }

    }

    
    
    [Function("GetPizzaOrderById")]
    public static async Task<HttpResponseData> Run2(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "pizzaorders/{pizzaId}")]
        HttpRequestData req,
        string pizzaId,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("GetPizzaOrderById");
        logger.LogInformation($"C# HTTP trigger function processed a request to get pizza order by id: {pizzaId}.");

        try
        {
            // LINQ query to select a pizza order by PizzaId
            var iterator = container.GetItemLinqQueryable<PizzaOrderEntity>()
                .Where(p => p.PizzaId == pizzaId)
                .ToFeedIterator();

            List<PizzaOrderEntity> matchingOrders = new List<PizzaOrderEntity>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                matchingOrders.AddRange(response);
            }

            if (matchingOrders.Any())
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(matchingOrders.FirstOrDefault()); // Assuming PizzaId is unique
                return response;
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while processing your request.");
            return errorResponse;
        }
    }

    private static DateTime ConvertTimestampToLocalTime(string timestamp)
    {
        if (DateTime.TryParse(timestamp, out DateTime dateTime))
        {
            return dateTime.ToLocalTime();
        }
        else
        {
            // Return Unix epoch start time if parsing fails
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
        }
    }
}