using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StrawberryShake;
using System.Net.Http.Headers;
using System.Text;
using TicketSwap.Api;
using TicketSwapPoller;

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
            client.BaseAddress = new Uri("https://www.ticketswap.com/api/graphql/public");

            //add required headers for ticketswap to be happy, with random data ofcourse
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            client.DefaultRequestHeaders.Add("device-id", Guid.NewGuid().ToString());
            var rbzid = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(" ", new Faker().Lorem.Words(3))));
            var sessionId = Guid.NewGuid().ToString().Replace("-", "");
            client.DefaultRequestHeaders.Add("Cookie", $"optimizely_id={Guid.NewGuid()}; favorites_banner_seen=true; rbzid={rbzid}; rbzsessionid={sessionId}; intercom-id-{Guid.NewGuid}; intercom-session-f9d90yaf=; intercom-device-id-f9d90yaf={Guid.NewGuid}");
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

await host.Services.GetRequiredService<Worker>().StartAsync();