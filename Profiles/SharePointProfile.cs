using System;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Config.SharePoint;
using Microsoft.Graph;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Profiles;

public class SharePointProfile : AutoMapper.Profile
{
    public SharePointProfile()
    {
        CreateMap<Activity, Models.Message>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Text))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTimeOffset.Now))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.TeamsId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => src.GetConversationReference()))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Role.user));

        CreateMap<ListItem, Models.User>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
            .ForMember(dest => dest.Mail, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Email)))
          //  .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.ContentType)))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Fields.AdditionalData.ContainsKey(FieldNames.AIDepartment) ? src.GetFieldValue(FieldNames.AIDepartment).NameToDepartment() : null));

        CreateMap<ListItem, Function>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIName)))
               .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AICategory)))
               .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIUrl)))
               .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIPublisher)))
               .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, Request>()
                .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, Department>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, Vault>()
               .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
               .ForMember(dest => dest.Owners, opt => opt.MapFrom(src => src.GetOwners()))
               .ForMember(dest => dest.Readers, opt => opt.MapFrom(src => src.GetReaders()));

    }

}