

using Basket.API.Data;
using BuildingBlocks.Exceptions.Handler;
using Discount.Grpc;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// Creates the host: loads configuration (appsettings.json, environment variables from
// docker-compose such as ConnectionStrings__Redis), logging and the DI container.
var builder = WebApplication.CreateBuilder(args);

// add services to the container
//Application services

// Handlers, validators and endpoint modules live in this project, so this assembly is scanned
// to register them automatically.
var assembly = typeof(Program).Assembly;

// Carter discovers the ICarterModule classes that define the minimal API endpoints.
builder.Services.AddCarter();

// MediatR implements CQRS: endpoints send commands/queries and MediatR routes them to handlers.
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    // Runs FluentValidation before each handler and rejects invalid requests.
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    // Logs each request/response and its execution time.
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Data services

// Marten stores shopping carts as JSON documents in PostgreSQL.
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);
    // A user has exactly one basket, so UserName is used as the document id instead of a Guid.
    opts.Schema.For<ShoppingCart>().Identity(x => x.UserName);
}).UseLightweightSessions(); // No change tracking, which makes simple reads and writes faster.

// Repository that reads and writes baskets through Marten (the database).
builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// Scrutor's Decorate wraps BasketRepository with CachedBasketRepository (decorator pattern).
// Anything asking for IBasketRepository gets the cached version, which checks Redis first
// and falls back to the database, without changing the handlers.
builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();

// Registers IDistributedCache backed by Redis; this is the cache CachedBasketRepository uses.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    //options.InstanceName = "Basket";
});

//gRPC services

// Registers a typed gRPC client for the Discount service, generated from discount.proto.
// Basket calls it to get each item's discount and subtract it from the price.
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(options =>
{
    // Discount service address, e.g. https://discount.grpc:8081 inside docker-compose.
    options.Address = new Uri(builder.Configuration["GrpcSettings:DiscountUrl"]!);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    // The Discount service uses the ASP.NET development certificate, which is not trusted
    // for the "discount.grpc" host name inside Docker. Accepting any certificate avoids
    // SSL errors in development. Do NOT use this in production.
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    return handler;
}); ;


//cross cutting services

// Converts unhandled exceptions into consistent ProblemDetails responses.
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

// Health checks for both dependencies: the service reports unhealthy if PostgreSQL or Redis is down.
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);


var app = builder.Build();

// configure the HTTP request pipeline

// Maps all endpoints defined in the Carter modules.
app.MapCarter();

// Enables the exception handler middleware so CustomExceptionHandler is used
// (the empty options lambda is required when no error path is configured).
app.UseExceptionHandler(options => { });

// Exposes GET /health with a detailed JSON status per dependency.
app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

// Starts the web server and blocks until the application shuts down.
app.Run();
