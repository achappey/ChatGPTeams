
#nullable enable

using System;
using System.Collections.Generic;

namespace achappey.ChatGPTeams.Database.Models
{
    public class Function
    {
        public string Id { get; set; } = null!;

        public IEnumerable<Conversation>? Conversations { get; set; }

        public IEnumerable<Assistant>? Assistants { get; set; } 

        public IEnumerable<Prompt>? Prompts { get; set; } 
    }
}
