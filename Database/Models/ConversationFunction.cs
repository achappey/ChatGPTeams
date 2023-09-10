
using System;

namespace achappey.ChatGPTeams.Database.Models
{
    public class ConversationFunction
    { 
        public Function Function { get; set; }
        public string FunctionId { get; set; }

        public Conversation Conversation { get; set; }
        public string ConversationId { get; set; }

    }
}
