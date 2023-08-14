using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
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
    Task<IEnumerable<Function>> GetFunctionsByConversation(string conversationTitle);

}

public class FunctionService : IFunctionService
{
    private readonly IFunctionRepository _functionRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IAssistantRepository _assistantRepository;

    public FunctionService(IFunctionRepository functionRepository,
        IConversationRepository conversationRepository, IAssistantRepository assistantRepository,
        IMessageRepository messageRepository)
    {
        _functionRepository = functionRepository;
        _conversationRepository = conversationRepository;
        _assistantRepository = assistantRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Function> GetFunctionByNameAsync(string name)
    {
        return await _functionRepository.GetByName(name);
    }

    public async Task AddFunctionRequest(ConversationReference reference, Function function, FunctionCall functionCall)
    {
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);

        await _messageRepository.Create(new Message()
        {
            Role = Role.assistant,
            Name = functionCall.Name,
            ConversationId = conversation.Id,
            Reference = reference,
            FunctionCall = functionCall
        });
    }

    public async Task HandleFunctionMissing(ConversationReference reference, FunctionCall functionCall)
    {
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);

        await _messageRepository.Create(new Message()
        {
            Role = Role.assistant,
            ConversationId = conversation.Id,
            Name = functionCall.Name,
            Reference = reference,
            FunctionCall = functionCall
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
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);

        await _messageRepository.Create(new Message()
        {
            Role = Role.function,
            Content = result,
            Name = functionCall.Name,
            ConversationId = conversation.Id,
            Reference = reference
        });
    }

    public async Task AddFunctionToConversationAsync(ConversationReference reference, string functionName)
    {
        var function = await _functionRepository.GetByName(functionName);
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);
        var functions = conversation.Functions.ToList();
        functions.Add(function);
        conversation.Functions = functions;

        await _conversationRepository.Update(conversation);
    }

    public async Task DeleteFunctionFromConversationAsync(ConversationReference reference, string functionName)
    {
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);
        conversation.Functions = conversation.Functions.Where(a => a.Name != functionName);
        await _conversationRepository.Update(conversation);
    }

    public async Task<Function> GetFunctionAsync(string id)
    {
        return await _functionRepository.Get(id);
    }

    public async Task<IEnumerable<Function>> GetAllFunctionsAsync()
    {
        return await _functionRepository.GetAll();
    }

    public async Task<IEnumerable<Function>> GetFunctionsByConversation(string conversationTitle)
    {
        var conversation = await _conversationRepository.GetByTitle(conversationTitle);
        var assistant = await _assistantRepository.Get(conversation.Assistant.Id);

        // Assuming assistant.Functions is also a collection of Functions
        var assistantFunctions = assistant.Functions;

        // Concatenating the functions from conversation and assistant
        var combinedFunctions = conversation.Functions.Concat(assistantFunctions);

        return combinedFunctions;
    }
}
