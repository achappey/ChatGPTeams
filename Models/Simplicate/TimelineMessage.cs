

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class TimelineMessage
{
    private string _content { get; set; }

    [JsonProperty("message_type")]
    public MessageType MessageType { get; set; }

    [JsonProperty("linked_to")]
    public List<LinkedTo> LinkedTo { get; set; }

    [JsonProperty("created_at")]
    public string CreatedAt { get; set; }

    [JsonProperty("content")]
    public string Content
    {
        get => _content?.Substring(0, Math.Min(_content?.Length ?? 0, 150));
        set
        {
            _content = value;
        }
    }

}


public class MessageType
{
    [JsonProperty("label")]
    public string Label { get; set; }
}

public class LinkedTo
{
    [JsonProperty("label")]
    public string Label { get; set; }
}
