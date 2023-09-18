using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ITeamsChatMessageRepository
{
    Task<IEnumerable<Message>> GetByConversation(string conversationId);
    Task<IEnumerable<Microsoft.Graph.ChatMessageAttachment>> GetMessageAttachments(
           string chatId, DateTimeOffset? cutOff);
}

public class TeamsChatMessageRepository : ITeamsChatMessageRepository
{
    private readonly ILogger<TeamsChatMessageRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;

    public TeamsChatMessageRepository(ILogger<TeamsChatMessageRepository> logger, IMapper mapper, IGraphClientFactory graphClientFactory)
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

    public async Task<IEnumerable<Microsoft.Graph.ChatMessageAttachment>> GetMessageAttachments(
           string chatId, DateTimeOffset? cutOff)
    {

        var messagesRequest = GraphService.Chats[chatId].Messages.Request()
                           .OrderBy("createdDateTime desc");
        var attachmentItems = new List<Microsoft.Graph.ChatMessageAttachment>();
        do
        {
            var messagesPage = await messagesRequest.GetAsync();
            attachmentItems.AddRange(messagesPage.Where(z => !cutOff.HasValue  || cutOff.HasValue && z.CreatedDateTime >= cutOff.Value).SelectMany(a => a.Attachments));

            messagesRequest = messagesPage.Count < 100 ? messagesPage.NextPageRequest : null;

        } while (messagesRequest != null);

        return attachmentItems;

    }

    public async Task<IEnumerable<Message>> GetByConversation(string conversationId)
    {
        var items = await GetMessagesFromChat(conversationId);

        return items.Select(t => _mapper.Map<Message>(t)).Where(t => !string.IsNullOrEmpty(t.Content?.Trim()));
    }

    private async Task<IEnumerable<Microsoft.Graph.ChatMessage>> GetMessagesFromChat(string id)
    {
        List<Microsoft.Graph.ChatMessage> chatMessages = new();

        var messagesRequest = GraphService.Chats[id].Messages.Request()
                           .OrderBy("createdDateTime desc");

        do
        {
            var messagesPage = await messagesRequest.GetAsync();
            chatMessages.AddRange(messagesPage);

            messagesRequest = chatMessages.Count < 100 ? messagesPage.NextPageRequest : null;

        } while (messagesRequest != null);

        return chatMessages.OrderBy(a => a.CreatedDateTime);
    }


}
