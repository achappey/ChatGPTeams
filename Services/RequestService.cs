using System.Threading.Tasks;
using achappey.ChatGPTeams.Repositories;

namespace achappey.ChatGPTeams.Services;

public interface IRequestService
{
    Task<string> CreateRequestAsync();
    Task DeleteRequestByTokenAsync(string token);
}

public class RequestService : IRequestService
{
    private readonly IRequestRepository _requestRepository;

    public RequestService(IRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<string> CreateRequestAsync()
    {
        return await _requestRepository.Create();
    }

    public async Task DeleteRequestByTokenAsync(string token)
    {
        var item = await _requestRepository.GetByToken(token);

        await _requestRepository.Delete(item.Id);
    }
}
