
using System;

namespace achappey.ChatGPTeams.Database.Models
{
    public class AssistantResource
    { 
        public Resource Resource { get; set; }
        public int ResourceId { get; set; }

        public Assistant Assistant { get; set; }
        public int AssistantId { get; set; }

    }
}
