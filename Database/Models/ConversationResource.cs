
using System;

namespace achappey.ChatGPTeams.Database.Models
{
    public class ConversationResource
    { 
        public Resource Resource { get; set; }
        public int ResourceId { get; set; }

        public Conversation Conversation { get; set; }
        public string ConversationId { get; set; }

    }
}
