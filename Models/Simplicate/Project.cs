

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;

public class Project
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("project_manager")]
    public Manager ProjectManager { get; set; }

    [JsonProperty("project_status")]
    public Status ProjectStatus { get; set; }

    [JsonProperty("organization")]
    public Organization OrganizationDetails { get; set; }

    [JsonProperty("person")]
    public Person PersonDetails { get; set; }

    [JsonProperty("project_number")]
    public string ProjectNumber { get; set; }

    [JsonProperty("billable")]
    public bool Billable { get; set; }

    [JsonProperty("start_date")]
    public string StartDate { get; set; }

    [JsonProperty("end_date")]
    public string EndDate { get; set; }

     [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("note")]
    public string Note { get; set; }
     
    [JsonProperty("simplicate_url")]
    public string SimplicateUrl { get; set; }
}

public class Manager
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("person_id")]
    public string PersonId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class Status
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }
}

