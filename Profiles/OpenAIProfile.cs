using AutoMapper;
using achappey.ChatGPTeams.Models;

namespace achappey.ChatGPTeams.Profiles;

public class OpenAIProfile : Profile
{
    public OpenAIProfile()
    {
        CreateMap<Message, OpenAI.ObjectModels.RequestModels.ChatMessage>();
        CreateMap<OpenAI.ObjectModels.RequestModels.ChatMessage, Message>();
        CreateMap<OpenAI.ObjectModels.RequestModels.FunctionCall, FunctionCall>();
        CreateMap<FunctionCall, OpenAI.ObjectModels.RequestModels.FunctionCall>();
    }
}