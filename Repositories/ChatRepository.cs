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

    public IAsyncEnumerable<Message> ChatStreamAsync(Conversation conversation)
    {
        return ChatWithFallback(conversation);
    }


    public IAsyncEnumerable<Message> ChatWithContextStreamAsync(IEnumerable<EmbeddingScore> items, Conversation conversation)
    {
        return ChatWithBestContextStreamAsync(items, conversation);
    }

    private IAsyncEnumerable<Message> ChatWithFallback(Conversation chat)
    {
        int maxTokenSize = chat.Assistant.Model == "gpt-4" ? (int)Math.Floor(8192 * 0.8) : (int)Math.Floor(16384 * 0.8);
        var encoding = GptEncoding.GetEncoding("cl100k_base");

        // Calculate the total token size
        int totalTokenSize = chat.Messages
            .Select(message => encoding.Encode(message.Content))
            .Sum(encoded => encoded.Count());

        // Remove oldest messages if the total token size exceeds the maximum
        while (totalTokenSize > maxTokenSize && chat.Messages.Count > 0)
        {
            var oldestMessage = chat.Messages.First();
            totalTokenSize -= encoding.Encode(oldestMessage.Content).Count();
            chat.Messages.RemoveAt(0);
        }

        return AttemptChatStreamAsync(chat);
    }

    private async IAsyncEnumerable<Message> AttemptChatStreamAsync(Conversation conversation)
    {
        var chatCompletionRequest = ToChatCompletionCreateRequest(conversation);

        await foreach (var response in _openAIService.ChatCompletion.CreateCompletionAsStream(chatCompletionRequest))
        {
            var message = response.Choices.FirstOrDefault()?.Message;
            yield return _mapper.Map<Message>(message);
        }
    }

    private IAsyncEnumerable<Message> ChatWithBestContextStreamAsync(IEnumerable<EmbeddingScore> topResults, Conversation chat)
    {
        int maxTokenSize = chat.Assistant.Model == "gpt-4" ? (int)Math.Floor(8192 * 0.8) : (int)Math.Floor(16384 * 0.8);

        var encoding = GptEncoding.GetEncoding("cl100k_base");

        // Calculate chat history tokens
        int chatHistoryTokens = chat.Messages
                .Select(message => encoding.Encode(message.Content))
                .Sum(encoded => encoded.Count());

        int remainingTokensAfterChat = maxTokenSize - chatHistoryTokens;
        int reservedTokensForEmbeddings = (remainingTokensAfterChat > maxTokenSize / 2) ? remainingTokensAfterChat : maxTokenSize / 2;

        // If chat history exceeds its 50% quota, trim it
        while (chatHistoryTokens > maxTokenSize / 2 && chat.Messages.Count > 0)
        {
            var oldestMessage = chat.Messages.First();
            chatHistoryTokens -= encoding.Encode(oldestMessage.Content).Count();
            chat.Messages.RemoveAt(0);
        }

        List<EmbeddingScore> selectedEmbeddings = CalculateAndLimitTokens(topResults, reservedTokensForEmbeddings, encoding);

        var contextQuery = CreateContextQuery(selectedEmbeddings);
        chat.Assistant.Prompt += contextQuery;

        return AttemptChatStreamAsync(chat);
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
                Texts = string.Join(" ", g.Select(r => r.Text))
            })
            .OrderByDescending(g => g.BestScore)
            .Select(g => g.Url + " " + g.Texts));
    }


}