using TicketSwapPoller.Models.Nodes;

namespace TicketSwapPoller.Models.Queries
{
    public class AddTicketsToCartResponse : TicketSwapNode
    {
        protected override string GetTypeName() => "AddTicketsToCartResponse";

        public IEnumerable<CartErrorNode> Errors { get; set; } = Enumerable.Empty<CartErrorNode>();
    }

    public record AddTicketToCartMutation(string ListingId, string ListingHash) : TicketSwapQueryBase<AddTicketsToCartResponse>
    {
        public AddTicketsToCartResponse? AddTicketsToCart { get; init; }

        protected override string QueryName => "addTicketsToCart";

        protected override string GetQueryString()
        {
            var mutation = @"mutation addTicketsToCart($input: AddTicketsToCartInput!) {
              addTicketsToCart(input: $input) {
                user {
                  id
                  __typename
                }
                errors {
                  ...cartError
                  __typename
                }
                __typename
              }
              __typename
            }";

            return $"{mutation}{Environment.NewLine}{CartErrorNode.GetFragments()}";
        }

        protected override object GetVariables() => new
        {
            Input = new
            {
                ListingId,
                ListingHash,
                AmountOfTickets = 1
            }
        };
    }
}
