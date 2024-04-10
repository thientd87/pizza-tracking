// See https://aka.ms/new-console-template for more information

using Microsoft.Azure.Cosmos;


public class Program
{
    private static CosmosClient cosmosClient =
        new CosmosClient("AccountEndpoint=https://pizza-event-store-db.documents.azure.com:443/;AccountKey=VJJj5nsRuj2zslMdUodAd6zS5klbpZJtV4tsbHJzt69bx0GJZugAr0JpKGpFiHcuxbpKtRTFVcVsACDb8I2Iag==;");

    private static Container container = cosmosClient.GetContainer("pizza-es-db", "Events");

    public static async Task Main(string[] args)
    {

        var pizzaId = "pizza-003";
        List<string> eventTypes = new List<string> { "ordered", "making", "baking", "baked", "packaging", "picked", "shipping", "delivered", "taken" };
        
        foreach (var eventType in eventTypes)
        {
            Event pizzaEvent = new Event
            {
                id = Guid.NewGuid().ToString(),
                PizzaId = pizzaId,
                EventType = eventType,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Payload = $"{pizzaId} is {eventType} by {GetRandomPersonName()}"
            };

            await container.CreateItemAsync(pizzaEvent, new PartitionKey(pizzaEvent.PizzaId));
            Console.WriteLine($"Inserted event: {pizzaEvent.Payload}");

            await Task.Delay(10000); // Wait for 10 seconds before sending the next event
        }
    }
    
    public static string GetRandomPersonName()
    {
        string[] personName = { "ThienTrinh", "DungNguyen", "Ludovic", "KimAnh", "ChiPhan" };
        Random rnd = new Random();
        int index = rnd.Next(personName.Length);
        return personName[index];
    }
}



public class Event
{
    public string id { get; set; }
    public string PizzaId { get; set; }
    public string EventType { get; set; }
    public string Timestamp { get; set; }
    public string Payload { get; set; }
}