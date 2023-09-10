using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Extensions
{
  public static class SharePointContextExtensions
  {

    public static async Task<IEnumerable<ListItem>> GetListItemsFromListAsync(this GraphServiceClient _graphService, string siteId, string title, string query, string select = null)
    {
      List<Option> options = new()
      {
        new HeaderOption("Prefer", "HonorNonIndexedQueriesWarningMayFailRandomly")
    };

      return await _graphService.Sites[siteId].Lists[title].Items
    .Request(options)
    .Filter(query)
    .Expand(string.IsNullOrEmpty(select) ? "fields" : $"fields($select={select})")
    .GetAsync();

    }


    public static async Task<IEnumerable<ListItem>> GetAllListItemFromListAsync(this GraphServiceClient _graphService, string siteId, string title, string select = null)
    {
      List<Option> options = new()
      {
        new HeaderOption("Prefer", "HonorNonIndexedQueriesWarningMayFailRandomly")
    };

      return await _graphService.Sites[siteId].Lists[title].Items
    .Request(options)
    .Expand(string.IsNullOrEmpty(select) ? "fields" : $"fields($select={select})")
    .GetAsync();
    }

    public static async Task<ListItem> GetListItemFromListAsync(this GraphServiceClient _graphService, string siteId, string title, string id, string select = null)
    {
      return await _graphService.Sites[siteId].Lists[title].Items[id]
    .Request()
    .Expand(string.IsNullOrEmpty(select) ? "fields" : $"fields($select={select})")
    .GetAsync();
    }


    public static async Task<ListItem> GetFirstListItemFromListAsync(this GraphServiceClient _graphService, string siteId, string title, string query, string select = null)
    {

      var items = await _graphService.GetListItemsFromListAsync(siteId, title, query, select);

      return items.FirstOrDefault();
    }


  }
}


