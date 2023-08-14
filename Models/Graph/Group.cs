using System.Collections.Generic;

namespace achappey.ChatGPTeams.Models.Graph;

public class Group
{
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Id { get; set; }
    public string Mail { get; set; }
    public Team Team { get; set; }
}