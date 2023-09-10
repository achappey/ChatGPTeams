using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Database;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Database.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace achappey.ChatGPTeams.Repositories;

public interface IPromptRepository
{
    Task<Prompt> Get(int id);
    Task<IEnumerable<Prompt>> GetPromptsByContent(string content, string userId, string departmentName);
    Task<IEnumerable<Prompt>> GetPromptsByUser(string userId, string departmentName);
    Task<int> Create(Prompt prompt);
    Task Update(Prompt prompt);
    Task Delete(int id);
    //Task<IEnumerable<Prompt>> GetAll();
    Task<IEnumerable<string>> GetCategories();
}

public class PromptRepository : IPromptRepository
{
    private readonly string _siteId;
    private readonly ILogger<PromptRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.AIVisibility},{FieldNames.Title},{FieldNames.AICategory},{FieldNames.AIPrompt},{FieldNames.AIAssistant},{FieldNames.AIAssistant.ToLookupField()},{FieldNames.AIOwner.ToLookupField()},{FieldNames.AIDepartment.ToLookupField()},{FieldNames.AIDepartment},{FieldNames.AIOwner},{FieldNames.AIFunctions}";

    private readonly ChatGPTeamsContext _context;

    public PromptRepository(ILogger<PromptRepository> logger,
     AppConfig config, IMapper mapper,
     IGraphClientFactory graphClientFactory,
     ChatGPTeamsContext dbContext)  // Inject DbContext here
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _mapper = mapper;
        _graphClientFactory = graphClientFactory;
        _context = dbContext;  // Initialize DbContext field
    }


    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    private IQueryable<Prompt> GetUserPrompts(string userId, string departmentName)
    {
        return _context.Prompts
        .Include(r => r.Owner)
        .Include(r => r.Assistant)
        .ThenInclude(r => r.Model)
        .Include(r => r.Functions)
        .Include(r => r.Department)
        .Where(t => t.Visibility == Visibility.Everyone ||
        (t.Visibility == Visibility.Owner && t.Owner.Id == userId) ||
        (departmentName != null && t.Visibility == Visibility.Department && t.Department.Name == departmentName));
        //string filter = BuildPromptFilter(userId, departmentId);
        //var items = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIPrompts, filter, _selectQuery);

        //return items.Select(a => _mapper.Map<Prompt>(a));
    }

    public async Task<IEnumerable<Prompt>> GetPromptsByUser(string userId, string departmentName)
    {
        return await GetUserPrompts(userId, departmentName).ToListAsync();
        // return _context.Prompts.Where(t => t.Visibility == Visibility.Everyone || 
        // (t.Visibility == Visibility.Owner && t.Owner.Id == userId) || 
        // (departmentName != null && t.Visibility == Visibility.Department && t.Department.Name == departmentName));
        //string filter = BuildPromptFilter(userId, departmentId);
        //var items = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIPrompts, filter, _selectQuery);

        //return items.Select(a => _mapper.Map<Prompt>(a));
    }

    public async Task<IEnumerable<Prompt>> GetPromptsByContent(string content, string userId, string departmentName)
    {
        return GetUserPrompts(userId, departmentName).ToList()
        .Where(a => a.Title.ToLowerInvariant().Contains(content.ToLowerInvariant())
            || a.Content.ToLowerInvariant().Contains(content.ToLowerInvariant())).ToList();
        /*        string filter = BuildPromptFilter(userId, departmentId);

                var items = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIPrompts, filter, _selectQuery);

                return items.Select(a => _mapper.Map<Prompt>(a)).Where(a => a.Title.ToLowerInvariant().Contains(content.ToLowerInvariant())
                        || a.Content.ToLowerInvariant().Contains(content.ToLowerInvariant()));*/
    }


    public async Task<IEnumerable<string>> GetCategories()
    {
        return _context.Prompts.GroupBy(t => t.Category).Select(a => a.Key).Distinct().ToList();
        //    var items = await GraphService.Sites[_siteId].Lists[ListNames.AIPrompts].Columns.Request().GetAsync();
        //   var choiceColumn = items.FirstOrDefault(a => a.DisplayName == FieldNames.AICategory);

        // return choiceColumn.Choice.Choices;
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

    public async Task<int> Create(Prompt prompt)
    {
        var functionIds = prompt.Functions?.Select(a => a.Id).ToList();
        var existingFunctions = _context.Functions.Where(r => functionIds.Contains(r.Id)).ToList();

        foreach (var function in existingFunctions)
        {
            _context.Attach(function);
        }

        prompt.Functions = existingFunctions;

        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;
        await _context.Prompts.AddAsync(prompt);
        await _context.SaveChangesAsync();
        return prompt.Id;
    }

    public async Task Update(Prompt prompt)
    {
        // Je moet de gerelateerde entiteiten loskoppelen
        var local = _context.Set<Prompt>()
            .Local
            .FirstOrDefault(entry => entry.Id.Equals(prompt.Id));

        if (local != null)
        {
            _context.Entry(local).State = EntityState.Detached;
        }

        _context.Entry(prompt).State = EntityState.Modified;

        if (prompt.Functions != null)
        {
            foreach (var function in prompt.Functions)
            {
                var existingFunction = await _context.Functions.FindAsync(function.Id);
                if (existingFunction != null)
                {
                    _context.Entry(existingFunction).State = EntityState.Unchanged;
                }
                else
                {
                    // Als de functie niet bestaat, maak een nieuwe
                    _context.Functions.Add(function);
                }
            }
        }

        await _context.SaveChangesAsync();
    }


    public async Task Updatedsadsadsa(Prompt prompt)
    {

        // Prepare your prompt
        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;

        // Update prompt
        _context.Prompts.Update(prompt);

        // Save Changes
        await _context.SaveChangesAsync();
    }
    public async Task Updatedasda(Prompt prompt)
    {
        // Clear previous Entity Framework tracking to avoid duplication
        _context.ChangeTracker.Clear();

        var functionIds = prompt.Functions?.Select(a => a.Id).ToList();

        // Fetch existing functions from DB
        var existingFunctions = await _context.Functions.Where(r => functionIds.Contains(r.Id)).ToListAsync();

        // Detach all existing functions
        foreach (var function in existingFunctions)
        {
            _context.Entry(function).State = EntityState.Detached;
        }

        // Prepare your prompt
        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;
        prompt.Functions = existingFunctions;

        // Update prompt
        _context.Prompts.Update(prompt);

        // Save Changes
        await _context.SaveChangesAsync();
    }

    public async Task Updatedasdsa(Prompt prompt)
    {
        // Clear any previous tracking
        _context.ChangeTracker.Clear();

        // Get existing function IDs
        var functionIds = prompt.Functions?.Select(a => a.Id).ToList();

        // Find existing functions
        var existingFunctions = await _context.Functions.Where(f => functionIds.Contains(f.Id)).ToListAsync();

        // New Functions
        var newFunctions = prompt.Functions?.Except(existingFunctions).ToList();

        // Add new functions if any
        if (newFunctions != null && newFunctions.Count() > 0)
        {
            await _context.Functions.AddRangeAsync(newFunctions);
        }

        // Setup prompt
        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;
        prompt.Functions = existingFunctions;

        // Attach and mark as modified
        _context.Prompts.Update(prompt);

        // Save changes
        await _context.SaveChangesAsync();
    }

    public async Task Updatedsdsds(Prompt prompt)
    {
        // Clear the EF Core change tracker
        _context.ChangeTracker.Clear();

        var functionIds = prompt.Functions?.Select(a => a.Id).ToList();

        // Fetch existing functions from the database
        var existingFunctions = await _context.Functions.Where(r => functionIds.Contains(r.Id)).AsNoTracking().ToListAsync();

        // Identify IDs that are not in the database
        var newFunctionIds = functionIds.Except(existingFunctions.Select(f => f.Id));

        // Add new functions
        foreach (var newId in newFunctionIds)
        {
            _context.Functions.Add(new Function { Id = newId });
        }

        // Setup prompt for update
        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;
        prompt.Functions = existingFunctions;

        _context.Entry(prompt).State = EntityState.Modified;

        await _context.SaveChangesAsync();
    }


    public async Task Update4(Prompt prompt)
    {
        var functionIds = prompt.Functions?.Select(a => a.Id).ToList();

        // Detach any tracked entities first
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            entry.State = EntityState.Detached;
        }

        // Find existing functions
        var existingFunctions = await _context.Functions.Where(f => functionIds.Contains(f.Id)).AsNoTracking().ToListAsync();

        // Find not existing function ids
        var notExistingFunctionIds = functionIds.Except(existingFunctions.Select(f => f.Id)).ToList();

        // Add new functions
        var newFunctions = notExistingFunctionIds.Select(id => new Function { Id = id }).ToList();
        await _context.AddRangeAsync(newFunctions);

        // Attach existing functions to the context
        foreach (var function in existingFunctions)
        {
            _context.Entry(function).State = EntityState.Unchanged;
        }

        // Update prompt
        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;

        _context.Entry(prompt).State = EntityState.Modified;

        await _context.SaveChangesAsync();
    }


    public async Task Update2(Prompt prompt)
    {
        var functionIds = prompt.Functions?.Select(a => a.Id).ToList();

        // Detach any tracked functions first to prevent duplication
        foreach (var function in _context.ChangeTracker.Entries<Function>().ToList())
        {
            function.State = EntityState.Detached;
        }

        // Get existing functions from the database
        var existingFunctions = _context.Functions.Where(r => functionIds.Contains(r.Id)).AsNoTracking().ToList();

        // Determine which functions do not exist in the database
        var notExistingFunctionIds = functionIds.Except(existingFunctions.Select(f => f.Id)).ToList();
        var notExistingFunctions = notExistingFunctionIds.Select(id => new Function { Id = id }).ToList();

        // Add non-existing functions to the database
        await _context.Functions.AddRangeAsync(notExistingFunctions);
        /*
            var functionIds = prompt.Functions?.Select(a => a.Id).ToList();
            var existingFunctions = _context.Functions.Where(r => functionIds.Contains(r.Id)).ToList();
            //var notExistingFunctions = functionIds.Where(a => a.) _context.Functions.Where(r => !functionIds.Contains(r.Id)).ToList();
            // Determine which functions do not exist in the database
            var notExistingFunctionIds = functionIds.Except(existingFunctions.Select(f => f.Id)).ToList();
            var notExistingFunctions = notExistingFunctionIds.Select(id => new Function { Id = id }).ToList();

            // Add non-existing functions to the database
            await _context.Functions.AddRangeAsync(notExistingFunctions);


            foreach (var function in existingFunctions)
            {
                _context.Attach(function);
            }
    */
        prompt.AssistantId = prompt.Assistant?.Id;
        prompt.Assistant = null;
        prompt.OwnerId = prompt.Owner.Id;
        prompt.Owner = null;

        _context.Prompts.Update(prompt);
        await _context.SaveChangesAsync();
        /*        var currentPrompt = await _context.Prompts.FindAsync(prompt.Id);
                if (currentPrompt != null)
                {
                    //_mapper.Map(assistant, currentPrompt);

                }*/
    }

    public async Task Delete(int id)
    {
        var itemToDelete = await _context.Prompts.FindAsync(id);
        if (itemToDelete != null)
        {
            _context.Prompts.Remove(itemToDelete);
            await _context.SaveChangesAsync();
        }
        // Handle not found scenario as needed.
    }

    public async Task<Prompt> Get(int id)
    {
        return await _context.Prompts
        .Include(t => t.Assistant)
        .ThenInclude(t => t.Model)
        .Include(t => t.Functions)
        .Include(t => t.Owner)
        .FirstOrDefaultAsync(a => a.Id == id);
        //  var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIPrompts, id);

        //return _mapper.Map<Prompt>(item);
    }
    /*

        public async Task<string> Create(Prompt prompt)
        {
            var newPrompt = new Dictionary<string, object>()
            {
                {FieldNames.Title, prompt.Title },
                {FieldNames.AIPrompt, prompt.Content},
                {FieldNames.AICategory, prompt.Category},
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
                {FieldNames.AICategory, prompt.Category},
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
        }*/
}
