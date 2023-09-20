using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Database;
using achappey.ChatGPTeams.Database.Models;
using achappey.ChatGPTeams.Extensions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface IConversationRepository
{
    Task<Conversation> Get(string id);
    Task<string> Create(Conversation conversation);
    Task Update(Conversation conversation);
    Task AddResource(string conversationId, int resourceId);
    Task AddFunction(string conversationId, string functionName);
    Task DeleteFunction(string conversationId, string functionName);
    Task Delete(string id);
}

public class ConversationRepository : IConversationRepository
{
    private readonly string _siteId;
    private readonly ILogger<ConversationRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ChatGPTeamsContext _context;
    private readonly string _selectQuery = $"{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.Title},{FieldNames.AITemperature},{FieldNames.AIFunctions},{FieldNames.AICutOff}";

    public ConversationRepository(ILogger<ConversationRepository> logger,
    AppConfig config, IMapper mapper, ChatGPTeamsContext chatGPTeamsContext,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _context = chatGPTeamsContext;
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
        var item = await _context.Conversations
        .Include(t => t.Assistant)
        .ThenInclude(a => a.Model)
        .Include(t => t.Assistant)
        .ThenInclude(a => a.Resources)
        .Include(t => t.Assistant)
        .ThenInclude(a => a.Functions)
        .Include(t => t.Assistant)
        .ThenInclude(a => a.Owner)
        .Include(t => t.Resources)
        .Include(t => t.Functions)
        .FirstOrDefaultAsync(r => r.Id == id);

        return _mapper.Map<Conversation>(item);
    }

    public async Task<string> Create(Conversation conversation)
    {
        await _context.Conversations.AddAsync(conversation);
        await _context.SaveChangesAsync();
        return conversation.Id;
    }
    public async Task DeleteFunction(string conversationId, string functionName)
    {
        string query = $"DELETE FROM ConversationFunction WHERE ConversationsId = '{conversationId}' AND FunctionsId = '{functionName}'";
        await _context.Database.ExecuteSqlRawAsync(query);
    }


    public async Task AddFunction(string conversationId, string functionName)
    {
        var assistant = await _context.Conversations.FindAsync(conversationId);
        var function = await _context.Functions.FindAsync(functionName);

        if (function == null)
        {
            function = new Function() { Id = functionName };
            await _context.Functions.AddAsync(function);
        }
        else
        {
            // Bevestig dat we een bestaand object gebruiken
            _context.Functions.Attach(function);
        }

        // Check if entities are not null.
        if (assistant != null)
        {
            // Initialize the Functions collection if it's null.
            if (assistant.Functions == null)
            {
                assistant.Functions = new List<Function>();
            }

            // Add the Function to the Assistant's Functions collection.
            assistant.Functions.Add(function);
            _context.Conversations.Update(assistant);
            await _context.SaveChangesAsync();
        }
        //return resource.Id;
    }

    public async Task AddResource(string conversationId, int resourceId)
    {
        var assistant = await _context.Conversations.FindAsync(conversationId);
        var resource = await _context.Resources.FindAsync(resourceId);

        // Check if entities are not null.
        if (assistant != null && resource != null)
        {
            // Initialize the Functions collection if it's null.
            if (assistant.Resources == null)
            {
                assistant.Resources = new List<Resource>();
            }

            // Add the Function to the Assistant's Functions collection.
            assistant.Resources.Add(resource);
            _context.Conversations.Update(assistant);
            // Save changes to the database.
            await _context.SaveChangesAsync();
        }
        //return resource.Id;
    }

    public async Task Update(Conversation conversation)
    {
        try
        {
            var local = _context.Set<Conversation>()
                .Local
                .FirstOrDefault(entry => entry.Id.Equals(conversation.Id));

            if (local != null)
            {
                _context.Entry(local).State = EntityState.Detached;
            }

            _context.Entry(conversation).State = EntityState.Modified;

            // Bijwerken van de entiteitsvelden
            conversation.CutOff = conversation.CutOff;
            conversation.AssistantId = conversation.Assistant.Id;
            conversation.Assistant = conversation.Assistant;
            conversation.Temperature = conversation.Temperature;

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Error logging
        }
    }



    public async Task Delete(string id)
    {
        var conversationToDelete = await _context.Conversations.FindAsync(id);
        if (conversationToDelete != null)
        {
            _context.Conversations.Remove(conversationToDelete);
            await _context.SaveChangesAsync();
        }
    }


}
