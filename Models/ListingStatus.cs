namespace TicketSwapPoller.Models
{
    public enum ListingStatus
    {
        Unknown,
        Available,
        Reserved,
        Sold
    }

    public static class ListingStatusExtensions
    {
        public static string ToQueryString(this ListingStatus value) => value.ToString().ToUpper();
    }
}
