using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSwapPoller.Models
{
    public class ListingEdgeNode : TicketSwapNode
    {
        protected override string GetTypeName() => "ListingEdge";
        public PublicListingNode? Node { get; set; }
    }
}
