namespace TicketSwapPoller.Models.Nodes
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

		public static string GetFragments()
		{
			var publicListingsFragment = @"fragment publicListings on PublicListing 
				{
					id
					hash
					description
					isPublic
					status
					dateRange 
					{
						startDate
						endDate
						__typename
					}
					uri 
					{
						path
						__typename
					}
					event 
					{
						id
						name
						startDate
						endDate
						slug
						status
						location 
						{
							id
							name
							city 
							{
								id
								name
								__typename
							}
							__typename
						}
						__typename
					}
					eventType 
					{
						id
						title
						startDate
						endDate
						__typename
					}
					seller 
					{
						id
						firstname
						avatar
						__typename
					}
					tickets(first: 99) 
					{
						edges 
						{
							node 
							{
								id
								status
								__typename
							}
							__typename
						}
						__typename
					}
					numberOfTicketsInListing
					numberOfTicketsStillForSale
					price 
					{
						originalPrice 
						{
							...money
							__typename
						}
						totalPriceWithTransactionFee 
						{
							...money
							__typename
						}
						sellerPrice 
						{
							...money
							__typename
						}
						__typename
					}
					__typename
				}";

			return $"{publicListingsFragment}{Environment.NewLine}{MoneyNode.GetFragments()}";
		}
	}
}
