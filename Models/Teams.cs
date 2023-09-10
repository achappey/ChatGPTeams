
#nullable enable

namespace achappey.ChatGPTeams.Models
{
    public class Teams
    {
        public string Id { get; set; } = null!;

        public string Title { get; set; } = null!;

        public Assistant? Assistant { get; set; }
    }

    public class Channel
    {
        public string Id { get; set; } = null!;

        public string Title { get; set; } = null!;

        public Assistant? Assistant { get; set; }

        public Teams Team { get; set; } = null!;
    }

}