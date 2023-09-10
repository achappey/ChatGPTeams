

using System.Collections.Generic;
using Newtonsoft.Json;

namespace achappey.ChatGPTeams.Models.Simplicate;

public class Person
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("full_name")]
    public string FullName { get; set; }

    public string Email { get; set; }

    public string Initials { get; set; }

    public string Phone { get; set; }

    [JsonProperty("linkedin_url")]
    public string LinkedinUrl { get; set; }

    [JsonProperty("simplicate_url")]
    public string SimplicateUrl { get; set; }

    [JsonProperty("relation_manager")]
    public RelationManager RelationManager { get; set; }

    [JsonProperty("linked_as_contact_to_organization")]
    public IEnumerable<LinkedContactPerson> LinkedAsContactToOrganization { get; set; }
}

public class LinkedContactPerson
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("work_function")]
    public string WorkFunction { get; set; }
}