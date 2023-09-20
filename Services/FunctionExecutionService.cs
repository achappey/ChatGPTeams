using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using AutoMapper;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace achappey.ChatGPTeams.Services;


public interface IFunctionExecutionService
{
    Task<string> ExecuteFunction(ConversationContext context, ConversationReference reference, FunctionCall functionCall);
}

public class FunctionExecutionService : IFunctionExecutionService
{
    private readonly AppConfig _appConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRequestService _requestService;
    private readonly IAssistantService _assistantService;
    private readonly IFunctionService _functionService;
    private readonly IConversationService _conversationService;
    private readonly IResourceService _resourceService;
    private readonly IMapper _mapper;
    private readonly IVaultService _vaultService;
    private readonly IFunctionDefinitonRepository _functionDefinitonRepository;
    private readonly IImageRepository _imageRepository;

    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ISimplicateClientFactory _simplicateClientFactory;
    private readonly IDataverseRepository _dataverseRepository;
    private readonly IPromptService _promptService;
    private readonly ITeamsService _teamsService;
    private readonly IUserService _userService;

    public FunctionExecutionService(AppConfig appConfig, IGraphClientFactory graphClientFactory,
    IVaultService vaultService,
    IFunctionService functionService, IFunctionDefinitonRepository functionDefinitonRepository,
    IResourceService resourceService, IConversationService conversationService,
    IHttpClientFactory httpClientFactory, IRequestService requestService, ITeamsService teamsService,
    ISimplicateClientFactory simplicateClientFactory, IPromptService promptService,
     IAssistantService assistantService, IImageRepository imageRepository, IDataverseRepository dataverseRepository, IUserService userService)
    {
        _appConfig = appConfig;
        _httpClientFactory = httpClientFactory;
        _requestService = requestService;
        _graphClientFactory = graphClientFactory;
        _teamsService = teamsService;
        _userService = userService;
        _vaultService = vaultService;
        _functionService = functionService;
        _imageRepository = imageRepository;
        _promptService = promptService;
        _resourceService = resourceService;
        _conversationService = conversationService;
        _dataverseRepository = dataverseRepository;
        _assistantService = assistantService;
        _functionDefinitonRepository = functionDefinitonRepository;
        _simplicateClientFactory = simplicateClientFactory;
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

    public async Task<string> ExecuteFunction(ConversationContext context, ConversationReference reference, FunctionCall functionCall)
    {

        //    var wut = await _dataverseRepository.GetEntityDefinitions("fakton");
        var conversation = await _conversationService.GetConversationAsync(reference.Conversation.Id);
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
                result = await GetFunctionResultAsync(context, reference, function, functionCall); // Get the result of the function execution
            }
            else
            {
                result = JsonConvert.SerializeObject(NotAllowedResponse(functionCall.Name));
            }

            await _functionService.AddFunctionResult(reference, functionCall, result);
        }

