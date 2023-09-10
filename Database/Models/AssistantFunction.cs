
using System;

namespace achappey.ChatGPTeams.Database.Models
{
    public class AssistantFunction
    { 
        public Function Function { get; set; }
        public string FunctionId { get; set; }

        public Assistant Assistant { get; set; }
        public int AssistantId { get; set; }

    }
}
