namespace TicketSwapPoller.Models.Nodes
{
    public class CartErrorNode : TicketSwapNode
    {
        protected override string GetTypeName() => "CartError";

        public static string GetFragments()
        {
            return @"fragment cartError on Error {
              code
              message
              __typename
            }";
        }
    }
}
