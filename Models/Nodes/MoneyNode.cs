namespace TicketSwapPoller.Models.Nodes
{
    public class MoneyNode : TicketSwapNode
    {
		public int Amount { get; set; }
		public string? Currency { get; set; }

		public static string GetFragments()
		{
			return @"fragment money on Money 
				{
					amount
					currency
					__typename
				}";
		}

		protected override string GetTypeName() => "Money";
    }
}
