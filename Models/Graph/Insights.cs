
namespace achappey.ChatGPTeams.Models.Graph;

public class Insights
{
    public string Id { get; set; }
    public ResourceVisualization ResourceVisualization { get; set; }
    public ResourceReference ResourceReference { get; set; }
}

public class UsedInsight : Insights
{
}

public class Trending : Insights
{
}

public class SharedInsight : Insights
{
}

public class ResourceVisualization
{
    public ResourceType Type { get; set; }
    public string Title { get; set; }
}

public class ResourceReference
{
    public string WebUrl { get; set; }
    public string Id { get; set; }

}

public enum ResourceType
{
    PowerPoint,
    Word,
    Excel,
    Pdf,
    OneNote,
    OneNotePage,
    InfoPath,
    Visio,
    Publisher,
    Project,
    Access,
    Mail,
    Csv,
    Archive,
    Xps,
    Audio,
    Video,
    Image,
    Web,
    Text,
    Xml,
    Story,
    ExternalContent,
    Folder,
    Spsite,
    Other
}

