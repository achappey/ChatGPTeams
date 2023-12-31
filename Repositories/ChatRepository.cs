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
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Repositories;

public interface IChatRepository
{
    IAsyncEnumerable<Message> ChatStreamAsync(Conversation conversation);
    IAsyncEnumerable<Message> ChatWithContextStreamAsync(IEnumerable<EmbeddingScore> items, Conversation conversation);
}

public class ChatRepository : IChatRepository
{
    private readonly ILogger<ChatRepository> _logger;
    private readonly IMapper _mapper;
    private readonly OpenAIService _openAIService;

    private const double MaxTokenSizeFactor = 0.8;
    private const double MaxReservedTokenFactor = 1 / 3.0;

    public ChatRepository(ILogger<ChatRepository> logger,
    OpenAIService openAIService, IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _openAIService = openAIService;
    }


    private async IAsyncEnumerable<Message> AttemptChatStreamAsync(Conversation conversation)
    {
        var chatCompletionRequest = ToChatCompletionCreateRequest(conversation);

        await foreach (var response in _openAIService.ChatCompletion.CreateCompletionAsStream(chatCompletionRequest))
        {
            if (response.Successful)
            {
                var message = response.Choices.FirstOrDefault()?.Message;
                yield return _mapper.Map<Message>(message);
            }
            else
            {
                throw new Exception(response.Error.Message);
            }
        }
    }

    private IAsyncEnumerable<Message> ChatWithFallback(Conversation chat)
    {
        var encoding = GptEncoding.GetEncoding("cl100k_base");
        int maxTokenSize = CalculateMaxTokenSize(chat.Assistant.Model.MaxTokenSize);
        EnsureTokenSizeWithinLimit(chat, encoding, maxTokenSize);

        return AttemptChatStreamAsync(chat);
    }
    private IAsyncEnumerable<Message> ChatWithBestContextStreamAsync(IEnumerable<EmbeddingScore> topResults, Conversation chat)
    {
        var encoding = GptEncoding.GetEncoding("cl100k_base");
        int maxTokenSize = CalculateMaxTokenSize(chat.Assistant.Model.MaxTokenSize);

        int chatHistoryTokens = CalculateChatHistoryTokens(chat.Messages, encoding);
        EnsureTokenSizeWithinLimit(chat, encoding, maxTokenSize, chatHistoryTokens);

        int remainingTokensAfterChat = maxTokenSize - chatHistoryTokens;
        int reservedTokensForEmbeddings = (remainingTokensAfterChat > maxTokenSize * MaxReservedTokenFactor)
        ? remainingTokensAfterChat
        : (int)(maxTokenSize * MaxReservedTokenFactor);

        List<EmbeddingScore> selectedEmbeddings = CalculateAndLimitTokens(topResults, reservedTokensForEmbeddings, encoding);

        var contextQuery = CreateContextQuery(selectedEmbeddings);
        chat.Assistant.Prompt += contextQuery;

        return AwaitAndMapChatStreamAsync(chat, CreateContextQueryJson(selectedEmbeddings));
    }

    private async IAsyncEnumerable<Message> AwaitAndMapChatStreamAsync(Conversation conversation, string contextQuery)
    {
        await foreach (var responseMessage in AttemptChatStreamAsync(conversation))
        {
            responseMessage.ContextQuery = contextQuery;
            yield return responseMessage;
        }
    }

    private int CalculateMaxTokenSize(int modelMaxTokenSize) => (int)Math.Floor(modelMaxTokenSize * MaxTokenSizeFactor);

    private int CalculateChatHistoryTokens(List<Message> messages, GptEncoding encoding) => messages.Select(message => encoding.Encode(message.Content)).Sum(encoded => encoded.Count());

    private void EnsureTokenSizeWithinLimit(Conversation chat, GptEncoding encoding, int maxTokenSize, int? currentChatHistoryTokens = null)
    {
        int chatHistoryTokens = currentChatHistoryTokens ?? CalculateChatHistoryTokens(chat.Messages, encoding);

        while (chatHistoryTokens > maxTokenSize * MaxReservedTokenFactor && chat.Messages.Count > 1)
        {
            var oldestMessage = chat.Messages.First();
            chatHistoryTokens -= encoding.Encode(oldestMessage.Content).Count();
            chat.Messages.RemoveAt(0);
        }
    }



    private ChatCompletionCreateRequest ToChatCompletionCreateRequest(Conversation conversation)
    {
        var messages = new List<ChatMessage>() {
            conversation.Assistant.ToChatMessage()
        };

        messages.AddRange(conversation.Messages.Select(a => _mapper.Map<ChatMessage>(a)));

        return new ChatCompletionCreateRequest
        {
            Model = conversation.Assistant.Model.Name,
            Temperature = conversation.Temperature,
            Messages = messages,
            Functions = conversation.FunctionDefinitions?.Count() > 0 ? conversation.FunctionDefinitions?.ToList() : null,
        };
    }

    public IAsyncEnumerable<Message> ChatStreamAsync(Conversation conversation)
    {
        return ChatWithFallback(conversation);
    }


    public IAsyncEnumerable<Message> ChatWithContextStreamAsync(IEnumerable<EmbeddingScore> items, Conversation conversation)
    {
        return ChatWithBestContextStreamAsync(items, conversation);
    }

 
    private List<EmbeddingScore> CalculateAndLimitTokens(IEnumerable<EmbeddingScore> topResults, int availableTokens, GptEncoding encoding)
    {
        int totalEmbeddingTokens = 0;
        List<EmbeddingScore> selectedEmbeddings = new List<EmbeddingScore>();

        // Iterate through the top results, encoding each one, and summing the token counts
        foreach (var result in topResults)
        {
            string textRepresentation = result.Text;
            int tokens = encoding.Encode(textRepresentation).Count();

            if (totalEmbeddingTokens + tokens <= availableTokens)
            {
                selectedEmbeddings.Add(result);
                totalEmbeddingTokens += tokens;
            }
            else
            {
                break; // Stop if adding this result would exceed the available tokens
            }
        }

        return selectedEmbeddings;
    }

    private string CreateContextQuery(IEnumerable<EmbeddingScore> topResults)
    {
        return string.Join(" ", topResults
            .GroupBy(r => r.Url)
            .Select(g => new
            {
                Url = g.Key,
                BestScore = g.Max(r => r.Score),
                Texts = string.Join(" ", g.OrderBy(t => t.Score).Select(r => r.Text.Trim()))
            })
            .OrderByDescending(g => g.BestScore)
            .Select(g => $"Attachments: [{g.Url}]:{g.Texts}"));
    }


    private string CreateContextQueryJson(IEnumerable<EmbeddingScore> topResults)
    {
        return JsonConvert.SerializeObject(topResults
            .GroupBy(r => r.Url)
            .Select(g => new
            {
                Url = g.Key,
                BestScore = g.Max(r => r.Score),
                Texts = g.OrderBy(t => t.Score).Select(r => r.Text.Trim())
            })
            .OrderByDescending(g => g.BestScore)
            .Select(g => new { g.Url, Text = g.Texts }));
    }


}