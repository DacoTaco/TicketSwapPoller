namespace TicketSwapPoller.Models.Nodes
{
    public class EventTypeNode : TicketSwapNode
    {
        protected override string GetTypeName() => "EventType";

        public string? Id { get; set; }
        public string? Slug { get; set; }
        public string? Title { get; set; }
        public ListingConnectionNode? ReservedListings { get; set; }
    }
}
