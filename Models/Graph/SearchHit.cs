using System.Web;

namespace achappey.ChatGPTeams.Models.Graph;

public class SearchHit
{
    public string Summary { get; set; }
    public Resource Resource { get; set; }
}

public class Resource
{
    public string _webUrl { get; set; }

    public string WebUrl
    {
        get => HttpUtility.UrlEncode(_webUrl);
        set
        {
            _webUrl = value;
        }
    }
}