using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IPromptRepository
{
    Task<Prompt> Get(string id);
    Task<IEnumerable<Prompt>> GetPromptsByContent(string content, int userId, int? departmentId);
    Task<IEnumerable<Prompt>> GetPromptsByUser(int userId, int? departmentId);
    Task<string> Create(Prompt prompt);
    Task Update(Prompt prompt);
    Task Delete(string id);
    Task<IEnumerable<Prompt>> GetAll();
}

public class PromptRepository : IPromptRepository
{
    private readonly string _siteId;
    private readonly ILogger<PromptRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.AIVisibility},{FieldNames.Title},{FieldNames.AIPrompt},{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.AIOwner.ToLookupField()},{FieldNames.AIDepartment.ToLookupField()},{FieldNames.AIDepartment},{FieldNames.AIOwner},{FieldNames.AIFunctions}";

    public PromptRepository(ILogger<PromptRepository> logger,
    AppConfig config, IMapper mapper,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<IEnumerable<Prompt>> GetPromptsByUser(int userId, int? departmentId)
    {
        string filter = BuildPromptFilter(userId, departmentId);
        var items = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIPrompts, filter, _selectQuery);

        return items.Select(a => _mapper.Map<Prompt>(a));
    }

    public async Task<IEnumerable<Prompt>> GetPromptsByContent(string content, int userId, int? departmentId)
    {
        string filter = BuildPromptFilter(userId, departmentId);

        var items = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIPrompts, filter, _selectQuery);

        return items.Select(a => _mapper.Map<Prompt>(a)).Where(a => a.Content.ToLowerInvariant().Contains(content.ToLowerInvariant()));
    }

    private string BuildPromptFilter(int userId, int? departmentId)
    {
        string everyoneFilter = $"fields/{FieldNames.AIVisibility} eq 'Everyone'";

        string ownerFilter = $"fields/{FieldNames.AIOwner}LookupId eq {userId} and fields/{FieldNames.AIVisibility} eq 'Owner'";

        string departmentFilter = departmentId.HasValue ? $"fields/{FieldNames.AIDepartment}LookupId eq {departmentId.Value} and fields/{FieldNames.AIVisibility} eq 'Department'" : "";

        string combinedFilter = everyoneFilter;

        if (!string.IsNullOrEmpty(ownerFilter))
        {
            combinedFilter += $" or {ownerFilter}";
        }

        if (!string.IsNullOrEmpty(departmentFilter))
        {
            combinedFilter += $" or {departmentFilter}";
        }

        return combinedFilter;
    }


    public async Task<Prompt> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIPrompts, id);

        return _mapper.Map<Prompt>(item);
    }


    public async Task<string> Create(Prompt prompt)
    {
        var newPrompt = new Dictionary<string, object>()
        {
            {FieldNames.Title, prompt.Title },
            {FieldNames.AIPrompt, prompt.Content},
            {FieldNames.AIVisibility, prompt.Visibility.ToValue()},
            {FieldNames.AIAssistant.ToLookupField(), prompt.Assistant?.Id},
            {FieldNames.AIFunctions.ToLookupField() + "@odata.type", "Collection(Edm.Int32)"},
            {FieldNames.AIFunctions.ToLookupField(), prompt.Functions?.Select(a => a.Id)},
            {FieldNames.AIDepartment.ToLookupField(), prompt.Department?.Id.ToInt()},
            {FieldNames.AIOwner.ToLookupField(), prompt.Owner.Id}
        }.ToListItem();

        var createdPrompt = await GraphService.Sites[_siteId].Lists[ListNames.AIPrompts].Items
            .Request()
            .AddAsync(newPrompt);

        return createdPrompt.Id;
    }

    public async Task Update(Prompt prompt)
    {
        var promptToUpdate = new Dictionary<string, object>()
        {
            {FieldNames.Title, prompt.Title},
            {FieldNames.AIPrompt, prompt.Content},
            {FieldNames.AIFunctions.ToLookupField() + "@odata.type", "Collection(Edm.Int32)"},
            {FieldNames.AIFunctions.ToLookupField(), prompt.Functions?.Select(a => a.Id)},
            {FieldNames.AIOwner.ToLookupField(), prompt.Owner?.Id},
            {FieldNames.AIAssistant.ToLookupField(), prompt.Assistant?.Id},
            {FieldNames.AIVisibility, prompt.Visibility.ToValue()},
            {FieldNames.AIDepartment.ToLookupField(), prompt.Department?.Id.ToInt()},
        }.ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AIPrompts].Items[prompt.Id].Fields
            .Request()
            .UpdateAsync(promptToUpdate);
    }

    public async Task Delete(string id)
    {
        await GraphService.Sites[_siteId].Lists[ListNames.AIPrompts].Items[id]
         .Request()
         .DeleteAsync();
    }

    public Task<IEnumerable<Prompt>> GetAll()
    {
        throw new System.NotImplementedException();
    }
}
