using Newtonsoft.Json;
using System.Collections.Generic;

namespace achappey.ChatGPTeams.Models;

public class SitePage
{
    [JsonProperty("@odata.context")]
    public string ODataContext { get; set; }

    [JsonProperty("@odata.etag")]
    public string ODataEtag { get; set; }

    public string Description { get; set; }

    public string ETag { get; set; }

    public string Id { get; set; }

    public string LastModifiedDateTime { get; set; }

    public string Name { get; set; }

    public string WebUrl { get; set; }

    public string Title { get; set; }

    public string PageLayout { get; set; }

    public string ThumbnailWebUrl { get; set; }

    public string PromotionKind { get; set; }

    public bool ShowComments { get; set; }

    public bool ShowRecommendedPages { get; set; }


    public ParentReference ParentReference { get; set; }

    public PublishingState PublishingState { get; set; }

    public Dictionary<string, object> Reactions { get; set; }

    public TitleArea TitleArea { get; set; }

    public string CanvasLayoutODataContext { get; set; }

    public CanvasLayout CanvasLayout { get; set; }
}



public class ParentReference
{
    public string SiteId { get; set; }
}

public class PublishingState
{
    public string Level { get; set; }

    public string VersionId { get; set; }
}

public class TitleArea
{
    public bool EnableGradientEffect { get; set; }

    public string ImageWebUrl { get; set; }

    public string Layout { get; set; }

    public bool ShowAuthor { get; set; }

    public bool ShowPublishedDate { get; set; }

    public bool ShowTextBlockAboveTitle { get; set; }

    public string TextAboveTitle { get; set; }

    public string TextAlignment { get; set; }

    public int ImageSourceType { get; set; }

    public string Title { get; set; }

    public string[] AuthorByline { get; set; }
}

public class CanvasLayout
{
    public string HorizontalSectionsODataContext { get; set; }

    public List<Section> HorizontalSections { get; set; }
    public List<Section> VerticalSections { get; set; }
}

public class Section
{
    public string Layout { get; set; }

    public string Id { get; set; }

    public string Emphasis { get; set; }

    public string ColumnsODataContext { get; set; }

    public List<Column> Columns { get; set; }
}

public class Column
{
    public string Id { get; set; }

    public int Width { get; set; }

    public string WebpartsODataContext { get; set; }

    public List<Webpart> Webparts { get; set; }
}

public class Webpart
{
    [JsonProperty("@odata.type")]
    public string ODataType { get; set; }

    public string Id { get; set; }

    public string InnerHtml { get; set; }
}
