namespace TicketSwapPoller.Models.Nodes
{
    public class PageInfoNode : TicketSwapNode
    {
        protected override string GetTypeName() => "PageInfo";

        public string? EndCursor { get; set; }
        public bool HasNextPage { get; set; }
    }
}
