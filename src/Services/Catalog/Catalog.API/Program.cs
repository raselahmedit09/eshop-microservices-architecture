using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// Creates the host: loads configuration (appsettings.json, environment variables such as
// the ConnectionStrings__Database set in docker-compose), logging and the DI container.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Handlers, validators and endpoint modules all live in this project, so this assembly is
// scanned to register them automatically instead of registering each class by hand.
var assembly = typeof(Program).Assembly;

// MediatR implements CQRS: each endpoint sends a command/query and MediatR routes it to its handler.
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    // Pipeline behaviors wrap every handler, like middleware for MediatR requests:
    // validate the request first (throws ValidationException on bad input)...
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    // ...then log the request, response and how long the handler took.
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Registers every FluentValidation validator so ValidationBehavior can find them.
builder.Services.AddValidatorsFromAssembly(assembly);

// Carter discovers the ICarterModule classes that define the minimal API endpoints.
builder.Services.AddCarter();

// Marten uses PostgreSQL as a document database: products are stored as JSON documents,
// so no tables or migrations need to be written.
builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions(); // Lightweight sessions skip identity-map tracking, which makes them faster for simple CRUD.

// Seeds sample products at startup, only in Development so real data is never overwritten.
if (builder.Environment.IsDevelopment())
    builder.Services.InitializeMartenWith<CatalogInitialData>();

// Converts unhandled exceptions (not found, validation, etc.) into consistent ProblemDetails responses.
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

// Health check that verifies the PostgreSQL database is reachable; used by Docker/monitoring.
builder.Services.AddHealthChecks().AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

var app = builder.Build();

// Configure the HTTP request pipeline.

// Maps all endpoints defined in the Carter modules.
app.MapCarter();

// Enables the exception handler middleware so CustomExceptionHandler is used. The empty
// options lambda is required because no fallback error path is configured.
app.UseExceptionHandler(options => { });

// Exposes GET /health; UIResponseWriter returns a detailed JSON status per dependency
// instead of the default plain "Healthy"/"Unhealthy" text.
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Starts the web server and blocks until the application shuts down.
app.Run();


