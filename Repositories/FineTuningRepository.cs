using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpToken;

namespace achappey.ChatGPTeams.Repositories;

public interface IFineTuningRepository
{
    Task<FineTuning> CreateJob(string model, string suffix, string fileName);
    Task<IEnumerable<FineTuning>> ListFineTunes();
    Task<FineTuning> RetrieveJob(string id);
}

public class FineTuningRepository : IFineTuningRepository
{
    private readonly ILogger<ChatRepository> _logger;
    private readonly IMapper _mapper;
    private readonly OpenAIService _openAIService;

    public FineTuningRepository(ILogger<ChatRepository> logger,
    OpenAIService openAIService, IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _openAIService = openAIService;
    }

    public async Task<FineTuning> CreateJob(string model, string suffix, string fileName)
    {
        var result = await _openAIService.CreateFineTune(new FineTuneCreateRequest()
        {
            TrainingFile = fileName,
            Model = model,
            Suffix = suffix,
        });

        if (result.Successful)
        {
            return new FineTuning()
            {
                Id = result.Id,
                FineTunedModel = result.FineTunedModel,
                Status = result.Status

            };
        }

        throw new System.Exception(result.Error.Message);
    }

    public async Task<FineTuning> RetrieveJob(string id)
    {
        var result = await _openAIService.RetrieveFineTune(id);

        if (result.Successful)
        {
            return new FineTuning()
            {
                Id = result.Id,
                FineTunedModel = result.FineTunedModel,
                Status = result.Status

            };
        }

        throw new System.Exception(result.Error.Message);
    }

    public async Task<IEnumerable<FineTuning>> ListFineTunes()
    {
        var result = await _openAIService.ListFineTunes();

        if (result.Successful)
        {
            return result.Data.Select(a => new FineTuning()
            {
                Id = a.Id,
                FineTunedModel = a.FineTunedModel,
                Status = a.Status
            });
        }

        throw new System.Exception(result.Error.Message);
    }

    private Dictionary<string, int> ValidateAndSummarizeJsonData(string datasetJson)
    {
        var dataset = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(datasetJson);
        var encoding = GptEncoding.GetEncoding("cl100k_base");

        var totalTokens = 0;  // Initialize total token count

        Dictionary<string, int> formatErrors = new Dictionary<string, int>();
        foreach (var example in dataset)
        {
            var messages = example.GetValueOrDefault("messages") as JArray;
            if (messages == null)
            {
                IncrementErrorCount("missing_messages_list", formatErrors);
                continue;
            }
            // Your additional checks here

            foreach (var message in messages)
            {
                // For example, you can calculate the token size of the "content" field in each message
                var content = message.Value<string>("content");
                if (content != null)
                {
                    totalTokens += encoding.Encode(content).Count();  // Replace with your actual token count logic
                }
            }
        }

        return formatErrors;  
    }

    private void IncrementErrorCount(string errorType, Dictionary<string, int> errors)
    {
        if (errors.ContainsKey(errorType))
        {
            errors[errorType]++;
        }
        else
        {
            errors[errorType] = 1;
        }
    }

}