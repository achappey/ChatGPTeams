

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;

public class Employee
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("person_id")]
    public string PersonId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("function")]
    public string Function { get; set; }

    [JsonProperty("employment_status")]
    public string EmploymentStatus { get; set; }

    [JsonProperty("civil_status")]
    public string CivilStatus { get; set; }

    [JsonProperty("work_phone")]
    public string WorkPhone { get; set; }

    [JsonProperty("work_mobile")]
    public string WorkMobile { get; set; }

    [JsonProperty("work_email")]
    public string WorkEmail { get; set; }

    [JsonProperty("hourly_sales_tariff")]
    public double HourlySalesTariff { get; set; }

    [JsonProperty("hourly_cost_tariff")]
    public double HourlyCostTariff { get; set; }

    [JsonProperty("created_at")]
    public string CreatedAt { get; set; }

    [JsonProperty("simplicate_url")]
    public string SimplicateUrl { get; set; }
}

public class Avatar
{
    [JsonProperty("url_small")]
    public string UrlSmall { get; set; }

    [JsonProperty("url_large")]
    public string UrlLarge { get; set; }

    [JsonProperty("initials")]
    public string Initials { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }
}
