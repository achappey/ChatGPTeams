using System;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using SharpToken;
using OpenAI.ObjectModels;
using System.Threading.Tasks;

namespace achappey.ChatGPTeams.Repositories;

public interface IImageRepository
{
    Task<IEnumerable<string>> CreateImages(string prompt);
}

public class ImageRepository : IImageRepository
{
    private readonly ILogger<ChatRepository> _logger;
    private readonly IMapper _mapper;
    private readonly OpenAIService _openAIService;

    public ImageRepository(ILogger<ChatRepository> logger,
    OpenAIService openAIService, IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _openAIService = openAIService;
    }

    private ChatCompletionCreateRequest ToChatCompletionCreateRequest(Conversation conversation)
    {
        var messages = new List<ChatMessage>() {
            conversation.Assistant.ToChatMessage()
        };

        messages.AddRange(conversation.Messages.Select(a => _mapper.Map<ChatMessage>(a)));

        return new ChatCompletionCreateRequest
        {
            Model = conversation.Assistant.Model,
            Temperature = conversation.Temperature,
            Messages = messages,
            Functions = conversation.FunctionDefinitions?.Count() > 0 ? conversation.FunctionDefinitions?.ToList() : null,
        };
    }

    public async Task<IEnumerable<string>> CreateImages(string prompt)
    {
        var image = await _openAIService.CreateImage(new ImageCreateRequest()
        {
            Prompt = prompt,
            N = 2,
            Size = StaticValues.ImageStatics.Size.Size1024,
            ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url,
        });

        return image.Results.Select(a => a.Url);
    }
}