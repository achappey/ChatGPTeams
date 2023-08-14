using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace achappey.ChatGPTeams.Repositories;

public interface ISharePointMessageRepository
{
    Task<IEnumerable<Message>> GetByConversation(string conversationId);
    Task<string> Create(Message message);
    Task Update(Message message);
    Task Delete(string id);
    Task DeleteByConversationAndTeamsId(string conversationId, string teamsId);
    Task<Message> GetByConversationAndTeamsId(string conversationId, string teamsId);
    Task DeleteByConversationAndDateTime(string conversationId, DateTime date);
}

public class SharePointMessageRepository : ISharePointMessageRepository
{
    private readonly string _siteId;
    private readonly ILogger<SharePointMessageRepository> _logger;
    private readonly IMapper _mapper;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly string _selectQuery = $"{FieldNames.AIRole},{FieldNames.AIContent},{FieldNames.AIFunction.ToLookupField()},{FieldNames.AIConversation.ToLookupField()},{FieldNames.AIArguments},{FieldNames.AITeamsId},{FieldNames.Title},{FieldNames.AIReference},{FieldNames.AIReactions},{FieldNames.Created}";

    public SharePointMessageRepository(ILogger<SharePointMessageRepository> logger,
    AppConfig config, IMapper mapper,
    IGraphClientFactory graphClientFactory)
    {
        _siteId = config.SharePointSiteId;
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

    public async Task<IEnumerable<Message>> GetByConversation(string conversationId)
    {
        var items = await GraphService.GetListItemsFromListAsync(_siteId, ListNames.AIMessages, $"fields/{FieldNames.AIConversation.ToLookupField()} eq {conversationId}", _selectQuery);

        return _mapper.Map<IEnumerable<Message>>(items);
    }


    public async Task<string> Create(Message message)
    {
        var newMessage = message.ToDictionary().ToListItem();

        var createdMessage = await GraphService.Sites[_siteId].Lists[ListNames.AIMessages].Items
            .Request()
            .AddAsync(newMessage);

        return createdMessage.Id;
    }

    public async Task Delete(string id)
    {
        await GraphService.Sites[_siteId].Lists[ListNames.AIMessages].Items[id]
         .Request()
         .DeleteAsync();
    }

    public async Task DeleteByConversationAndTeamsId(string conversationId, string teamsId)
    {
        var item = await GetByConversationAndTeamsId(conversationId, teamsId);

        if (item != null)
        {
            await GraphService.Sites[_siteId].Lists[ListNames.AIMessages].Items[item.Id]
             .Request()
             .DeleteAsync();
        }
    }

    public async Task DeleteByConversationAndDateTime(string conversationId, DateTime date)
    {
        var items = await GraphService.GetListItemsFromListAsync(_siteId,
                                                                 ListNames.AIMessages,
                                                                 $"fields/{FieldNames.Created} lt '{date:o}' and fields/{FieldNames.AIConversation.ToLookupField()} eq {conversationId}",
                                                                 "Id");

        foreach (var item in items)
        {
            await GraphService.Sites[_siteId].Lists[ListNames.AIMessages].Items[item.Id]
             .Request()
             .DeleteAsync();

        }
    }

    public async Task<Message> GetByConversationAndTeamsId(string conversationId, string teamsId)
    {
        var item = await GraphService.GetFirstListItemFromListAsync(_siteId,
                                                                    ListNames.AIMessages,
                                                                    $"fields/{FieldNames.AITeamsId} eq '{teamsId}' and fields/{FieldNames.AIConversation.ToLookupField()} eq {conversationId}",
                                                                    _selectQuery);

        return item != null ? _mapper.Map<Message>(item) : null;
    }

    public async Task Update(Message message)
    {

        var messageToUpdate = message.ToDictionary().ToFieldValueSet();

        await GraphService.Sites[_siteId].Lists[ListNames.AIMessages].Items[message.Id].Fields
            .Request()
            .UpdateAsync(messageToUpdate);
    }

}
