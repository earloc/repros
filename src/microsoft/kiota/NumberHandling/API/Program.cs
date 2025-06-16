using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/{numbersAsStrings:bool}", (bool numbersAsStrings) =>
{

    
    object result = numbersAsStrings
        ? new WrongSample
        {
            Integer = "42",
            Double = "13.37"
        }
        : new Sample
        {
            Integer = 42,
            Double = 13.37
        };

    return Results.Ok(result);
})
.Produces<Sample>();

app.MapOpenApi();

app.Run();

public class Sample
{
    [JsonPropertyName("integer")]
    public int Integer { get; set; }

    [JsonPropertyName("double")]
    public double Double { get; set; }
}

public class WrongSample
{
    [JsonPropertyName("integer")]
    public string Integer { get; set; }

    [JsonPropertyName("double")]
    public string Double { get; set; }
}
