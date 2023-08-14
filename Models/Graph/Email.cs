
namespace achappey.ChatGPTeams.Models.Graph;

public class Email
{
    public string Id { get; set; }
    public string Subject { get; set; }
    public string BodyPreview { get; set; }
    public string WebLink { get; set; }
    public ItemBody Body { get; set; }
    public Recipient From { get; set; }
}

public class ItemBody
{
    public string Content { get; set; }

}

public class Recipient
{
    public EmailAddress EmailAddress { get; set; }

}
public class EmailAddress
{
    public string Address { get; set; }

}