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
using Newtonsoft.Json.Linq;

namespace achappey.ChatGPTeams.Services;


public interface IFunctionExecutionService
{
    Task<string> ExecuteFunction(ConversationReference reference, FunctionCall functionCall);
}

public class FunctionExecutionService : IFunctionExecutionService
{
    private readonly AppConfig _appConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRequestService _requestService;
    private readonly IAssistantService _assistantService;
    private readonly IAssistantRepository _assistantRepository;
    private readonly IFunctionService _functionService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IResourceService _resourceService;
    private readonly IMapper _mapper;
    private readonly IVaultService _vaultService;
    private readonly IFunctionDefinitonRepository _functionDefinitonRepository;

    private readonly IGraphClientFactory _graphClientFactory;

    public FunctionExecutionService(AppConfig appConfig, IGraphClientFactory graphClientFactory, IVaultService vaultService, IAssistantRepository assistantRepository,
    IFunctionService functionService, IFunctionDefinitonRepository functionDefinitonRepository, IResourceService resourceService, IConversationRepository conversationRepository,
    IHttpClientFactory httpClientFactory, IRequestService requestService, IAssistantService assistantService)
    {
        _appConfig = appConfig;
        _httpClientFactory = httpClientFactory;
        _requestService = requestService;
        _graphClientFactory = graphClientFactory;
        _vaultService = vaultService;
        _functionService = functionService;
        _assistantRepository = assistantRepository;
        _resourceService = resourceService;
        _conversationRepository = conversationRepository;
        _assistantService = assistantService;
        _functionDefinitonRepository = functionDefinitonRepository;
    }

