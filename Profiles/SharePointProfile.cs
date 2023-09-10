using System;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Config.SharePoint;
using Microsoft.Graph;
using System.Linq;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Globalization;

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

        CreateMap<ListItem, Assistant>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
            .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIModel)))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.GetDepartment()))
            .ForMember(dest => dest.Visibility, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIVisibility).ToVisibility()))
            .ForMember(dest => dest.Functions, opt => opt.MapFrom(src => src.GetFieldValues(FieldNames.AIFunctions).Select(a => a.ToFunction())))
            //.ForMember(dest => dest.Owners, opt => opt.MapFrom(src => src.GetOwners()))
            .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => float.Parse(src.GetFieldValue(FieldNames.AITemperature), CultureInfo.InvariantCulture)))
            .ForMember(dest => dest.Prompt, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIPrompt)));

        CreateMap<ListItem, Models.User>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
            .ForMember(dest => dest.Mail, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Email)))
          //  .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.ContentType)))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Fields.AdditionalData.ContainsKey(FieldNames.AIDepartment) ? src.GetFieldValue(FieldNames.AIDepartment).NameToDepartment() : null));

        CreateMap<ListItem, Models.Conversation>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
            .ForMember(dest => dest.CutOff, opt => opt.MapFrom(src => src.Fields.AdditionalData.ContainsKey(FieldNames.AICutOff) ? (DateTimeOffset?)DateTimeOffset.Parse(src.GetFieldValue(FieldNames.AICutOff)) : null))
            .ForMember(dest => dest.Functions, opt => opt.MapFrom(src => src.GetFieldValues(FieldNames.AIFunctions).Select(a => a.ToFunction())))
            .ForMember(dest => dest.Assistant, opt => opt.MapFrom(src => src.GetAssistant()))
            .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => float.Parse(src.GetFieldValue(FieldNames.AITemperature), CultureInfo.InvariantCulture)));

        CreateMap<ListItem, Resource>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIContentUrl)))
            .ForMember(dest => dest.Conversation, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIConversation.ToLookupField()) != null 
                ? src.GetFieldValue(FieldNames.AIConversation.ToLookupField()).ToConversation() : null))
            .ForMember(dest => dest.Assistant, opt => opt.MapFrom(src => src.GetAssistant()));

        CreateMap<ListItem, Function>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIName)))
               .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AICategory)))
               .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIUrl)))
               .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIPublisher)))
               .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, FunctionCall>()
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
               .ForMember(dest => dest.Arguments, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIArguments)));

        CreateMap<ListItem, Request>()
                .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, Department>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, Reaction>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)));

        CreateMap<ListItem, Models.Prompt>()
               .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIPrompt)))
               .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
               .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AICategory)))
               .ForMember(dest => dest.Assistant, opt => opt.MapFrom(src => src.GetAssistant()))
               .ForMember(dest => dest.Visibility, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIVisibility).ToVisibility()))
               .ForMember(dest => dest.Functions, opt => opt.MapFrom(src => src.GetFieldValues(FieldNames.AIFunctions).Select(a => a.ToFunction())))
               .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.GetOwner()))
               .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIDepartment.ToLookupField()).LookupToDepartment()));

        CreateMap<ListItem, Vault>()
               .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
               .ForMember(dest => dest.Owners, opt => opt.MapFrom(src => src.GetOwners()))
               .ForMember(dest => dest.Readers, opt => opt.MapFrom(src => src.GetReaders()));

        CreateMap<ListItem, Models.Message>()
               .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIRole).ToRole()))
               .ForMember(dest => dest.ConversationId, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIConversation.ToLookupField())))
               .ForMember(dest => dest.TeamsId, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AITeamsId)))
               .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => JsonConvert.DeserializeObject<ConversationReference>(src.GetFieldValue(FieldNames.AIReference))))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ToMessageName()))
               .ForMember(dest => dest.FunctionCall, opt => opt.MapFrom(src => src.GetFunctionCall()))
               .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTimeOffset.Parse(src.GetFieldValue(FieldNames.Created))))
               .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Fields.AdditionalData.ContainsKey(FieldNames.AIContent)
                                                                          && src.Fields.AdditionalData[FieldNames.AIContent] != null ? src.GetFieldValue(FieldNames.AIContent) : string.Empty));
    }

}