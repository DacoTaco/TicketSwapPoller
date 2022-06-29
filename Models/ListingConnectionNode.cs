namespace TicketSwapPoller.Models
{
    public class ListingConnectionNode : TicketSwapNode
    {
        protected override string GetTypeName() => "ListingConnection";

        public PageInfoNode? PageInfo { get; set; }
        public IEnumerable<ListingEdgeNode> Edges { get; set; } = Enumerable.Empty<ListingEdgeNode>();
    }
}
