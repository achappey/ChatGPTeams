using achappey.ChatGPTeams.Models;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Profiles;

public class DatabaseProfile : AutoMapper.Profile
{
    public DatabaseProfile()
    {
        CreateMap<Database.Models.Assistant, Assistant>().ReverseMap();
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


    }

}