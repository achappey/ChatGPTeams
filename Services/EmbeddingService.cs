using System.Threading.Tasks;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;


public interface IEmbeddingService
{
    Task<byte[]> GetEmbeddingFromTextAsync(string content);
}

public class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingRepository _embeddingRepository;

    public EmbeddingService(IEmbeddingRepository embeddingRepository)
    {
        _embeddingRepository = embeddingRepository;
    }

    public async Task<byte[]> GetEmbeddingFromTextAsync(string content)
    {
        return await _embeddingRepository.GetEmbeddingFromTextAsync(content);
    }
}
