using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using achappey.ChatGPTeams.Extensions;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IResourceService
{
    Task<int> CreateResourceAsync(Resource resource);
    Task DeleteResourceAsync(int id);
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
    private readonly IMapper _mapper;  // Declare the field
    private readonly IMemoryCache _cache;

    public ResourceService(
        IResourceRepository resourceRepository,
        IConversationRepository conversationRepository,
        IEmbeddingRepository embeddingRepository,
        IMemoryCache cache,
        IMapper mapper)  // Inject AutoMapper
    {
        _resourceRepository = resourceRepository;
        _conversationRepository = conversationRepository;
        _embeddingRepository = embeddingRepository;
        _mapper = mapper;  // Initialize the AutoMapper field
        _cache = cache;
    }

    public async Task<string> GetFileName(Resource resource)
    {
        return await _resourceRepository.GetFileName(_mapper.Map<Database.Models.Resource>(resource));
    }
    public async Task<Resource> GetResource(string id)
    {
        var item = await _resourceRepository.Get(id);

        return _mapper.Map<Resource>(item);
    }

    public async Task<int> ImportResourceAsync(ConversationReference reference, Resource resource)
    {
        if (resource.Name.StartsWith("https://") && (resource.Name.IsSharePointUrl() || resource.Name.IsOutlookUrl())) // Check if the resource is a SharePoint URL
        {
            resource.Name = await GetFileName(resource); // Get the file name if it is a SharePoint URL
        }

        var cacheKey = resource.Url;

        var currentResources = await _resourceRepository.GetByConversation(reference.Conversation.Id);

        if (!currentResources.Any(e => e.Url == resource.Url))
        {
            var mappedResource = _mapper.Map<Database.Models.Resource>(resource);

            var lines = await _resourceRepository.Read(mappedResource);

            if (lines != null && lines.Count() > 0)
            {
                var embeddings = await _embeddingRepository.GetEmbeddingsFromLinesAsync(lines);

                if (embeddings != null)
                {
                    var cacheEntry = new Tuple<IEnumerable<string>, IEnumerable<byte[]>>(lines, embeddings);

                    _cache.Set(cacheKey, cacheEntry);

                    var resourceId = await _resourceRepository.Create(mappedResource);
                    await _conversationRepository.AddResource(reference.Conversation.Id, resourceId);


                    return lines.Count();
                }
            }
        }

        return 0;
    }

    public async Task<int> CreateResourceAsync(Resource resource)
    {
        return await _resourceRepository.Create(_mapper.Map<Database.Models.Resource>(resource));
    }

    public async Task UpdateResourceAsync(Resource resource)
    {
        await _resourceRepository.Update(_mapper.Map<Database.Models.Resource>(resource));
    }

    public async Task DeleteResourceAsync(int id)
    {
        await _resourceRepository.Delete(id);
    }
}
