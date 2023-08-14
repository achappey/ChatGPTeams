using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IConversationRepository
{
    Task<Conversation> Get(string id);
    Task<Conversation> GetByTitle(string title);
    Task<string> Create(Conversation conversation);
    Task Update(Conversation conversation);
    Task Delete(string id);
}

public class ConversationRepository : IConversationRepository
{
    private readonly string _siteId;
    private readonly ILogger<ConversationRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.Title},{FieldNames.AITemperature},{FieldNames.AIFunctions},{FieldNames.AICutOff}";

    public ConversationRepository(ILogger<ConversationRepository> logger,
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

    public async Task<Conversation> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIConversations, id, _selectQuery);

        return _mapper.Map<Conversation>(item);
    }

    public async Task<Conversation> GetByTitle(string title)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIConversations, $"fields/{FieldNames.Title} eq '{title}'", _selectQuery);
        
        return _mapper.Map<Conversation>(item);
    }

    public async Task<string> Create(Conversation conversation)
    {
        var newConversation = new Dictionary<string, object>()
        {
            {FieldNames.Title, conversation.Title},
            {FieldNames.AITemperature, conversation.Temperature},
            {FieldNames.AIAssistant.ToLookupField(), conversation.Assistant.Id.ToInt()},
        }.ToListItem();

        var createdConversation = await GraphService.Sites[_siteId].Lists[ListNames.AIConversations].Items
            .Request()
            .AddAsync(newConversation);

        return createdConversation.Id;
    }

    public async Task Update(Conversation conversation)
    {
        var conversationToUpdate = new Dictionary<string, object>()
        {
            {FieldNames.AITemperature, conversation.Temperature},
            {FieldNames.AICutOff, conversation.CutOff},
            {FieldNames.AIAssistant.ToLookupField(), conversation.Assistant.Id.ToInt()},
            {FieldNames.AIFunctions.ToLookupField() + "@odata.type", "Collection(Edm.Int32)"},
            {FieldNames.AIFunctions.ToLookupField(), conversation.Functions.Select(a => a.Id.ToInt())}
        }.ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AIConversations].Items[conversation.Id].Fields
            .Request()
            .UpdateAsync(conversationToUpdate);
    }

    public async Task Delete(string id)
    {
        await GraphService.Sites[_siteId].Lists[ListNames.AIConversations].Items[id]
         .Request()
         .DeleteAsync();
    }
}
