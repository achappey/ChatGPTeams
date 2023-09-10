

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;

public class LeaveType
{
    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("blocked")]
    public bool Blocked { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

    [JsonProperty("affects_balance")]
    public bool AffectsBalance { get; set; }
}