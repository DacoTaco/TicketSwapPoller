using GraphQL;

namespace TicketSwapPoller.Models
{
    public abstract record TicketSwapQueryBase<TRequest> where TRequest : TicketSwapNode
    {
        protected abstract string QueryName { get; }
        protected abstract object GetVariables();
        protected abstract string GetQueryString();

        public GraphQLRequest ToGraphQuery()
        {
            var request = new GraphQLRequest
            {
                OperationName = QueryName,
                Variables = GetVariables(),
                Query = GetQueryString(),
            };

            return request;
        }
    }
}
