namespace TicketSwapPoller.Models.Nodes
{
    public class ListingConnectionNode : TicketSwapNode
    {
        protected override string GetTypeName() => "ListingConnection";

        public PageInfoNode? PageInfo { get; set; }
        public IEnumerable<ListingEdgeNode> Edges { get; set; } = Enumerable.Empty<ListingEdgeNode>();

        public static string GetFragments()
        { 
            var fragment = @"fragment listingConnections on ListingConnection 
				{
					edges 
					{
						node 
						{
							...publicListings
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
				}";

			return $"{fragment}{Environment.NewLine}{ListingEdgeNode.GetFragments()}";
		}
    }
}
