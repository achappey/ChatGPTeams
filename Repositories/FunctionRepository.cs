using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Database.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using achappey.ChatGPTeams.Database;
using Microsoft.EntityFrameworkCore;

namespace achappey.ChatGPTeams.Repositories;
public interface IFunctionRepository
{
    Task<Function> Get(string id);
    Task<IEnumerable<Function>> GetAll();
}

public class FunctionRepository : IFunctionRepository
{
    private readonly ILogger<FunctionRepository> _logger;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ChatGPTeamsContext _context;  // Add this line

    public FunctionRepository(ILogger<FunctionRepository> logger,
        IGraphClientFactory graphClientFactory, ChatGPTeamsContext context)  // Add YourDbContext here
    {
        _logger = logger;
        _graphClientFactory = graphClientFactory;
        _context = context;  // Initialize the context
    }
    
    public async Task<IEnumerable<Function>> GetAll()
    {
        return await _context.Functions.ToListAsync();
    }


    public async Task<Function> Get(string id)
    {
        return await _context.Functions.FindAsync(id);
    }


}
