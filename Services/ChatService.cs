using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IChatService
{

    IAsyncEnumerable<Message> SendRequestStream(Conversation conversation);
    Task<Conversation> GetChatConversation(ConversationContext context);
}

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IMessageService _messageService;
    private readonly IConversationService _conversationService;
    private readonly IResourceRepository _resourceRepository;
    private readonly IFunctionDefinitonRepository _functionDefinitonRepository;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    public ChatService(IChatRepository chatRepository, IEmbeddingRepository embeddingRepository, IMemoryCache cache,
    IMapper mapper,
    IMessageService messageService, IConversationService conversationService,
    IFunctionDefinitonRepository functionDefinitonRepository, IResourceRepository resourceService)
    {
        _chatRepository = chatRepository;
        _embeddingRepository = embeddingRepository;
        _messageService = messageService;
        _resourceRepository = resourceService;
        _mapper = mapper;
        _conversationService = conversationService;
        _functionDefinitonRepository = functionDefinitonRepository;
        _cache = cache;

    }

    public async Task<Conversation> GetChatConversation(ConversationContext context)
    {
        var conversation = await _conversationService.GetConversationAsync(context.Id);

     //   TimeZoneInfo localTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(context.LocalTimezone);
      //  DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZoneInfo);
        string formattedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        conversation.Assistant.Prompt += $"Current date/time: {formattedTime}. Current timezone: {context.LocalTimezone}";// Current timezone: {context.LocalTimezone}

        var functions = await _functionDefinitonRepository.GetByNames(conversation.AllFunctionNames);
        conversation.FunctionDefinitions = functions.Select(t => t.FunctionDefinition);
        conversation.Messages = (await _messageService.GetByConversationAsync(context, conversation.Id)).ToList();

        return conversation;
    }

    public IAsyncEnumerable<Message> SendRequestStream(Conversation conversation)
    {
        if (conversation.AllResources.Count() > 0)
        {
            return ChatWithEmbeddingsStream(conversation);

        }
        else
        {
            return _chatRepository.ChatStreamAsync(conversation);
        }
    }

    private async IAsyncEnumerable<Message> ChatWithEmbeddingsStream(Conversation conversation)
    {
        var queryEmbedding = await _embeddingRepository.GetEmbeddingFromTextAsync(conversation.Messages.Last().Content);
        var results = new List<EmbeddingScore>();

        // Retrieve or calculate lines and embeddings for each resource
        foreach (var resource in conversation.AllResources)
        {
            var cacheKey = resource.Url;

            if (!_cache.TryGetValue(cacheKey, out Tuple<IEnumerable<string>, IEnumerable<byte[]>> cacheEntry))
            {
                // Calculate lines and embeddings if they aren't in the cache
                var lines = await _resourceRepository.Read(_mapper.Map<Database.Models.Resource>(resource));

                if (lines != null && lines.Count() > 0)
                {
                    var embeddings = await _embeddingRepository.GetEmbeddingsFromLinesAsync(lines);

                    // Store lines and embeddings in the cache
                    cacheEntry = new Tuple<IEnumerable<string>, IEnumerable<byte[]>>(lines, embeddings);
                    _cache.Set(cacheKey, cacheEntry);

                }
            }

            if (cacheEntry != null)
            {
                var scores = _embeddingRepository.CompareEmbeddings(queryEmbedding, cacheEntry.Item2);

                results.AddRange(cacheEntry.Item1.Select((line, index) => new EmbeddingScore()
                {
                    Url = resource.Url,
                    Text = line,
                    Score = scores.ElementAt(index)
                }
                ));
            }
        }

        var topItems = results.OrderByDescending(a => a.Score).Take(400);

        await foreach (var message in _chatRepository.ChatWithContextStreamAsync(topItems, conversation))
        {
            yield return message;
        }
    }
}