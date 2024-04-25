using Serilog; // Ensure this namespace is included
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration) // Read configuration from appsettings.json
    .WriteTo.Console()
    .WriteTo.File("logs/temp.txt", rollingInterval: RollingInterval.Day));

// Logging for Windows Event Log
builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "My ASP.NET Core Application";
});

// Add services to the container.
builder.Services.AddReverseProxy()
       .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

// Retrieve the logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        // Log all request headers
        // foreach (var header in context.Request.Headers)
        // {
        //     logger.LogInformation("Header: {Key}: {Value}", header.Key, header.Value);
        // }

        // Log user information if available
        if (context.User.Identity?.IsAuthenticated == true)
        {
            logger.LogInformation("Authenticated User: {Name}", context.User.Identity.Name);
            context.Request.Headers["X-Authenticated-User"] = context.User.Identity.Name;
        }
        else
        {
            logger.LogInformation("User is not authenticated.");
        }

        // Continue processing the request
        await next.Invoke();
    });
});

app.Run();
