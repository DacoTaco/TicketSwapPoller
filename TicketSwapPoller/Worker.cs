using Bogus;
using Microsoft.Extensions.Logging;
using StrawberryShake;
using System.Diagnostics;
using System.Text;
using TicketSwap.Api;

namespace TicketSwapPoller;
public class Worker
{
    //https://api.ticketswap.com/graphql/public
    private readonly ITicketSwapClient _client;
    private readonly ILogger<Worker> _logger;
    private readonly int _eventId;
    private readonly string? _accessToken;
    private readonly int _workerId;

    public Worker(ILogger<Worker> logger, int workerId, ITicketSwapClient client, int eventId, string? accessToken)
    {
        _logger = logger;
        _eventId = eventId;
        _workerId = workerId;
        _client = client;
        _accessToken = accessToken;

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
                var tickets = new List<IListingList>();
                try
                {
                    await AuthenticationHandler.GetLockAsync(cancellationToken);
                    AuthenticationHandler.SetAuthenticationInfo(_accessToken);

                    //get available tickets
                    var response = await _client.GetTicketListings.ExecuteAsync(eventId, ListingStatus.Available, 10, null, cancellationToken);
                    response.EnsureNoErrors();

                    var listings = (response.Data?.Node?.Listings?.Edges ?? new List<IGetTicketListings_Node_Listings_Edges>())
                        .Where(edge => edge?.Node != null)
                        .Select(edge => edge?.Node)
                        .Cast<IListingList>();

                    tickets.AddRange(listings);

                    //get reserved tickets
                    response = await _client.GetTicketListings.ExecuteAsync(eventId, ListingStatus.Reserved, 10, null, cancellationToken);
                    response.EnsureNoErrors();

                    listings = (response.Data?.Node?.Listings?.Edges ?? new List<IGetTicketListings_Node_Listings_Edges>())
                        .Where(edge => edge?.Node != null)
                        .Select(edge => edge?.Node)
                        .Cast<IListingList>();

                    tickets.AddRange(listings);
                }
                finally
                {
                    AuthenticationHandler.ReleaseLock();
                }

                foreach(var ticket in tickets)
                {
                    if (ticket == null)
                        continue; 

                    if (ticket.Status == ListingStatus.Reserved)
                    {
                        LogInformation("found {NumberOfTicketsInListing} Reserved Ticket(s)", ticket.NumberOfTicketsInListing);
                        continue;
                    }

                    LogInformation("TICKET FOUND!!!!!!!!!!!!!!!!!!!!! -> '{ticketPath}'", string.Empty);// ticket.Uri?.Path);
                    if (string.IsNullOrWhiteSpace(ticket.Id) || string.IsNullOrWhiteSpace(ticket.Hash) || openedLinks.ContainsKey(ticket.Id))
                    {
                        LogInformation("empty id or already opened recently.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(_accessToken))
                    {
                        //open ticket url
                        /*if (!string.IsNullOrWhiteSpace(ticket.Uri?.Path))
                            Process.Start(new ProcessStartInfo($"https://www.ticketswap.be{ticket.Uri.Path}") { UseShellExecute = true });*/

                        continue;
                    }

                    //put ticket in cart and open cart
                    await AuthenticationHandler.GetLockAsync(cancellationToken);

                    try
                    {
                        AuthenticationHandler.SetAuthenticationInfo(_accessToken);

                        var input = new AddTicketsToCartInput
                        {
                            AmountOfTickets = 1,
                            ListingHash = ticket.Hash,
                            ListingId = ticket.Id
                        };
                        var response = await _client.AddTicketsToCart.ExecuteAsync(input, cancellationToken);
                        response.EnsureNoErrors();

                        //add ticket to used tickets
                        openedLinks.Add(ticket.Id, DateTimeOffset.Now);

                        //open ticketswap cart
                        Process.Start(new ProcessStartInfo($"https://www.ticketswap.be/cart") { UseShellExecute = true });
                    }
                    catch
                    {
                        /*try
                        {
                            //open ticket url
                            if (!string.IsNullOrWhiteSpace(ticket.Uri?.Path))
                                Process.Start(new ProcessStartInfo($"https://www.ticketswap.be{ticket.Uri.Path}") { UseShellExecute = true });
                        }
                        catch { }*/
                        throw;
                    }
                    finally
                    {
                        AuthenticationHandler.ReleaseLock();
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