
#nullable enable

#nullable enable

using System.Collections.Generic;

namespace achappey.ChatGPTeams.Database.Models
{
    public class Resource
    {
        public string Url { get; set; } = null!;
        
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public IEnumerable<Conversation> Conversations { get; set; } = null!;

        public IEnumerable<Assistant> Assistants { get; set; } = null!;

    }
}
