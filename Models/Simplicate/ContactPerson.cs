
namespace achappey.ChatGPTeams.Models.Simplicate;
using Newtonsoft.Json;

public class ContactPerson
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("organization")]
    public OrganizationContactPerson Organization { get; set; }

    [JsonProperty("person")]
    public PersonContactPerson Person { get; set; }

    [JsonProperty("created_at")]
    public string CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("work_function")]
    public string WorkFunction { get; set; }

    [JsonProperty("work_email")]
    public string WorkEmail { get; set; }

    [JsonProperty("work_phone")]
    public string WorkPhone { get; set; }

    [JsonProperty("work_mobile")]
    public string WorkMobile { get; set; }


}


public class OrganizationContactPerson
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("relation_number")]
    public string RelationNumber { get; set; }
}



public class PersonContactPerson
{
    [JsonProperty("full_name")]
    public string FullName { get; set; }

    [JsonProperty("relation_number")]
    public string RelationNumber { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

}
