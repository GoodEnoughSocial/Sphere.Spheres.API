using Serilog;
using Sphere.Shared;

Log.Logger = SphericalLogger.SetupLogger();

Log.Information("Starting up");

var registration = Services.Spheres.GetServiceRegistration();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(SphericalLogger.ConfigureLogger);
    builder.Services.AddHealthChecks();

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    try
    {
        var result = await Services.RegisterService(registration);
    }
    catch (Exception e) when (!app.Environment.IsDevelopment())
    {
        Log.Fatal(e, "Must be able to register service in non Development environments. Make sure Consul is running.");
    }

    app.MapHealthChecks("/health");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    if (ex.GetType().Name != "StopTheHostException")
    {
        Log.Fatal(ex, "Unhandled exception");
    }
}
finally
{
    await Services.UnregisterService(registration);

    Log.Information("Shutting down");
    Log.CloseAndFlush();
}
