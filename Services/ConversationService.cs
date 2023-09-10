using System.Threading.Tasks;
using achappey.ChatGPTeams.Models;
using System.Linq;
using achappey.ChatGPTeams.Repositories;
using Microsoft.Bot.Schema;
using System;
using achappey.ChatGPTeams.Extensions;
using AutoMapper;

namespace achappey.ChatGPTeams.Services;

public interface IConversationService
{
    Task<Conversation> GetConversationByContextAsync(ConversationContext context);
    Task EnsureConversationAsync(string conversationId, string teamsId = null, string channelId = null);
    Task UpdateConversationAsync(Conversation conversation);
    Task<string> GetConversationIdByContextAsync(ConversationContext context);
    Task DeleteConversationAsync(ConversationReference reference);
    Task ClearConversationHistoryAsync(ConversationContext context, int? messagesToKeep);
    Task ChangeConversationAssistantAsync(ConversationContext context, string assistantName);

    Task<Conversation> GetConversationAsync(string id);


}

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IAssistantService _assistantService;
    private readonly IResourceRepository _resourceRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ITeamsService _teamsService;
    private readonly IMapper _mapper;
    private readonly IMigrateRepository _migrateRepository;

    public ConversationService(IConversationRepository conversationRepository, IMigrateRepository migrateRepository,
     IResourceRepository resourceRepository, IMapper mapper,
        IAssistantService assistantService, IMessageRepository messageRepository, ITeamsService teamsService)
    {
        _conversationRepository = conversationRepository;
        _assistantService = assistantService;
        _messageRepository = messageRepository;
        _mapper = mapper;
        _resourceRepository = resourceRepository;
        _teamsService = teamsService;
        _migrateRepository = migrateRepository;
    }

    public async Task<Conversation> GetConversationByContextAsync(ConversationContext context)
    {
        var conversationDb = await _conversationRepository.Get(context.Id);
        var conversation = _mapper.Map<Conversation>(conversationDb);

        return conversation;
    }

    public async Task<Conversation> GetConversationAsync(string id)
    {
        var conversation = await _conversationRepository.Get(id);
        return _mapper.Map<Conversation>(conversation);
    }


    public async Task EnsureConversationAsync(string conversationId, string teamsId = null, string channelId = null)
    {
       // await this._migrateRepository.Migrate();
        var conversation = await _conversationRepository.Get(conversationId);

        if (conversation == null)
        {
            Assistant defaultAssistant = null;

            if (teamsId != null && channelId != null)
            {
                //defaultAssistant = await _teamsService.GetDefaultAssistant(teamsId, channelId);
            }

            if (defaultAssistant == null)
            {
                var assistants = await _assistantService.GetMyAssistants();
                defaultAssistant = assistants.FirstOrDefault();
            }

            await _conversationRepository.Create(new Database.Models.Conversation()
            {
                Id = conversationId,
                AssistantId = defaultAssistant.Id,
                Temperature = defaultAssistant.Temperature,
            });
        }
    }

    public async Task ClearConversationHistoryAsync(ConversationContext context, int? messagesToKeep)
    {
        var date = DateTime.Now;
        var conversation = await GetConversationByContextAsync(context);

        if (messagesToKeep.HasValue && messagesToKeep.Value >= 0)
        {
            var messages = await _messageRepository.GetAllByConversation(context, conversation);

            // Ensure messages are sorted by creation date, then take the required message
            if (messages != null && messages.Count() > messagesToKeep.Value)
            {
                var sortedMessages = messages.OrderByDescending(m => m.Created).ToList();

                // If value is 0, you take the last message, if it's 5 you take the fifth from last, etc.
                var messageToKeep = sortedMessages.Skip(messagesToKeep.Value).FirstOrDefault();
                if (messageToKeep != null)
                {
                    date = messageToKeep.Created.Value.DateTime;
                }
            }
        }

        if (context.ChatType == ChatType.personal)
        {
            await _messageRepository.DeleteByConversationAndDateTime(conversation.Id, date);

        }

        conversation.CutOff = date;

        await _conversationRepository.Update(_mapper.Map<Database.Models.Conversation>(conversation));
    }

    public async Task<string> GetConversationIdByContextAsync(ConversationContext context)
    {
        var conversation = await _conversationRepository.Get(context.Id);

        return conversation.Id;
    }

    public async Task ChangeConversationAssistantAsync(ConversationContext context, string assistantName)
    {
        var assistant = await _assistantService.GetAssistantByName(assistantName);
        var conversation = await _conversationRepository.Get(context.Id);
        conversation.Assistant = _mapper.Map<Database.Models.Assistant>(assistant);
        conversation.Temperature = assistant.Temperature;

        await _conversationRepository.Update(conversation);
    }

    public async Task UpdateConversationAsync(Conversation conversation)
    {
        await _conversationRepository.Update(_mapper.Map<Database.Models.Conversation>(conversation));
    }


    public async Task DeleteConversationAsync(ConversationReference reference)
    {
        var conversation = await _conversationRepository.Get(reference.Conversation.Id);

        await _conversationRepository.Delete(conversation.Id);
    }
}
