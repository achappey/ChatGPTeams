using System;
using achappey.ChatGPTeams.Extensions;
using achappey.ChatGPTeams.Models;
using achappey.ChatGPTeams.Config.SharePoint;
using System.Linq;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Globalization;

namespace achappey.ChatGPTeams.Profiles;

public class DatabaseProfile : AutoMapper.Profile
{
    public DatabaseProfile()
    {
        CreateMap<Database.Models.Assistant, Assistant>().ReverseMap();
      //  .ForMember(dest => dest.ModelId, opt => opt.MapFrom(src => src.Model.Id));
        CreateMap<Database.Models.Conversation, Conversation>().ReverseMap();
        CreateMap<Database.Models.Resource, Resource>().ReverseMap();
        CreateMap<Database.Models.Prompt, Prompt>().ReverseMap();
        CreateMap<Database.Models.Function, Function>().ReverseMap();
        CreateMap<Database.Models.Model, Model>().ReverseMap();
        CreateMap<Database.Models.Department, Department>().ReverseMap();
        CreateMap<Database.Models.Message, Message>()
        .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => JsonConvert.DeserializeObject<ConversationReference>(src.Reference)))
        .ForMember(dest => dest.FunctionCall, opt => opt.MapFrom(src => src.FunctionCall != null ? JsonConvert.DeserializeObject<FunctionCall>(src.FunctionCall) : null))
        .ReverseMap()
        .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Reference)));

         CreateMap<Database.Models.User, User>().ReverseMap();


        //CreateMap<Database.Models.FunctionCall, FunctionCall>();
        /*
                CreateMap<ListItem, Models.User>()
                    .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Title)))
                    .ForMember(dest => dest.Mail, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.Email)))
                    .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.ContentType)))
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
                       .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GetFieldValue(FieldNames.AIName)))
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

                                                                                  */
    }

}