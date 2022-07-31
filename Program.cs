using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TicketSwapPoller;

var workerId = 0;
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration( (context, builder) =>
    {
        builder.AddUserSecrets<Program>();
    })
    .ConfigureLogging((context, logging) =>
    {
        // Specifying the configuration for serilog
        Log.Logger = new LoggerConfiguration() // initiate the logger configuration
            .ReadFrom.Configuration(context.Configuration) // connect serilog to our configuration folder
            .Enrich.FromLogContext() //Adds more information to our logs from built in Serilog 
            .CreateLogger(); //initialise the logger
    })
    .ConfigureServices((context, services) =>
    {
        var eventId = context.Configuration.GetValue<int>("EventId");
        var accessCodes = context.Configuration.GetSection("AccessCodes").Get<IEnumerable<string>>();
        services.AddTransient<Worker>((provider) =>
        {
            var accessCode = (accessCodes ?? Enumerable.Empty<string>()).Any()
                ? accessCodes?.ElementAtOrDefault(workerId)
                : "";
                
            return new Worker(provider.GetRequiredService<ILogger<Worker>>(), workerId++, eventId, accessCode);
        });
    })
    .UseSerilog()
    .Build();

await Task.WhenAll(
    host.Services.GetRequiredService<Worker>().StartAsync()
    //host.Services.GetRequiredService<Worker>().StartAsync()
);