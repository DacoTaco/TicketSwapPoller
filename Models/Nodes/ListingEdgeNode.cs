namespace TicketSwapPoller.Models.Nodes
{
    public class ListingEdgeNode : TicketSwapNode
    {
        protected override string GetTypeName() => "ListingEdge";
        public PublicListingNode? Node { get; set; }

        public static string GetFragments() => PublicListingNode.GetFragments();
    }
}
