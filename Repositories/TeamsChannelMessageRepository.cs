using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ITeamsChannelMessageRepository
{
    Task<IEnumerable<Message>> GetByConversation(string teamsId, string channelId, string messageId);
}

public class TeamsChannelMessageRepository : ITeamsChannelMessageRepository
{
    private readonly ILogger<TeamsChannelMessageRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    public TeamsChannelMessageRepository(ILogger<TeamsChannelMessageRepository> logger, IMapper mapper, IGraphClientFactory graphClientFactory)
    {
        _logger = logger;
        _mapper = mapper;
        _graphClientFactory = graphClientFactory;
    }

    private Microsoft.Graph.GraphServiceClient GraphService
    {
        get
        {
            return _graphClientFactory.Create();
        }
    }


    public async Task<IEnumerable<Message>> GetByConversation(string teamsId, string channelId, string messageId)
    {
        var rootMessage = await GetRootMessageFromConversation(teamsId, channelId, messageId);
        var items = await GetAllMessagesFromConversation(teamsId, channelId, rootMessage);

        return items.Select(t => _mapper.Map<Message>(t)).Where(t => !string.IsNullOrEmpty(t.Content?.Trim()));
    }

      private async Task<string> GetRootMessageFromConversation(
                string teamId, string channelId, string messageId)
        {
            var baseMessage = await GraphService.Teams[teamId].Channels[channelId].Messages[messageId].Request().GetAsync();

            return string.IsNullOrEmpty(baseMessage.ReplyToId) ? messageId : baseMessage.ReplyToId;
        }

        private async Task<IEnumerable<Microsoft.Graph.ChatMessage>> GetAllMessagesFromConversation(
                 string teamId, string channelId, string messageId)
        {
            var conversationMessages = new List<Microsoft.Graph.ChatMessage>();
            
            // Get the root message using the messageId.
            var baseMessage = await GraphService.Teams[teamId].Channels[channelId].Messages[messageId].Request().GetAsync();
            
            conversationMessages.Add(baseMessage);

            // Get all replies to the root message.
            var repliesRequest = GraphService.Teams[teamId].Channels[channelId].Messages[messageId].Replies.Request();

            do
            {
                var repliesPage = await repliesRequest.GetAsync();

                // Add each reply's content to the list.
                conversationMessages.AddRange(repliesPage.Where(t => !string.IsNullOrEmpty(t.Body.Content?.Trim())));

                // Get the next page of replies, if there is one.
                repliesRequest = repliesPage.NextPageRequest;

            } while (repliesRequest != null);

            return conversationMessages.OrderBy(a => a.CreatedDateTime);
        }
}
