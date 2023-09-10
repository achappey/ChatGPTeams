using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Extensions;
using System.Linq;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IPromptService
{
    Task<Prompt> GetPromptAsync(int id);
    Task<IEnumerable<Prompt>> GetMyPromptsAsync();
    Task<IEnumerable<Prompt>> GetPromptByContentAsync(string content);
    Task<IEnumerable<string>> GetCategories();
    Task<int> CreatePromptAsync(Prompt prompt);
    Task UpdatePromptAsync(Prompt prompt);
    Task<IEnumerable<Prompt>> GetMyPromptsByCategoryAsync(string category);
    Task DeletePromptAsync(int id);
}

public class PromptService : IPromptService
{
    private readonly IPromptRepository _promptRepository;
    private readonly IUserService _userService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IFunctionRepository _functionRepository;
    private readonly IMapper _mapper;

    public PromptService(IPromptRepository promptRepository, IUserService userService, IMapper mapper,
    IFunctionRepository functionRepository, IDepartmentRepository departmentRepository)
    {
        _promptRepository = promptRepository;
        _userService = userService;
        _departmentRepository = departmentRepository;
        _functionRepository = functionRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Prompt>> GetPromptByContentAsync(string content)
    {
        var user = await _userService.GetCurrentUser();
        var items = await _promptRepository.GetPromptsByContent(content, user.Id, user.Department?.Name);

        return items.Select(_mapper.Map<Prompt>);
    }

    public async Task<IEnumerable<Prompt>> GetMyPromptsAsync()
    {
        var user = await _userService.GetCurrentUser();

        var items = await _promptRepository.GetPromptsByUser(user.Id, user.Department.Name);
        var users = await _userService.GetAll();
        
        return items.Select(item =>
        {
            var mappedPrompt = _mapper.Map<Prompt>(item);
            var owner = users.FirstOrDefault(u => u.Id == mappedPrompt.Owner.Id);
            if (owner != null)
            {
                mappedPrompt.Owner.DisplayName = owner.DisplayName;
            }
            return mappedPrompt;
        });
    }

    public async Task<IEnumerable<Prompt>> GetMyPromptsByCategoryAsync(string category)
    {
        var items = await GetMyPromptsAsync();

        return items.Where(a => a.Category == category);
    }

    public async Task<IEnumerable<string>> GetCategories()
    {
        return await _promptRepository.GetCategories();
    }

    public async Task<Prompt> GetPromptAsync(int id)
    {
        var item = await _promptRepository.Get(id);

        return _mapper.Map<Prompt>(item);
    }

    public async Task UpdatePromptAsync(Prompt prompt)
    {
        await _promptRepository.Update(_mapper.Map<Database.Models.Prompt>(prompt));
    }

    public async Task<int> CreatePromptAsync(Prompt prompt)
    {
        return await _promptRepository.Create(_mapper.Map<Database.Models.Prompt>(prompt));
    }

    public async Task DeletePromptAsync(int id)
    {
        await _promptRepository.Delete(id);
    }
}


