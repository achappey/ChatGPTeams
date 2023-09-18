using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Database;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Database.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Repositories;

public interface IAssistantRepository
{
    Task<Assistant> Get(int id);
    Task<Assistant> GetByName(string name);
    Task<int> Create(Assistant assistant);
    Task Update(Assistant assistant);
    Task Delete(int id);
    Task<IEnumerable<Assistant>> GetAll();

}

public class AssistantRepository : IAssistantRepository
{
    private readonly string _siteId;
    private readonly ILogger<AssistantRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ChatGPTeamsContext _context;
    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIPrompt},{FieldNames.AIModel},{FieldNames.AIDepartment.ToLookupField()},{FieldNames.AIDepartment},{FieldNames.AIFunctions},{FieldNames.AITemperature},{FieldNames.AIOwners},{FieldNames.AIVisibility}";

    public AssistantRepository(ILogger<AssistantRepository> logger,
    AppConfig config, IMapper mapper, ChatGPTeamsContext chatGPTeamsContext,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _context = chatGPTeamsContext;
        _graphClientFactory = graphClientFactory;
    }

    private GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }
   
    public async Task<Assistant> Get(int id)
    {
        return await _context.Assistants
        .Include(a => a.Model)
        .Include(a => a.Owner)
        .Include(a => a.Resources)
        .Include(a => a.Functions)
        .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Assistant> GetByName(string name)
    {
        var item = await _context.Assistants
                                 .Include(a => a.Model)
                                 .Include(a => a.Owner)
                                 .Include(a => a.Resources)
                                 .Include(a => a.Functions)
                                 .FirstOrDefaultAsync(a => a.Name == name);
        return _mapper.Map<Assistant>(item);
    }

    public async Task<IEnumerable<Assistant>> GetAll()
    {
        return await _context.Assistants
        .Include(a => a.Model)
        .Include(a => a.Resources)
        .Include(a => a.Functions)
        .Include(a => a.Owner)
        .ToListAsync();
    }

    public async Task<int> Create(Assistant assistant)
    {
        try
        {
            await _context.Assistants.AddAsync(assistant);
            await _context.SaveChangesAsync();
            return assistant.Id;
        }
        catch (DbUpdateException ex)
        {
            // Log the detailed error
            _logger.LogError(ex.InnerException?.Message ?? ex.Message);
            throw;
        }
    }

    public async Task Update(Assistant assistant)
    {

        var existingConversation = await _context.Assistants
                                        .FirstOrDefaultAsync(c => c.Id == assistant.Id);

        // Update the properties of the existing conversation
        if (existingConversation != null)
        {
            existingConversation.Prompt = assistant.Prompt;
            existingConversation.Name = assistant.Name;
            existingConversation.DepartmentId = assistant.Department?.Id;
            existingConversation.Temperature = assistant.Temperature;
            existingConversation.OwnerId = assistant.Owner.Id;
            existingConversation.Visibility = assistant.Visibility;

            _context.Assistants.Update(existingConversation);
        }

        await _context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        var assistantToDelete = await _context.Assistants.FindAsync(id);
        if (assistantToDelete != null)
        {
            _context.Assistants.Remove(assistantToDelete);
            await _context.SaveChangesAsync();
        }
        // Handle not found scenario as needed.
    }
}