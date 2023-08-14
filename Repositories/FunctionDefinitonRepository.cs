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
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels.RequestModels;

namespace achappey.ChatGPTeams.Repositories;

public interface IFunctionDefinitonRepository
{
    Task<Department> Get(string id);
    Task<Department> GetByName(string name);
    Task<IEnumerable<FunctionDefinition>> GetAll();
    Task<IEnumerable<FunctionDefinition>> GetByNames(IEnumerable<string> names);
}

public class FunctionDefinitonRepository : IFunctionDefinitonRepository
{
    private readonly string _siteId;
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

    public async Task<IEnumerable<FunctionDefinition>> GetByNames(IEnumerable<string> names)
    {
        var items = await GetAll();

        return items.Where(y => names.Any(i => i == y.Name));
    }

    public async Task<IEnumerable<FunctionDefinition>> GetAll()
    {
        return await _cache.GetOrCreateAsync("FunctionDefinitions", async entry =>
     {
         entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // set cache expiration

         var graph = GetGraphFunctionDefinitions().ToList();
         graph.AddRange(await GetCustomFunctionDefinitions());
         graph.AddRange(GetBuiltinFunctionDefinitions());
         graph.AddRange(GetBuiltinVaultFunctionDefinitions());

         return graph;
     });
    }

    public async Task<Department> GetByName(string name)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId, ListNames.AIDepartments, $"fields/{FieldNames.Title} eq '{name}'");

        return _mapper.Map<Department>(item);
    }

    public async Task<Department> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId, ListNames.AIDepartments, id);

        return _mapper.Map<Department>(item);
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
                Name = "GetFunctions",
                Description = "Search all functions",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
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
                Name = "GetChatResources",
                Description = "Gets all resources attached to this chat",
                Parameters = new FunctionParameters() {
                    Properties = new Dictionary<string, FunctionParameterPropertyValue>()
                }
            },
                 new FunctionDefinition() {
                Name = "GetFunctionDefinitions",
                Description = "Search all function definitions",
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
        var customFunctions = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIFunctions, $"fields/{FieldNames.AIUrl} ne null");
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

    /// <summary>
    /// Retrieves function definitions from the GraphFunctionsClient type.
    /// Allows filtering of methods by provided names and constructs a set of
    /// FunctionDefinition objects, including information like name, description,
    /// and parameters.
    /// </summary>
    /// <param name="methodNames">Optional list of method names to filter the results (if null, all methods are included).</param>
    /// <returns>An enumerable of FunctionDefinition objects representing the desired methods.</returns>
    private IEnumerable<FunctionDefinition> GetGraphFunctionDefinitions(IEnumerable<string> methodNames = null)
    {
        var result = new List<FunctionDefinition>();

        // Get all instance, public, declared-only methods from GraphFunctionsClient
        var methods = typeof(GraphFunctionsClient).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

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
                Properties = method.GetParameters().ToDictionary(param => param.Name, param => MapClrTypeToJsonSchemaType(param.ParameterType)),
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
    private static FunctionParameterPropertyValue MapClrTypeToJsonSchemaType(Type type)
    {
        if (type.IsEnum)
        {
            return new FunctionParameterPropertyValue
            {
                Type = "string",
                Description = type.GetCustomAttribute<ParameterDescriptionAttribute>()?.Description ?? string.Empty,
                Enum = Enum.GetNames(type)
            };
        }

        if (TypeMap.TryGetValue(type, out var jsonSchemaType))
        {
            return new FunctionParameterPropertyValue
            {
                Type = jsonSchemaType,
                Description = type.GetCustomAttribute<ParameterDescriptionAttribute>()?.Description ?? string.Empty
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
