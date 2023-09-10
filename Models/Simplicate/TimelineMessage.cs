

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;
using System.Collections.Generic;


public class TimelineMessage
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("message_type")]
    public MessageType MessageType { get; set; }

    [JsonProperty("linked_to")]
    public List<LinkedTo> LinkedTo { get; set; }

    [JsonProperty("created_at")]
    public string CreatedAt { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }

}


public class MessageType
{
    [JsonProperty("id")]
    public string Id { get; set; }

     [JsonProperty("label")]
    public string Label { get; set; }
}

public class LinkedTo
{
    [JsonProperty("id")]
    public string Id { get; set; }

      [JsonProperty("label")]
    public string Label { get; set; }
}
