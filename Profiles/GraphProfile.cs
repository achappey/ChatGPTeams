using System.Linq;
using AutoMapper;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using System.Collections.Generic;

namespace achappey.ChatGPTeams.Profiles;

public class GraphProfile : Profile
{
    public GraphProfile()
    {
        CreateMap<Microsoft.Graph.User, Models.Graph.User>();
        CreateMap<Microsoft.Graph.Group, Models.Graph.Group>();
        CreateMap<Microsoft.Graph.Team, Models.Graph.Team>();
        CreateMap<Microsoft.Graph.Message, Models.Graph.Email>();
        CreateMap<Microsoft.Graph.Recipient, Models.Graph.Recipient>();
        CreateMap<Microsoft.Graph.PlannerTask, Models.Graph.PlannerTask>();
        CreateMap<Microsoft.Graph.PlannerTaskDetails, Models.Graph.PlannerTaskDetails>();
        CreateMap<Microsoft.Graph.Channel, Models.Graph.Channel>();
        CreateMap<Microsoft.Graph.Trending, Models.Graph.Trending>();
        CreateMap<Microsoft.Graph.UsedInsight, Models.Graph.UsedInsight>();
        CreateMap<Microsoft.Graph.SharedInsight, Models.Graph.SharedInsight>();
        CreateMap<Microsoft.Graph.ResourceVisualization, Models.Graph.ResourceVisualization>();
        CreateMap<Microsoft.Graph.ResourceReference, Models.Graph.ResourceReference>();
        CreateMap<Microsoft.Graph.EmailAddress, Models.Graph.EmailAddress>();
        CreateMap<Microsoft.Graph.Site, Models.Graph.Site>();
        CreateMap<Microsoft.Graph.ChatMessage, Models.Graph.ChatMessage>();
        CreateMap<Microsoft.Graph.ItemBody, Models.Graph.ItemBody>();
        CreateMap<Microsoft.Graph.ChatMessageFromIdentitySet, Models.Graph.ChatMessageFromIdentitySet>();
        CreateMap<Microsoft.Graph.Identity, Models.Graph.Identity>();
        CreateMap<Microsoft.Graph.SearchHit, Models.Graph.SearchHit>();
        CreateMap<Microsoft.Graph.DriveItem, Models.Graph.Resource>();
        CreateMap<Microsoft.Graph.TeamworkDevice, Models.Graph.TeamworkDevice>();
        CreateMap<Microsoft.Graph.TeamworkHardwareDetail, Models.Graph.HardwareDetail>();
        CreateMap<Microsoft.Graph.Message, Models.Graph.Resource>();
        CreateMap<Microsoft.Graph.Event, Models.Graph.Event>();
        CreateMap<Microsoft.Graph.SitePage, Models.Graph.Page>();
        CreateMap<Microsoft.Graph.DateTimeTimeZone, Models.Graph.DateTimeTimeZone>();

        CreateMap<Microsoft.Graph.ConversationMember, User>();
        CreateMap<Microsoft.Graph.ChatMessage, Models.Graph.Resource>()
            .ForMember(dest => dest.WebUrl, opt => opt.MapFrom(src => src.AdditionalData["webLink"].ToString()));

        CreateMap<Microsoft.Bot.Schema.Attachment, Resource>()
              .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.ToUrl()))
              .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ToAttachmentName()));

        CreateMap<Microsoft.Bot.Schema.Attachment, IEnumerable<Resource>>()
            .ConvertUsing<AttachmentToResourcesConverter>();

        CreateMap<Microsoft.Graph.ChatMessageAttachment, Resource>()
              .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.ContentUrl))
              .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

        CreateMap<Microsoft.Graph.ChatMessageReaction, Reaction>()
              .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.ReactionType));

        CreateMap<Microsoft.Graph.ChatMessage, Message>()
               .ForMember(dest => dest.Reactions, opt => opt.MapFrom(src => src.Reactions))
               .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.CreatedDateTime.Value))
               .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.From.Application != null ? Role.assistant : Role.user))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.From.Application != null ? null : src.From.User.DisplayName.ToChatHandle()))
               .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Body.Content.ConvertHtmlToPlainText()));

        CreateMap<Microsoft.Graph.Chat, Models.Graph.TeamsChat>()
               .ForMember(dest => dest.Members, opt => opt.MapFrom(src => string.Join(",", src.Members.Select(m => m.DisplayName))));


    }

    public class AttachmentToResourcesConverter : ITypeConverter<Microsoft.Bot.Schema.Attachment, IEnumerable<Resource>>
    {
        public IEnumerable<Resource> Convert(Microsoft.Bot.Schema.Attachment source, IEnumerable<Resource> destination, ResolutionContext context)
        {
            return source.ExtractResources(); 
        }
    }
}