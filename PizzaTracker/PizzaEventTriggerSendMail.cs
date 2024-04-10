using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PizzaTracker;

public class PizzaEventTriggerSendMail
{
    private readonly ILogger _logger;
    private static readonly HttpClient httpClient = new HttpClient();
    public PizzaEventTriggerSendMail(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PizzaEventTriggerSendMail>();
    }

    [Function("PizzaEventTriggerSendMail")]
    public async Task Run([CosmosDBTrigger(
            databaseName: "pizza-es-db",
            containerName: "Events",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "pizza-leases-notification", CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<Event>? input, FunctionContext context)
    {
        if (input is { Count: > 0 })
        {
            foreach (var item in input)
            {
                string status = item.EventType;
                if (status == "delivered" || status == "eaten")
                {
                    // Prepare the request payload
                    var payload = new
                    {
                        data = $"Email notification sent for pizza {item.PizzaId} with status {item.EventType}."
                        // Any other details you want to send
                    };

                    // Serialize the payload to JSON
                    string jsonPayload = JsonSerializer.Serialize(payload);

                    // Replace 'YourLogicAppURL' with the URL you got from Azure Logic App step
                    var requestUri = "https://prod-11.southeastasia.logic.azure.com:443/workflows/facbac10c0564146845cab7054d6d91f/triggers/When_a_HTTP_request_is_received/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2FWhen_a_HTTP_request_is_received%2Frun&sv=1.0&sig=MEABjIvNUdUPK9vCqfx-9i5p2RNFQ5_PCH8zjWFy85s";

                    // Send a POST request to the Logic App
                    var response = await httpClient.PostAsync(requestUri,
                        new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation(
                            $"Email notification sent for pizza {item.PizzaId} with status {item.EventType}.");
                    }
                    else
                    {
                        _logger.LogError(
                            $"Failed to send email notification for pizza {item.PizzaId}. HTTP status code: {response.StatusCode}");
                    }
                }
            }
        }

        
    }
}

public class MyDocument
{
    public string Id { get; set; }

    public string Text { get; set; }

    public int Number { get; set; }

    public bool Boolean { get; set; }
}