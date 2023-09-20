using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using AutoMapper;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Services;

public interface IFunctionService
{
    Task<Function> GetFunctionAsync(string id);
    Task<IEnumerable<Function>> GetAllFunctionsAsync();
    Task<Function> GetFunctionByNameAsync(string name);
    Task AddFunctionRequest(ConversationReference reference, Function function, FunctionCall functionCall);
    Task AddFunctionToConversationAsync(ConversationReference reference, string functionName);
    Task DeleteFunctionFromConversationAsync(ConversationReference reference, string functionName);
    Task AddFunctionResult(ConversationReference reference, FunctionCall functionCall, string result);
    Task HandleFunctionMissing(ConversationReference reference, FunctionCall functionCall);
    Task<IEnumerable<Function>> GetFunctionsByFiltersAsync(string publisher = null, string category = null);
    Task<IEnumerable<string>> GetFunctionsPublishersAsync();
    Task<IEnumerable<string>> GetFunctionsCategoriesAsync();


}

public class FunctionService : IFunctionService
{
    private readonly IFunctionRepository _functionRepository;
    private readonly IFunctionDefinitonRepository _functionDefinitonRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMapper _mapper;
    private readonly IAssistantRepository _assistantRepository;

    public FunctionService(
        IFunctionRepository functionRepository,
        IConversationRepository conversationRepository,
        IFunctionDefinitonRepository functionDefinitonRepository,
        IAssistantRepository assistantRepository,
        IMessageRepository messageRepository,
        IMapper mapper) // Add this line
    {
        _functionRepository = functionRepository;
        _conversationRepository = conversationRepository;
        _assistantRepository = assistantRepository;
        _messageRepository = messageRepository;
        _functionDefinitonRepository = functionDefinitonRepository;
        _mapper = mapper; // Add this line
    }

    public async Task<Function> GetFunctionByNameAsync(string name)
    {
        var items = await _functionDefinitonRepository.GetByNames(new List<string>() { name });

        return items.FirstOrDefault();
    }

    public async Task AddFunctionRequest(ConversationReference reference, Function function, FunctionCall functionCall)
    {
        var conversation = await _conversationRepository.Get(reference.Conversation.Id);

        await _messageRepository.Create(new Database.Models.Message()
        {
            Role = Database.Models.Role.assistant,
            Created = DateTimeOffset.Now,
            Content = "",
            ConversationId = conversation.Id,
            Reference = reference != null ? JsonConvert.SerializeObject(reference) : null,
            FunctionCall = functionCall != null ? JsonConvert.SerializeObject(functionCall) : null
        });
    }

    public async Task HandleFunctionMissing(ConversationReference reference, FunctionCall functionCall)
    {
        var conversation = await _conversationRepository.Get(reference.Conversation.Id);

        await _messageRepository.Create(new Database.Models.Message()
        {
            Role = Database.Models.Role.assistant,
            ConversationId = conversation.Id,
            Name = functionCall.Name,
            Created = DateTimeOffset.Now,
            Reference = reference != null ? JsonConvert.SerializeObject(reference) : null,
            FunctionCall = functionCall != null ? JsonConvert.SerializeObject(functionCall) : null
        });

        await AddFunctionResult(reference, functionCall, JsonConvert.SerializeObject(NotFoundResponse(functionCall.Name)));
    }

    private Models.Response NotFoundResponse(string name)
    {
        return new Models.Response
        {
            Status = "error",
            Message = $"The function {name} was not found",
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task AddFunctionResult(ConversationReference reference, FunctionCall functionCall, string result)
    {
        var conversation = await _conversationRepository.Get(reference.Conversation.Id);

        await _messageRepository.Create(new Database.Models.Message()
        {
            Role = Database.Models.Role.function,
            Content = result,
            Created = DateTimeOffset.Now,
            Name = functionCall.Name,
            ConversationId = conversation.Id,
            Reference = reference != null ? JsonConvert.SerializeObject(reference) : null,
        });
    }

    public async Task AddFunctionToConversationAsync(ConversationReference reference, string functionName)
    {
        await _conversationRepository.AddFunction(reference.Conversation.Id, functionName);
    
    }

    public async Task DeleteFunctionFromConversationAsync(ConversationReference reference, string functionName)
    {
        await _conversationRepository.DeleteFunction(reference.Conversation.Id, functionName);

    }

    public async Task<Function> GetFunctionAsync(string id)
    {
        var item = await _functionRepository.Get(id);

        return _mapper.Map<Function>(item);
    }

    public async Task<IEnumerable<Function>> GetAllFunctionsAsync()
    {
        var items = await _functionDefinitonRepository.GetAll();

        return items;
    }

    public async Task<IEnumerable<Function>> GetFunctionsByFiltersAsync(string publisher = null, string category = null)
    {
        var items = await _functionDefinitonRepository.GetAll();

        if (!string.IsNullOrEmpty(publisher))
        {
            items = items.Where(a => a.Publisher.ToLowerInvariant().Contains(publisher.ToLowerInvariant()));
        }

        if (!string.IsNullOrEmpty(category))
        {
            items = items.Where(a => a.Category.ToLowerInvariant().Contains(category.ToLowerInvariant()));
        }

        return items;
    }

    public async Task<IEnumerable<string>> GetFunctionsPublishersAsync()
    {
        var items = await _functionDefinitonRepository.GetAll();

        return items.Select(a => a.Publisher).Distinct();
    }

    public async Task<IEnumerable<string>> GetFunctionsCategoriesAsync()
    {
        var items = await _functionRepository.GetAll();

        return items.Select(a => "").Distinct();
    }

}
