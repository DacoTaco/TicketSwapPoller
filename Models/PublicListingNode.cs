namespace TicketSwapPoller.Models
{
    public class PublicListingNode : TicketSwapNode
    {
        protected override string GetTypeName() => "PublicListing";
        public string? Id { get; set; }
        public string? Hash { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public UriNode? Uri { get; set; }
        public int NumberOfTicketsInListing { get; set; }
        public int NumberOfTicketsStillForSale { get; set; }
    }
}
