using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using achappey.ChatGPTeams.Extensions;

namespace achappey.ChatGPTeams.Services;

public interface IResourceService
{
    Task<IEnumerable<Resource>> GetResourcesByConversationTitleAsync(string conversationTitle);
    Task<IEnumerable<Resource>> GetResourcesByAssistantAsync(Assistant assistant);
    Task<string> CreateResourceAsync(Resource resource);
    Task DeleteResourceAsync(string id);
    Task<int> ImportResourceAsync(ConversationReference reference, Resource resource);
    Task<string> GetFileName(Resource resource);
    Task<Resource> GetResource(string id);
    Task UpdateResourceAsync(Resource resource);
}

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAssistantRepository _assistantRepository;
    private readonly IMemoryCache _cache;

    public ResourceService(IResourceRepository resourceRepository, IConversationRepository conversationRepository,
    IEmbeddingRepository embeddingRepository, IMemoryCache cache)
    {
        _resourceRepository = resourceRepository;
        _conversationRepository = conversationRepository;
        _embeddingRepository = embeddingRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<Resource>> GetResourcesByConversationTitleAsync(string conversationTitle)
    {
        var conversation =  await _conversationRepository.GetByTitle(conversationTitle);
        var resources =  await _resourceRepository.GetByConversation(conversation.Id);
        var assistantResources = await _resourceRepository.GetByAssistant(conversation.Assistant.Id);

        return assistantResources.Concat(resources);
    }

    public async Task<string> GetFileName(Resource resource)
    {
        return await _resourceRepository.GetFileName(resource);
    }

     public async Task<Resource> GetResource(string id)
    {
        return await _resourceRepository.Get(id);
    }

    public async Task<int> ImportResourceAsync(ConversationReference reference, Resource resource)
    {
        if (resource.Name.StartsWith("https://") && (resource.Name.IsSharePointUrl() || resource.Name.IsOutlookUrl())) // Check if the resource is a SharePoint URL
        {
            resource.Name = await GetFileName(resource); // Get the file name if it is a SharePoint URL
        }

        var cacheKey = resource.Url;
        resource.Conversation = await _conversationRepository.GetByTitle(reference.Conversation.Id);

        var currentResources = await GetResourcesByConversationTitleAsync(reference.Conversation.Id);

        if (!currentResources.Any(e => e.Url == resource.Url))
        {
            var lines = await _resourceRepository.Read(resource);

            if (lines != null && lines.Count() > 0)
            {
                var embeddings = await _embeddingRepository.GetEmbeddingsFromLinesAsync(lines);

                if (embeddings != null)
                {
                    var cacheEntry = new Tuple<IEnumerable<string>, IEnumerable<byte[]>>(lines, embeddings);

                    _cache.Set(cacheKey, cacheEntry);

                    await _resourceRepository.Create(resource);

                    return lines.Count();
                }
            }
        }

        return 0;
    }

    public async Task<IEnumerable<Resource>> GetResourcesByAssistantAsync(Assistant assistant)
    {
        return await _resourceRepository.GetByAssistant(assistant.Id);
    }

    public async Task<string> CreateResourceAsync(Resource resource)
    {
        return await _resourceRepository.Create(resource);
    }

    public async Task UpdateResourceAsync(Resource resource)
    {
        await _resourceRepository.Update(resource);
    }

    public async Task DeleteResourceAsync(string id)
    {
        await _resourceRepository.Delete(id);
    }
}
