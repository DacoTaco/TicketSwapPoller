using TicketSwapPoller.Models.Nodes;

namespace TicketSwapPoller.Models.Queries;
public record TicketsByTypeQuery(string? EventId, ListingStatus ListingStatus) : TicketSwapQueryBase<EventTypeNode>
{
    public EventTypeNode? Node { get; init; }

    protected override string QueryName => "getReservedListings";

    protected override string GetQueryString()
    {
        var query = $"query {QueryName} " + @" ($id: ID!, $first: Int, $after: String) 
							{
								node(id: $id) 
								{
									... on EventType 
									{
										" + nameof(EventTypeNode.Id).ToLower() + @"
										" + nameof(EventTypeNode.Slug).ToLower() + @"
										" + nameof(EventTypeNode.Title).ToLower() + @"
										reservedListings: listings
										(
											first: $first
											filter: {listingStatus: " + ListingStatus.ToQueryString() + @"}
											after: $after
										) {
											...listingConnections
											__typename
										}
										__typename
									}
								__typename
								}
							}";

		return $"{query}{Environment.NewLine}{ListingConnectionNode.GetFragments()}\n";
	}

    protected override object GetVariables() => new
    {
        Id = EventId,
        First = 10
    };
}