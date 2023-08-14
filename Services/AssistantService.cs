using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Extensions;

namespace achappey.ChatGPTeams.Services;

public interface IAssistantService
{
    Task<Assistant> GetAssistant(string id);
    Task<Assistant> GetAssistantByName(string name);
    Task UpdateAssistantAsync(Assistant assistant);
    Task<string> CreateAssistantAsync(Assistant assistant);
    Task<IEnumerable<Assistant>> GetMyAssistants();
    Task<Assistant> CloneAssistantAsync(string assistantId);
    Task<Assistant> GetAssistantByConversationTitle(string conversationTitle);
}

public class AssistantService : IAssistantService
{
    private readonly IAssistantRepository _assistantRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserService _userService;
    private readonly IResourceRepository _resourceRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IFunctionRepository _functionRepository;

    public AssistantService(IAssistantRepository assistantRepository, IUserService userService, 
    IConversationRepository conversationRepository, IFunctionRepository functionRepository,
    IDepartmentRepository departmentRepository, IResourceRepository resourceRepository)
    {
        _assistantRepository = assistantRepository;
        _userService = userService;
        _departmentRepository = departmentRepository;
        _functionRepository = functionRepository;
        _conversationRepository = conversationRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<Assistant> GetAssistantByConversationTitle(string conversationTitle)
    {
        var item = await _conversationRepository.GetByTitle(conversationTitle);
        return item.Assistant;
    }

    public async Task<string> CreateAssistantAsync(Assistant assistant)
    {
        return await _assistantRepository.Create(assistant);
    }

    public async Task<Assistant> GetAssistantByName(string name)
    {
        return await GetAssistantWithLookups(await _assistantRepository.GetByName(name));
    }

    public async Task<Assistant> GetAssistant(string id)
    {
        return await GetAssistantWithLookups(await _assistantRepository.Get(id));
    }

    private async Task<Assistant> GetAssistantWithLookups(Assistant assistant)
    {
        if (assistant.Department != null)
        {
            assistant.Department = await _departmentRepository.Get(assistant.Department.Id);
        }

        if (assistant.Owners != null)
        {
            var owners = new List<User>();

            foreach (var owner in assistant.Owners)
            {
                owners.Add(await _userService.Get(owner.Id));
            }

            assistant.Owners = owners;
        }

        assistant.Resources = await _resourceRepository.GetByAssistant(assistant.Id);

        var functions = new List<Function>();

        foreach (var function in assistant.Functions)
        {
            functions.Add(await _functionRepository.Get(function.Id));
        }

        assistant.Functions = functions;

        return assistant;
    }

    public async Task<IEnumerable<Assistant>> GetMyAssistants()
    {
        var user = await _userService.GetCurrentUser();
        var assistants = await _assistantRepository.GetAll();

        var assistantsWithLookups = new List<Assistant>();

        foreach (var assistant in assistants)
        {
            assistantsWithLookups.Add(await GetAssistantWithLookups(assistant));
        }

        return assistantsWithLookups.Where(a =>
              a.Visibility == Visibility.Everyone
          || (a.Visibility == Visibility.Owner && a.Owners.Any(z => z.Id == user.Id))
          || (a.Visibility == Visibility.Department && user.Department != null && a.Department?.Id == user.Department.Id));
    }

    public async Task UpdateAssistantAsync(Assistant assistant)
    {
        await _assistantRepository.Update(assistant);
    }

    public async Task<Assistant> CloneAssistantAsync(string assistantId)
    {
        var assistant = await GetAssistant(assistantId);
        var user = await _userService.GetCurrentUser();

        assistant.Name = assistant.Name.GenerateNewAssistantTitle();
        assistant.Visibility = Visibility.Owner;
        assistant.Owners = new List<User>() { user };
        assistant.Id = await CreateAssistantAsync(assistant);

        return assistant;
    }
}
