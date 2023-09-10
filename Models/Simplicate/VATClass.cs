

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;

public class VATClass
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("percentage")]
    public float Percentage { get; set; }
}
