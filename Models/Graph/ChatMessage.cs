
using System;

namespace achappey.ChatGPTeams.Models.Graph;

public class ChatMessage
{
    public ItemBody Body { get; set; }
    public ChatMessageFromIdentitySet From { get; set; }
    public string Id { get; set; }
    public string WebUrl { get; set; }
    public string Summary { get; set; }
    public DateTimeOffset? CreatedDateTime { get; set; }


}
public class ChatMessageFromIdentitySet
{
    public Identity User { get; set; }

}


public class Identity
{
    public string Id { get; set; }
    public string DisplayName { get; set; }


}