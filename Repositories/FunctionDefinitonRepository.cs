using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Services.Graph;
using achappey.ChatGPTeams.Services.Simplicate;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels.RequestModels;

namespace achappey.ChatGPTeams.Repositories;

public interface IFunctionDefinitonRepository
{
    Task<IEnumerable<Function>> GetAll();
    Task<IEnumerable<Function>> GetByNames(IEnumerable<string> names);
}

public class FunctionDefinitonRepository : IFunctionDefinitonRepository
{
    private readonly string _siteId;
    private readonly string _appName;
    private readonly ILogger<FunctionDefinitonRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IMemoryCache _cache;

    public FunctionDefinitonRepository(ILogger<FunctionDefinitonRepository> logger,
    AppConfig config, IMapper mapper, IMemoryCache cache,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
        _logger = logger;
        _appName = config.ConnectionName;
        _mapper = mapper;
        _cache = cache;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<IEnumerable<Function>> GetByNames(IEnumerable<string> names)
    {
        var items = await GetAll();

        return items.Where(y => names.Any(i => i == y.Id));
    }

    private IEnumerable<Function> MapFunctionDefinitions(IEnumerable<FunctionDefinition> functionDefinitions, string publisherId)
    {
        return functionDefinitions.Select(f =>
        {
            string category = string.Empty;

            // Check if there's a pipe in the description
            if (f.Description.Contains("|"))
            {
                var parts = f.Description.Split('|');
                category = parts[0].Trim();  // Text before the pipe
                f.Description = parts[1].Trim();  // Text after the pipe, altering f.Description
            }

            return new Function
            {
                FunctionDefinition = f,
                Id = f.Name,
                Category = category,  // Set the Category
                Publisher = publisherId
            };
        }).ToList();
    }



    private IEnumerable<Function> MapFunctionDefinitions2(IEnumerable<FunctionDefinition> functionDefinitions, string publisherId)
    {
        return functionDefinitions.Select(f => new Function
        {
            FunctionDefinition = f,
            Id = f.Name,
            Publisher = publisherId
        });
    }

    public async Task<IEnumerable<Function>> GetAll()
    {
        return await _cache.GetOrCreateAsync("Functions", async entry =>
     {
         entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // set cache expiration

         var graphFuncs = MapFunctionDefinitions(GetGraphFunctionDefinitions(), "Microsoft");
         var simplicateFuncs = MapFunctionDefinitions(GetSimplicateFunctionDefinitions(), "Simplicate");
         var customFuncs = MapFunctionDefinitions(await GetCustomFunctionDefinitions(), _appName);
         var builtinFuncs = MapFunctionDefinitions(GetBuiltinFunctionDefinitions(), _appName + "GPT");
         var builtinVaultFuncs = MapFunctionDefinitions(GetBuiltinVaultFunctionDefinitions(), "Microsoft");

         // Combine all mapped Function objects into a single list
         return graphFuncs.Concat(simplicateFuncs)
                          .Concat(customFuncs)
                          .Concat(builtinFuncs)
                          .Concat(builtinVaultFuncs)
                          .ToList();
     });
    }


    private IEnumerable<FunctionDefinition> GetBuiltinVaultFunctionDefinitions()
    {
        return new List<FunctionDefinition>()
         {
            new FunctionDefinition() {
                Name = "GetVaults",
                Description = "Gets all vaults for the current user",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
                }
            },
              new FunctionDefinition() {
                Name = "GetSecrets",
                Description = "Get secrets from a vault",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"vault", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Title of the vault",
                        }}
                    },
                    Required = new List<string>() { "vault" }
                }
            },
              new FunctionDefinition() {
                Name = "CreateSecret",
                Description = "Creates a new secret in a vault",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"vault", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Title of the vault",
                        }},
                         {"name", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Name of the secret",
                        }},
                         {"password", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Password of the secret",
                        }},
                         {"username", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Username of the secret",
                        }}
                    },
                    Required = new List<string>() { "vault", "name", "password", "username" }
                }
            },
              new FunctionDefinition() {
                Name = "UpdateSecret",
                Description = "Updates a secret in a vault",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"vault", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Title of the vault",
                        }},
                         {"name", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Name of the secret",
                        }},
                         {"password", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "New password of the secret",
                        }}
                    },
                    Required = new List<string>() { "vault", "name", "password" }
                }
            },
              new FunctionDefinition() {
                Name = "DeleteSecret",
                Description = "Deletes a secret in a vault",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"vault", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Title of the vault",
                        }},
                         {"name", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Name of the secret",
                        }}
                    },
                    Required = new List<string>() { "vault", "name" }
                }
            },
              new FunctionDefinition() {
                Name = "SendSecret",
                Description = "Sends a secret to the current user",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"vault", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Title of the vault",
                        }},
                         {"name", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Name of the secret",
                        }}
                    },
                    Required = new List<string>() { "vault", "name" }
                }
            },
         };

    }

    private IEnumerable<FunctionDefinition> GetBuiltinFunctionDefinitions(IEnumerable<string> methodNames = null)
    {
        var items = new List<FunctionDefinition>()
         {
            new FunctionDefinition() {
                Name = "GetMyAssistants",
                Description = "Search all AI-assistants",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
                }
            },
             new FunctionDefinition() {
                Name = "GetDialogCategories",
                Description = "Gets all dialog categories",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
                }
            },
            new FunctionDefinition() {
                Name = "GetFunctions",
                Description = "Search all functions",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"publisher", new FunctionParameterPropertyValue() {
                            Type = "string",
                            Description = "Publisher of the API function",
                        }},
                         {"category", new FunctionParameterPropertyValue() {
                            Type = "string",
                            Description = "Category of the function",
                        }}
                    }
                }
            },
            new FunctionDefinition() {
                Name = "GetDialogs",
                Description = "Search all dialogs",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"category", new FunctionParameterPropertyValue() {
                            Type = "string",
                            Description = "Category of the dialog, exact match",
                        }},
                        {"content", new FunctionParameterPropertyValue() {
                            Type = "string",
                            Description = "Content of the dialog, searches in title and prompt",
                        }}
                    }
                }
            },
            new FunctionDefinition() {
                Name = "AddResourceToChat",
                Description = "Adds a document to this chat",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"url", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Url of the document",
                        }}
                    },
                    Required = new List<string>() { "url" }
                }
            },
            new FunctionDefinition() {
                Name = "UpdateTeamsAssistant",
                Description = "Updates the default AI-assistant for current Teams team.",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"assistantName", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The exact name of the assistant",
                        }}
                    },
                    Required = new List<string>() { "assistantName" }
                }
            },
            new FunctionDefinition() {
                Name = "UpdateChannelAssistant",
                Description = "Updates the default AI-assistant for current Teams channel.",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"assistantName", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The exact name of the assistant",
                        }}
                    },
                    Required = new List<string>() { "assistantName" }
                }
            },
            new FunctionDefinition() {
                Name = "AddFunctionToChat",
                Description = "Adds a ChatGPT API function to this chat",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"functionName", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Name of the function",
                        }}
                    },
                    Required = new List<string>() { "functionName" }
                }
            },
             new FunctionDefinition() {
                Name = "RemoveFunctionFromChat",
                Description = "Removes a ChatGPT API function from this chat",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"functionName", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "Name of the function",
                        }}
                    },
                    Required = new List<string>() { "functionName" }
                }
            },
            new FunctionDefinition() {
                Name = "CreateImages",
                Description = "Creates images with DALL-E AI image generation",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"prompt", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The image prompt",
                        }}
                    },
                    Required = new List<string>() { "prompt" }
                }
            },
            new FunctionDefinition() {
                Name = "SaveDialog",
                Description = "Save a dialog in the dialog book",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"name", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The name of the dialog",
                        }},
                         {"prompt", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The AI chat prompt of the dialog",
                        }},
                        {"category", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The category of the dialog",
                        }},
                         {"assistantName", new FunctionParameterPropertyValue() {
                            Type = "string",
                            Description = "The name of the assistant",
                        }}
                    },
                    Required = new List<string>() { "name", "prompt" }
                }
            },
             new FunctionDefinition() {
                Name = "GetChatResources",
                Description = "Gets all resources attached to this chat",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
                }
            },
                new FunctionDefinition() {
                Name = "GetFunctionDefinitions",
                Description = "Gets function definition by function name",
                Parameters = new FunctionParameters() {
                  Properties = new Dictionary<string, FunctionParameterPropertyValue>() {
                        {"functionName", new FunctionParameterPropertyValue() {
                             Type = "string",
                            Description = "The name of the function",
                        }}
                    },
                    Required = new List<string>() { "functionName" }
                }
            },
            new FunctionDefinition() {
                Name = "GetFunctionCategories",
                Description = "Gets all function categories",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
                }
            }
         };

        if (methodNames != null)
        {
            return items.Where(a => methodNames.Any(y => y == a.Name)).ToList();
        }

        return items;
    }

    private async Task<IEnumerable<FunctionDefinition>> GetCustomFunctionDefinitions(IEnumerable<string> methodNames = null)
    {
        var result = new List<FunctionDefinition>();
        var contentTypesCollectionPage = await GraphService.Sites[_siteId].ContentTypes.Request().Expand("columns").GetAsync();
        // var customFunctions = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIFunctions, $"fields/{FieldNames.AIUrl} ne null");
        var allFunctions = await GraphService.GetAllListItemFromListAsync(_siteId, ListNames.AIFunctions);
        var customFunctions = allFunctions.Where(a => a.GetFieldValue(FieldNames.AIUrl) != null);
        var items = contentTypesCollectionPage.ToList();

        if (methodNames != null)
        {
            items = items.Where(a => methodNames.Any(y => y == a.Name)).ToList();
        }

        foreach (var contentType in items)
        {
            var customFunction = customFunctions.FirstOrDefault(a => a.GetFieldValue(FieldNames.AIDefiniton) == contentType.Name);

            if (customFunction != null)
            {
                var columns = contentType.Columns.Where(a => a.ColumnGroup == "AI");

                result.Add(new FunctionDefinition()
                {
                    Name = customFunction.GetFieldValue(FieldNames.AIName),
                    Description = contentType.Description,
                    Parameters = new FunctionParameters()
                    {
                        Properties = columns.ToDictionary(
                            field => field.DisplayName,
                            field => new FunctionParameterPropertyValue
                            {
                                Type = field.Type.SharePointFieldToJson(),
                                Description = field.Description,
                                Enum = field.Type == Microsoft.Graph.ColumnTypes.Choice ? field.Choice.Choices.ToList() : null
                            }),
                        Required = columns
                         .Where(a => a.Required.HasValue && a.Required.Value)
                         .Select(a => a.Name)
                         .ToList()
                    }
                });
            }
        }

        return result;
    }

    private IEnumerable<FunctionDefinition> GetGraphFunctionDefinitions(IEnumerable<string> methodNames = null)
    {
        return GetTypedFunctionDefinitions<GraphFunctionsClient>(methodNames);

    }

    private IEnumerable<FunctionDefinition> GetSimplicateFunctionDefinitions(IEnumerable<string> methodNames = null)
    {
        return GetTypedFunctionDefinitions<SimplicateFunctionsClient>(methodNames);
    }

    private IEnumerable<FunctionDefinition> GetTypedFunctionDefinitions<T>(IEnumerable<string> methodNames = null)
    {
        var result = new List<FunctionDefinition>();

        // Get all instance, public, declared-only methods from GraphFunctionsClient
        var methods = typeof(T).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

        // New Filtering
        methods = methods.Where(m => m.GetCustomAttribute<MethodDescriptionAttribute>() != null).ToArray();

        // If method names are provided, filter the methods by these names
        if (methodNames != null)
        {
            methods = methods.Where(a => methodNames.Any(y => y == a.Name)).ToArray();
        }

        // Iterate through the methods and construct the function definitions
        foreach (var method in methods)
        {
            var functionParameters = new FunctionParameters
            {
                Properties = method.GetParameters().ToDictionary(param => param.Name, param => MapClrTypeToJsonSchemaType(param.ParameterType, param)),
                Required = method.GetParameters().Where(param => !param.IsOptional).Select(param => param.Name).ToList()
            };

            result.Add(new FunctionDefinition
            {
                Name = method.Name,
                Description = method.GetCustomAttribute<MethodDescriptionAttribute>()?.Description ?? string.Empty,
                Parameters = functionParameters
            });
        }

        return result;
    }

    /// <summary>
    /// Maps CLR types to JSON schema types.
    /// </summary>
    private static FunctionParameterPropertyValue MapClrTypeToJsonSchemaType(Type type, ParameterInfo paramInfo)
    {
        //  Console.WriteLine("Debug Type: " + type.FullName);
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;
        if (actualType.IsEnum)
        {
            return new FunctionParameterPropertyValue
            {
                Type = "string",
                Description = actualType.GetCustomAttribute<ParameterDescriptionAttribute>()?.Description ?? string.Empty,
                Enum = Enum.GetNames(actualType)
            };
        }

        if (TypeMap.TryGetValue(actualType, out var jsonSchemaType))
        {
            return new FunctionParameterPropertyValue
            {
                Type = jsonSchemaType,
                Description = paramInfo.GetCustomAttribute<ParameterDescriptionAttribute>()?.Description ?? string.Empty
                //Description = actualType.GetCustomAttribute<ParameterDescriptionAttribute>()?.Description ?? string.Empty
            };
        }

        return new FunctionParameterPropertyValue { Type = "object" };
    }

    /// <summary>
    /// Provides extension methods related to Graph.
    /// </summary>

    private static readonly Dictionary<Type, string> TypeMap = new Dictionary<Type, string>
    {
        { typeof(int), "integer" },
        { typeof(long), "integer" },
        { typeof(float), "number" },
        { typeof(double), "number" },
        { typeof(bool), "boolean" },
        { typeof(string), "string" },
    };

}
