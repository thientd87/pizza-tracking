using System.Text.Json.Serialization;

namespace PizzaTracker.Models;

public class PizzaOrderEntity
{
   
    [JsonPropertyName("id")]
    public string id { get; set; }
    
    public string PizzaId { get; set; }

    public string Status { get; set; }

    public List<string> Events { get; set; } = new List<string>();
}