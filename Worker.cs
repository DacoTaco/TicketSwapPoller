using Bogus;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using TicketSwapPoller.Models;
using TicketSwapPoller.Models.Nodes;
using TicketSwapPoller.Models.Queries;

namespace TicketSwapPoller;
public class Worker
{
    //https://api.ticketswap.com/graphql/public
    private static readonly SemaphoreSlim _semaphore = new(1);
    private static GraphQLHttpClient _graphQlClient = null!;
    private readonly ILogger<Worker> _logger;
    private readonly int _eventId;
    private readonly string? _accessCode;
    private readonly int _workerId;

    public Worker(ILogger<Worker> logger, int workerId, int eventId, string? accessCode)
    {
        _logger = logger;
        _eventId = eventId;
        _accessCode = accessCode;
        _workerId = workerId;
        try
        {
            _semaphore.Wait();
            if (_graphQlClient == null)
                _graphQlClient = new GraphQLHttpClient("https://api.ticketswap.com/graphql/public", new SystemTextJsonSerializer(ApplicationConstants.JsonOptions));
        }
        finally
        {
            _semaphore.Release();
        }    

        _logger.LogInformation("Worker setup.");
    }

    private void LogInformation(string msg, params object?[] args)
    {
        IEnumerable<object?> msgArgs = new object?[]{ _workerId };
        foreach (var arg in args)
            msgArgs = msgArgs.Append(arg);

        var appendedMsg = $"worker {{workerId}}: {msg}";
        _logger.LogInformation(appendedMsg, msgArgs.ToArray());
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var totalWaitTime = new TimeSpan(0);
        var faker = new Faker();
        var random = new Random();
        var openedLinks = new Dictionary<string, DateTimeOffset>();
        LogInformation("Running worker...");
        while (true)
        {
			try
			{
                var eventId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"EventType:{_eventId}"));
				var getAvailableListings = new TicketsByTypeQuery(eventId, ListingStatus.Available);
                var getReservedListings = new TicketsByTypeQuery(eventId, ListingStatus.Reserved);
                var responses = new List<EventTypeNode>();
                try
                {
                    await _semaphore.WaitAsync(cancellationToken);

                    _graphQlClient.HttpClient.DefaultRequestHeaders.Authorization = null;
                    _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                    _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(faker.Lorem.Word(), faker.Random.Number(0, 255).ToString()));

                    var response = await _graphQlClient.SendQueryAsync<TicketsByTypeQuery>(getAvailableListings.ToGraphQuery(), cancellationToken);
                    if ((response.Errors == null || !response.Errors.Any()) && response?.Data?.Node != null)
                        responses.Add(response.Data.Node);

                    response = await _graphQlClient.SendQueryAsync<TicketsByTypeQuery>(getReservedListings.ToGraphQuery(), cancellationToken);
                    if ((response.Errors == null || !response.Errors.Any()) && response?.Data?.Node != null)
                        responses.Add(response.Data.Node);
                }
                finally
                {
                    _semaphore.Release();
                }

                foreach (var listing in responses.Where(listing => listing != null).SelectMany(response => response.ReservedListings.Edges.Select(edge => edge.Node)))
                {
                    if (listing?.Status == "RESERVED")
                    {
                        LogInformation("found {NumberOfTicketsInListing} Reserved Ticket(s)", listing.NumberOfTicketsInListing);
                        continue;
                    }

                    LogInformation("TICKET FOUND!!!!!!!!!!!!!!!!!!!!! -> '{ticketPath}'", listing?.Uri?.Path);
                    if (string.IsNullOrWhiteSpace(listing?.Id) || string.IsNullOrWhiteSpace(listing.Hash) || openedLinks.ContainsKey(listing.Id))
                    {
                        LogInformation("empty id or already opened recently.");
                        continue;
                    }

                    await _semaphore.WaitAsync(cancellationToken);

                    try
                    {
                        if (string.IsNullOrWhiteSpace(_accessCode))
                            continue;

                        _graphQlClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessCode);
                        _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                        _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(faker.Lorem.Word(), faker.Random.Number(0, 255).ToString()));

                        var response = await _graphQlClient.SendQueryAsync<AddTicketToCartMutation>(new AddTicketToCartMutation(listing.Id, listing.Hash).ToGraphQuery(), cancellationToken);
                        if (response.Errors != null && response.Errors.Any())
                            throw new Exception("Error adding ticket to cart");

                        openedLinks.Add(listing.Id, DateTimeOffset.Now);
                        //open ticketswap cart
                        Process.Start(new ProcessStartInfo($"https://www.ticketswap.be/cart") { UseShellExecute = true });
                    }
                    finally
                    {
                        if (!string.IsNullOrWhiteSpace(listing?.Uri?.Path))
                            Process.Start(new ProcessStartInfo($"https://www.ticketswap.be{listing.Uri.Path}") { UseShellExecute = true });

                        _semaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                LogInformation("exception thrown : {exceptionMessage}", ex.Message);
            }

            var waitTime = new TimeSpan(0, 0, 1);
            if (totalWaitTime.Minutes >= 4 && totalWaitTime.Seconds > 30)
            {
                totalWaitTime = new TimeSpan(0);
                waitTime = new TimeSpan(0, 0, random.Next(30, 41));
            }
            else
            {
                waitTime = new TimeSpan(0, 0, 0, 0, random.Next(150, 200));
                totalWaitTime += waitTime;
            }

            foreach(var link in openedLinks)
            {
                if (link.Value.AddMinutes(30) < DateTimeOffset.Now)
                    openedLinks.Remove(link.Key);
            }

            LogInformation("looped through tickets, waiting ... (dice roll) ... {waitTimeSeconds}:{waitTimeMilliseconds} seconds (total is {totalWaitTime})", waitTime.Seconds, waitTime.Milliseconds, totalWaitTime);
            Thread.Sleep(waitTime);
        }
    }
}