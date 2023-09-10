using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Database;
using achappey.ChatGPTeams.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ISharePointMessageRepository
{
    Task<IEnumerable<Message>> GetByConversation(string conversationId);
    Task<int> Create(Message message);
    Task Delete(int id);
    Task DeleteByConversationAndTeamsId(string conversationId, string teamsId);
    Task DeleteByConversationAndDateTime(string conversationId, DateTime date);
}

public class SharePointMessageRepository : ISharePointMessageRepository
{
    private readonly ILogger<SharePointMessageRepository> _logger;
    private readonly ChatGPTeamsContext _context; // Add this line for DbContext
    private readonly IGraphClientFactory _graphClientFactory;

    public SharePointMessageRepository(ILogger<SharePointMessageRepository> logger,
    IGraphClientFactory graphClientFactory,
    ChatGPTeamsContext context)  
    {
        _logger = logger;
        _graphClientFactory = graphClientFactory;
        _context = context;  
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }

    public async Task<IEnumerable<Message>> GetByConversation(string conversationId)
    {
        return await _context.Messages.Where(t => t.Conversation.Id == conversationId).OrderBy(a => a.Created).ToListAsync();
    }

    public async Task<int> Create(Message assistant)
    {
        await _context.Messages.AddAsync(assistant);
        await _context.SaveChangesAsync();
        return assistant.Id;
    }

    public async Task Delete(int id)
    {
        var itemToDelete = await _context.Messages.FindAsync(id);
        if (itemToDelete != null)
        {
            _context.Messages.Remove(itemToDelete);
            await _context.SaveChangesAsync();
        }
        // Handle not found scenario as needed.
    }
 

    public async Task DeleteByConversationAndTeamsId(string conversationId, string teamsId)
    {
        var itemToDelete = await _context.Messages.FirstOrDefaultAsync(a => a.Conversation.Id == conversationId && a.TeamsId == teamsId);
        if (itemToDelete != null)
        {
            _context.Messages.Remove(itemToDelete);
            await _context.SaveChangesAsync();
        }

    }

    public async Task DeleteByConversationAndDateTime(string conversationId, DateTime date)
    {
        var items = await _context.Messages.Where(a => a.Conversation.Id == conversationId && a.Created < date).ToListAsync();
        _context.Messages.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

}
