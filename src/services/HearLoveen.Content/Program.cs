
var app = WebApplication.CreateBuilder(args).Build();
app.MapGet("/content/{id}", (string id) => Results.Ok(new { id, version = 1, approved = true }));
app.Run();
