using Discount.Grpc.Data;
using Discount.Grpc.Services;
using Microsoft.EntityFrameworkCore;

// Creates the host: loads configuration (appsettings.json, environment variables from
// docker-compose such as ConnectionStrings__Database), logging and the DI container.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Registers the gRPC server infrastructure (protobuf serialization, HTTP/2 handling).
builder.Services.AddGrpc();

// Registers the EF Core DbContext with SQLite. Discount data is small and self-contained,
// so a file-based database avoids running a separate database container.
builder.Services.AddDbContext<DiscountContext>(opts =>
        opts.UseSqlite(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

// Configure the HTTP request pipeline.

// Applies pending EF Core migrations at startup, which creates the SQLite database, the
// Coupons table and the seed coupons if they don't exist yet (see Data/Extentions.cs).
app.UseMigration();

// Exposes DiscountService's RPC methods (GetDiscount, CreateDiscount, ...) defined in discount.proto.
app.MapGrpcService<DiscountService>();

// gRPC can't be called from a browser, so this plain GET on "/" just explains that.
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// Starts the web server and blocks until the application shuts down.
app.Run();