        return result;
    }

    private async Task<string> GetFunctionResultAsync(ConversationContext context, ConversationReference reference, Function function, FunctionCall functionCall)
    {
        try
        {
            return function.Publisher switch
            {
                "Microsoft" => await ExecuteMicrosoftFunction(functionCall),
                "Simplicate" => await ExecuteSimplicateFunction(reference.User.AadObjectId, functionCall),
                "Azure" => await ExecuteVaultFunction(function, functionCall),
                _ when function.Publisher == _appConfig.ConnectionName => await ExecuteCustomFunction(function, functionCall),
                _ => await ExecuteBuiltinFunction(context, reference, functionCall),
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

    private async Task<string> ExecuteBuiltinFunction(ConversationContext context, ConversationReference reference, FunctionCall functionCall)
    {
        var arguments = JObject.Parse(functionCall.Arguments);

        // Define a local function to get argument values
        string Arg(string name) => arguments[name]?.ToString();

        // Create a dictionary to map function names to their implementations
        var functions = new Dictionary<string, Func<Task<string>>>
        {
            ["GetMyAssistants"] = async () => JsonConvert.SerializeObject(await _assistantService.GetMyAssistants()),
            ["GetFunctions"] = async () =>
            {
                var publisher = Arg("publisher");
                var category = Arg("category");
                var result = await _functionService.GetFunctionsByFiltersAsync(publisher, category);
                return JsonConvert.SerializeObject(result);
            },
            ["GetDialogs"] = async () =>
            {
                var category = Arg("category");
                var content = Arg("content");
                IEnumerable<Prompt> result1 = new List<Prompt>();
                IEnumerable<Prompt> result2 = new List<Prompt>();

                if (!string.IsNullOrEmpty(category))
                {
                    result1 = await _promptService.GetMyPromptsByCategoryAsync(category);
                }

                if (!string.IsNullOrEmpty(content))
                {
                    result2 = await _promptService.GetPromptByContentAsync(content);
                }

                var combinedResults = result1.Concat(result2);

                var uniqueResults = combinedResults
                    .GroupBy(item => item.Id)
                    .Select(group => group.First())
                    .ToList();

                return JsonConvert.SerializeObject(uniqueResults);
            },
            ["GetDialogCategories"] = async () => JsonConvert.SerializeObject(await _promptService.GetCategories()),
            ["GetFunctionCategories"] = async () => JsonConvert.SerializeObject(await _functionService.GetFunctionsCategoriesAsync()),
            ["GetChatResources"] = async () => {
                var conversation = await _conversationService.GetConversationAsync(reference.Conversation.Id);
                return JsonConvert.SerializeObject(conversation.AllResources);
            },
            ["AddResourceToChat"] = async () =>
            {
                var url = Arg("url");
                var result = await _resourceService.ImportResourceAsync(reference, new Resource() { Url = url, Name = url, Id = 0 });
                return JsonConvert.SerializeObject(result);
            },
            ["AddFunctionToChat"] = async () =>
            {
                var functionName = Arg("functionName");
                await _functionService.AddFunctionToConversationAsync(reference, functionName);
                return JsonConvert.SerializeObject(SuccessResponse());
            },
            ["UpdateTeamsAssistant"] = async () =>
            {
                if(context.TeamsId == null || context.ChannelId == null) {
                    throw new Exception("This function can only be used in Teams channels");
                }

                var assistantName = Arg("assistantName");

                Assistant assistant = null;
                
                if (assistantName != null)
                {
                    assistant = await _assistantService.GetAssistantByName(assistantName);

                    if(assistant == null) {
                        throw new Exception("Assistant not found");
                    }
                }

                var teamsItem = await _teamsService.UpdateTeamsAssistant(context.TeamsId, assistant);

                return JsonConvert.SerializeObject(teamsItem);
            },
            ["UpdateChannelAssistant"] = async () =>
            {
                if(context.TeamsId == null || context.ChannelId == null) {
                    throw new Exception("This function can only be used in Teams channels");
                }

                var assistantName = Arg("assistantName");

                Assistant assistant = null;
                
                if (assistantName != null)
                {
                    assistant = await _assistantService.GetAssistantByName(assistantName);

                    if(assistant == null) {
                        throw new Exception("Assistant not found");
                    }
                }

                var channelItem = await _teamsService.UpdateChannelAssistant(context.TeamsId, context.ChannelId, assistant);

                return JsonConvert.SerializeObject(channelItem);
            },
            ["RemoveFunctionFromChat"] = async () =>
            {
                var functionName = Arg("functionName");
                await _functionService.DeleteFunctionFromConversationAsync(reference, functionName);
                return JsonConvert.SerializeObject(SuccessResponse());
            },
            ["SaveDialog"] = async () =>
            {
                var name = Arg("name");
                var prompt = Arg("prompt");
                var category = Arg("category");
                var assistantName = Arg("assistantName");
                var currentUser = await _userService.GetCurrentUser();

                Assistant assistant = null;

                if (!string.IsNullOrEmpty(assistantName))
                {
                    assistant = await _assistantService.GetAssistantByName(assistantName);
                }

                var newItem = new Prompt()
                {
                    Title = name,
                    Content = prompt,
                    Assistant = assistant,
                    Owner = currentUser,
                    Category = category,
                    Visibility = Visibility.Owner
                };

                var result = await _promptService.CreatePromptAsync(newItem);

                return JsonConvert.SerializeObject(result);
            },
            ["CreateImages"] = async () =>
            {
                var prompt = Arg("prompt");
                return JsonConvert.SerializeObject(await _imageRepository.CreateImages(prompt));
            },
            ["GetFunctionDefinitions"] = async () =>
            {
                var functionName = Arg("functionName");
                var result = await _functionDefinitonRepository.GetByNames(new List<string>() { functionName });
                return JsonConvert.SerializeObject(result);
            }
        };

        // Find the corresponding function and execute it
        if (functions.TryGetValue(functionCall.Name, out var func))
        {
            return await func();
        }

        throw new Exception("Unknown function");
    }

    private List<object> GetOrderedArguments(MethodInfo method, Dictionary<string, object> arguments)
    {
        var orderedArguments = new List<object>();
        foreach (var param in method.GetParameters())
        {
            Type paramType = param.ParameterType;
            if (Nullable.GetUnderlyingType(paramType) is Type underlyingType)
            {
                paramType = underlyingType;
            }

            if (paramType.IsEnum && arguments.ContainsKey(param.Name))
            {
                var enumValue = Enum.Parse(paramType, arguments[param.Name].ToString());
                orderedArguments.Add(enumValue);
            }
            else if (paramType == typeof(string) && arguments.ContainsKey(param.Name))
            {
                var arg = arguments[param.Name];
                // Als het argument van het type DateTime is
                if (arg is DateTime)
                {
                    // Converteer DateTime naar string
                    arg = ((DateTime)arg).ToString("o");  // ISO 8601 formaat
                }
                orderedArguments.Add(arg);
            }
            else
            {
                orderedArguments.Add(arguments.ContainsKey(param.Name) ? arguments[param.Name] : null);
            }
        }
        return orderedArguments;
    }

    private async Task<string> ExecuteFunction(string userId, FunctionCall functionCall, Func<string, object> getClient)
    {
        var client = getClient(userId); // Get the client
        var method = client.GetType().GetMethod(functionCall.Name);
        if (method == null) throw new KeyNotFoundException();

        var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(functionCall.Arguments);
        var orderedArguments = GetOrderedArguments(method, arguments);
        var methodResult = method.Invoke(client, orderedArguments.ToArray());

        if (methodResult is Task task)
        {
            await task;
            if (task.GetType().IsGenericType)
            {
                var taskResult = ((dynamic)task).Result;
                return JsonConvert.SerializeObject(taskResult);
            }
        }

        return null;
    }

    private async Task<string> ExecuteSimplicateFunction(string userId, FunctionCall functionCall)
    {
        return await ExecuteFunction(userId, functionCall, id => _simplicateClientFactory.GetFunctionsClient(id));
    }

    private async Task<string> ExecuteMicrosoftFunction(FunctionCall functionCall)
    {
        return await ExecuteFunction(null, functionCall, _ => _graphClientFactory.GetFunctionsClient());
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
