
#nullable enable

namespace achappey.ChatGPTeams.Models
{
    public class Resource
    {
        public required string Url { get; set; }
        public required string Id { get; set; }
        public required string Name { get; set; }
        public Conversation? Conversation { get; set; }
        public Assistant? Assistant { get; set; }

    }
}
