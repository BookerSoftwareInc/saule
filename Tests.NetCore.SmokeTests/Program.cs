using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Saule.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().ConfigureJsonApi();

var app = builder.Build();
app.MapControllers();
app.Run();

// Exposed so Microsoft.AspNetCore.Mvc.Testing's WebApplicationFactory<Program> can host this app
// in-memory for the smoke tests below.
public partial class Program
{
}
