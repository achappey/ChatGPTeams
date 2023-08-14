using System;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace achappey.ChatGPTeams.Repositories;

public interface IChatRepository
{
    Task<Message> ChatAsync(Conversation conversation);
    Task<Message> ChatWithContextAsync(IEnumerable<EmbeddingScore> items, Conversation conversation);
}

public class ChatRepository : IChatRepository
{
    private readonly ILogger<ChatRepository> _logger;
    private readonly IMapper _mapper;
    private readonly OpenAIService _openAIService;

    public ChatRepository(ILogger<ChatRepository> logger,
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

    public async Task<Message> ChatAsync(Conversation conversation)
    {
        return await ChatWithFallback(conversation);
    }

    public async Task<Message> ChatWithContextAsync(IEnumerable<EmbeddingScore> items, Conversation conversation)
    {
        return await ChatWithBestContextAsync(items, conversation);
    }

    private async Task<Message> ChatWithFallback(Conversation chat)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(1);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    await Task.Delay(delay);  // Wait before retrying
                    delay *= 2;  // Increase delay
                }

                if (chat.Messages.Count() > 0)
                {
                    return await AttemptChatAsync(chat);
                }
                else
                {
                    throw new InvalidOperationException("The chat history could not be shortened further without success.");
                }
            }
            catch (FormatException)
            {
                chat.ShortenChatHistory();
            }
        }

        throw new InvalidOperationException("Exceeded maximum attempts to chat.");
    }

    private async Task<Message> AttemptChatAsync(Conversation conversation)
    {
        var chatCompletionRequest = ToChatCompletionCreateRequest(conversation);

        var response = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionRequest).ConfigureAwait(false);

        if (!response.Successful)
        {
            throw response.Error?.Code switch
            {
                "context_length_exceeded" => new FormatException(response.Error.Message),
                _ => new Exception(response.Error?.Message),
            };
        }

        var message = response.Choices.FirstOrDefault()?.Message;

        return _mapper.Map<Message>(message);
    }


    private async Task<Message> ChatWithBestContextAsync(IEnumerable<EmbeddingScore> topResults, Conversation chat)
    {
        var delayTime = 1000;

        while (topResults.Any())
        {
            var contextQuery = CreateContextQuery(topResults);
            chat.Assistant.Prompt += contextQuery;

            try
            {
                return await AttemptChatAsync(chat);
            }
            catch (FormatException)
            {
                (topResults, delayTime) = await HandleFormatErrorAsync(chat, topResults, delayTime);
            }
        }

        throw new Exception("Failed to chat with context.");
    }

    private string CreateContextQuery(IEnumerable<EmbeddingScore> topResults)
    {
        return string.Join(" ", topResults
            .GroupBy(r => r.Url)
            .Select(g => new
            {
                Url = g.Key,
                BestScore = g.Max(r => r.Score),
                Texts = string.Join(" ", g.Select(r => r.Text))
            })
            .OrderByDescending(g => g.BestScore)
            .Select(g => g.Url + " " + g.Texts));
    }

    private async Task<(IEnumerable<EmbeddingScore> TopResults, int DelayTime)> 
            HandleFormatErrorAsync(Conversation chat, IEnumerable<EmbeddingScore> topResults, int delayTime)
    {
        var topResultsCount = topResults.Count();

        try
        {
            chat.ShortenChatHistory(10, 5);
        }
        catch (InvalidOperationException) { }

        if (topResultsCount > 1)
        {
            topResults = topResults.Take(topResultsCount / 2).ToList();
            await Task.Delay(delayTime);
            delayTime *= 2;  // Exponential backoff
        }
        else
        {
            throw new Exception("Failed to chat with context.");
        }

        return (topResults, delayTime);
    }



}