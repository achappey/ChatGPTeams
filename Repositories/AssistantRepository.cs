using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Repositories;

public interface IAssistantRepository
{
    Task<Assistant> Get(string id);
    Task<Assistant> GetByName(string name);
    Task<string> Create(Assistant assistant);
    Task Update(Assistant assistant);
    Task Delete(string id);
    Task<IEnumerable<Assistant>> GetAll();

}

public class AssistantRepository : IAssistantRepository
{
    private readonly string _siteId;
    private readonly ILogger<AssistantRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIPrompt},{FieldNames.AIModel},{FieldNames.AIDepartment.ToLookupField()},{FieldNames.AIDepartment},{FieldNames.AIFunctions},{FieldNames.AITemperature},{FieldNames.AIOwners},{FieldNames.AIVisibility}";

    public AssistantRepository(ILogger<AssistantRepository> logger,
    AppConfig config, IMapper mapper,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _graphClientFactory = graphClientFactory;
    }

    private GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<Assistant> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIAssistants, id, _selectQuery);

        return _mapper.Map<Assistant>(item);
    }

    public async Task<Assistant> GetByName(string name)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIAssistants, $"fields/{FieldNames.Title} eq '{name}'", _selectQuery);

        return _mapper.Map<Assistant>(item);
    }

    public async Task<IEnumerable<Assistant>> GetAll()
    {
        var items = await GraphService.GetAllListItemFromListAsync(_siteId, ListNames.AIAssistants, _selectQuery);

        return items.Select(a => _mapper.Map<Assistant>(a));
    }


    public async Task<string> Create(Assistant assistant)
    {
        var newAssistant = new Dictionary<string, object>()
                    {
                        {FieldNames.Title, assistant.Name},
                        {FieldNames.AIPrompt, assistant.Prompt},
                        {FieldNames.AITemperature, assistant.Temperature},
                        {FieldNames.AIModel, assistant.Model},
                        {FieldNames.AIVisibility, assistant.Visibility.ToValue()},
                        {FieldNames.AIOwners.ToLookupField() + "@odata.type", "Collection(Edm.Int32)"},
                        {FieldNames.AIOwners.ToLookupField(), assistant.Owners?.Select(a => a.Id)},
                        {FieldNames.AIDepartment.ToLookupField(), assistant.Department?.Id.ToInt()},
                        {FieldNames.AIFunctions.ToLookupField() + "@odata.type", "Collection(Edm.Int32)"},
                        {FieldNames.AIFunctions.ToLookupField(), assistant.Functions.Select(a => a.Id.ToInt())}
                    }.ToListItem();

        // Add the new assistant
        var createdAssistant = await GraphService.Sites[_siteId].Lists[ListNames.AIAssistants].Items
            .Request()
            .AddAsync(newAssistant);

        return createdAssistant.Id;
    }

    public async Task Update(Assistant assistant)
    {
        var assistantToUpdate = new Dictionary<string, object>()
                    {
                        { FieldNames.Title, assistant.Name },
                        { FieldNames.AIPrompt, assistant.Prompt },
                        { FieldNames.AIDepartment.ToLookupField(), assistant.Department?.Id.ToInt()},
                        { FieldNames.AITemperature, assistant.Temperature},
                        { FieldNames.AIVisibility, assistant.Visibility.ToValue()},
                        { FieldNames.AIFunctions.ToLookupField() + "@odata.type", "Collection(Edm.Int32)"},
                        { FieldNames.AIFunctions.ToLookupField(), assistant.Functions.Select(a => a.Id.ToInt())}
                    }.ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AIAssistants].Items[assistant.Id].Fields
            .Request()
            .UpdateAsync(assistantToUpdate);
    }

    public async Task Delete(string id)
    {
        await GraphService.Sites[_siteId].Lists[ListNames.AIAssistants].Items[id]
         .Request()
         .DeleteAsync();
    }
}
