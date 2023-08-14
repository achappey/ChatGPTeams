using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace achappey.ChatGPTeams.Services;

public interface IChatService
{

    Task<Message> SendRequest(ConversationContext context);
}

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IMembersRepository _membersRepository;
    private readonly IMessageService _messageService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAssistantRepository _assistantRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IFunctionDefinitonRepository _functionDefinitonRepository;
    private readonly IMemoryCache _cache;

    public ChatService(IChatRepository chatRepository, IEmbeddingRepository embeddingRepository, IMemoryCache cache,
    IAssistantRepository assistantRepository,
    IMembersRepository membersRepository, IMessageService messageService, IConversationRepository conversationRepository,
    IFunctionDefinitonRepository functionDefinitonRepository, IResourceRepository resourceService)
    {
        _chatRepository = chatRepository;
        _embeddingRepository = embeddingRepository;
        _messageService = messageService;
        _resourceRepository = resourceService;
        _membersRepository = membersRepository;
        _conversationRepository = conversationRepository;
        _functionDefinitonRepository = functionDefinitonRepository;
        _assistantRepository = assistantRepository;
        _cache = cache;

    }
//, Conversation conversation
    public async Task<Message> SendRequest(ConversationContext context)
    {
        var conversation = await _conversationRepository.GetByTitle(context.Id);
        conversation.Assistant = await _assistantRepository.Get(conversation.Assistant.Id);
        conversation.Assistant.Prompt += $"{DateTime.Now.ToLocalTime()}";
        conversation.Assistant.Resources = await _resourceRepository.GetByAssistant(conversation.Assistant.Id);
        conversation.FunctionDefinitions = await _functionDefinitonRepository.GetByNames(conversation.AllFunctionNames);
        conversation.Messages = await _messageService.GetByConversationAsync(context, conversation.Id);
        conversation.Resources = await _resourceRepository.GetByConversation(conversation.Id);
        
        if (conversation.Resources.Count() > 0)
        {
            return await ChatWithEmbeddings(conversation);
        }

        var result = await _chatRepository.ChatAsync(conversation);
        result.Reference = conversation.Messages.Last().Reference;

        return result;
    }

    private async Task<Message> ChatWithEmbeddings(Conversation conversation)
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
                var lines = await _resourceRepository.Read(resource);

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

        var topItems = results.OrderByDescending(a => a.Score).Take(50);

        var result = await _chatRepository.ChatWithContextAsync(topItems, conversation);
        result.Reference = conversation.Messages.Last().Reference;

        return result;
    }
}