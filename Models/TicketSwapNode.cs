using System.Text.Json.Serialization;

namespace TicketSwapPoller.Models
{
    public abstract class TicketSwapNode
    {
        [JsonPropertyName("__typename")]
        public string TypeName => GetTypeName();

        protected abstract string GetTypeName();
    }
}
