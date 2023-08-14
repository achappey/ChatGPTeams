
using System.Collections.Generic;

namespace achappey.ChatGPTeams.Models.Graph;

public class Team
{
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Id { get; set; }
    public string WebUrl { get; set; }

    public IEnumerable<Channel> Channels { get; set; }

}