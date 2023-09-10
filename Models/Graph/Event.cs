
namespace achappey.ChatGPTeams.Models.Graph;

public class Event
{
    public string Id { get; set; }
    public string Subject { get; set; }
    public ItemBody Body { get; set; }
    public Recipient From { get; set; }
    public DateTimeTimeZone Start { get; set; }
    public DateTimeTimeZone End { get; set; }
}

public class DateTimeTimeZone
{
    public string DateTime { get; set; }

}