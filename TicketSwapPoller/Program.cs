using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StrawberryShake;
using TicketSwapPoller;
using TicketSwap.Api;

var workerId = 0;
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, builder) =>
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
        //Setup TicketSwap client
        services.AddTransient<AuthenticationHandler>();
        services
        .AddTicketSwapClient(ExecutionStrategy.NetworkOnly)
        .ConfigureHttpClient((client) =>
        {
            client.BaseAddress = new Uri("https://api.ticketswap.com/graphql/public");
        }, (clientBuilder) =>
        {
            clientBuilder.AddHttpMessageHandler<AuthenticationHandler>();
        });

        //Setup Worker
        var eventId = context.Configuration.GetValue<int>("EventId");
        var accessTokens = context.Configuration.GetSection("AccessTokens").Get<IEnumerable<string>>();
        services.AddTransient<Worker>((provider) =>
        {
            var accessToken = (accessTokens ?? Enumerable.Empty<string>()).Any()
                ? accessTokens?.ElementAtOrDefault(workerId)
                : "";

            return new Worker(provider.GetRequiredService<ILogger<Worker>>(), workerId++, provider.GetRequiredService<ITicketSwapClient>(), eventId, accessToken);
        });
    })
    .UseSerilog()
    .Build();

await Task.WhenAll(
    host.Services.GetRequiredService<Worker>().StartAsync()
    //host.Services.GetRequiredService<Worker>().StartAsync()
);