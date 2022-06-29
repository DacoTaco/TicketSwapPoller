using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TicketSwapPoller;

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
        var eventIdConfig = context.Configuration.GetSection("EventId").Value;
        var eventId = int.Parse(eventIdConfig);
        services.AddTransient<Worker>((provider) => new Worker(provider.GetRequiredService<ILogger<Worker>>(), eventId));
    })
    .UseSerilog()
    .Build();

var my = host.Services.GetRequiredService<Worker>();
await my.StartAsync();