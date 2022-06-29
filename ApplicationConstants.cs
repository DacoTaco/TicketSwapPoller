using GraphQL.Client.Serializer.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketSwapPoller.Json;

namespace TicketSwapPoller;
public class ApplicationConstants
{
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(new ConstantCaseJsonNamingPolicy(), allowIntegerValues: false),
            new TicketSwapNodeJsonConverter()
        }
    }.SetupImmutableConverter();
}