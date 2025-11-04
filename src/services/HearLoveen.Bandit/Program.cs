
using System.Text.Json;
var app = WebApplication.CreateBuilder(args).Build();
app.MapGet("/decide", (double meanA, double meanB) => {
    // Thompson Sampling (placeholder using normal means)
    var rnd = new Random();
    var sampleA = meanA + rnd.NextDouble();
    var sampleB = meanB + rnd.NextDouble();
    var pick = sampleA >= sampleB ? "A" : "B";
    return Results.Text(JsonSerializer.Serialize(new { variant = pick }));
});
app.Run();
