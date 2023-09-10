
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Models.Simplicate;

public class Organization
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("phone")]
    public string Phone { get; set; }

    [JsonProperty("coc_code")]
    public string CocCode { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("relation_manager")]
    public RelationManager RelationManager { get; set; }

    [JsonProperty("industry")]
    public Industry Industry { get; set; }

}

public class RelationManager
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class Industry
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class MyOrganizationProfile
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("organization_id")]
    public string OrganizationId { get; set; }

    [JsonProperty("vat_number")]
    public string VatNumber { get; set; }

    [JsonProperty("coc_code")]
    public string CocCode { get; set; }

    [JsonProperty("bank_account")]
    public string BankAccount { get; set; }

}


public class RelationType
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}