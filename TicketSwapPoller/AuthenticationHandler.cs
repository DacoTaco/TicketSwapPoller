using Bogus;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace TicketSwapPoller;

public class AuthenticationHandler : DelegatingHandler
{
    private static string? _accessToken;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async static Task GetLockAsync(CancellationToken cancellationToken = default) => await _semaphore.WaitAsync(cancellationToken);
    public static void ReleaseLock() => _semaphore.Release();

    public static void SetAuthenticationInfo(string? accessToken)
    {
        _accessToken = accessToken;
    }

    private readonly ILogger<AuthenticationHandler> _logger;
    public AuthenticationHandler(ILogger<AuthenticationHandler> logger) : base()
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestBody = request.Content != null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : "";

        _logger.LogDebug("Sending request : {requestBody}", requestBody);
        _logger.LogDebug("Using access token : {accessToken}", _accessToken);

        //set access token
        request.Headers.Authorization = string.IsNullOrWhiteSpace(_accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", _accessToken);

        //generate random user agent
        var faker = new Faker();
        request.Headers.UserAgent.Clear();
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(faker.Lorem.Word(), faker.Random.Number(0, 255).ToString()));

        var response = await base.SendAsync(request, cancellationToken);

        var responseBody = response.Content != null
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : "";
        _logger.LogDebug("Received response : {responseBody}", responseBody);

        return response;
    }
}