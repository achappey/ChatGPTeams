

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;

public class Sales
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("responsible_employee")]
    public SalesEmployee ResponsibleEmployee { get; set; }

    [JsonProperty("organization")]
    public OrganizationSales Organization { get; set; }

    [JsonProperty("person")]
    public PersonSales Person { get; set; }

    [JsonProperty("status")]
    public Status Status { get; set; }

    [JsonProperty("subject")]
    public string Subject { get; set; }

    [JsonProperty("simplicate_url")]
    public string SimplicateUrl { get; set; }

}


public class PersonSales
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("full_name")]
    public string FullName { get; set; }

    [JsonProperty("relation_number")]
    public string RelationNumber { get; set; }
}

public class OrganizationSales
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("relation_number")]
    public string RelationNumber { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class SalesEmployee
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("person_id")]
    public string PersonId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class RevenueGroup
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }
}

