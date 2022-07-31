namespace TicketSwapPoller.Models.Nodes
{
    public class UriNode : TicketSwapNode
    {
        protected override string GetTypeName() => "Uri";
        public string? Path { get; set; }
    }
}
