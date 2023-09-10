using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Extensions;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IAssistantService
{
    Task<Assistant> GetAssistant(int id);
    Task<Assistant> GetAssistantByName(string name);
    Task UpdateAssistantAsync(Assistant assistant);
    Task<int> CreateAssistantAsync(Assistant assistant);
    Task<IEnumerable<Assistant>> GetMyAssistants();
    Task<Assistant> CloneAssistantAsync(int assistantId);
    Task<Assistant> GetAssistantByConversationTitle(string conversationTitle);
}

public class AssistantService : IAssistantService
{
    private readonly IAssistantRepository _assistantRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserService _userService;
    private readonly IResourceService _resourceService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMapper _mapper;
    private readonly IFunctionRepository _functionRepository;

    public AssistantService(
        IAssistantRepository assistantRepository,
        IUserService userService,
        IConversationRepository conversationRepository,
        IFunctionRepository functionRepository,
        IDepartmentRepository departmentRepository,
        IResourceService resourceService,
        IMapper mapper)  // Inject AutoMapper here
    {
        _assistantRepository = assistantRepository;
        _userService = userService;
        _departmentRepository = departmentRepository;
        _functionRepository = functionRepository;
        _conversationRepository = conversationRepository;
        _resourceService = resourceService;
        _mapper = mapper;  // Initialize the AutoMapper field
    }


    public async Task<Assistant> GetAssistantByConversationTitle(string conversationTitle)
    {
        var item = await _conversationRepository.Get(conversationTitle);
        return _mapper.Map<Assistant>(item.Assistant);
    }

    public async Task<int> CreateAssistantAsync(Assistant assistant)
    {
        return await _assistantRepository.Create(_mapper.Map<Database.Models.Assistant>(assistant));
    }

    public async Task<Assistant> GetAssistantByName(string name)
    {
        var item = await _assistantRepository.GetByName(name);

        return _mapper.Map<Assistant>(item);
    }

    public async Task<Assistant> GetAssistant(int id)
    {
        var item = await _assistantRepository.Get(id);

        return _mapper.Map<Assistant>(item);
        //        return await GetAssistantWithLookups(await _assistantRepository.Get(id));
    }
    /*
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

            assistant.Resources = await _resourceService.GetFileName(assistant.Id.ToInt().Value);

            var functions = new List<Function>();

            foreach (var function in assistant.Functions)
            {
                functions.Add(await _functionRepository.Get(function.Id));
            }

            assistant.Functions = functions;

            return assistant;
        }
    */
    public async Task<IEnumerable<Assistant>> GetMyAssistants()
    {
        var user = await _userService.GetCurrentUser();
        var assistants = await _assistantRepository.GetAll();
        var mapped = assistants.Select(_mapper.Map<Assistant>);

        return mapped.Where(a =>
              a.Visibility == Visibility.Everyone
          || (a.Visibility == Visibility.Owner && a.Owner.Id == user.Id)
          || (a.Visibility == Visibility.Department && user.Department != null && a.Department?.Id == user.Department.Id));
    }

    public async Task UpdateAssistantAsync(Assistant assistant)
    {
        await _assistantRepository.Update(_mapper.Map<Database.Models.Assistant>(assistant));
    }

    public async Task<Assistant> CloneAssistantAsync(int assistantId)
    {
        var assistant = await GetAssistant(assistantId);
        var user = await _userService.GetCurrentUser();

        var newAssistant = new Database.Models.Assistant()
        {
            Name = assistant.Name.GenerateNewAssistantTitle(),
            Visibility = Database.Models.Visibility.Owner,
            Prompt = assistant.Prompt,
            ModelId = assistant.Model.Id,
            //Functions =  assistant.Functions,
            //Resources = assistant.Resources,
            Temperature = assistant.Temperature,
            // Owners = new List<Database.Models.User>() { _mapper.Map<Database.Models.User>(user) }
            OwnerId = user.Id
        };

        //      assistant.Name = assistant.Name.GenerateNewAssistantTitle();
        //    assistant.Visibility = Visibility.Owner;
        //  assistant.Owners = new List<User>() { user };
        newAssistant.Id = await _assistantRepository.Create(newAssistant);
        //newAssistant.Id = await CreateAssistantAsync(newAssistant);

        return _mapper.Map<Assistant>(newAssistant);
    }
}