    private Response NotAllowedResponse(string name)
    {
        return new Response
        {
            Status = $"Unauthorized",
            Message = $"Function {name} is not allowed",
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<string> ExecuteFunction(ConversationReference reference, FunctionCall functionCall)
    {
        var conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);
        conversation.Assistant  = await _assistantRepository.Get(conversation.Assistant.Id);
        var function = await _functionService.GetFunctionByNameAsync(functionCall.Name); // Retrieve function details based on the name
        string result = null;

        if (function == null)
        {
            await _functionService.HandleFunctionMissing(reference, functionCall);
        }
        else
        {
            await _functionService.AddFunctionRequest(reference, function, functionCall); // Add request for function execution

            if (conversation.AllFunctionNames.Any(t => t == functionCall.Name))
            {
                result = await GetFunctionResultAsync(reference, function, functionCall); // Get the result of the function execution
            }
            else
            {
                result = JsonConvert.SerializeObject(NotAllowedResponse(functionCall.Name));
            }

            await _functionService.AddFunctionResult(reference, functionCall, result);
        }

        return result;
    }

    private async Task<string> GetFunctionResultAsync(ConversationReference reference, Function function, FunctionCall functionCall)
    {
        try
        {
            return function.Publisher switch
            {
                "Microsoft" => await ExecuteMicrosoftFunction(functionCall),
                "Azure" => await ExecuteVaultFunction(function, functionCall),
                _ when function.Publisher == _appConfig.ConnectionName => await ExecuteCustomFunction(function, functionCall),
                _ => await ExecuteBuiltinFunction(reference, functionCall),
            };
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    private async Task<string> ExecuteVaultFunction(Function function, FunctionCall functionCall)
    {
        var arguments = JObject.Parse(functionCall.Arguments);

        // Define a local function to get argument values
        string Arg(string name) => arguments[name]?.ToString();

        // Define a success response
        string Success() => JsonConvert.SerializeObject(SuccessResponse());

        // Create a dictionary to map function names to their implementations
        var functions = new Dictionary<string, Func<Task<string>>>
        {
            ["GetVaults"] = async () => JsonConvert.SerializeObject(await _vaultService.GetMyVaultsAsync()),
            ["GetSecrets"] = async () => JsonConvert.SerializeObject(await _vaultService.GetSecretsAsync(Arg("vault"))),
            ["CreateSecret"] = async () => { await _vaultService.CreateSecret(Arg("vault"), Arg("name"), Arg("password"), Arg("username")); return Success(); },
            ["UpdateSecret"] = async () => { await _vaultService.UpdateSecret(Arg("vault"), Arg("name"), Arg("password")); return Success(); },
            ["DeleteSecret"] = async () => { await _vaultService.DeleteSecret(Arg("vault"), Arg("name")); return Success(); },
            ["SendSecret"] = async () => { await _vaultService.SendSecretAsync(Arg("vault"), Arg("name")); return Success(); }
        };

        // Find the corresponding function and execute it
        if (functions.TryGetValue(functionCall.Name, out var func))
        {
            return await func();
        }

        throw new Exception("Unknown function");
    }

    private async Task<string> ExecuteCustomFunction(Function function, FunctionCall functionCall)
    {
        var token = await _requestService.CreateRequestAsync();
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(function.Url, new StringContent(JsonConvert.SerializeObject(new
        {
            requestId = token,
            name = functionCall.Name,
            arguments = JsonConvert.DeserializeObject(functionCall.Arguments)
        }), Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            await _requestService.DeleteRequestByTokenAsync(token);

            return await response.Content.ReadAsStringAsync();
        }

        throw new Exception(response.ReasonPhrase);
    }

    public async Task<string> ExecuteBuiltinFunction(ConversationReference reference, FunctionCall functionCall)
    {
        var arguments = JObject.Parse(functionCall.Arguments);

        // Define a local function to get argument values
        string Arg(string name) => arguments[name]?.ToString();

        // Create a dictionary to map function names to their implementations
        var functions = new Dictionary<string, Func<Task<string>>>
        {
            ["GetMyAssistants"] = async () => JsonConvert.SerializeObject(await _assistantService.GetMyAssistants()),
            ["GetFunctions"] = async () => JsonConvert.SerializeObject(await _functionService.GetAllFunctionsAsync()),
            ["GetChatResources"] = async () => JsonConvert.SerializeObject(await _resourceService.GetResourcesByConversationTitleAsync(reference.Conversation.Id)),
            ["AddResourceToChat"] = async () =>
            {
                var url = Arg("url");
                return JsonConvert.SerializeObject(await _resourceService.ImportResourceAsync(reference, new Resource() { Url = url, Name = url, Id = null }));
            },
            ["GetFunctionDefinitions"] = async () => JsonConvert.SerializeObject(await _functionDefinitonRepository.GetAll())
        };

        // Find the corresponding function and execute it
        if (functions.TryGetValue(functionCall.Name, out var func))
        {
            return await func();
        }

        throw new Exception("Unknown function");
    }


    private async Task<string> ExecuteMicrosoftFunction(FunctionCall functionCall)
    {
        var client = _graphClientFactory.GetFunctionsClient(); // Get the client

        // Use reflection to get the method on the GraphFunctionsClient class
        var method = client.GetType().GetMethod(functionCall.Name);

        if (method != null)
        {
            // Deserialize the arguments JSON into a dictionary
            var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(functionCall.Arguments);

            // Ensure the arguments are in the correct order
            var orderedArguments = method.GetParameters()
                .Select(p => arguments.ContainsKey(p.Name) ? arguments[p.Name] : null)
                .ToArray();

            // Invoke the method dynamically
            var methodResult = method.Invoke(client, orderedArguments);

            // If the method is asynchronous (returns a Task), we need to await it
            if (methodResult is Task task)
            {
                await task;
                // If the task has a result (i.e., if it's a Task<T>), we can get it
                if (task.GetType().IsGenericType)
                {
                    var taskResult = ((dynamic)task).Result;

                    return JsonConvert.SerializeObject(taskResult); // Return serialized result
                }
            }
        }

        // Throw an exception if the method is not found
        throw new KeyNotFoundException();
    }

    private Response SuccessResponse()
    {
        return new Response
        {
            Status = "success",
            Message = "The function was executed successfully.",
            Timestamp = DateTime.UtcNow
        };
    }

}
