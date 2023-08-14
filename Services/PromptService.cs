using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using achappey.ChatGPTeams.Extensions;
using System.Linq;

namespace achappey.ChatGPTeams.Services;

public interface IPromptService
{
    Task<Prompt> GetPromptAsync(string id);
    Task<IEnumerable<Prompt>> GetMyPromptsAsync();
    Task<IEnumerable<Prompt>> GetPromptByContentAsync(string content);
    Task<string> CreatePromptAsync(Prompt prompt);
    Task UpdatePromptAsync(Prompt prompt);
    Task DeletePromptAsync(string id);
}

public class PromptService : IPromptService
{
    private readonly IPromptRepository _promptRepository;
    private readonly IUserService _userService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IFunctionRepository _functionRepository;

    public PromptService(IPromptRepository promptRepository, IUserService userService,
    IFunctionRepository functionRepository, IDepartmentRepository departmentRepository)
    {
        _promptRepository = promptRepository;
        _userService = userService;
        _departmentRepository = departmentRepository;
        _functionRepository = functionRepository;
    }

    public async Task<IEnumerable<Prompt>> GetPromptByContentAsync(string content)
    {
        var user = await _userService.GetCurrentUser();

        return await EnrichPromptsAsync(await _promptRepository.GetPromptsByContent(content, user.Id, user.Department?.Id.ToInt()));
    }

    public async Task<IEnumerable<Prompt>> GetMyPromptsAsync()
    {
        var user = await _userService.GetCurrentUser();

        return await EnrichPromptsAsync(await _promptRepository.GetPromptsByUser(user.Id, user.Department?.Id.ToInt()));
    }

    private async Task<IEnumerable<Prompt>> EnrichPromptsAsync(IEnumerable<Prompt> prompts)
    {
        var items = new List<Prompt>();

        foreach (var prompt in prompts)
        {
            items.Add(await EnrichPromptAsync(prompt));
        }

        return items.OrderBy(a => a.Title);
    }

    private async Task<Prompt> EnrichPromptAsync(Prompt prompt)
    {
        prompt.Owner = await _userService.Get(prompt.Owner.Id);

        if (prompt.Department != null)
        {
            prompt.Department = await _departmentRepository.Get(prompt.Department.Id);
        }

        var functions = new List<Function>();

        foreach (var function in prompt.Functions)
        {
            functions.Add(await _functionRepository.Get(function.Id));
        }

        prompt.Functions = functions;

        return prompt;
    }

    public async Task<Prompt> GetPromptAsync(string id)
    {
        return await EnrichPromptAsync(await _promptRepository.Get(id));
    }

    public async Task UpdatePromptAsync(Prompt prompt)
    {
        await _promptRepository.Update(prompt);
    }

    public async Task<string> CreatePromptAsync(Prompt prompt)
    {
        return await _promptRepository.Create(prompt);
    }

    public async Task DeletePromptAsync(string id)
    {
        await _promptRepository.Delete(id);
    }
}
