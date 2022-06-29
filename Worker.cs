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
    private readonly GraphQLHttpClient _graphQlClient;
    private readonly ILogger<Worker> _logger;
    private readonly int _eventId;

    public Worker(ILogger<Worker> logger, int eventId)
    {
        _logger = logger;
        _eventId = eventId;
        _graphQlClient = new GraphQLHttpClient("https://api.ticketswap.com/graphql/public", new SystemTextJsonSerializer(ApplicationConstants.JsonOptions));

        _logger.LogInformation("Worker setup.");
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var totalWaitTime = 0;
        var faker = new Faker();
        var random = new Random();
        var openedLinks = new List<string>();

        _logger.LogInformation("Running worker...");
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
                    Query = "query getReservedListings($id: ID!, $first: Int, $after: String) {\n  node(id: $id) {\n    ... on EventType {\n      id\n      slug\n      title\n      reservedListings: listings(\n        first: $first\n        filter: {listingStatus: RESERVED}\n        after: $after\n      ) {\n        ...listings\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n}\n\nfragment listings on ListingConnection {\n  edges {\n    node {\n      ...listingList\n      __typename\n    }\n    __typename\n  }\n  pageInfo {\n    endCursor\n    hasNextPage\n    __typename\n  }\n  __typename\n}\n\nfragment listingList on PublicListing {\n  id\n  hash\n  description\n  isPublic\n  status\n  dateRange {\n    startDate\n    endDate\n    __typename\n  }\n  uri {\n    path\n    __typename\n  }\n  event {\n    id\n    name\n    startDate\n    endDate\n    slug\n    status\n    location {\n      id\n      name\n      city {\n        id\n        name\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n  eventType {\n    id\n    title\n    startDate\n    endDate\n    __typename\n  }\n  seller {\n    id\n    firstname\n    avatar\n    __typename\n  }\n  tickets(first: 99) {\n    edges {\n      node {\n        id\n        status\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n  numberOfTicketsInListing\n  numberOfTicketsStillForSale\n  price {\n    originalPrice {\n      ...money\n      __typename\n    }\n    totalPriceWithTransactionFee {\n      ...money\n      __typename\n    }\n    sellerPrice {\n      ...money\n      __typename\n    }\n    __typename\n  }\n  __typename\n}\n\nfragment money on Money {\n  amount\n  currency\n  __typename\n}\n",
                };

                _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                _graphQlClient.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(faker.Lorem.Word(), faker.Random.Number(0, 255).ToString()));

                var responseql = await _graphQlClient.SendQueryAsync<AvailableTicketsResponse>(ticketsRequest, cancellationToken);
                if (responseql.Errors != null && responseql.Errors.Any())
                    continue;

                var listings = responseql.Data?.Node?.ReservedListings?.Edges?.Select(edge => edge.Node) ?? Enumerable.Empty<PublicListingNode>();
                foreach (var listing in listings.Where(listing => listing != null))
                {
                    if (listing?.Status != "RESERVED")
                    {
                        _logger.LogInformation("TICKET FOUND!!!!!!!!!!!!!!!!!!!!! -> '{ticketPath}'", listing?.Uri?.Path);
                        if (!string.IsNullOrWhiteSpace(listing?.Uri?.Path) && openedLinks.Contains(listing.Uri.Path))
                        {
                            openedLinks.Add(listing.Uri.Path);
                            Process.Start(new ProcessStartInfo($"https://www.ticketswap.be{listing.Uri.Path}") { UseShellExecute = true });
                        }
                    }
                    else
                    {
                        _logger.LogInformation("found {NumberOfTicketsInListing} Reserved Ticket(s)", listing.NumberOfTicketsInListing);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("exception thrown : {exceptionMessage}", ex.Message);
            }

            var waitTime = new TimeSpan(0, 0, 1);
            if (totalWaitTime > 500)
            {
                totalWaitTime = 0;
                waitTime = new TimeSpan(0, 0, random.Next(30, 41));
            }
            else
            {
                waitTime = new TimeSpan(0, 0, 0, 0, random.Next(100, 300));
                totalWaitTime += waitTime.Seconds+1 * 2;
            }

            if(totalWaitTime % 30 == 0)
                openedLinks.Clear();

            _logger.LogInformation("looped through tickets, waiting ... (dice roll) ... {waitTimeSeconds}:{waitTimeMilliseconds} seconds (total is {totalWaitTime})", waitTime.Seconds, waitTime.Milliseconds, totalWaitTime);
            Thread.Sleep(waitTime);
        }
    }
}