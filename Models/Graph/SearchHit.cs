using System.Collections.Generic;

namespace achappey.ChatGPTeams.Models.Graph;

public class SearchHit
{
    public string Summary { get; set; }
    public Resource Resource { get; set; }
}

public class Resource
{
    public string WebUrl { get; set; }
}