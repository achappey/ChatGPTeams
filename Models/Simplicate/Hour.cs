namespace achappey.ChatGPTeams.Models.Simplicate;

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class Hour
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("employee")]
    public HourEmployee Employee { get; set; }

    [JsonProperty("project")]
    public HourProject Project { get; set; }

    [JsonProperty("projectservice")]
    public HourProjectService ProjectService { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("tariff")]
    public double Tariff { get; set; }

    [JsonProperty("hours")]
    public double Hours { get; set; }

    [JsonProperty("start_date")]
    public string StartDate { get; set; }

    [JsonProperty("end_date")]
    public string EndDate { get; set; }

}

[JsonConverter(typeof(StringEnumConverter))]
public enum HourStatus
{
    [EnumMember(Value = "supervisor_rejected")]
    SupervisorRejected,

    [EnumMember(Value = "supervisor_approved")]
    SupervisorApproved,

    [EnumMember(Value = "projectmanager_approved")]
    ProjectManagerApproved,

    [EnumMember(Value = "forwarded")]
    Forwarded,

    [EnumMember(Value = "to_forward")]
    ToForward
}


public class HourProject
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}


public class HourProjectService
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class HoursType
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("tariff")]
    public string Tariff { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

}


public class HourEmployee
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("person_id")]
    public string PersonId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}