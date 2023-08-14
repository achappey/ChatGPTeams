using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Repositories;

public interface IVaultRepository
{
    Task<Vault> Get(string id);
    Task<IEnumerable<Vault>> GetAll();
}

public class VaultRepository : IVaultRepository
{
    private readonly string _siteId;
    private readonly ILogger<ResourceRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    private readonly string _selectQuery = $"{FieldNames.Title},{FieldNames.AIOwners},{FieldNames.AIReaders}";

    public VaultRepository(ILogger<ResourceRepository> logger,
    AppConfig config, IMapper mapper,
    IGraphClientFactory graphClientFactory)
    {
        _logger = logger;
        _mapper = mapper;
        _siteId = config.SharePointSiteId;
        _graphClientFactory = graphClientFactory;
    }

    private GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<Vault> Get(string id)
    {
        var item = await GraphService.GetListItemFromListAsync(_siteId,
                    ListNames.AIVaults,
                    id);

        return _mapper.Map<Vault>(item);
    }

    public async Task<IEnumerable<Vault>> GetAll()
    {
        var items = await GraphService.GetAllListItemFromListAsync(_siteId,
            ListNames.AIVaults,
            _selectQuery);

        return items.Select(a => _mapper.Map<Vault>(a));
    }
}
