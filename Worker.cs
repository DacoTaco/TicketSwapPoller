using Bogus;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using TicketSwapPoller.Models;

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
                var ticketsRequest = new GraphQLRequest()
                {
                    OperationName = "getReservedListings",
                    Variables = new
                    {
                        Id = eventId,
                        First = 10
                    },
                    //originally the query had 'filter: {listingStatus: RESERVED}', changed to 'filter: {listingStatus: AVAILABLE}'
                    Query = @"query getReservedListings($id: ID!, $first: Int, $after: String) 
							{
								node(id: $id) 
								{
									... on EventType 
									{
										id
										slug
										title
										reservedListings: listings
										(
											first: $first
											filter: {listingStatus: RESERVED}
											after: $after
										) {
											...listings
											__typename
										}
										__typename
									}
								__typename
								}
							}

							fragment listings on ListingConnection 
							{
								edges 
								{
									node 
									{
										...listingList
										__typename
									}
									__typename
								}
								pageInfo 
								{
									endCursor
									hasNextPage
									__typename
								}
								__typename
							}

							fragment listingList on PublicListing 
							{
								id
								hash
								description
								isPublic
								status
								dateRange 
								{
									startDate
									endDate
									__typename
								}
								uri 
								{
									path
									__typename
								}
								event 
								{
									id
									name
									startDate
									endDate
									slug
									status
									location 
									{
										id
										name
										city 
										{
											id
											name
											__typename
										}
										__typename
									}
									__typename
								}
								eventType 
								{
									id
									title
									startDate
									endDate
									__typename
								}
								seller 
								{
									id
									firstname
									avatar
									__typename
								}
								tickets(first: 99) 
								{
									edges 
									{
										node 
										{
											id
											status
											__typename
										}
										__typename
									}
									__typename
								}
								numberOfTicketsInListing
								numberOfTicketsStillForSale
								price 
								{
									originalPrice 
									{
										...money
										__typename
									}
									totalPriceWithTransactionFee 
									{
										...money
										__typename
									}
									sellerPrice 
									{
										...money
										__typename
									}
									__typename
								}
								__typename
							}

							fragment money on Money 
							{
								amount
								currency
								__typename
							}",
                };

                GraphQLResponse<AvailableTicketsResponse> responseql;
                try
                {
                    await _semaphore.WaitAsync(cancellationToken);

                    _graphQlClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessCode);
                    _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                    _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(faker.Lorem.Word(), faker.Random.Number(0, 255).ToString()));

                    responseql = await _graphQlClient.SendQueryAsync<AvailableTicketsResponse>(ticketsRequest, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }

                if (responseql.Errors != null && responseql.Errors.Any())
                    continue;

                var listings = responseql.Data?.Node?.ReservedListings?.Edges?.Select(edge => edge.Node) ?? Enumerable.Empty<PublicListingNode>();
                foreach (var listing in listings.Where(listing => listing != null))
                {
                    if (listing?.Status != "RESERVED")
                    {
                        LogInformation("TICKET FOUND!!!!!!!!!!!!!!!!!!!!! -> '{ticketPath}'", listing?.Uri?.Path);
                        if (!string.IsNullOrWhiteSpace(listing?.Uri?.Path) && !openedLinks.ContainsKey(listing.Uri.Path))
                        {
                            openedLinks.Add(listing.Uri.Path, DateTimeOffset.Now);
                            Process.Start(new ProcessStartInfo($"https://www.ticketswap.be{listing.Uri.Path}") { UseShellExecute = true });
                        }
                        else
                        {
                            LogInformation("empty url or already opened recently.");
                        }
                    }
                    else
                    {
                        LogInformation("found {NumberOfTicketsInListing} Reserved Ticket(s)", listing.NumberOfTicketsInListing);
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
                if (link.Value.AddSeconds(3) < DateTimeOffset.Now)
                    openedLinks.Remove(link.Key);
            }

            LogInformation("looped through tickets, waiting ... (dice roll) ... {waitTimeSeconds}:{waitTimeMilliseconds} seconds (total is {totalWaitTime})", waitTime.Seconds, waitTime.Milliseconds, totalWaitTime);
            Thread.Sleep(waitTime);
        }
    }
}